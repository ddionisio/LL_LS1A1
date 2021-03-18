using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "motility", menuName = "Game/Organism/Component/Motility")]
    public class OrganismComponentMotility : OrganismComponent {
        [Header("Movement")]
        public float forwardAccel = 1f;
        public float turnAccel = 5f;
        public bool isBidirectional;

        [Header("Explore")]
        public float exploreForwardDuration = 0.5f;
        public float exploreForwardEndDelay = 0.5f;
        public float exploreTurnDuration = 0.3f;
        public float exploreTurnEndDelay = 0.5f;

        [Header("Seek")]
        public float seekDelay = 0.3f; //delay until refresh

        [Header("Retreat")]
        public float retreatDelay = 0.3f; //delay until refresh

        [Header("Energy")]
        public float energyMinScale = 0.15f; //minimum energy percentage to activate
        public float energyRate = 1f; //energy consumption per second when active

        public override OrganismComponentControl GenerateControl(OrganismEntity organismEntity) {
            return new OrganismComponentMotilityControl();
        }
    }

    public class OrganismComponentMotilityControl : OrganismComponentControl {
        public enum State {
            Rest,
            Explore,
            Seek,
            Retreat
        }

        public enum ExploreState {
            Forward,
            ForwardWait,

            Turn,
            TurnWait,

            TurnTo //used for turning away from solid
        }

        private OrganismComponentMotility mComp;
        private State mState;
        private ExploreState mExploreState;

        private OrganismEntity mTargetOrganism; //organism to seek or run away from
        private EnergySource mTargetEnergy; //energy source to seek

        private M8.MathUtil.Side mTurnSide;
        private Vector2 mTurnToDir;

        private float mLastTime;

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            mComp = (OrganismComponentMotility)owner;
        }

        public override void Despawn(OrganismEntity ent) {
            mTargetOrganism = null;
            mTargetEnergy = null;
        }

        public override void Spawn(OrganismEntity ent, M8.GenericParams parms) {
            ChangeToState(State.Rest, null, null);
        }

        public override void Update(OrganismEntity ent) {
            if(ent.physicsLocked)
                return;

            var stats = ent.stats;

            var time = Time.time;
            var dt = Time.deltaTime;

            switch(mState) {
                case State.Rest:
                    //energy locked (e.g. dividing), or ran out of energy
                    if(stats.energyLocked || stats.energyScale <= mComp.energyMinScale)
                        return;

                    //check for danger
                    if(ent.sensor) {
                        for(int i = 0; i < ent.sensor.organisms.Count; i++) {
                            var organism = ent.sensor.organisms[i];

                            //can this organism eat us?
                            if(organism && !organism.isReleased && organism.stats.CanEat(stats)) {
                                ChangeToState(State.Retreat, organism, null);
                                return;
                            }
                        }
                    }

                    //explore if our energy rate is stagnant, then explore
                    if(ent.contactEnergies.Count == 0 || stats.energyDelta <= 0f)
                        ChangeToState(State.Explore, null, null);
                    break;

                case State.Explore:
                    //energy locked (e.g. dividing), or ran out of energy
                    if(stats.energyLocked || stats.energyScale <= mComp.energyMinScale) {
                        ChangeToState(State.Rest, null, null);
                        return;
                    }

                    //seek/danger
                    if(ent.sensor) {
                        //check for danger, or an organism we can eat
                        for(int i = 0; i < ent.sensor.organisms.Count; i++) {
                            var organism = ent.sensor.organisms[i];
                            if(organism && !organism.isReleased) {
                                //can this organism eat us?
                                if(organism.stats.CanEat(stats)) {
                                    ChangeToState(State.Retreat, organism, null);
                                    return;
                                }
                                //can we eat it?
                                else if(stats.CanEat(organism.stats)) {
                                    ChangeToState(State.Seek, organism, null);
                                    return;
                                }
                            }
                        }

                        //check for energy source
                        if(stats.energySources.Count > 0 && ent.contactEnergies.Count == 0) {
                            for(int i = 0; i < ent.sensor.energies.Count; i++) {
                                var energy = ent.sensor.energies[i];
                                if(energy && energy.isActive) {
                                    ChangeToState(State.Seek, null, energy);
                                    return;
                                }
                            }
                        }
                    }

                    //check if we need to turn/move away from solid collision
                    if(ent.solidHitCount > 0) {
                        if(mComp.isBidirectional) { //just turn randomly again
                            if(mExploreState != ExploreState.Turn)
                                ExploreChangeToState(ExploreState.Turn);
                        }
                        else if(mExploreState != ExploreState.TurnTo) { //turn away
                            var forward = ent.forward;
                            mTurnToDir = forward;

                            for(int i = 0; i < ent.solidHitCount; i++) {
                                var hit = ent.solidHits[i];
                                mTurnToDir = Vector2.Reflect(mTurnToDir, hit.normal);
                            }

                            if(mTurnToDir == forward)
                                mTurnToDir = Random.Range(0, 2) == 0 ? ent.left : ent.right;

                            mTurnSide = M8.MathUtil.CheckSide(forward, mTurnToDir);
                            mExploreState = ExploreState.TurnTo;
                        }
                    }

                    switch(mExploreState) {
                        case ExploreState.Forward:
                            if(time - mLastTime < mComp.exploreForwardDuration) {
                                ent.velocity += ent.forward * mComp.forwardAccel * dt;

                                stats.energy -= mComp.energyRate * dt;
                            }
                            else
                                ExploreChangeToState(ExploreState.ForwardWait);
                            break;

                        case ExploreState.ForwardWait:
                            if(time - mLastTime >= mComp.exploreForwardEndDelay)
                                ExploreChangeToState(ExploreState.Turn);
                            break;

                        case ExploreState.Turn:
                            if(time - mLastTime < mComp.exploreTurnDuration) {
                                switch(mTurnSide) {
                                    case M8.MathUtil.Side.Left:
                                        ent.angularVelocity += mComp.turnAccel * dt;
                                        break;
                                    case M8.MathUtil.Side.Right:
                                        ent.angularVelocity -= mComp.turnAccel * dt;
                                        break;
                                }

                                stats.energy -= mComp.energyRate * dt;
                            }
                            else
                                ExploreChangeToState(ExploreState.TurnWait);
                            break;

                        case ExploreState.TurnWait:
                            if(time - mLastTime >= mComp.exploreTurnEndDelay)
                                ExploreChangeToState(ExploreState.Forward); //just have to keep moving forward
                            break;

                        case ExploreState.TurnTo:
                            switch(mTurnSide) {
                                case M8.MathUtil.Side.Left:
                                    if(M8.MathUtil.CheckSide(ent.forward, mTurnToDir) == M8.MathUtil.Side.Left) { //keep turning if we are still on the same side
                                        ent.angularVelocity -= mComp.turnAccel * dt;

                                        stats.energy -= mComp.energyRate * dt;
                                    }
                                    else
                                        ExploreChangeToState(ExploreState.Forward);
                                    break;
                                case M8.MathUtil.Side.Right:
                                    if(M8.MathUtil.CheckSide(ent.forward, mTurnToDir) == M8.MathUtil.Side.Right) { //keep turning if we are still on the same side
                                        ent.angularVelocity += mComp.turnAccel * dt;

                                        stats.energy -= mComp.energyRate * dt;
                                    }
                                    else
                                        ExploreChangeToState(ExploreState.Forward);
                                    break;
                                default:
                                    ExploreChangeToState(ExploreState.Forward);
                                    break;
                            }
                            break;
                    }
                    break;

                case State.Seek:
                    //energy locked (e.g. dividing), or ran out of energy
                    if(stats.energyLocked || stats.energyScale <= mComp.energyMinScale)
                        ChangeToState(State.Rest, null, null);
                    //seeking organism
                    else if(mTargetOrganism) {
                        if(mTargetOrganism.isReleased)
                            ChangeToState(State.Explore, null, null);
                        else {
                            Steer(ent, mTargetOrganism.position, false, dt);

                            stats.energy -= mComp.energyRate * dt;
                        }
                    }
                    //seeking energy source
                    else if(mTargetEnergy) {
                        if(!mTargetEnergy.isActive || ent.contactEnergies.Count > 0) //energy no longer valid, or we already found what we seek
                            ChangeToState(State.Rest, null, null);
                        else {
                            Steer(ent, mTargetEnergy.transform.position, false, dt);

                            stats.energy -= mComp.energyRate * dt;
                        }
                    }
                    else
                        ChangeToState(State.Rest, null, null);
                    break;

                case State.Retreat:
                    //energy locked (e.g. dividing), or ran out of energy
                    //target is no longer valid
                    if(stats.energyLocked || stats.energyScale <= mComp.energyMinScale || !mTargetOrganism || mTargetOrganism.isReleased)
                        ChangeToState(State.Rest, null, null);
                    else {
                        Steer(ent, mTargetOrganism.position, true, dt);

                        stats.energy -= mComp.energyRate * dt;
                    }
                    break;
            }
        }

        private void ChangeToState(State toState, OrganismEntity organismTarget, EnergySource energyTarget) {
            mState = toState;

            switch(mState) {
                case State.Explore:
                    mExploreState = Random.Range(0, 2) == 0 ? ExploreState.Forward : ExploreState.Turn;
                    break;
            }

            mTargetOrganism = organismTarget;
            mTargetEnergy = energyTarget;

            mLastTime = Time.time;
        }

        private void ExploreChangeToState(ExploreState toState) {
            mExploreState = toState;

            mLastTime = Time.time;
        }

        private void Steer(OrganismEntity ent, Vector2 targetPos, bool isAway, float timeDelta) {
            var dpos = isAway ? ent.position - targetPos : targetPos - ent.position;

            var targetDist = dpos.magnitude;
            if(targetDist == 0f)
                return;

            var gameDat = GameData.instance;

            var targetDir = dpos / targetDist;

            float diffAngle, diffAngleAbs;

            if(mComp.isBidirectional) {
                var fDiffAngle = Vector2.SignedAngle(ent.forward, targetDir);
                var fDiffAngleAbs = Mathf.Abs(fDiffAngle);

                var bDiffAngle = Vector2.SignedAngle(ent.forward, -targetDir);
                var bDiffAngleAbs = Mathf.Abs(bDiffAngle);

                if(fDiffAngleAbs <= bDiffAngleAbs) {
                    diffAngle = fDiffAngle;
                    diffAngleAbs = fDiffAngleAbs;
                }
                else {
                    diffAngle = bDiffAngle;
                    diffAngleAbs = bDiffAngleAbs;
                }
            }
            else {
                diffAngle = Vector2.SignedAngle(ent.forward, targetDir);
                diffAngleAbs = Mathf.Abs(diffAngle);
            }
            
            if(diffAngleAbs > gameDat.organismSeekTurnAngleThreshold)
                ent.angularVelocity -= Mathf.Sign(diffAngle) * mComp.turnAccel * timeDelta;

            if(diffAngleAbs <= gameDat.organismSeekAngleThreshold)
                ent.velocity += targetDir * mComp.forwardAccel * timeDelta;
        }
    }
}
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

            TurnAway //used for turning away from solid
        }

        private OrganismComponentMotility mComp;

        private State mState;
        private ExploreState mExploreState;

        private Transform mTarget; //position to seek/retreat

        private M8.MathUtil.Side mTurnSide;
        private Vector2 mTurnAway;

        private float mLastTime;

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            base.Init(ent, owner);

            mComp = (OrganismComponentMotility)owner;

            if(ent.sensor)
                ent.sensor.refreshCallback += OnSensorUpdate;
        }

        public override void Despawn() {
            mTarget = null;
        }

        public override void Spawn(M8.GenericParams parms) {
            ChangeToState(State.Rest, null);
        }

        public override void Update() {
            if(entity.physicsLocked)
                return;

            var stats = entity.stats;

            //energy locked (e.g. dividing), or ran out of energy
            if(stats.energyLocked || stats.energyScale <= mComp.energyMinScale)
                return;

            var time = Time.time;
            var dt = Time.deltaTime;

            switch(mState) {
                case State.Rest:
                    //explore if our energy rate is stagnant
                    if(stats.energyDelta <= 0f)
                        ChangeToState(State.Explore, null);
                    break;

                case State.Explore:
                    //check if we need to turn/move away from solid collision
                    if(mExploreState != ExploreState.TurnAway && entity.contactCount > 0) {
                        if(mComp.isBidirectional) { //just turn randomly again
                            if(mExploreState != ExploreState.Turn)
                                ExploreChangeToState(ExploreState.Turn);
                        }
                        else { //turn away
                            mTurnAway = Vector2.zero;
                            int validCount = 0;

                            for(int i = 0; i < entity.contactCount; i++) {
                                var distInfo = entity.contactDistances[i];
                                if(distInfo.isValid && distInfo.isOverlapped) {
                                    mTurnAway += distInfo.normal;
                                    validCount++;
                                }
                            }

                            if(validCount > 0) {
                                mTurnAway.Normalize();
                                mExploreState = ExploreState.TurnAway;
                            }
                        }
                    }

                    //move forward or turn
                    switch(mExploreState) {
                        case ExploreState.Forward:
                            if(time - mLastTime < mComp.exploreForwardDuration) {
                                entity.velocity += entity.forward * mComp.forwardAccel * dt;

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
                                        entity.angularVelocity += mComp.turnAccel * dt;
                                        break;
                                    case M8.MathUtil.Side.Right:
                                        entity.angularVelocity -= mComp.turnAccel * dt;
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

                        case ExploreState.TurnAway:
                            var angle = Vector2.SignedAngle(mTurnAway, entity.forward);
                            if(Mathf.Abs(angle) < GameData.instance.organismTurnAwayAngle) {
                                entity.angularVelocity -= Mathf.Sign(angle) * mComp.turnAccel * dt;

                                stats.energy -= mComp.energyRate * dt;
                            }
                            else
                                ExploreChangeToState(ExploreState.TurnWait);
                            break;
                    }
                    break;

                case State.Seek:
                    if(mTarget) {
                        Steer(mTarget.position, false, dt);

                        stats.energy -= mComp.energyRate * dt;
                    }
                    else
                        ChangeToState(State.Rest, null);
                    break;

                case State.Retreat:
                    if(mTarget) {
                        Steer(mTarget.position, true, dt);

                        stats.energy -= mComp.energyRate * dt;
                    }
                    else
                        ChangeToState(State.Rest, null);
                    break;
            }
        }

        void OnSensorUpdate(OrganismSensor sensor) {
            if(entity.physicsLocked)
                return;

            var stats = entity.stats;

            //energy locked (e.g. dividing), or ran out of energy
            if(stats.energyLocked || stats.energyScale <= mComp.energyMinScale)
                return;

            //seek/danger
            //check for danger, or an organism we can eat
            for(int i = 0; i < sensor.organisms.Count; i++) {
                var organism = sensor.organisms[i];
                if(organism && !organism.isReleased) {
                    //can this organism eat us?
                    if(organism.stats.CanEat(stats)) {
                        ChangeToState(State.Retreat, organism.transform);
                        return;
                    }
                    //can we eat it?
                    else if(stats.CanEat(organism.stats)) {
                        ChangeToState(State.Seek, organism.transform);
                        return;
                    }
                }
            }

            //check for energy source
            if(stats.energySources.Count > 0 && entity.contactEnergies.Count == 0) {
                for(int i = 0; i < sensor.energies.Count; i++) {
                    var energy = sensor.energies[i];
                    if(energy && energy.isActive) {
                        ChangeToState(State.Seek, energy.transform);
                        return;
                    }
                }
            }

            if(mState == State.Seek || mState == State.Retreat)
                ChangeToState(State.Rest, null);
        }

        private void ChangeToState(State toState, Transform target) {
            mState = toState;

            switch(mState) {
                case State.Explore:
                    mExploreState = Random.Range(0, 2) == 0 ? ExploreState.Forward : ExploreState.Turn;
                    break;
            }

            mTarget = target;

            mLastTime = Time.time;
        }

        private void ExploreChangeToState(ExploreState toState) {
            mExploreState = toState;

            switch(mExploreState) {
                case ExploreState.Turn:
                    mTurnSide = Random.Range(0, 2) == 0 ? M8.MathUtil.Side.Left : M8.MathUtil.Side.Right;
                    break;
            }

            mLastTime = Time.time;
        }

        private void Steer(Vector2 targetPos, bool isAway, float timeDelta) {
            var dpos = isAway ? entity.position - targetPos : targetPos - entity.position;

            var targetDist = dpos.magnitude;
            if(targetDist == 0f)
                return;

            var gameDat = GameData.instance;

            var targetDir = dpos / targetDist;

            float diffAngle, diffAngleAbs;

            if(mComp.isBidirectional) { //compare either forward or backward, whichever is closest to target dir
                var fDiffAngle = Vector2.SignedAngle(entity.forward, targetDir);
                var fDiffAngleAbs = Mathf.Abs(fDiffAngle);

                var bDiffAngle = Vector2.SignedAngle(entity.forward, -targetDir);
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
                diffAngle = Vector2.SignedAngle(entity.forward, targetDir);
                diffAngleAbs = Mathf.Abs(diffAngle);
            }
            
            if(diffAngleAbs > gameDat.organismSeekTurnAngleThreshold)
                entity.angularVelocity -= Mathf.Sign(diffAngle) * mComp.turnAccel * timeDelta;

            if(diffAngleAbs <= gameDat.organismSeekAngleThreshold)
                entity.velocity += targetDir * mComp.forwardAccel * timeDelta;
        }
    }
}
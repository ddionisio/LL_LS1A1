using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "motility", menuName = "Game/Organism/Component/Motility")]
    public class OrganismComponentMotility : OrganismComponent {
        [Header("Movement")]
        public float forwardAccel = 1f;
        public float turnAccel = 5f;

        public float exploreWaitDelay = 0.5f;
        public float exploreForwardDelay = 0.5f;
        public float exploreTurnDelay = 0.3f;
        public float seekDelay = 0.3f; //delay until refresh
        public float retreatDelay = 0.3f; //delay until refresh

        public bool isBidirectional;

        [Header("Energy")]
        public float energyMinScale = 0.15f; //minimum energy percentage to activate
        public float energyRate = 1f; //energy consumption per second when active

        public override OrganismComponentControl GenerateControl(OrganismEntity organismEntity) {
            return new OrganismComponentMotilityControl();
        }
    }

    public class OrganismComponentMotilityControl : OrganismComponentControl {
        public enum State {
            Explore,
            Seek,
            Retreat
        }

        public enum ExploreState {
            Wait,
            Forward,
            Turn,
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
            ChangeToState(State.Explore);
        }

        public override void Update(OrganismEntity ent) {
            if(ent.physicsLocked)
                return;

            var stats = ent.stats;

            if(stats.energyLocked || stats.energyScale <= mComp.energyMinScale) //energy locked (e.g. dividing), or ran out of energy
                return;

            var time = Time.time;
            var dt = Time.deltaTime;

            switch(mState) {
                case State.Explore:
                    //check if we need to change state
                    var sensor = ent.sensor;
                    if(sensor) {
                        if(sensor.organisms.Count > 0) {
                            //check for danger/seek

                        }
                        else if(stats.energyDelta <= 0f && stats.energySources.Count > 0 && sensor.energies.Count > 0) {

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
                        case ExploreState.Wait:
                            if(time - mLastTime >= mComp.exploreWaitDelay && stats.energyDelta <= 0f)
                                ExploreChangeToState(ExploreState.Forward); //just have to keep moving forward
                            break;

                        case ExploreState.Forward:
                            if(time - mLastTime < mComp.exploreForwardDelay) {
                                ent.velocity += ent.forward * mComp.forwardAccel * dt;

                                stats.energy -= mComp.energyRate * dt;
                            }
                            else
                                ExploreChangeToState(ExploreState.Turn);
                            break;

                        case ExploreState.Turn:
                            if(time - mLastTime < mComp.exploreTurnDelay) {
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
                                ExploreChangeToState(ExploreState.Wait);
                            break;

                        case ExploreState.TurnTo:
                            switch(mTurnSide) {
                                case M8.MathUtil.Side.Left:
                                    if(M8.MathUtil.CheckSide(ent.forward, mTurnToDir) == M8.MathUtil.Side.Left) { //keep turning if we are still on the same side
                                        ent.angularVelocity -= mComp.turnAccel * dt;

                                        stats.energy -= mComp.energyRate * dt;
                                    }
                                    else
                                        ExploreChangeToState(ExploreState.Wait);
                                    break;
                                case M8.MathUtil.Side.Right:
                                    if(M8.MathUtil.CheckSide(ent.forward, mTurnToDir) == M8.MathUtil.Side.Right) { //keep turning if we are still on the same side
                                        ent.angularVelocity += mComp.turnAccel * dt;

                                        stats.energy -= mComp.energyRate * dt;
                                    }
                                    else
                                        ExploreChangeToState(ExploreState.Wait);
                                    break;
                                default:
                                    ExploreChangeToState(ExploreState.Wait);
                                    break;
                            }
                            break;
                    }
                    break;

                case State.Seek:
                    if(mTargetOrganism) {
                        if(mTargetOrganism.isReleased) {
                            mTargetOrganism = null;
                            ChangeToState(State.Explore);
                        }
                        else {
                            Steer(ent, mTargetOrganism.position, false, dt);

                            stats.energy -= mComp.energyRate * dt;
                        }
                    }
                    else if(mTargetEnergy) {
                        if(!mTargetEnergy.isActive || stats.energyDelta > 0f) {
                            mTargetEnergy = null;
                            ChangeToState(State.Explore);
                        }
                        else {
                            Steer(ent, mTargetEnergy.transform.position, false, dt);

                            stats.energy -= mComp.energyRate * dt;
                        }
                    }
                    break;

                case State.Retreat:
                    if(mTargetOrganism.isReleased) //target is no longer valid
                        ChangeToState(State.Explore);
                    else {

                    }
                    break;
            }
        }

        private void ChangeToState(State toState) {


            
            mLastTime = Time.time;
        }

        private void ExploreChangeToState(ExploreState toState) {

        }

        private void Steer(OrganismEntity ent, Vector2 targetPos, bool isAway, float timeDelta) {
            var dpos = isAway ? ent.position - targetPos : targetPos - ent.position;

            var targetDist = dpos.magnitude;
            if(targetDist == 0f)
                return;

            var gameDat = GameData.instance;

            var targetDir = dpos / targetDist;

            var diffAngle = Vector2.SignedAngle(ent.forward, targetDir);
            var diffAngleAbs = Mathf.Abs(diffAngle);

            if(diffAngleAbs > gameDat.organismSeekTurnAngleThreshold)
                ent.angularVelocity -= Mathf.Sign(diffAngle) * mComp.turnAccel * timeDelta;

            if(diffAngleAbs <= gameDat.organismSeekAngleThreshold)
                ent.velocity += targetDir * mComp.forwardAccel * timeDelta;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "motility", menuName = "Game/Organism/Component/Motility")]
    public class OrganismComponentMotility : OrganismComponent {
        [Header("Display")]
        [SerializeField]
        GameObject _editPrefab;
        [SerializeField]
        GameObject _gamePrefab;
        [SerializeField]
        string _anchor;

        [Header("Movement")]
        public bool isBidirectional;

        [Header("Explore")]
        public float exploreForwardDuration = 0.5f;
        public float exploreForwardEndDelay = 0.5f;
        public float exploreTurnDuration = 0.3f;
        public float exploreTurnAngleVelocityMin = 5f;
        public float exploreTurnAngleVelocityMax = 90f;
        public float exploreTurnAwayAngle = 30f;

        [Header("Energy")]
        public float energyMinScale = 0.15f; //minimum energy percentage to activate
        public float energyRate = 1f; //energy consumption per second when active

        public override GameObject editPrefab => _editPrefab;
        public override GameObject gamePrefab => _gamePrefab;
        public override string anchorName => _anchor;

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

        public bool isLocked;

        public State state { get { return mState; } }
        public ExploreState stateExplore { get { return mExploreState; } }

        private OrganismComponentMotility mComp;

        private OrganismBodySingleCellControl mBodyCtrl;

        private State mState;
        private ExploreState mExploreState;

        private Transform mTarget; //position to seek/retreat

        private M8.MathUtil.Side mTurnSide;
        private Vector2 mTurnAway;

        private float mLastTime;

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            base.Init(ent, owner);

            mComp = (OrganismComponentMotility)owner;

            mBodyCtrl = ent.GetComponentControl<OrganismBodySingleCellControl>();

            if(ent.sensor)
                ent.sensor.refreshCallback += OnSensorUpdate;
        }

        public override void Despawn() {
            mTarget = null;
        }

        public override void Spawn(M8.GenericParams parms) {
            isLocked = false;

            ChangeToState(State.Explore, null);
        }

        public override void Update() {
            if(entity.physicsLocked)
                return;

            var stats = entity.stats;

            //energy locked (e.g. dividing), or ran out of energy
            if(isLocked || stats.energyLocked || stats.energyScale <= mComp.energyMinScale)
                return;

            var time = Time.time;
            var dt = Time.deltaTime;

            switch(mState) {
                case State.Rest:
                    //explore if our energy rate is stagnant
                    if(stats.energyDelta <= 0f && !mBodyCtrl.isStickied)
                        ChangeToState(State.Explore, null);
                    else {
                        //try to dampen our movement
                        //if(entity.speed > 0f)
                            //entity.speed -= stats.forwardAccel * dt;

                        entity.AngularVelocityDampen(stats.turnAccel * dt, 1f);
                    }
                    break;

                case State.Explore:
                    if(mBodyCtrl.isStickied) {
                        ChangeToState(State.Rest, null);
                        return;
                    }

                    //check if we need to turn/move away from solid collision
                    if(mExploreState != ExploreState.TurnAway && entity.contactSolids.Count > 0) {
                        if(mComp.isBidirectional) { //just turn randomly again
                            if(mExploreState != ExploreState.Turn)
                                ExploreChangeToState(ExploreState.Turn);
                        }
                        else { //turn away
                            mTurnAway = Vector2.zero;
                            int validCount = 0;

                            for(int i = 0; i < entity.contactSolids.Count; i++) {
                                var distInfo = entity.GetContactDistanceInfo(entity.contactSolids[i]);
                                if(distInfo.isValid && distInfo.distance <= 0f) {
                                    mTurnAway += distInfo.normal;
                                    validCount++;
                                }
                            }

                            if(validCount > 0) {
                                if(validCount > 1)
                                    mTurnAway.Normalize();

                                mTurnSide = Mathf.Sign(Vector2.SignedAngle(mTurnAway, entity.forward)) < 0f ? M8.MathUtil.Side.Left : M8.MathUtil.Side.Right;
                                mTurnAway = entity.forward;

                                entity.angularVelocity = 0f;

                                mExploreState = ExploreState.TurnAway;
                            }
                        }
                    }

                    //move forward or turn

                    //get forward and turn scale
                    float moveScale, turnScale;
                    GetMovementScale(out moveScale, out turnScale);

                    switch(mExploreState) {
                        case ExploreState.Forward:
                            if(time - mLastTime < mComp.exploreForwardDuration) {
                                entity.velocity += entity.forward * stats.forwardAccel * dt * moveScale;

                                stats.energy -= mComp.energyRate * dt;
                            }
                            else if(CheckEnergy())
                                ChangeToState(State.Rest, null);
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
                                        if(entity.angularVelocity <= mComp.exploreTurnAngleVelocityMax)
                                            entity.angularVelocity += stats.turnAccel * dt * turnScale;
                                        break;
                                    case M8.MathUtil.Side.Right:
                                        if(-entity.angularVelocity <= mComp.exploreTurnAngleVelocityMax)
                                            entity.angularVelocity -= stats.turnAccel * dt * turnScale;
                                        break;
                                }

                                stats.energy -= mComp.energyRate * dt;
                            }
                            else if(CheckEnergy())
                                ChangeToState(State.Rest, null);
                            else
                                ExploreChangeToState(ExploreState.TurnWait);
                            break;

                        case ExploreState.TurnWait:
                            if(Mathf.Abs(entity.angularVelocity) <= mComp.exploreTurnAngleVelocityMin)
                                ExploreChangeToState(ExploreState.Forward); //just have to keep moving forward
                            else
                                entity.AngularVelocityDampen(stats.turnAccel * dt, 1f);
                            break;

                        case ExploreState.TurnAway:
                            var angle = Vector2.Angle(mTurnAway, entity.forward);
                            if(angle < mComp.exploreTurnAwayAngle) {
                                if(mTurnSide == M8.MathUtil.Side.Right)
                                    entity.angularVelocity -= stats.turnAccel * dt * turnScale;
                                else
                                    entity.angularVelocity += stats.turnAccel * dt * turnScale;

                                stats.energy -= mComp.energyRate * dt;
                            }
                            else if(CheckEnergy())
                                ChangeToState(State.Rest, null);
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
            mTarget = null;

            if(entity.physicsLocked)
                return;

            var stats = entity.stats;

            //energy locked (e.g. dividing), or ran out of energy
            if(stats.energyLocked || stats.energyScale <= mComp.energyMinScale)
                return;

            if(mBodyCtrl.isStickied)
                return;

            var pos = entity.position;

            //seek/retreat from closest
            Transform seek = null;
            float seekDistSqr = float.MaxValue;

            Transform retreat = null;
            float retreatDistSqr = float.MaxValue;


            //check for danger, or an organism we can eat
            for(int i = 0; i < sensor.organisms.Count; i++) {
                var organism = sensor.organisms[i];
                if(organism && !organism.isReleased) {
                    //can this organism eat us? Ignore retreat if we want to be eaten (endobiotics)
                    if(organism.stats.CanEat(stats) && (stats.flags & OrganismFlag.Endobiotic) == 0) {
                        var distSqr = (organism.position - pos).sqrMagnitude;
                        if(distSqr < retreatDistSqr) {
                            retreat = organism.transform;
                            retreatDistSqr = distSqr;
                        }
                    }
                    //can we eat it, if we are not retreating?
                    else if(!retreat && stats.CanEat(organism.stats)) {
                        var distSqr = (organism.position - pos).sqrMagnitude;
                        if(distSqr < seekDistSqr) {
                            seek = organism.transform;
                            seekDistSqr = distSqr;
                        }
                    }
                }
            }

            //check for energy source, if not retreating
            if(!retreat && entity.contactEnergies.Count == 0) {
                for(int i = 0; i < sensor.energies.Count; i++) {
                    var energy = sensor.energies[i];
                    if(energy && energy.isActive) {
                        var distSqr = ((Vector2)energy.transform.position - pos).sqrMagnitude;
                        if(distSqr < seekDistSqr) {
                            seek = energy.transform;
                            seekDistSqr = distSqr;
                        }
                    }
                }
            }

            if(retreat)
                ChangeToState(State.Retreat, retreat);
            else if(seek)
                ChangeToState(State.Seek, seek);
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

            var stats = entity.stats;

            //get forward and turn scale
            float moveScale, turnScale;
            GetMovementScale(out moveScale, out turnScale);

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
                entity.angularVelocity -= Mathf.Sign(diffAngle) * stats.turnAccel * timeDelta * turnScale;
            else
                entity.AngularVelocityDampen(stats.turnAccel * timeDelta, gameDat.organismSeekTurnAngleDampenScale);

            if(diffAngleAbs <= gameDat.organismSeekAngleThreshold)
                entity.velocity += targetDir * stats.forwardAccel * timeDelta * moveScale;
        }

        private void GetMovementScale(out float moveScale, out float turnScale) {
            moveScale = 1.0f;
            turnScale = 1.0f;

            var env = GameModePlay.instance.environmentCurrentControl;
            var stats = entity.stats;

            for(int i = 0; i < env.hazards.Length; i++) {
                var hazard = env.hazards[i];
                if(hazard && hazard.isActive && !stats.HazardMatch(hazard.hazard)) {
                    moveScale *= hazard.moveScale;
                    turnScale *= hazard.turnScale;
                }
            }
        }

        private bool CheckEnergy() {
            var energyCount = entity.contactEnergies.Count;
            if(energyCount > 0) {
                int etherealCount = 0;
                for(int i = 0; i < energyCount; i++) {
                    if(entity.contactEnergies[i].data.ethereal)
                        etherealCount++;
                }

                return etherealCount < energyCount;
            }
            else if(entity.contactOrganisms.Count > 0)
                return entity.stats.energyDelta > 0f;

            return false;
        }
    }
}
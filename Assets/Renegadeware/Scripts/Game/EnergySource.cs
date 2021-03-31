using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class EnergySource : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn {
        public const string parmAnchorPos = "esrcAnchorP";
        public const string parmAnchorRadius = "esrcAnchorR";

        public enum MoveMode {
            None,
            Wander,
        }

        public enum State {
            None,
            Spawning,
            Active,
            Despawning
        }

        [Header("Data")]
        public EnergyData data;
        public float energyCapacity;
        public bool energyIsUnlimited; //energy is not drained when consumed
        public float lifespan; //set to 0 for unlimited lifespan

        [Header("Movement")]
        public MoveMode moveMode = MoveMode.None;
        public float moveAccel = 0f;
        public float moveSpeedCap = 2f;
        //public float moveVelocityReceiveScale = 1f;
        public float moveDelay = 0.3f;
        public float moveWait = 0.5f;

        public bool moveSolidCheck;
        public float moveSolidCheckDelay = 0.3f;

        [Header("Animation")]
        public M8.Animator.Animate animator;
        [M8.Animator.TakeSelector(animatorField = "animator")]
        public string takeSpawn;
        [M8.Animator.TakeSelector(animatorField = "animator")]
        public string takeDespawn;

        public float energy {
            get { return mEnergy; }
            set {
                if(!energyIsUnlimited)
                    mEnergy = Mathf.Clamp(value, 0f, energyCapacity);
            }
        }

        public Vector2 position {
            get { return transform.position; }
            set {
                var pos = transform.position;
                pos.x = value.x;
                pos.y = value.y;
                transform.position = pos;
            }
        }

        public bool isActive { get { return mState == State.Active; } }

        public bool isSolid { get { return bodyCollider ? !bodyCollider.isTrigger : false; } }

        public Collider2D bodyCollider { get; private set; }

        public M8.PoolDataController poolData { get { return mPoolDataCtrl; } }

        private float mEnergy;

        private M8.PoolDataController mPoolDataCtrl;
        private State mState;
        private float mLastActiveTime;
        
        private bool mAnchorIsValid;
        private Vector2 mAnchorPos;
        private float mAnchorRadius;

        private float mLastMoveTime;
        private float mLastMoveSolidCheckTime;

        private Vector2 mMoveCurDir;

        private Vector2 mMoveVelocity;

        private Collider2D[] mSolidContactCache;

        void M8.IPoolInit.OnInit() {
            mPoolDataCtrl = GetComponent<M8.PoolDataController>();
        }

        void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
            mAnchorIsValid = false;
            mAnchorRadius = 0f;

            if(parms != null) {
                if(parms.ContainsKey(parmAnchorPos)) {
                    mAnchorPos = parms.GetValue<Vector2>(parmAnchorPos);
                    mAnchorIsValid = true;
                }

                if(parms.ContainsKey(parmAnchorRadius))
                    mAnchorRadius = parms.GetValue<float>(parmAnchorRadius);
            }

            Spawn();
        }

        void OnEnable() {
            if(!mPoolDataCtrl) //if placed on scene
                Spawn();

            //ensure we are in the proper depth
            var pos = transform.position;
            pos.z = GameData.instance.energyDepth;
            transform.position = pos;
        }

        void Awake() {
            bodyCollider = GetComponent<Collider2D>();

            if(moveSolidCheck)
                mSolidContactCache = new Collider2D[4];
        }

        void Update() {
            switch(mState) {
                case State.Spawning:
                    if(!animator.isPlaying)
                        Active();
                    break;

                case State.Active:
                    var time = Time.time;
                    if(mEnergy == 0f || (lifespan > 0f && time - mLastActiveTime >= lifespan))
                        Despawn();
                    else if(moveMode != MoveMode.None) {
                        var dt = Time.deltaTime;

                        var curMoveTime = time - mLastMoveTime;

                        if(curMoveTime < moveDelay) {
                            mMoveVelocity += mMoveCurDir * moveAccel * dt;
                        }
                        else if(curMoveTime > moveDelay + moveWait) {
                            MoveUpdateDir();
                            mLastMoveTime = time;
                        }

                        //check for solids
                        if(moveSolidCheck && time - mLastMoveSolidCheckTime >= moveSolidCheckDelay) {
                            var contactCount = bodyCollider.OverlapCollider(GameData.instance.organismSolidContactFilter, mSolidContactCache);
                            for(int i = 0; i < contactCount; i++) {
                                var coll = mSolidContactCache[i];
                                var distInfo = bodyCollider.Distance(coll);

                                //clip position if overlapping
                                if(distInfo.isValid && distInfo.distance < 0f) {
                                    position += distInfo.normal * distInfo.distance;
                                    mMoveVelocity = Vector2.Reflect(mMoveVelocity, distInfo.normal);
                                }
                            }

                            mLastMoveSolidCheckTime = time;
                        }

                        if(mMoveVelocity != Vector2.zero) {
                            //move towards anchor if outside radius
                            if(mAnchorIsValid && mAnchorRadius > 0f) {
                                var dpos = mAnchorPos - position;
                                var distSqr = dpos.sqrMagnitude;
                                if(distSqr > mAnchorRadius * mAnchorRadius) {
                                    mMoveCurDir = dpos / Mathf.Sqrt(distSqr);
                                    mMoveVelocity += mMoveCurDir * moveAccel * dt;
                                }
                            }

                            //cap/dampen speed
                            var speed = mMoveVelocity.magnitude;
                            var moveDir = mMoveVelocity / speed;

                            if(moveSpeedCap > 0f && speed > moveSpeedCap) {
                                speed = moveSpeedCap;

                                mMoveVelocity = moveDir * speed;
                            }
                            else {
                                var env = GameModePlay.instance.environmentCurrentControl;

                                speed -= env.linearDrag * dt;

                                mMoveVelocity = moveDir * speed;
                            }

                            position += mMoveVelocity * dt;
                        }
                    }
                    break;

                case State.Despawning:
                    if(!animator.isPlaying)
                        Release();
                    break;
            }
        }

        private void Spawn() {
            mEnergy = energyCapacity;

            if(animator && !string.IsNullOrEmpty(takeSpawn)) {
                animator.Play(takeSpawn);
                mState = State.Spawning;
            }
            else
                Active();

            if(moveMode != MoveMode.None) {
                var time = Time.time;

                mMoveVelocity = Vector2.zero;
                MoveUpdateDir();

                mLastMoveTime = time;
                mLastMoveSolidCheckTime = time;
            }
        }

        private void Active() {
            mState = State.Active;
            mLastActiveTime = Time.time;
        }

        private void Despawn() {
            if(animator && !string.IsNullOrEmpty(takeDespawn)) {
                animator.Play(takeDespawn);
                mState = State.Despawning;
            }
            else
                Release();
        }

        private void Release() {
            mState = State.None;

            if(mPoolDataCtrl)
                mPoolDataCtrl.Release();
            else
                gameObject.SetActive(false);
        }

        private void MoveUpdateDir() {
            mMoveCurDir = M8.MathUtil.Rotate(Vector2.up, Random.Range(0f, M8.MathUtil.TwoPI));
        }
    }
}
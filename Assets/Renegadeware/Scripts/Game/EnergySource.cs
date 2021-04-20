using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class EnergySource : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolDespawn {
        public const string parmAnchorPos = "esrcAnchorP"; //Vector2
        public const string parmAnchorRadius = "esrcAnchorR"; //float

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
        public float energyRate = 1.0f; //energy per second given to entity
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

        [Header("Rotate")]
        public Transform rotateTarget;
        public bool rotateSpinEnabled;
        public M8.RangeFloat rotateSpinSpeed;
        public bool rotateSpawnRandom;

        [Header("Display")]
        public M8.SpriteColorGroup spriteColorGroup;
        public SpriteShapeColorGroup spriteShapeColorGroup;

        [Header("Animation")]
        public float animationDelay = 0.3f;

        public DG.Tweening.Ease spawnEase = DG.Tweening.Ease.OutSine;
        public DG.Tweening.Ease despawnEase = DG.Tweening.Ease.InSine;

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
        private float mLastTime;
        
        private bool mAnchorIsValid;
        private Vector2 mAnchorPos;
        private float mAnchorRadius;

        private float mLastMoveTime;
        private float mLastMoveSolidCheckTime;

        private Vector2 mMoveCurDir;

        private Vector2 mMoveVelocity;

        private float mRotateSpeed;

        private Collider2D[] mSolidContactCache;

        private DG.Tweening.EaseFunction mSpawnEaseFunc;
        private DG.Tweening.EaseFunction mDespawnEaseFunc;

        private float mSpriteRotateSpeed;

        public void Release() {
            mState = State.None;

            if(mPoolDataCtrl)
                mPoolDataCtrl.Release();
            else
                gameObject.SetActive(false);
        }

        public void Despawn() {
            if(mState != State.None && mState != State.Despawning)
                SetToDespawn();
        }

        void M8.IPoolInit.OnInit() {
            mPoolDataCtrl = GetComponent<M8.PoolDataController>();
        }

        void M8.IPoolDespawn.OnDespawned() {
            if(spriteColorGroup)
                spriteColorGroup.Revert();
            if(spriteShapeColorGroup)
                spriteShapeColorGroup.Revert();
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

            mSpawnEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(spawnEase);
            mDespawnEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(despawnEase);

            if(!rotateTarget)
                rotateSpinEnabled = false;
        }

        void Update() {
            float t;
            var time = Time.time;

            switch(mState) {
                case State.Spawning:
                    t = time - mLastTime;
                    if(t <= animationDelay) {
                        var clr = Color.Lerp(Color.clear, Color.white, mSpawnEaseFunc(t, animationDelay, 0f, 0f));

                        if(spriteColorGroup)
                            spriteColorGroup.color = clr;
                        if(spriteShapeColorGroup)
                            spriteShapeColorGroup.color = clr;
                    }
                    else {
                        if(spriteColorGroup)
                            spriteColorGroup.Revert();
                        if(spriteShapeColorGroup)
                            spriteShapeColorGroup.Revert();

                        Active();
                    }
                    break;

                case State.Active:
                    if(mEnergy == 0f || (lifespan > 0f && time - mLastTime >= lifespan))
                        SetToDespawn();
                    else if(moveMode != MoveMode.None) {
                        var dt = Time.deltaTime;

                        t = time - mLastMoveTime;

                        if(t < moveDelay) {
                            mMoveVelocity += mMoveCurDir * moveAccel * dt;
                        }
                        else if(t > moveDelay + moveWait) {
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
                    t = time - mLastTime;
                    if(t <= animationDelay) {
                        var clr = Color.Lerp(Color.white, Color.clear, mDespawnEaseFunc(t, animationDelay, 0f, 0f));

                        if(spriteColorGroup)
                            spriteColorGroup.color = clr;
                        if(spriteShapeColorGroup)
                            spriteShapeColorGroup.color = clr;
                    }
                    else
                        Release();
                    break;
            }

            if(rotateSpinEnabled) {
                var rot = rotateTarget.localEulerAngles;
                rot.z += mRotateSpeed * Time.deltaTime;
                rotateTarget.localEulerAngles = rot;
            }
        }

        private void Spawn() {
            mEnergy = energyCapacity;

            if(spriteColorGroup || spriteShapeColorGroup) {
                mState = State.Spawning;
                mLastTime = Time.time;

                if(spriteColorGroup)
                    spriteColorGroup.color = Color.clear;
                if(spriteShapeColorGroup)
                    spriteShapeColorGroup.color = Color.clear;
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

            if(rotateSpawnRandom && rotateTarget) {
                var rot = rotateTarget.localEulerAngles;
                rot.z = Random.Range(0f, 359f);
                rotateTarget.localEulerAngles = rot;
            }

            if(rotateSpinEnabled)
                mRotateSpeed = (Random.Range(0, 2) == 0 ? -1f : 1f) * rotateSpinSpeed.random;
        }

        private void Active() {
            mState = State.Active;
            mLastTime = Time.time;
        }

        private void SetToDespawn() {
            if(spriteColorGroup || spriteShapeColorGroup) {
                mState = State.Despawning;
                mLastTime = Time.time;
            }
            else
                Release();
        }

        private void MoveUpdateDir() {
            mMoveCurDir = M8.MathUtil.Rotate(Vector2.up, Random.Range(0f, M8.MathUtil.TwoPI));
        }
    }
}
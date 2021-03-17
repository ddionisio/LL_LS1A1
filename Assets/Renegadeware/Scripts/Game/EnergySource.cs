using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class EnergySource : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn {
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
        public float lifespan;

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
                    mEnergy = value;
            }
        }

        public bool isActive { get { return mState == State.Active; } }

        private float mEnergy;

        private M8.PoolDataController mPoolDataCtrl;
        private State mState;
        private float mLastActiveTime;

        void M8.IPoolInit.OnInit() {
            mPoolDataCtrl = GetComponent<M8.PoolDataController>();
        }

        void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
            Spawn();
        }

        void OnEnable() {
            if(!mPoolDataCtrl) //if placed on scene
                Spawn();
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
    }
}
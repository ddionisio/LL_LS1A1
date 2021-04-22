using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class EnergySourceSpawner : MonoBehaviour {
        public enum State {
            None,
            Spawn,
            SpawnWait,
            Delay
        }

        public string poolGroup = "energySourcePool";

        [Header("Template")]
        public EnergySource template;        
        public bool templateIsPrefab = true;
        public int templateCapacity;

        [Header("Spawn Info")]
        public bool spawnDefaultLocked;
        public int spawnStartCount;
        public int spawnCount;
        public float spawnWait;
        public float spawnDelay;

        [Header("Signals")]
        public M8.SignalBoolean signalListenSpawnLock;

        public bool spawnLocked {
            get { return mSpawnLocked; }
            set {
                if(mSpawnLocked != value) {
                    mSpawnLocked = value;

                    if(mSpawnLocked) {
                        //despawn all actives
                        if(mEnergyActives != null) {
                            for(int i = mEnergyActives.Count - 1; i >= 0; i--) {
                                var energySrc = mEnergyActives[i];
                                if(energySrc)
                                    energySrc.Despawn();
                            }

                            mState = State.None;
                        }
                    }
                    else {
                        SpawnStart();
                    }
                }
            }
        }

        private M8.PoolController mPool;

        private SpawnPoint[] mSpawnPoints;

        private M8.CacheList<EnergySource> mEnergyActives;

        private State mState = State.None;
        private int mSpawnIndex;
        private int mSpawnPointIndex = -1;
        private float mLastTime;

        private bool mSpawnLocked;

        private M8.GenericParams mSpawnParms = new M8.GenericParams();

        void OnDisable() {
            if(signalListenSpawnLock)
                signalListenSpawnLock.callback -= OnSignalSpawnLock;

            if(GameData.isInstantiated)
                GameData.instance.signalEnvironmentChanged.callback -= OnSignalEnvironmentChange;
        }

        void OnEnable() {
            mSpawnLocked = spawnDefaultLocked;

            if(mSpawnLocked) {
                //completely clear out energies
                ClearAll();
            }
            else if(mState == State.None)
                SpawnStart();
                

            if(signalListenSpawnLock)
                signalListenSpawnLock.callback += OnSignalSpawnLock;

            GameData.instance.signalEnvironmentChanged.callback += OnSignalEnvironmentChange;
        }

        void Awake() {
            mPool = M8.PoolController.CreatePool(poolGroup);

            mPool.AddType(template.gameObject, templateCapacity, templateCapacity);

            if(!templateIsPrefab)
                template.gameObject.SetActive(false);

            mEnergyActives = new M8.CacheList<EnergySource>(spawnCount);

            mSpawnPoints = GetComponentsInChildren<SpawnPoint>();
        }

        void Update() {
            switch(mState) {
                case State.Spawn:
                    if(mEnergyActives.IsFull)
                        ChangeState(State.SpawnWait);
                    else if(Time.time - mLastTime >= spawnWait) {
                        SpawnIncrement();
                        mLastTime = Time.time;
                    }
                    break;

                case State.SpawnWait:
                    if(mEnergyActives.Count == 0) //wait for all energies to be gone (ensure template has limited energy and life duration)
                        ChangeState(State.Delay);
                    break;

                case State.Delay:
                    if(Time.time - mLastTime >= spawnDelay)
                        ChangeState(State.Spawn);
                    break;
            }
        }

        void OnDespawn(M8.PoolDataController pdc) {
            for(int i = 0; i < mEnergyActives.Count; i++) {
                var energySrc = mEnergyActives[i];
                if(energySrc.poolData == pdc) {
                    mEnergyActives.RemoveAt(i);
                    break;
                }
            }

            pdc.despawnCallback -= OnDespawn;
        }

        void OnSignalSpawnLock(bool aLock) {
            spawnLocked = aLock;
        }

        void OnSignalEnvironmentChange(int ind) {
            ClearAll();
        }

        private void SpawnStart() {
            if(mState == State.None) {
                mSpawnIndex = 0;

                //first time spawn
                if(spawnStartCount > 0) {
                    for(int i = 0; !mEnergyActives.IsFull && i < spawnStartCount; i++)
                        SpawnIncrement();
                }

                ChangeState(State.Spawn);
            }
        }

        private void ChangeState(State toState) {
            switch(toState) {
                case State.Spawn:
                    break;

                case State.SpawnWait:
                    mSpawnPointIndex = -1;
                    break;

                case State.Delay:
                    break;
            }

            mState = toState;
            mLastTime = Time.time;
        }

        private void SpawnIncrement() {
            if(mSpawnPointIndex == -1) {
                M8.ArrayUtil.Shuffle(mSpawnPoints);
                mSpawnPointIndex = 0;
            }

            var spawnPt = mSpawnPoints[mSpawnPointIndex];

            Spawn(spawnPt);

            if(mSpawnIndex + 1 >= spawnPt.count) {
                SpawnPointNext();
                mSpawnIndex = 0;
            }
            else
                mSpawnIndex++;
        }

        private void SpawnPointNext() {
            if(mSpawnPointIndex + 1 == mSpawnPoints.Length) {
                M8.ArrayUtil.Shuffle(mSpawnPoints);
                mSpawnPointIndex = 0;
            }
            else
                mSpawnPointIndex++;
        }

        private void Spawn(SpawnPoint spawnPoint) {
            if(mEnergyActives.IsFull)
                return;

            Vector2 pt = spawnPoint.GetPoint();
            Vector3 spawnPt = new Vector3(pt.x, pt.y, GameData.instance.energyDepth);

            mSpawnParms[EnergySource.parmAnchorPos] = spawnPoint.position;
            mSpawnParms[EnergySource.parmAnchorRadius] = spawnPoint.radius;

            var energySrc = mPool.Spawn<EnergySource>(template.name, template.name, transform, spawnPt, mSpawnParms);

            mEnergyActives.Add(energySrc);

            energySrc.poolData.despawnCallback += OnDespawn;
        }

        private void ClearAll() {
            if(mEnergyActives != null) {
                for(int i = mEnergyActives.Count - 1; i >= 0; i--) {
                    var energySrc = mEnergyActives[i];
                    if(energySrc) {
                        if(energySrc.poolData)
                            energySrc.poolData.despawnCallback -= OnDespawn;
                        energySrc.Release();
                    }
                }

                mEnergyActives.Clear();
            }

            mState = State.None;
        }
    }
}
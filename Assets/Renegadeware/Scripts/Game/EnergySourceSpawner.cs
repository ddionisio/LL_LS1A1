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

        [System.Serializable]
        public struct SpawnPointInfo {
            public Transform root;
            public float radius;
            public int count;

            public Vector2 GetPoint() {
                return (Vector2)root.position + (Random.insideUnitCircle * radius);
            }
        }

        public string poolGroup = "energySourcePool";

        public EnergySource template;        
        public bool templateIsPrefab = true;
        public int templateCapacity;

        public SpawnPointInfo[] spawnPoints;

        public int spawnCount;
        public float spawnWait;
        public float spawnDelay;

        private M8.PoolController mPool;

        private M8.CacheList<EnergySource> mEnergyActives;

        private State mState = State.None;
        private int mSpawnIndex;
        private int mSpawnPointIndex = -1;
        private float mLastTime;

        private M8.GenericParams mSpawnParms = new M8.GenericParams();

        void Awake() {
            mPool = M8.PoolController.CreatePool(poolGroup);

            mPool.AddType(template.gameObject, templateCapacity, templateCapacity, transform);

            if(!templateIsPrefab)
                template.gameObject.SetActive(false);

            mEnergyActives = new M8.CacheList<EnergySource>(spawnCount);
        }

        void OnEnable() {
            if(mState == State.None)
                ChangeState(State.Spawn);
        }

        void Update() {
            switch(mState) {
                case State.Spawn:
                    if(mEnergyActives.IsFull)
                        ChangeState(State.SpawnWait);
                    else if(Time.time - mLastTime >= spawnWait) {
                        var spawnPt = spawnPoints[mSpawnPointIndex];

                        Spawn(spawnPt);

                        if(mSpawnIndex + 1 == spawnPt.count) {
                            SpawnPointNext();
                            mSpawnIndex = 0;
                        }
                        else
                            mSpawnIndex++;

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

        private void ChangeState(State toState) {
            switch(toState) {
                case State.Spawn:
                    if(mSpawnPointIndex == -1) {
                        M8.ArrayUtil.Shuffle(spawnPoints);
                        mSpawnPointIndex = 0;
                    }

                    mSpawnIndex = 0;
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

        private void SpawnPointNext() {
            if(mSpawnPointIndex + 1 == spawnPoints.Length) {
                M8.ArrayUtil.Shuffle(spawnPoints);
                mSpawnPointIndex = 0;
            }
            else
                mSpawnPointIndex++;
        }

        private void Spawn(SpawnPointInfo spawnPointInfo) {
            Vector2 pt = spawnPointInfo.GetPoint();
            Vector3 spawnPt = new Vector3(pt.x, pt.y, GameData.instance.energyDepth);

            mSpawnParms[EnergySource.parmAnchorPos] = (Vector2)spawnPointInfo.root.position;
            mSpawnParms[EnergySource.parmAnchorRadius] = spawnPointInfo.radius;

            var energySrc = mPool.Spawn<EnergySource>(template.name, null, spawnPt, mSpawnParms);

            energySrc.poolData.despawnCallback += OnDespawn;
        }

        void OnDrawGizmos() {
            Gizmos.color = new Color(1.0f, 1.0f, 0f, 0.3f);

            for(int i = 0; i < spawnPoints.Length; i++) {
                var spawnPt = spawnPoints[i];
                if(spawnPt.root && spawnPt.radius > 0f) {
                    Gizmos.DrawWireSphere(spawnPt.root.position, spawnPt.radius);
                }
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismEntitySpawnerPlayerCount : MonoBehaviour {
        public enum State {
            None,
            Spawn,
            Wait,
        }

        public string poolGroup = "entitySpawnerPool";

        [Header("Template")]
        public OrganismTemplate template; //if set, templateEntity is generated
        [M8.TagSelector]
        public string templateTag;

        public OrganismEntity templateEntity;
        public bool templateEntityIsPrefab = true;

        public int templateCapacity;

        [Header("Criteria")]
        public int playerCount = 50;

        [Header("Spawn Info")]
        public int spawnCount;
        public float spawnWait;

        private M8.PoolController mPool;
        private string mPoolTypename;

        private SpawnPoint[] mSpawnPoints;

        private M8.CacheList<OrganismEntity> mEntityActives;

        private State mState = State.None;
        private int mSpawnIndex;
        private int mSpawnPointIndex = -1;
        private float mLastTime;

        private int mSpawnCurCount;

        private M8.GenericParams mSpawnParms = new M8.GenericParams();

        void OnDisable() {
            if(GameData.isInstantiated) {
                GameData.instance.signalEnvironmentChanged.callback -= OnSignalEnvironmentChange;
                GameData.instance.signalModeSelectChange.callback -= OnSignalModeSelectChange;
            }
        }

        void OnEnable() {
            if(mEntityActives != null) {
                //clear out active spawns
                ClearAll();
            }

            mState = State.Wait;

            GameData.instance.signalEnvironmentChanged.callback += OnSignalEnvironmentChange;
            GameData.instance.signalModeSelectChange.callback += OnSignalModeSelectChange;
        }

        void Awake() {
            mPool = M8.PoolController.CreatePool(poolGroup);

            if(template) {
                mPoolTypename = template.name;

                if(!mPool.IsFactoryTypeExists(mPoolTypename)) {
                    var templateEntityInst = OrganismEntity.CreateTemplate(template, mPoolTypename, templateTag, mPool.transform);
                    templateEntityInst.gameObject.SetActive(false);

                    mPool.AddType(mPoolTypename, templateEntityInst.gameObject, templateCapacity, templateCapacity);
                }
            }
            else {
                mPoolTypename = templateEntity.name;

                mPool.AddType(mPoolTypename, templateEntity.gameObject, templateCapacity, templateCapacity);

                if(!templateEntityIsPrefab)
                    templateEntity.gameObject.SetActive(false);
            }

            mEntityActives = new M8.CacheList<OrganismEntity>(spawnCount);

            mSpawnPoints = GetComponentsInChildren<SpawnPoint>();
        }

        void Update() {
            switch(mState) {
                case State.Wait:
                    if(mEntityActives.Count == 0) {
                        //check criteria
                        var curPlayerCount = GameModePlay.instance.gameSpawner.entityCount;
                        if(curPlayerCount >= playerCount)
                            SpawnStart();
                    }
                    break;

                case State.Spawn:
                    if(mSpawnCurCount == spawnCount)
                        mState = State.Wait;
                    else if(Time.time - mLastTime >= spawnWait) {
                        SpawnIncrement();
                        mLastTime = Time.time;

                        mSpawnCurCount++;
                    }
                    break;
            }
        }

        void OnDespawn(M8.PoolDataController pdc) {
            for(int i = 0; i < mEntityActives.Count; i++) {
                var ent = mEntityActives[i];
                if(ent.poolControl == pdc) {
                    mEntityActives.RemoveAt(i);
                    break;
                }
            }

            pdc.despawnCallback -= OnDespawn;
        }

        void OnSignalEnvironmentChange(int ind) {
            ClearAll();
        }

        void OnSignalModeSelectChange(ModeSelect curMode, ModeSelect toMode) {
            if(toMode == ModeSelect.Edit)
                ClearAll();
        }

        private void SpawnStart() {
            mSpawnIndex = 0;

            mSpawnCurCount = 0;

            ChangeState(State.Spawn);
        }

        private void ChangeState(State toState) {
            switch(toState) {
                case State.Spawn:
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
            if(mEntityActives.IsFull)
                return;

            Vector2 pt = spawnPoint.GetPoint();
            Vector3 spawnPt = new Vector3(pt.x, pt.y, GameData.instance.organismDepth);

            mSpawnParms[OrganismEntity.parmForwardRandom] = true;

            var ent = mPool.Spawn<OrganismEntity>(mPoolTypename, mPoolTypename, transform, spawnPt, mSpawnParms);

            //don't allow division
            ent.stats.flags |= OrganismFlag.DivideLocked;

            ent.poolControl.despawnCallback += OnDespawn;

            mEntityActives.Add(ent);
        }

        private void ClearAll() {
            for(int i = 0; i < mEntityActives.Count; i++) {
                var ent = mEntityActives[i];
                if(ent && !ent.isReleased) {
                    if(ent.poolControl)
                        ent.poolControl.despawnCallback -= OnDespawn;
                    ent.Release();
                }
            }

            mEntityActives.Clear();
        }
    }
}
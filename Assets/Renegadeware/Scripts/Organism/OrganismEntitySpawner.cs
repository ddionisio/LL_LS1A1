using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismEntitySpawner : MonoBehaviour {
        public enum State {
            None,
            Spawn,
            SpawnWait,
            Delay
        }

        public string poolGroup = "entitySpawnerPool";

        [Header("Template")]
        public OrganismTemplate template; //if set, templateEntity is generated
        [M8.TagSelector]
        public string templateTag;

        public OrganismEntity templateEntity;
        public bool templateEntityIsPrefab = true;

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
                        if(mEntityActives != null) {
                            for(int i = mEntityActives.Count - 1; i >= 0; i--) {
                                var ent = mEntityActives[i];
                                if(ent)
                                    ent.stats.energy = 0f;
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
        private string mPoolTypename;

        private SpawnPoint[] mSpawnPoints;

        private M8.CacheList<OrganismEntity> mEntityActives;

        private State mState = State.None;
        private int mSpawnIndex;
        private int mSpawnPointIndex = -1;
        private float mLastTime;

        private bool mSpawnLocked;

        private M8.GenericParams mSpawnParms = new M8.GenericParams();

        void OnDisable() {
            if(signalListenSpawnLock)
                signalListenSpawnLock.callback -= OnSignalSpawnLock;
        }

        void OnEnable() {
            mSpawnLocked = spawnDefaultLocked;

            if(mSpawnLocked) {
                //completely clear out energies
                if(mEntityActives != null) {
                    for(int i = mEntityActives.Count - 1; i >= 0; i--) {
                        var ent = mEntityActives[i];
                        if(ent)
                            ent.Release();
                    }

                    mEntityActives.Clear();
                }

                mState = State.None;
            }
            else if(mState == State.None)
                SpawnStart();


            if(signalListenSpawnLock)
                signalListenSpawnLock.callback += OnSignalSpawnLock;
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
                case State.Spawn:
                    if(mEntityActives.IsFull)
                        ChangeState(State.SpawnWait);
                    else if(Time.time - mLastTime >= spawnWait) {
                        SpawnIncrement();
                        mLastTime = Time.time;
                    }
                    break;

                case State.SpawnWait:
                    if(mEntityActives.Count == 0) //wait for all energies to be gone (ensure template has limited energy and life duration)
                        ChangeState(State.Delay);
                    break;

                case State.Delay:
                    if(Time.time - mLastTime >= spawnDelay)
                        ChangeState(State.Spawn);
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

        void OnSignalSpawnLock(bool aLock) {
            spawnLocked = aLock;
        }

        private void SpawnStart() {
            if(mState == State.None) {
                mSpawnIndex = 0;

                //first time spawn
                if(spawnStartCount > 0) {
                    for(int i = 0; !mEntityActives.IsFull && i < spawnStartCount; i++)
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
            if(mEntityActives.IsFull)
                return;

            Vector2 pt = spawnPoint.GetPoint();
            Vector3 spawnPt = new Vector3(pt.x, pt.y, GameData.instance.organismDepth);

            mSpawnParms[OrganismEntity.parmForwardRandom] = true;

            var ent = mPool.Spawn<OrganismEntity>(mPoolTypename, mPoolTypename, transform, spawnPt, mSpawnParms);

            mEntityActives.Add(ent);

            ent.poolControl.despawnCallback += OnDespawn;
        }
    }
}
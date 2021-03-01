﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismEntity : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolDespawn {
        [Header("Body")]
        [SerializeField]
        OrganismDisplayBody _bodyDisplay = null;
        [SerializeField]
        OrganismSensor _bodySensor = null;
        [SerializeField]
        Collider2D _bodyCollider = null;        

        [Header("Components")]
        [SerializeField]
        OrganismComponent[] _comps = null;

        public M8.PoolDataController poolControl { get; private set; }

        public OrganismDisplayBody bodyDisplay { get { return _bodyDisplay; } }
        public OrganismSensor bodySensor { get { return _bodySensor; } }
        public Collider2D bodyCollider { get { return _bodyCollider; } }        

        public Vector2 position { get { return transform.localPosition; } set { transform.localPosition = value; } }

        public Vector2 forward { 
            get { return mForward; }
            set {
                if(mForward != value) {
                    mForward = value;
                    transform.up = mForward;
                }
            }
        }

        public Vector2 velocity {
            get { return mVelocity; }
            set {
                if(mVelocity != value) {
                    mVelocity = value;
                    mIsVelocityUpdated = true;
                }
            }
        }

        public Vector2 velocityDir { 
            get {
                if(mIsVelocityUpdated)
                    UpdateVelocityData();

                return mVelocityDir;
            }
            set {
                if(mIsVelocityUpdated)
                    UpdateVelocityData();

                if(mVelocityDir != value) {
                    mVelocityDir = value;
                    mVelocity = mVelocityDir * mSpeed;
                }
            }
        }

        public float speed { 
            get {
                if(mIsVelocityUpdated)
                    UpdateVelocityData();

                return mSpeed; 
            }
            set {
                if(mIsVelocityUpdated)
                    UpdateVelocityData();

                if(mSpeed != value) {
                    mSpeed = value;
                    mVelocity = mVelocityDir * mSpeed;
                }
            }
        }

        private Vector2 mForward;

        private Vector2 mVelocity;
        private bool mIsVelocityUpdated;
        private Vector2 mVelocityDir;
        private float mSpeed;

        private ISpawn[] mISpawns;
        private IDespawn[] mIDespawns;
        private IUpdate[] mIUpdates;

        /// <summary>
        /// Call this after creating the prefab, before generating the pool.
        /// </summary>
        public static OrganismEntity CreateTemplate(string templateName, OrganismTemplate template, Transform root) {
            //initialize body
            var bodyComp = template.body;
            if(!bodyComp) {
                Debug.LogWarning("No Body Component found: " + templateName);
                return null;
            }

            var bodyGO = Instantiate(bodyComp.gamePrefab, Vector3.zero, Quaternion.identity, root);
            bodyGO.name = templateName;
            bodyGO.tag = GameData.instance.organismSpawnTag;

            OrganismEntity ent = bodyGO.AddComponent<OrganismEntity>();            

            ent._bodyDisplay = bodyGO.GetComponent<OrganismDisplayBody>();
            if(!ent._bodyDisplay) {
                Debug.LogWarning("No Body Display found: "+ templateName);
                return ent;
            }

            ent._bodySensor = bodyGO.GetComponent<OrganismSensor>();

            ent._bodyCollider = bodyGO.GetComponent<Collider2D>();

            //initialize components
            ent._comps = new OrganismComponent[template.componentIDs.Length];

            ent._comps[0] = bodyComp;

            for(int i = 1; i < ent._comps.Length; i++) {
                var comp = GameData.instance.GetOrganismComponent<OrganismComponent>(template.componentIDs[i]);

                //instantiate prefab for component
                var compPrefab = comp.gamePrefab;
                if(compPrefab) {
                    var anchorList = ent._bodyDisplay.GetAnchors(comp.anchorName);
                    if(anchorList != null) {
                        for(int j = 0; j < anchorList.Count; j++) {
                            var t = anchorList[j];
                            Instantiate(compPrefab, Vector3.zero, Quaternion.identity, t);
                        }
                    }
                }

                ent._comps[i] = comp;
            }

            for(int i = 0; i < ent._comps.Length; i++)
                ent._comps[i].SetupTemplate(ent);

            //setup stats

            return ent;
        }

        void M8.IPoolInit.OnInit() {
            //grab interfaces for components
            var iSpawnList = new List<ISpawn>();
            var iDespawnList = new List<IDespawn>();
            var iUpdateList = new List<IUpdate>();

            for(int i = 0; i < _comps.Length; i++) {
                var comp = _comps[i];

                var iSpawn = comp as ISpawn;
                if(iSpawn != null)
                    iSpawnList.Add(iSpawn);

                var iDespawn = comp as IDespawn;
                if(iDespawn != null)
                    iDespawnList.Add(iDespawn);

                var iUpdate = comp as IUpdate;
                if(iUpdate != null)
                    iUpdateList.Add(iUpdate);
            }

            mISpawns = iSpawnList.ToArray();
            mIDespawns = iDespawnList.ToArray();
            mIUpdates = iUpdateList.ToArray();

            poolControl = GetComponent<M8.PoolDataController>();
        }

        void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
            //do spawn stuff here

            for(int i = 0; i < mISpawns.Length; i++)
                mISpawns[i].OnSpawn(this);

            StartCoroutine(DoUpdate());
        }

        void M8.IPoolDespawn.OnDespawned() {
            StopAllCoroutines();

            for(int i = 0; i < mIDespawns.Length; i++)
                mIDespawns[i].OnDespawn(this);

            //do despawn stuff here
        }

        void FixedUpdate() {
            //physics update
            float dt = Time.fixedDeltaTime;

            //update orientation

            //update velocity

            //update position
            position += velocity * dt;
        }

        IEnumerator DoUpdate() {
            var wait = new WaitForSeconds(GameData.instance.organismUpdateDelay);

            while(true) {
                yield return wait;

                //general update

                for(int i = 0; i < mIUpdates.Length; i++)
                    mIUpdates[i].OnUpdate(this);
            }
        }

        private void UpdateVelocityData() {
            mSpeed = mVelocity.magnitude;
            if(mSpeed > 0f)
                mVelocityDir = mVelocity / mSpeed;
            else
                mVelocityDir = Vector2.zero;

            mIsVelocityUpdated = false;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismEntity : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolDespawn {
        public const int solidHitCapacity = 4;
        public const int contactCapacity = 8;

        public const string parmForwardRandom = "fwdRnd"; //bool
        public const string parmForward = "fwd"; //Vector2 dir (normalized)

        public struct UpdateData {
            public float lastTime;
            public IUpdate iUpdate;
        }

        [Header("Body")]
        [SerializeField]
        OrganismDisplayBody _bodyDisplay = null;
        [SerializeField]
        Collider2D _bodyCollider = null;        

        [Header("Components")]
        [SerializeField]
        OrganismComponent[] _comps = null;

        public M8.PoolDataController poolControl { get; private set; }

        public OrganismBody bodyComponent { get { return _comps != null && _comps.Length > 0 ? _comps[0] as OrganismBody : null; } }

        public OrganismDisplayBody bodyDisplay { get { return _bodyDisplay; } }
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

        public float angularVelocity { get; set; }

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
                    if(mSpeed < 0f)
                        mSpeed = 0f;

                    mVelocity = mVelocityDir * mSpeed;
                }
            }
        }

        public float speedLimit { get; set; } //set to 0 for no limit

        public bool moveLocked { get; set; }

        public RaycastHit2D[] solidHits { get { return mSolidHits; } }
        public int solidHitCount { get; private set; }

        public Collider2D[] contacts { get { return mContacts; } }
        public int contactCount { get; private set; }

        public M8.CacheList<OrganismEntity> contactOrganisms { get { return mContactOrganisms; } }

        private Vector2 mForward;

        private Vector2 mVelocity;
        private bool mIsVelocityUpdated;
        private Vector2 mVelocityDir;
        private float mSpeed;

        private RaycastHit2D[] mSolidHits = new RaycastHit2D[solidHitCapacity];

        private float mContactsUpdateLastTime;
        private Collider2D[] mContacts = new Collider2D[contactCapacity];
        private M8.CacheList<OrganismEntity> mContactOrganisms = new M8.CacheList<OrganismEntity>(contactCapacity);

        private ISpawn[] mISpawns;
        private IDespawn[] mIDespawns;
        private IVelocityAdd[] mIVelocityAdds;
        private UpdateData[] mUpdates;

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
            var iVelocityAddList = new List<IVelocityAdd>();
            var iUpdateList = new List<IUpdate>();

            for(int i = 0; i < _comps.Length; i++) {
                var comp = _comps[i];

                var iSpawn = comp as ISpawn;
                if(iSpawn != null)
                    iSpawnList.Add(iSpawn);

                var iDespawn = comp as IDespawn;
                if(iDespawn != null)
                    iDespawnList.Add(iDespawn);

                var iVelocityAdd = comp as IVelocityAdd;
                if(iVelocityAdd != null)
                    iVelocityAddList.Add(iVelocityAdd);

                var iUpdate = comp as IUpdate;
                if(iUpdate != null)
                    iUpdateList.Add(iUpdate);
            }

            mISpawns = iSpawnList.ToArray();
            mIDespawns = iDespawnList.ToArray();
            mIVelocityAdds = iVelocityAddList.ToArray();

            mUpdates = new UpdateData[iUpdateList.Count];
            for(int i = 0; i < mUpdates.Length; i++)
                mUpdates[i] = new UpdateData { iUpdate = iUpdateList[i] };

            poolControl = GetComponent<M8.PoolDataController>();
        }

        void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
            //do spawn stuff here

            //initialize data
            transform.localRotation = Quaternion.identity;
            mForward = Vector2.up;

            angularVelocity = 0f;

            mVelocity = Vector2.zero;
            mVelocityDir = Vector2.zero;
            mSpeed = 0f;
            moveLocked = false;

            solidHitCount = 0;
            contactCount = 0;

            if(parms != null) {
                //forward setting
                if(parms.ContainsKey(parmForwardRandom) && parms.GetValue<bool>(parmForwardRandom))
                    forward = M8.MathUtil.RotateAngle(forward, Random.Range(0f, 360f));
                else if(parms.ContainsKey(parmForward))
                    forward = parms.GetValue<Vector2>(parmForward);
            }

            for(int i = 0; i < mISpawns.Length; i++)
                mISpawns[i].OnSpawn(this);

            var time = Time.time;

            for(int i = 0; i < mUpdates.Length; i++)
                mUpdates[i].lastTime = time;

            mContactsUpdateLastTime = time;
        }

        void M8.IPoolDespawn.OnDespawned() {
            for(int i = 0; i < mIDespawns.Length; i++)
                mIDespawns[i].OnDespawn(this);

            //do despawn stuff here
        }

        void Update() {
            var gameDat = GameData.instance;

            var time = Time.time;

            //update contacts
            if(time - mContactsUpdateLastTime >= gameDat.organismContactsUpdateDelay) {
                mContactsUpdateLastTime = time;

                contactCount = _bodyCollider.OverlapCollider(gameDat.organismContactFilter, mContacts);

                mContactOrganisms.Clear();
                for(int i = 0; i < contactCount; i++) {
                    var organismEnt = mContacts[i].GetComponent<OrganismEntity>();
                    if(organismEnt)
                        mContactOrganisms.Add(organismEnt);
                }
            }

            for(int i = 0; i < mUpdates.Length; i++) {
                var iUpdate = mUpdates[i].iUpdate;

                if(time - mUpdates[i].lastTime >= iUpdate.delay) {
                    mUpdates[i].lastTime = time;
                    iUpdate.OnUpdate(this);
                }
            }
        }

        void FixedUpdate() {
            var env = GameModePlay.instance.environmentCurrent;
            var gameDat = GameData.instance;

            //physics update
            float dt = Time.fixedDeltaTime;
            
            if(!moveLocked) {
                //update orientation
                if(angularVelocity != 0f)
                    forward = M8.MathUtil.RotateAngle(forward, angularVelocity * dt);

                //update velocity
                var addVel = Vector2.zero;

                for(int i = 0; i < mIVelocityAdds.Length; i++)
                    addVel += mIVelocityAdds[i].OnAddVelocity(this);

                velocity += addVel;

                //limit/dampen speed
                if(speed > 0f) {
                    speed -= env.linearDrag * dt;

                    if(speedLimit > 0f && speed > speedLimit)
                        speed = speedLimit;
                }

                //dampen angular speed
                if(angularVelocity > 0f) {
                    angularVelocity -= env.angularDrag * dt;
                    if(angularVelocity < 0f)
                        angularVelocity = 0f;
                }
                else if(angularVelocity < 0f) {
                    angularVelocity += env.angularDrag * dt;
                    if(angularVelocity > 0f)
                        angularVelocity = 0f;
                }
            }

            if(speed > 0f) {
                var moveDist = speed * dt;

                //update solid hits
                solidHitCount = _bodyCollider.Cast(mVelocityDir, gameDat.organismSolidContactFilter, mSolidHits, moveDist, true);

                //clip movement
                for(int i = 0; i < solidHitCount; i++) {
                    var hit = mSolidHits[i];
                    if(hit.fraction < moveDist)
                        moveDist = hit.fraction;
                }

                position += mVelocityDir * moveDist;
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
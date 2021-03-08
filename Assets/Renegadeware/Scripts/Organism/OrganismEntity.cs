using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismEntity : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolDespawn {
        public const int solidHitCapacity = 4;
        public const int contactCapacity = 8;

        public const string parmForwardRandom = "fwdRnd"; //bool
        public const string parmForward = "fwd"; //Vector2 dir (normalized)

        public enum State {
            Normal,
            Reproducing,
            Death
        }

        [Header("Stats")]
        [SerializeField]
        OrganismStats _stats;

        [Header("Body")]
        [SerializeField]
        OrganismDisplayBody _bodyDisplay = null;
        [SerializeField]
        Collider2D _bodyCollider = null;

        [Header("Displays")]
        [SerializeField]
        OrganismDisplaySpawn _spawn;
        [SerializeField]
        OrganismDisplayDeath _death;

        [Header("Components")]
        [SerializeField]
        OrganismComponent[] _comps = null;

        public M8.PoolDataController poolControl { get; private set; }

        public OrganismStats stats {
            get {
                if(_stats == null)
                    _stats = new OrganismStats();
                return _stats;
            } 
        }

        public OrganismBody bodyComponent { get { return _comps != null && _comps.Length > 0 ? _comps[0] as OrganismBody : null; } }

        public OrganismDisplayBody bodyDisplay { get { return _bodyDisplay; } }
        public Collider2D bodyCollider { get { return _bodyCollider; } }

        public OrganismDisplaySpawn spawn { get { return _spawn; } }
        public OrganismDisplayDeath death { get { return _death; } }

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

        private OrganismComponentControl[] mControls;

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

                //append stats
                ent.stats.Append(comp.stats);

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

            return ent;
        }

        void M8.IPoolInit.OnInit() {
            //grab controls for components
            var controlList = new List<OrganismComponentControl>();

            for(int i = 0; i < _comps.Length; i++) {                
                var comp = _comps[i];

                var ctrl = comp.GenerateControl(this);
                if(ctrl != null) {
                    ctrl.Init(this, comp);

                    controlList.Add(ctrl);
                }
            }

            mControls = controlList.ToArray();

            poolControl = GetComponent<M8.PoolDataController>();
        }

        void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
            //do spawn stuff here

            stats.Reset();

            //initialize data
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
                    mForward = M8.MathUtil.RotateAngle(forward, Random.Range(0f, 360f));
                else if(parms.ContainsKey(parmForward))
                    mForward = parms.GetValue<Vector2>(parmForward);
            }

            transform.up = mForward;

            mContactsUpdateLastTime = Time.time;

            for(int i = 0; i < mControls.Length; i++)
                mControls[i].Spawn(this, parms);
        }

        void M8.IPoolDespawn.OnDespawned() {
            for(int i = 0; i < mControls.Length; i++)
                mControls[i].Despawn(this);

            //do despawn stuff here
        }

        void Update() {
            var gameDat = GameData.instance;

            var time = Time.time;

            //update contacts
            if(time - mContactsUpdateLastTime >= gameDat.organismContactsUpdateDelay) {
                mContactsUpdateLastTime = time;

                contactCount = _bodyCollider.GetContacts(gameDat.organismContactFilter, mContacts);

                mContactOrganisms.Clear();
                for(int i = 0; i < contactCount; i++) {
                    var organismEnt = mContacts[i].GetComponent<OrganismEntity>();
                    if(organismEnt)
                        mContactOrganisms.Add(organismEnt);
                }
            }

            for(int i = 0; i < mControls.Length; i++)
                mControls[i].Update(this);
        }

        void FixedUpdate() {
            var env = GameModePlay.instance.environmentCurrent;
            var gameDat = GameData.instance;

            //physics update
            float dt = Time.fixedDeltaTime;
            
            if(!moveLocked) {
                //limit/dampen speed
                if(speed > 0f) {
                    speed -= env.linearDrag * dt;

                    var speedLimit = stats.speedLimit;
                    if(speedLimit > 0f && speed > speedLimit)
                        speed = speedLimit;
                }

                //update orientation
                if(angularVelocity != 0f)
                    forward = M8.MathUtil.RotateAngle(forward, angularVelocity * dt);

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
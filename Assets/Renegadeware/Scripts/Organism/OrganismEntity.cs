using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismEntity : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolDespawn {
        public const int solidHitCapacity = 4;
        public const int contactCapacity = 8;

        public const string parmForwardRandom = "fwdRnd"; //bool
        public const string parmForward = "fwd"; //Vector2 dir (normalized)

        [Header("Stats")]
        [SerializeField]
        OrganismStats _stats;

        [Header("Body")]
        [SerializeField]
        OrganismDisplayBody _bodyDisplay = null;
        [SerializeField]
        OrganismSensor _sensor = null;
        [SerializeField]
        Collider2D _bodyCollider = null;

        [Header("Animation")]
        [SerializeField]
        M8.Animator.Animate _animator; //NOTE: if default is set, this is played on spawn

        [Header("Components")]
        [SerializeField]
        OrganismComponent[] _comps = null;

        public M8.PoolDataController poolControl { get; private set; }

        public bool isReleased { get { return poolControl ? !poolControl.isSpawned : gameObject.activeSelf; } }

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

        public OrganismSensor sensor { get { return _sensor; } }

        public M8.Animator.Animate animator { get { return _animator; } }

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

        public Vector2 left { get { return new Vector2(-mForward.y, mForward.x); } }
        public Vector2 right { get { return new Vector2(mForward.y, -mForward.x); } }

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

        /// <summary>
        /// this is in local space (including scale)
        /// </summary>
        public Vector2 size { get; private set; }

        /// <summary>
        /// Lock movement and disable collision.
        /// </summary>
        public bool physicsLocked { 
            get { return mPhysicsLocked; }
            set {
                if(mPhysicsLocked != value) {
                    mPhysicsLocked = value;
                    ApplyPhysicsLocked();
                }
            }
        }

        public RaycastHit2D[] solidHits { get { return mSolidHits; } }
        public int solidHitCount { get; private set; }

        public M8.CacheList<OrganismEntity> contactOrganisms { get { return mContactOrganisms; } }

        public M8.CacheList<EnergySource> contactEnergies { get { return mContactEnergies; } }

        private static RaycastHit2D[] mSolidHit = new RaycastHit2D[4];

        private bool mPhysicsLocked;

        private Vector2 mForward;

        private Vector2 mVelocity;
        private bool mIsVelocityUpdated;
        private Vector2 mVelocityDir;
        private float mSpeed;

        private RaycastHit2D[] mSolidHits = new RaycastHit2D[solidHitCapacity];

        private float mContactsUpdateLastTime;
        private Collider2D[] mContacts = new Collider2D[contactCapacity];
        private M8.CacheList<OrganismEntity> mContactOrganisms = new M8.CacheList<OrganismEntity>(contactCapacity);
        private M8.CacheList<EnergySource> mContactEnergies = new M8.CacheList<EnergySource>(contactCapacity);

        private OrganismComponentControl[] mControls;

        /// <summary>
        /// Call this after creating the prefab, before generating the pool.
        /// </summary>
        public static OrganismEntity CreateTemplate(OrganismTemplate template, string templateName, string tag, Transform root) {
            //initialize body
            var bodyComp = template.body;
            if(!bodyComp) {
                Debug.LogWarning("No Body Component found: " + templateName);
                return null;
            }

            var bodyGO = Instantiate(bodyComp.gamePrefab, Vector3.zero, Quaternion.identity, root);
            bodyGO.name = templateName;
            bodyGO.tag = tag;

            OrganismEntity ent = bodyGO.AddComponent<OrganismEntity>();            

            ent._bodyDisplay = bodyGO.GetComponent<OrganismDisplayBody>();
            if(!ent._bodyDisplay) {
                Debug.LogWarning("No Body Display found: "+ templateName);
                return ent;
            }

            ent._bodyCollider = bodyGO.GetComponent<Collider2D>();

            ent._sensor = bodyGO.GetComponent<OrganismSensor>();

            ent._animator = bodyGO.GetComponent<M8.Animator.Animate>();

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

        public void Release() {
            if(poolControl)
                poolControl.Release();
            else
                gameObject.SetActive(false);
        }

        public bool IsOrganismContact(OrganismEntity ent) {
            return mContactOrganisms.Exists(ent);
        }

        public bool IsEnergyContact(EnergySource energy) {
            return mContactEnergies.Exists(energy);
        }

        public bool SolidCast(Vector2 dir, float dist, out RaycastHit2D hit) {
            int count = _bodyCollider.Cast(dir, GameData.instance.organismSolidContactFilter, mSolidHit, dist, true);
            if(count > 0) {
                var ind = 0;
                var frac = mSolidHits[0].fraction;

                for(int i = 1; i < count; i++) {
                    if(mSolidHit[i].fraction < frac) {
                        ind = i;
                        frac = mSolidHit[i].fraction;
                    }
                }

                hit = mSolidHit[ind];
                return true;
            }

            hit = new RaycastHit2D();
            return false;
        }

        public Vector2 SolidClip(Vector2 dir, float dist) {
            RaycastHit2D hit;
            if(SolidCast(dir, dist, out hit))
                dist = hit.fraction;

            return position + (dir * dist);
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

            //compute general size of collider (NOTE: assumes shape is centered)
            if(bodyCollider) {
                var scale = transform.localScale;

                if(bodyCollider is BoxCollider2D)
                    size = (bodyCollider as BoxCollider2D).size * scale;
                else if(bodyCollider is CircleCollider2D) {
                    var diameter = (bodyCollider as CircleCollider2D).radius * 2f;
                    size = new Vector2(diameter, diameter);
                }
                else if(bodyCollider is CapsuleCollider2D)
                    size = (bodyCollider as CapsuleCollider2D).size * scale;
                else if(bodyCollider is PolygonCollider2D) {
                    var polyColl = bodyCollider as PolygonCollider2D;

                    Vector2 sMin = new Vector2(float.MaxValue, float.MaxValue), sMax = new Vector2(float.MinValue, float.MinValue);

                    for(int i = 0; i < polyColl.points.Length; i++) {
                        var pt = polyColl.points[i];
                        if(pt.x < sMin.x)
                            sMin.x = pt.x;
                        if(pt.y < sMin.y)
                            sMin.y = pt.y;

                        if(pt.x > sMax.x)
                            sMax.x = pt.x;
                        if(pt.y > sMax.y)
                            sMax.y = pt.y;
                    }

                    size = new Vector2(Mathf.Abs(sMax.x - sMin.x) * scale.x, Mathf.Abs(sMax.y - sMin.y) * scale.y);
                }
            }
            else
                size = Vector2.zero;

            if(sensor)
                sensor.Setup(stats);
        }

        void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
            stats.Reset();

            //initialize data
            mForward = Vector2.up;

            mPhysicsLocked = false;

            ResetPhysics();

            if(parms != null) {
                //forward setting
                if(parms.ContainsKey(parmForwardRandom) && parms.GetValue<bool>(parmForwardRandom))
                    mForward = M8.MathUtil.RotateAngle(forward, Random.Range(0f, 360f));
                else if(parms.ContainsKey(parmForward))
                    mForward = parms.GetValue<Vector2>(parmForward);
            }

            transform.up = mForward;

            mContactsUpdateLastTime = Time.time;

            //general spawn
            if(bodyDisplay.colorGroup)
                bodyDisplay.colorGroup.Revert();

            if(animator)
                animator.PlayDefault();

            //component control spawns
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

            var env = GameModePlay.instance.environmentCurrentControl;

            if(!stats.energyLocked) {
                stats.EnergyUpdateLast();

                //environment hazard
                for(int i = 0; i < env.hazards.Length; i++) {
                    var hazard = env.hazards[i];
                    if(hazard)
                        hazard.Apply(stats);
                }

                //environment energy
                for(int i = 0; i < env.energySources.Length; i++) {
                    var energySrc = env.energySources[i];
                    if(energySrc)
                        energySrc.Apply(stats);
                }
            }

            //update contacts
            if(!physicsLocked) {
                var time = Time.time;

                if(time - mContactsUpdateLastTime >= gameDat.organismContactsUpdateDelay) {
                    mContactsUpdateLastTime = time;

                    var contactCount = _bodyCollider.GetContacts(gameDat.organismContactFilter, mContacts);

                    mContactOrganisms.Clear();
                    mContactEnergies.Clear();

                    for(int i = 0; i < contactCount; i++) {
                        var contact = mContacts[i];

                        if(contact.CompareTag(gameDat.energyTag)) {
                            var energySrc = contact.GetComponent<EnergySource>();
                            if(energySrc && energySrc.isActive && stats.EnergyMatch(energySrc.data))
                                mContactEnergies.Add(energySrc);
                        }
                        else if(M8.Util.CheckTag(contact, gameDat.organismEntityTags)) {
                            var organismEnt = contact.GetComponent<OrganismEntity>();
                            if(organismEnt)
                                mContactOrganisms.Add(organismEnt);
                        }
                    }
                }
            }

            //physics stuff here
            if(!physicsLocked) {
                var dt = Time.deltaTime;

                //environmental velocity
                if(env.velocityControl)
                    velocity += env.velocityControl.GetVelocity(position, forward, dt) * stats.velocityReceiveScale;

                //limit/dampen speed
                if(speed > 0f) {
                    speed -= env.linearDrag * dt;

                    var speedLimit = stats.speedLimit;
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

            for(int i = 0; i < mControls.Length; i++)
                mControls[i].Update(this);
        }

        void FixedUpdate() {
            if(!physicsLocked) {
                //update orientation
                if(angularVelocity != 0f)
                    forward = M8.MathUtil.RotateAngle(forward, angularVelocity * Time.fixedDeltaTime);

                //update position
                if(speed > 0f) {
                    var moveDist = speed * Time.fixedDeltaTime;

                    //update solid hits
                    solidHitCount = _bodyCollider.Cast(mVelocityDir, GameData.instance.organismSolidContactFilter, mSolidHits, moveDist, true);

                    //clip movement
                    for(int i = 0; i < solidHitCount; i++) {
                        var hit = mSolidHits[i];
                        if(hit.fraction < moveDist)
                            moveDist = hit.fraction;
                    }

                    position += mVelocityDir * moveDist;
                }
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

        private void ResetPhysics() {
            angularVelocity = 0f;

            mVelocity = Vector2.zero;
            mVelocityDir = Vector2.zero;
            mSpeed = 0f;

            mIsVelocityUpdated = false;

            solidHitCount = 0;

            mContactOrganisms.Clear();
            mContactEnergies.Clear();
        }

        private void ApplyPhysicsLocked() {
            if(physicsLocked) {
                ResetPhysics();

                if(bodyCollider)
                    bodyCollider.enabled = false;
            }
            else {
                if(bodyCollider)
                    bodyCollider.enabled = true;
            }
        }
    }
}
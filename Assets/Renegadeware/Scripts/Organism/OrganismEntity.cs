using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismEntity : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolDespawn {
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
        [SerializeField]
        Vector2 _size = Vector2.zero; //used for approximates

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

        public Vector2 position { 
            get { return transform.localPosition; } 
            set {
                var pos = transform.localPosition;
                transform.localPosition = new Vector3(value.x, value.y, pos.z);
            } 
        }

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
        public Vector2 size { get { return _size; } }

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

        public Collider2D[] contactColliders { get { return mContactCache; } }
        public ColliderDistance2D[] contactDistances { get { return mContactDistances; } }
        public int contactCount { get; private set; }

        public M8.CacheList<Collider2D> contactSolids { get { return mContactSolids; } }
        public M8.CacheList<OrganismEntity> contactOrganisms { get { return mContactOrganisms; } }
        public M8.CacheList<OrganismEntity> contactOrganismMatches { get { return mContactOrganismMatches; } }
        public M8.CacheList<EnergySource> contactEnergies { get { return mContactEnergies; } }

        private struct ComponentControlInfo {
            public OrganismComponent component;
            public OrganismComponentControl control;
        }

        private bool mPhysicsLocked;

        private Vector2 mForward;

        private Vector2 mVelocity;
        private bool mIsVelocityUpdated;
        private Vector2 mVelocityDir;
        private float mSpeed;

        private Collider2D[] mContactCache = new Collider2D[contactCapacity];
        private ColliderDistance2D[] mContactDistances = new ColliderDistance2D[contactCapacity];

        private float mContactsUpdateLastTime;

        private M8.CacheList<Collider2D> mContactSolids = new M8.CacheList<Collider2D>(contactCapacity);
        private M8.CacheList<OrganismEntity> mContactOrganisms = new M8.CacheList<OrganismEntity>(contactCapacity);
        private M8.CacheList<OrganismEntity> mContactOrganismMatches = new M8.CacheList<OrganismEntity>(contactCapacity);
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

            ent.stats.Copy(bodyComp.stats);

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

            ent.GenerateSize();

            ent.ComponentSetupTemplate();

            return ent;
        }

        public void Release() {
            if(poolControl)
                poolControl.Release();
            else
                gameObject.SetActive(false);
        }

        public bool IsMatchTemplate(OrganismEntity ent) {
            return poolControl && ent.poolControl && poolControl.factoryKey == ent.poolControl.factoryKey;
        }

        public bool IsOrganismContact(OrganismEntity ent) {
            return mContactOrganisms.Exists(ent);
        }

        public bool IsEnergyContact(EnergySource energy) {
            return mContactEnergies.Exists(energy);
        }

        public ColliderDistance2D GetContactDistanceInfo(int index) {
            return mContactDistances[index];
        }

        public ColliderDistance2D GetContactDistanceInfo(Collider2D coll) {
            int ind = System.Array.IndexOf(mContactCache, coll);

            if(ind != -1)
                return mContactDistances[ind];
            else
                return new ColliderDistance2D() { isValid = false };
        }

        public ColliderDistance2D GetContactDistanceInfo(OrganismEntity ent) {
            return GetContactDistanceInfo(ent.bodyCollider);
        }

        public ColliderDistance2D GetContactDistanceInfo(EnergySource energy) {
            return GetContactDistanceInfo(energy.bodyCollider);
        }

        public void AngularVelocityDampen(float amt) {
            if(angularVelocity > 0f) {
                angularVelocity -= amt;
                if(angularVelocity < 0f)
                    angularVelocity = 0f;
            }
            else if(angularVelocity < 0f) {
                angularVelocity += amt;
                if(angularVelocity > 0f)
                    angularVelocity = 0f;
            }
        }

        public T GetComponentControl<T>() where T : OrganismComponentControl {
            for(int i = 0; i < mControls.Length; i++) {
                var ctrl = mControls[i];
                if(ctrl is T)
                    return (T)ctrl;
            }

            return null;
        }

        /// <summary>
        /// Used by inspector, this is for creating an organism entity manually.
        /// </summary>
        public void GenerateStats() {
            _stats = new OrganismStats();

            for(int i = 0; i < _comps.Length; i++) {
                _stats.Append(_comps[i].stats);
            }
        }

        public void GenerateSize() {
            //compute general size of collider (NOTE: assumes shape is centered)
            if(_bodyCollider) {
                var scale = transform.localScale;

                if(_bodyCollider is BoxCollider2D)
                    _size = (_bodyCollider as BoxCollider2D).size * scale;
                else if(_bodyCollider is CircleCollider2D) {
                    var diameter = (_bodyCollider as CircleCollider2D).radius * 2f;
                    _size = new Vector2(diameter, diameter) * scale;
                }
                else if(_bodyCollider is CapsuleCollider2D)
                    _size = (_bodyCollider as CapsuleCollider2D).size * scale;
                else if(_bodyCollider is PolygonCollider2D) {
                    var polyColl = _bodyCollider as PolygonCollider2D;

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

                    _size = new Vector2(Mathf.Abs(sMax.x - sMin.x) * scale.x, Mathf.Abs(sMax.y - sMin.y) * scale.y);
                }
            }
            else
                _size = Vector2.zero;
        }

        /// <summary>
        /// Used in inspector, this is for creating an organism entity manually.
        /// </summary>
        public void ComponentSetupTemplate() {
            for(int i = 0; i < _comps.Length; i++)
                _comps[i].SetupTemplate(this);
        }

        void M8.IPoolInit.OnInit() {
            poolControl = GetComponent<M8.PoolDataController>();

            if(sensor)
                sensor.Init(stats);

            //grab controls for components, initialize later once all controls are added (allows some controls to know which controls are available during init)
            var controlList = new List<ComponentControlInfo>();

            for(int i = 0; i < _comps.Length; i++) {
                var comp = _comps[i];

                var ctrl = comp.GenerateControl(this);
                if(ctrl != null)
                    controlList.Add(new ComponentControlInfo { component=comp, control=ctrl });
            }

            mControls = new OrganismComponentControl[controlList.Count];
            for(int i = 0; i < mControls.Length; i++)
                mControls[i] = controlList[i].control;

            //initialize controls
            for(int i = 0; i < controlList.Count; i++) {
                var ctrlInfo = controlList[i];
                ctrlInfo.control.Init(this, ctrlInfo.component);
            }
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
                mControls[i].Spawn(parms);
        }

        void M8.IPoolDespawn.OnDespawned() {
            for(int i = 0; i < mControls.Length; i++)
                mControls[i].Despawn();

            //do despawn stuff here
        }

        void Update() {
            var gameDat = GameData.instance;

            var env = GameModePlay.instance.environmentCurrentControl;

            //update contacts
            if(!physicsLocked) {
                var time = Time.time;
                
                if(time - mContactsUpdateLastTime >= gameDat.organismContactsUpdateDelay) {
                    mContactsUpdateLastTime = time;

                    contactCount = _bodyCollider.OverlapCollider(gameDat.organismContactFilter, mContactCache);

                    mContactSolids.Clear();
                    mContactOrganisms.Clear();
                    mContactOrganismMatches.Clear();
                    mContactEnergies.Clear();

                    for(int i = 0; i < contactCount; i++) {
                        var contact = mContactCache[i];

                        if(contact.isTrigger)
                            mContactDistances[i].isValid = false;
                        else
                            mContactDistances[i] = _bodyCollider.Distance(contact);

                        if(contact.CompareTag(gameDat.energyTag)) {
                            var energySrc = contact.GetComponent<EnergySource>();
                            if(energySrc && energySrc.isActive && stats.EnergyMatch(energySrc.data))
                                mContactEnergies.Add(energySrc);
                        }
                        else if(M8.Util.CheckTag(contact, gameDat.organismEntityTags)) {
                            var organismEnt = contact.GetComponent<OrganismEntity>();
                            if(organismEnt) {
                                mContactOrganisms.Add(organismEnt);

                                if(IsMatchTemplate(organismEnt))
                                    mContactOrganismMatches.Add(organismEnt);
                            }
                        }
                        else if(contact.CompareTag(gameDat.environmentSolidTag)) {
                            mContactSolids.Add(contact);

                            if(mContactDistances[i].distance < 0f) {
                                position += mContactDistances[i].normal * mContactDistances[i].distance;

                                velocity = Vector2.Reflect(velocityDir, -mContactDistances[i].normal) * speed * 0.3f;
                            }
                        }
                    }
                }
            }

            //update energy
            if(!stats.energyLocked) {
                var dt = Time.deltaTime;

                stats.EnergyUpdate(dt);

                //initial energy from other matching organisms
                float energyAmt = stats.energyShare;
                stats.energyShare = 0f;

                //check for energy source contacts and absorb its energy
                if(mContactEnergies.Count > 0) {
                    for(int i = 0; i < mContactEnergies.Count; i++) {
                        var energySrc = mContactEnergies[i];

                        var energyConsume = energySrc.energyRate * dt;

                        energySrc.energy -= energyConsume;
                        energyAmt += energyConsume;
                    }
                }
                
                if(energyAmt > 0f) {
                    //divide the amount of energy received, and share it to matching organisms
                    if(mContactOrganismMatches.Count > 0) {
                        var shareAmt = energyAmt / (mContactOrganismMatches.Count + 1);

                        for(int i = 0; i < mContactOrganismMatches.Count; i++) {
                            var entStats = mContactOrganismMatches[i].stats;
                            if(!entStats.energyLocked && !entStats.isEnergyFull && entStats.energyDelta <= 0f) {
                                entStats.energyShare += shareAmt;
                                energyAmt -= shareAmt;
                            }
                        }
                    }

                    stats.energy += energyAmt;
                }

                //environment energy
                for(int i = 0; i < env.energySources.Length; i++) {
                    var energySrc = env.energySources[i];
                    if(energySrc && energySrc.isActive && stats.EnergyMatch(energySrc.energySource))
                        stats.energy += energySrc.energyRate * dt;
                }

                //environment hazard
                for(int i = 0; i < env.hazards.Length; i++) {
                    var hazard = env.hazards[i];
                    if(hazard && hazard.isActive && !stats.HazardMatch(hazard.hazard))
                        stats.energy -= stats.energyCapacity * hazard.energyDrainScale * dt;
                }
            }

            //update component controls
            for(int i = 0; i < mControls.Length; i++)
                mControls[i].Update();

            //update physics
            if(!physicsLocked) {
                var dt = Time.deltaTime;

                //environmental velocity
                if(env.velocityControl && env.velocityControl.isActive)
                    velocity += env.velocityControl.GetVelocity(position, forward, dt) * stats.velocityReceiveScale;

                //limit/dampen speed
                if(speed > 0f) {
                    speed -= env.linearDrag * dt;

                    var speedLimit = stats.speedLimit;
                    if(speedLimit > 0f && speed > speedLimit)
                        speed = speedLimit;

                    //update position
                    position += velocity * dt;
                }

                //dampen angular speed
                if(angularVelocity != 0f) {
                    AngularVelocityDampen(env.angularDrag * dt);

                    //update orientation
                    forward = M8.MathUtil.RotateAngle(forward, angularVelocity * dt);
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

            contactCount = 0;

            mContactSolids.Clear();
            mContactOrganisms.Clear();
            mContactOrganismMatches.Clear();
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
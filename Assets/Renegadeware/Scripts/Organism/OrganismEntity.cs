using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismEntity : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolDespawn {
        [Header("Body")]
        [SerializeField]
        OrganismBodyDisplay _bodyDisplay = null;
        [SerializeField]
        Collider2D _bodyCollider = null;

        [Header("Components")]
        [SerializeField]
        OrganismComponent[] _comps = null;

        public M8.PoolDataController poolControl { get; private set; }

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

            ent._bodyDisplay = bodyGO.GetComponent<OrganismBodyDisplay>();
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
    }
}
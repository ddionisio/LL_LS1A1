using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismEntity : MonoBehaviour, M8.IPoolSpawn, M8.IPoolDespawn {
        [Header("Body")]
        [SerializeField]
        GameObject _bodyGO = null;
        [SerializeField]
        OrganismBodyDisplay _bodyDisplay = null;

        [Header("Components")]
        [SerializeField]
        OrganismComponent[] _comps = null;

        private bool mIsInit;

        private ISpawn[] mISpawns;
        private IDespawn[] mIDespawns;
        private IUpdate[] mIUpdates;

        /// <summary>
        /// Call this after creating the prefab, before generating the pool.
        /// </summary>
        public void SetupTemplate(OrganismTemplate organismTemplate) {
            //initialize body
            var bodyComp = organismTemplate.body;
            if(!bodyComp) {
                Debug.LogWarning("No Body Component found: "+organismTemplate.name);
                return;
            }

            _bodyGO = Instantiate(bodyComp.gamePrefab, Vector3.zero, Quaternion.identity, transform);
            _bodyDisplay = _bodyGO.GetComponent<OrganismBodyDisplay>();

            if(!_bodyDisplay) {
                Debug.LogWarning("No Body Display found: "+organismTemplate.name);
                return;
            }

            //initialize components
            _comps = new OrganismComponent[organismTemplate.componentIDs.Length];

            _comps[0] = bodyComp;

            for(int i = 1; i < _comps.Length; i++) {
                var comp = GameData.instance.GetOrganismComponent<OrganismComponent>(organismTemplate.componentIDs[i]);

                //instantiate prefab for component
                var compPrefab = comp.gamePrefab;
                if(compPrefab) {
                    var anchorList = _bodyDisplay.GetAnchors(comp.anchorName);
                    if(anchorList != null) {
                        for(int j = 0; j < anchorList.Count; j++) {
                            var t = anchorList[j];
                            Instantiate(compPrefab, Vector3.zero, Quaternion.identity, t);
                        }
                    }
                }

                _comps[i] = comp;
            }

            for(int i = 0; i < _comps.Length; i++)
                _comps[i].SetupTemplate(this);
        }

        void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
            if(!mIsInit) {
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

                mIsInit = true;
            }

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
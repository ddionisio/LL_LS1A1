using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismDisplayEdit : MonoBehaviour {
        public const int componentDataCapacity = 16;

        public class ComponentAnchorData {

            public bool isActive { get { return mActives.Count > 0; } }

            private M8.CacheList<GameObject> mActives;
            private M8.CacheList<GameObject> mCache;

            private GameObject mPrefabGO;
            private Transform mCacheRoot;

            public ComponentAnchorData(GameObject prefab, Transform cacheRoot) {
                mPrefabGO = prefab;
                mCacheRoot = cacheRoot;

                mActives = new M8.CacheList<GameObject>(componentDataCapacity);
                mCache = new M8.CacheList<GameObject>(componentDataCapacity);
            }

            public void Activate(List<Transform> anchorList) {
                for(int i = 0; i < anchorList.Count; i++) {
                    var anchorT = anchorList[i];

                    GameObject go;
                    if(mCache.Count > 0)
                        go = mCache.RemoveLast();
                    else
                        go = Instantiate(mPrefabGO);

                    var t = go.transform;
                    t.SetParent(anchorT, false);
                    t.localPosition = Vector3.zero;
                    t.localRotation = Quaternion.identity;

                    go.SetActive(true);

                    mActives.Add(go);
                }
            }

            public void Deactivate() {
                for(int i = 0; i < mActives.Count; i++) {
                    var go = mActives[i];

                    go.transform.SetParent(mCacheRoot, false);
                    go.SetActive(false);

                    mCache.Add(go);
                }

                mActives.Clear();
            }

            public void Destroy() {
                //destroy actives
                for(int i = 0; i < mActives.Count; i++) {
                    Object.Destroy(mActives[i]);
                }

                mActives.Clear();

                //destroy cache
                for(int i = 0; i < mCache.Count; i++) {
                    Object.Destroy(mCache[i]);
                }

                mCache.Clear();
            }
        }

        [Header("Cache Info")]
        public Transform cacheRoot; //move unused instances here

        private OrganismTemplate mTemplate;

        private OrganismBodyDisplay mBodyDisplay;

        private Dictionary<int, OrganismBodyDisplay> mBodyCache = new Dictionary<int, OrganismBodyDisplay>(); //component id, body display
        private Dictionary<int, ComponentAnchorData> mComponentAnchorInstances = new Dictionary<int, ComponentAnchorData>(); //component id, data

        private int[] mComponentEssentialIDs;
        private int[] mComponentIDs;

        public void Setup(OrganismTemplate organismTemplate) {
            ResetCache();

            mTemplate = organismTemplate;

            //duplicate component ids
            mComponentEssentialIDs = new int[mTemplate.componentEssentialIDs.Length];
            System.Array.Copy(mTemplate.componentEssentialIDs, mComponentEssentialIDs, mComponentEssentialIDs.Length);

            mComponentIDs = new int[mTemplate.componentIDs.Length];
            System.Array.Copy(mTemplate.componentIDs, mComponentIDs, mComponentIDs.Length);

            SetBody(mTemplate.body);
        }

        void OnDisable() {
            if(GameData.isInstantiated) {
                var gameDat = GameData.instance;

                //unhook signals
                gameDat.signalEditBodyPreview.callback -= OnEditBodyPreview;
                gameDat.signalEditComponentEssentialPreview.callback -= OnEditComponentEssentialPreview;
                gameDat.signalEditComponentPreview.callback -= OnEditComponentPreview;
                gameDat.signalEditRefresh.callback -= OnRefresh;

                gameDat.signalOrganismBodyChanged.callback -= OnRefresh;
                gameDat.signalOrganismComponentEssentialChanged.callback -= OnOrganismComponentEssentialChanged;
                gameDat.signalOrganismComponentChanged.callback -= OnOrganismComponentChanged;
            }
        }

        void OnEnable() {
            var gameDat = GameData.instance;

            //hook up signals
            gameDat.signalEditBodyPreview.callback += OnEditBodyPreview;
            gameDat.signalEditComponentEssentialPreview.callback += OnEditComponentEssentialPreview;
            gameDat.signalEditComponentPreview.callback += OnEditComponentPreview;
            gameDat.signalEditRefresh.callback += OnRefresh;

            gameDat.signalOrganismBodyChanged.callback += OnRefresh;
            gameDat.signalOrganismComponentEssentialChanged.callback += OnOrganismComponentEssentialChanged;
            gameDat.signalOrganismComponentChanged.callback += OnOrganismComponentChanged;
        }

        void OnEditBodyPreview(int bodyCompID) {
            var body = GameData.instance.GetOrganismComponent<OrganismBody>(bodyCompID);
            SetBody(body);
        }

        void OnEditComponentEssentialPreview(int compIndex, int compID) {
            //clear out component essentials
            for(int i = 0; i < mComponentEssentialIDs.Length; i++)
                ClearComponent(mComponentEssentialIDs[i]);

            //refresh component essential IDs
            System.Array.Copy(mTemplate.componentEssentialIDs, mComponentEssentialIDs, mComponentEssentialIDs.Length);

            //set component and refresh display
            mComponentEssentialIDs[compIndex] = compID;

            for(int i = 0; i < mComponentEssentialIDs.Length; i++)
                ApplyComponent(mComponentEssentialIDs[i]);
        }

        void OnEditComponentPreview(int compIndex, int compID) {
            if(mComponentIDs[compIndex] != compID) {
                //clear out current component
                ClearComponent(mComponentIDs[compIndex]);

                //set component and refresh display
                mComponentIDs[compIndex] = compID;

                ApplyComponent(mComponentIDs[compIndex]);
            }
        }

        void OnRefresh() {
            //refresh component IDs from template
            System.Array.Copy(mTemplate.componentEssentialIDs, mComponentEssentialIDs, mComponentEssentialIDs.Length);
            System.Array.Copy(mTemplate.componentIDs, mComponentIDs, mComponentIDs.Length);

            //just refresh the entire thing
            SetBody(mTemplate.body);
        }

        void OnOrganismComponentEssentialChanged(int compIndex) {
            //refresh component essential ID cache, update display
            var newID = mTemplate.componentEssentialIDs[compIndex];

            if(mComponentEssentialIDs[compIndex] != newID) {
                ClearComponent(mComponentEssentialIDs[compIndex]);

                mComponentEssentialIDs[compIndex] = newID;

                ApplyComponent(mComponentEssentialIDs[compIndex]);
            }
        }

        void OnOrganismComponentChanged(int compIndex) {
            //refresh component ID cache, update display
            var newID = mTemplate.componentIDs[compIndex];

            if(mComponentIDs[compIndex] != newID) {
                ClearComponent(mComponentIDs[compIndex]);

                mComponentIDs[compIndex] = newID;

                ApplyComponent(mComponentIDs[compIndex]);
            }
        }

        private void SetBody(OrganismBody body) {
            ClearComponents();

            //hide previous body
            if(mBodyDisplay) {
                mBodyDisplay.gameObject.SetActive(false);
                mBodyDisplay = null;
            }

            //setup body display
            if(!mBodyCache.TryGetValue(body.ID, out mBodyDisplay)) {
                //instantiate body
                var bodyGOInst = Instantiate(body.editPrefab);
                bodyGOInst.transform.SetParent(transform, false);

                mBodyDisplay = bodyGOInst.GetComponent<OrganismBodyDisplay>();

                mBodyCache.Add(body.ID, mBodyDisplay);
            }

            //setup essentials
            for(int i = 0; i < mComponentEssentialIDs.Length; i++)
                ApplyComponent(mComponentEssentialIDs[i]);

            //setup components
            for(int i = 1; i < mComponentIDs.Length; i++)
                ApplyComponent(mComponentIDs[i]);

            mBodyDisplay.gameObject.SetActive(true);
        }

        private void ApplyComponent(int compID) {
            if(compID == GameData.invalidID)
                return;

            var comp = GameData.instance.GetOrganismComponent<OrganismComponent>(compID);
            if(!comp)
                return;

            //don't attach if no prefab
            var prefab = comp.editPrefab;
            if(!prefab)
                return;

            //don't attach if no anchor
            var anchorList = mBodyDisplay.GetAnchors(comp.anchorName);
            if(anchorList == null || anchorList.Count == 0)
                return;

            ComponentAnchorData anchorDat;
            if(!mComponentAnchorInstances.TryGetValue(compID, out anchorDat)) {
                anchorDat = new ComponentAnchorData(prefab, cacheRoot);
                mComponentAnchorInstances.Add(compID, anchorDat);
            }

            anchorDat.Activate(anchorList);
        }

        private void ClearComponent(int compID) {
            if(compID == GameData.invalidID)
                return;

            ComponentAnchorData anchorDat;
            if(mComponentAnchorInstances.TryGetValue(compID, out anchorDat)) {
                anchorDat.Deactivate();
            }
        }

        private void ClearComponents() {
            foreach(var pair in mComponentAnchorInstances)
                pair.Value.Deactivate();
        }

        private void ResetCache() {
            //destroy bodies
            foreach(var pair in mBodyCache) {
                Destroy(pair.Value.gameObject);
            }

            mBodyCache.Clear();

            foreach(var pair in mComponentAnchorInstances) {
                pair.Value.Destroy();
            }

            mComponentAnchorInstances.Clear();
        }
    }
}
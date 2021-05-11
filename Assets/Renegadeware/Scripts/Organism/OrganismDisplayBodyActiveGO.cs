using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismDisplayBodyActiveGO : MonoBehaviour, M8.IPoolInit {
        [System.Serializable]
        public class ComponentDisplay {
            [OrganismComponentID]
            public int bodyID;

            public GameObject activeGO;

            public void ApplyActive(int curBodyID) {
                if(activeGO && bodyID == curBodyID)
                    activeGO.SetActive(true);
            }

            public void Hide() {
                if(activeGO)
                    activeGO.SetActive(false);
            }
        }

        public ComponentDisplay[] displays;

        public OrganismDisplayEdit displayEdit {
            get {
                if(!mDisplayEdit)
                    mDisplayEdit = GetComponentInParent<OrganismDisplayEdit>();
                return mDisplayEdit;
            }
        }

        private OrganismDisplayEdit mDisplayEdit;

        void OnEnable() {
            for(int i = 0; i < displays.Length; i++)
                displays[i].Hide();

            if(displayEdit) {
                var curBodyID = displayEdit.bodyID;

                for(int i = 0; i < displays.Length; i++)
                    displays[i].ApplyActive(curBodyID);
            }
        }

        void M8.IPoolInit.OnInit() {
            if(!mDisplayEdit)
                mDisplayEdit = GetComponentInParent<OrganismDisplayEdit>();
        }
    }
}
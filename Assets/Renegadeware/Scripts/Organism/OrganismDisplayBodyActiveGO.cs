﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismDisplayBodyActiveGO : MonoBehaviour {
        [System.Serializable]
        public class ComponentDisplay {
            [OrganismComponentID]
            public int bodyID;

            public GameObject activeGO;

            public void ApplyActive(int curBodyID) {
                if(activeGO)
                    activeGO.SetActive(bodyID == curBodyID);
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
            if(displayEdit) {
                var curBodyID = displayEdit.bodyID;

                for(int i = 0; i < displays.Length; i++)
                    displays[i].ApplyActive(curBodyID);
            }
        }
    }
}
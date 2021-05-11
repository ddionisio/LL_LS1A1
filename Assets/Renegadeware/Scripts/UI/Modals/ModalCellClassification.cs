using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class ModalCellClassification : M8.ModalController, M8.IModalPush {
        public const string parmIndex = "ccInd";

        [Header("Display")]
        public GameObject[] classificationRootGOs;

        void M8.IModalPush.Push(M8.GenericParams parms) {
            int ind = -1;

            if(parms != null) {
                if(parms.ContainsKey(parmIndex))
                    ind = parms.GetValue<int>(parmIndex);
            }

            for(int i = 0; i < classificationRootGOs.Length; i++) {
                var go = classificationRootGOs[i];
                if(go)
                    go.SetActive(i == ind);
            }
        }
    }
}
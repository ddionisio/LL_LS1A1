using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "cellTemplate", menuName = "Game/Cell Template")]
    public class CellTemplate : ScriptableObject {
        public int[] componentIDs;

        private const string userDataKeySub = "_cellTemplate";
        private const string userDataKeySubCompCount = "_compCount";
        private const string userDataKeySubComp = "_comp";

        /// <summary>
        /// Instantiate a template from user data. Remember to delete this when done.
        /// </summary>
        public static CellTemplate LoadFrom(string key) {
            if(!LoLManager.isInstantiated)
                return null;

            if(!LoLManager.instance.userData)
                return null;

            var newCellTemplate = CreateInstance<CellTemplate>();

            var usrData = LoLManager.instance.userData;

            var keyCellTemplate = key + userDataKeySub;

            //grab components
            int componentCount = usrData.GetInt(keyCellTemplate + userDataKeySubCompCount);

            newCellTemplate.componentIDs = new int[componentCount];

            var keyComponent = keyCellTemplate + userDataKeySubComp;

            for(int i = 0; i < componentCount; i++)
                newCellTemplate.componentIDs[i] = usrData.GetInt(keyComponent + i, -1);

            return newCellTemplate;
        }

        public void Save(string key) {
            if(!LoLManager.isInstantiated)
                return;

            if(!LoLManager.instance.userData)
                return;

            var usrData = LoLManager.instance.userData;

            var keyCellTemplate = key + userDataKeySub;

            //save components
            usrData.SetInt(keyCellTemplate + userDataKeySubCompCount, componentIDs.Length);

            for(int i = 0; i < componentIDs.Length; i++)
                usrData.SetInt(keyCellTemplate + userDataKeySubComp, componentIDs[i]);
        }
    }
}
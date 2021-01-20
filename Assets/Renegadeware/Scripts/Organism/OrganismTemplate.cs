using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "organismTemplate", menuName = "Game/Organism Template")]
    public class OrganismTemplate : ScriptableObject {
        public const int invalidID = -1;

        public int ID;
        public int[] componentIDs;

        private const string userDataKeySubID = "_id";
        private const string userDataKeySubCompCount = "_compCount";
        private const string userDataKeySubComp = "_comp";

        /// <summary>
        /// Instantiate a template from user data. Remember to delete this when done.
        /// </summary>
        public static OrganismTemplate LoadFrom(M8.UserData usrData, string key) {
            var newCellTemplate = CreateInstance<OrganismTemplate>();

            //grab ID
            newCellTemplate.ID = usrData.GetInt(key + userDataKeySubID, -1);

            //grab components
            int componentCount = usrData.GetInt(key + userDataKeySubCompCount);

            newCellTemplate.componentIDs = new int[componentCount];

            var keyComponent = key + userDataKeySubComp;

            for(int i = 0; i < componentCount; i++)
                newCellTemplate.componentIDs[i] = usrData.GetInt(keyComponent + i, -1);

            return newCellTemplate;
        }

        public void SaveTo(M8.UserData usrData, string key) {
            //save ID
            usrData.SetInt(key + userDataKeySubID, ID);

            //save components
            usrData.SetInt(key + userDataKeySubCompCount, componentIDs.Length);

            for(int i = 0; i < componentIDs.Length; i++)
                usrData.SetInt(key + userDataKeySubComp, componentIDs[i]);
        }
    }
}
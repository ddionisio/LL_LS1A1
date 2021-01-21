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

            newCellTemplate.Load(usrData, key);

            return newCellTemplate;
        }

        public static OrganismTemplate CreateEmpty() {
            var newCellTemplate = CreateInstance<OrganismTemplate>();
            newCellTemplate.Reset();
            return newCellTemplate;
        }

        public static OrganismTemplate Clone(OrganismTemplate src) {
            var newCellTemplate = CreateInstance<OrganismTemplate>();
            newCellTemplate.CopyFrom(src);
            return newCellTemplate;
        }

        public void Reset() {
            ID = invalidID;
            componentIDs = new int[0];
        }

        public void Load(M8.UserData usrData, string key) {
            //grab ID
            ID = usrData.GetInt(key + userDataKeySubID, -1);

            //grab components
            int componentCount = usrData.GetInt(key + userDataKeySubCompCount);

            componentIDs = new int[componentCount];

            var keyComponent = key + userDataKeySubComp;

            for(int i = 0; i < componentCount; i++)
                componentIDs[i] = usrData.GetInt(keyComponent + i, -1);
        }

        public void SaveTo(M8.UserData usrData, string key) {
            //save ID
            usrData.SetInt(key + userDataKeySubID, ID);

            //save components
            usrData.SetInt(key + userDataKeySubCompCount, componentIDs.Length);

            for(int i = 0; i < componentIDs.Length; i++)
                usrData.SetInt(key + userDataKeySubComp, componentIDs[i]);
        }

        public void CopyFrom(OrganismTemplate src) {
            ID = src.ID;
            componentIDs = new int[src.componentIDs.Length];
            System.Array.Copy(src.componentIDs, componentIDs, componentIDs.Length);
        }
    }
}
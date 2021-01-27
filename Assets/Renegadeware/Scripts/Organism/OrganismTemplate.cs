using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "organismTemplate", menuName = "Game/Organism Template")]
    public class OrganismTemplate : ScriptableObject {
        public const int invalidID = -1;

        public int ID;
        public int[] componentEssentialIDs;
        public int[] componentIDs;

        private const string userDataKeySubID = "_id";

        private const string userDataKeySubCompEssentialCount = "_compECount";
        private const string userDataKeySubCompEssential = "_compE";

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

        //Game API

        public OrganismBody GetBody() {
            if(componentIDs != null && componentIDs.Length > 0)
                return GameData.instance.GetOrganismComponent<OrganismBody>(componentIDs[0]);
            else
                return null;
        }

        public bool IsEssentialComponentsFilled() {
            if(componentEssentialIDs == null)
                return false;

            var body = GetBody();
            if(!body)
                return false;

            int fillCount = 0;
            for(int i = 0; i < componentEssentialIDs.Length; i++) {
                if(componentEssentialIDs[i] != invalidID)
                    fillCount++;
            }

            return fillCount == componentEssentialIDs.Length;
        }

        public void SetBody(OrganismBody newBody) {
            //try to re-attach essential components
            var newCompEssentialIds = new int[newBody.componentEssentials.Length];

            for(int i = 0; i < newBody.componentEssentials.Length; i++) {
                var comp = newBody.componentEssentials[i];

                //grab compatible component for this group
                int compId = -1;
                if(componentEssentialIDs != null) {
                    for(int j = 0; j < componentEssentialIDs.Length; j++) {
                        if(componentEssentialIDs[j] != invalidID && componentEssentialIDs[j] == comp.ID) {
                            compId = componentEssentialIDs[j];
                            componentEssentialIDs[j] = invalidID;
                            break;
                        }
                    }
                }

                newCompEssentialIds[i] = compId;
            }

            componentEssentialIDs = newCompEssentialIds;

            //try to re-attach existing components that match new body
            var newCompIds = new int[newBody.componentGroups.Length + 1];
            newCompIds[0] = newBody.ID;

            for(int i = 0; i < newBody.componentGroups.Length; i++) {
                var grp = newBody.componentGroups[i];

                //grab compatible component for this group
                int compId = -1;
                if(componentIDs != null) {
                    for(int j = 1; j < componentIDs.Length; j++) {
                        if(componentIDs[j] != invalidID && grp.GetIndex(componentIDs[j]) != -1) {
                            compId = componentIDs[j];
                            componentIDs[j] = invalidID;
                            break;
                        }
                    }
                }

                newCompIds[i + 1] = compId;
            }

            componentIDs = newCompIds;

            //signal body change
            GameData.instance.signalOrganismBodyChanged.Invoke();
        }

        public void SetComponentEssentialID(int index, int id) {
            if(index >= componentEssentialIDs.Length)
                return;

            componentEssentialIDs[index] = id;

            //signal component change
            GameData.instance.signalOrganismComponentEssentialChanged.Invoke(index);
        }

        public void SetComponentID(int index, int id) {
            if(index >= componentIDs.Length)
                return;

            componentIDs[index] = id;

            //signal component change
            GameData.instance.signalOrganismComponentChanged.Invoke(index);
        }

        public int GetComponentIndex(int id) {
            if(componentIDs == null)
                return -1;

            for(int i = 0; i < componentIDs.Length; i++) {
                if(componentIDs[i] == id)
                    return i;
            }

            return -1;
        }

        public int GetComponentEssentialIndex(int id) {
            if(componentEssentialIDs == null)
                return -1;

            for(int i = 0; i < componentEssentialIDs.Length; i++) {
                if(componentEssentialIDs[i] == id)
                    return i;
            }

            return -1;
        }

        //User Data API

        public void Reset() {
            ID = invalidID;
            componentEssentialIDs = new int[0];
            componentIDs = new int[0];
        }

        public void Load(M8.UserData usrData, string key) {
            //grab ID
            ID = usrData.GetInt(key + userDataKeySubID, -1);

            //grab component essentials
            int componentEssentialCount = usrData.GetInt(key + userDataKeySubCompEssentialCount);

            componentIDs = new int[componentEssentialCount];

            var keyComponent = key + userDataKeySubCompEssential;

            for(int i = 0; i < componentEssentialCount; i++)
                componentEssentialIDs[i] = usrData.GetInt(keyComponent + i, -1);

            //grab components
            int componentCount = usrData.GetInt(key + userDataKeySubCompCount);

            componentIDs = new int[componentCount];

            keyComponent = key + userDataKeySubComp;

            for(int i = 0; i < componentCount; i++)
                componentIDs[i] = usrData.GetInt(keyComponent + i, -1);
        }

        public void SaveTo(M8.UserData usrData, string key) {
            //save ID
            usrData.SetInt(key + userDataKeySubID, ID);

            //save component essentials
            usrData.SetInt(key + userDataKeySubCompEssentialCount, componentEssentialIDs.Length);

            for(int i = 0; i < componentEssentialIDs.Length; i++)
                usrData.SetInt(key + userDataKeySubCompEssential + i, componentEssentialIDs[i]);

            //save components
            usrData.SetInt(key + userDataKeySubCompCount, componentIDs.Length);

            for(int i = 0; i < componentIDs.Length; i++)
                usrData.SetInt(key + userDataKeySubComp + i, componentIDs[i]);
        }

        public void CopyFrom(OrganismTemplate src) {
            ID = src.ID;

            componentEssentialIDs = new int[src.componentEssentialIDs.Length];
            System.Array.Copy(src.componentEssentialIDs, componentEssentialIDs, componentEssentialIDs.Length);

            componentIDs = new int[src.componentIDs.Length];
            System.Array.Copy(src.componentIDs, componentIDs, componentIDs.Length);
        }
    }
}
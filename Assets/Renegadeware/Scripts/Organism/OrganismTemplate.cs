using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "organismTemplate", menuName = "Game/Organism/Template")]
    public class OrganismTemplate : ScriptableObject {
        [ID(group = "organismTemplate", invalidID = GameData.invalidID)]
        public int ID;
        [OrganismComponentID]
        public int[] componentEssentialIDs;
        [OrganismComponentID]
        public int[] componentIDs;

        public OrganismBody body {
            get {
                if(componentIDs != null && componentIDs.Length > 0)
                    return GameData.instance.GetOrganismComponent<OrganismBody>(componentIDs[0]);
                else
                    return null;
            }

            set {
                if(!value) { //clear out?
                    componentEssentialIDs = new int[0];
                    componentIDs = new int[0];

                    //signal body change
                    GameData.instance.signalOrganismBodyChanged.Invoke();

                    return;
                }

                if(componentIDs.Length > 1 && componentIDs[0] == value.ID) //check if already set
                    return;

                //try to re-attach essential components
                var newCompEssentialIds = new int[value.componentEssentials.Length];

                for(int i = 0; i < newCompEssentialIds.Length; i++) {
                    int ind = GetComponentEssentialIndex(value.componentEssentials[i].ID);
                    if(ind != -1)
                        newCompEssentialIds[i] = componentEssentialIDs[ind];
                    else
                        newCompEssentialIds[i] = GameData.invalidID;
                }

                componentEssentialIDs = newCompEssentialIds;
                //

                //try to re-attach existing components that match new body
                componentIDs = GetNewComponents(value);

                //signal body change
                GameData.instance.signalOrganismBodyChanged.Invoke();
            }
        }

        public bool isEssentialComponentsFilled {
            get {
                if(componentEssentialIDs == null)
                    return false;

                if(!body)
                    return false;

                int fillCount = 0;
                for(int i = 0; i < componentEssentialIDs.Length; i++) {
                    if(componentEssentialIDs[i] != GameData.invalidID)
                        fillCount++;
                }

                return fillCount == componentEssentialIDs.Length;
            }
        }

        public bool isValid {
            get {
                if(!isEssentialComponentsFilled)
                    return false;

                //check components
                for(int i = 0; i < componentIDs.Length; i++) {
                    if(componentIDs[i] == GameData.invalidID)
                        return false;
                }

                return true;
            }
        }

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

        public int[] GetNewComponents(OrganismBody bodyPreview) {
            //try to re-attach existing components that match new body
            var newCompIds = new int[bodyPreview.componentGroups.Length + 1];
            newCompIds[0] = bodyPreview.ID;

            for(int i = 0; i < bodyPreview.componentGroups.Length; i++) {
                var grp = bodyPreview.componentGroups[i];
                var grpCompInd = grp.GetIndex(componentIDs, 1);
                if(grpCompInd != -1)
                    newCompIds[i + 1] = grp.components[grpCompInd].ID;
                else //set as default from group
                    newCompIds[i + 1] = grp.defaultComponentID;
            }

            return newCompIds;
        }

        //User Data API

        public void Reset() {
            ID = GameData.invalidID;
            componentEssentialIDs = new int[0];
            componentIDs = new int[0];
        }

        public void Load(M8.UserData usrData, string key) {
            //grab ID
            ID = usrData.GetInt(key + userDataKeySubID, GameData.invalidID);

            //grab component essentials
            var keyComponent = key + userDataKeySubCompEssential;

            int componentEssentialCount = usrData.GetInt(key + userDataKeySubCompEssentialCount);

            componentEssentialIDs = new int[componentEssentialCount];
            for(int i = 0; i < componentEssentialCount; i++)
                componentEssentialIDs[i] = usrData.GetInt(keyComponent + i, GameData.invalidID);

            //grab components
            keyComponent = key + userDataKeySubComp;

            int componentCount = usrData.GetInt(key + userDataKeySubCompCount);

            componentIDs = new int[componentCount];
            for(int i = 0; i < componentCount; i++)
                componentIDs[i] = usrData.GetInt(keyComponent + i, GameData.invalidID);
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
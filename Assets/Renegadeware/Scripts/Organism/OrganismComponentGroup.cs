using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "organismComponentGroup", menuName = "Game/Organism/Component Group")]
    public class OrganismComponentGroup : InfoData {
        [Tooltip("Set to true to hide from edit mode.")]
        public bool isHidden;

        [Header("Components")]
        public int defaultIndex = 0;
        public OrganismComponent[] components;

        public int defaultComponentID { 
            get {
                if(defaultIndex >= 0 && defaultIndex < components.Length)
                    return components[defaultIndex].ID;

                return GameData.invalidID;
            } 
        }

        public int GetIndex(int compId) {
            for(int i = 0; i < components.Length; i++) {
                if(components[i].ID == compId)
                    return i;
            }

            return -1;
        }

        public int GetIndex(OrganismComponent comp) {
            for(int i = 0; i < components.Length; i++) {
                if(components[i] == comp)
                    return i;
            }

            return -1;
        }

        public int GetIndex(int[] ids, int startIndex) {
            if(ids == null)
                return -1;

            for(int i = startIndex; i < ids.Length; i++) {
                var id = ids[i];
                for(int j = 0; j < components.Length; j++) {
                    if(components[j].ID == id)
                        return j;
                }
            }

            return -1;
        }
    }
}
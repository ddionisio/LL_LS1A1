using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "organismComponentGroup", menuName = "Game/Organism Component Group")]
    public class OrganismComponentGroup : InfoData {
        [Header("Components")]
        public OrganismComponent[] components;

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
    }
}
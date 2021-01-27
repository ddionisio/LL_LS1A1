using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "cellbody", menuName = "Game/Organism Component/Body")]
    public class OrganismBody : OrganismComponent {
        [Header("Body Info")]
        public OrganismComponent[] componentEssentials; //essential organelles for this body (used after picking body the first time)
        public OrganismComponentGroup[] componentGroups;

        public int GetComponentEssentialIndex(int id) {
            for(int i = 0; i < componentEssentials.Length; i++) {
                if(componentEssentials[i].ID == id)
                    return i;
            }

            return -1;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "body", menuName = "Game/Organism/Component/Body")]
    public class OrganismBody : OrganismComponent {
        [Header("Templates")]
        [SerializeField]
        GameObject _editPrefab = null;
        [SerializeField]
        GameObject _gamePrefab = null;

        [Header("Body Info")]
        public OrganismComponent[] componentEssentials; //essential organelles for this body (used after picking body the first time)
        public OrganismComponentGroup[] componentGroups;

        public override GameObject editPrefab => _editPrefab;
        public override GameObject gamePrefab => _gamePrefab;

        public int GetComponentEssentialIndex(int id) {
            for(int i = 0; i < componentEssentials.Length; i++) {
                if(componentEssentials[i].ID == id)
                    return i;
            }

            return -1;
        }
    }
}
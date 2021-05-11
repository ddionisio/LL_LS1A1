using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismBody : OrganismComponent {
        [Header("Templates")]
        [SerializeField]
        GameObject _editPrefab = null;
        [SerializeField]
        GameObject _gamePrefab = null;

        [Header("Body Info")]
        public OrganismComponent[] componentEssentials; //essential organelles for this body (used after picking body the first time)
        public OrganismComponentGroup[] componentGroups;

        [Header("Body Display")]
        public Color bodyColor = Color.white;

        public override GameObject editPrefab => _editPrefab;
        public override GameObject gamePrefab => _gamePrefab;

        public int GetComponentEssentialIndex(int id) {
            for(int i = 0; i < componentEssentials.Length; i++) {
                if(componentEssentials[i].ID == id)
                    return i;
            }

            return -1;
        }

        public override void SetupTemplate(OrganismEntity organismEntity) {
            if(organismEntity.bodyDisplay.colorGroup)
                organismEntity.bodyDisplay.colorGroup.ApplyColor(bodyColor);
        }

        public override void SetupEditBody(OrganismDisplayBody displayBody) {
            if(displayBody.colorGroup)
                displayBody.colorGroup.ApplyColor(bodyColor);
        }
    }
}
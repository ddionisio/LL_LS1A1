using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "component", menuName = "Game/Organism/Component/Generic")]
    public class OrganismComponentGeneric : OrganismComponent {
        [Header("Prefabs")]
        [SerializeField]
        GameObject _editPrefab;
        [SerializeField]
        GameObject _gamePrefab;

        [Header("Body")]
        public Color bodyColor = Color.white;

        [SerializeField]
        string _anchor;

        public override string anchorName => _anchor;

        public override GameObject editPrefab => _editPrefab;
        public override GameObject gamePrefab => _gamePrefab;

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
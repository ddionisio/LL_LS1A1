using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "none", menuName = "Game/Organism/Component/Essential")]
    public class OrganismComponentEssential : OrganismComponent {
        [Header("Component Info")]
        [SerializeField]
        GameObject _editPrefab = null;

        [SerializeField]
        string _anchorName = "";

        public override GameObject editPrefab => _editPrefab;

        public override string anchorName => _anchorName;
    }
}
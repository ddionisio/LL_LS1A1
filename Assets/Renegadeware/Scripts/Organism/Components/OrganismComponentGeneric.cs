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

        public override GameObject editPrefab => _editPrefab;
        public override GameObject gamePrefab => _gamePrefab;


    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "cellbody", menuName = "Game/Cell Component/Body")]
    public class CellBody : CellComponent {
        [System.Serializable]
        public struct Category {
            public CategoryData info;
            public CellComponent[] components;
        }

        [Header("Body Info")]
        public Category[] componentCategories;
    }
}
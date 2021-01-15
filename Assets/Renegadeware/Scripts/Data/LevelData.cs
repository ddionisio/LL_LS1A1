using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "level", menuName = "Game/Level")]
    public class LevelData : ScriptableObject {
        [Header("Cell Bodies")]
        public CategoryData bodyCategory;
        public CellComponent[] bodyComponents;

        //environment selections

        //cell spawn restriction, etc.
    }
}
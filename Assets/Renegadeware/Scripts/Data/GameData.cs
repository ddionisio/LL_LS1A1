using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "gameData", menuName = "Game/Game Data", order = 0)]
    public class GameData : M8.SingletonScriptableObject<GameData> {
        [Header("Cell Components")]
        public CellComponent[] cellComponents;

        public CellComponent GetCellComponent(int id) {
            for(int i = 0; i < cellComponents.Length; i++) {
                var comp = cellComponents[i];
                if(comp && comp.id == id)
                    return comp;
            }

            return null;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "gameData", menuName = "Game/Game Data", order = 0)]
    public class GameData : M8.SingletonScriptableObject<GameData> {
        public struct EnvironmentStat {
            public string ID;
            public int cellTemplateID;
            public int cellCount;
        }

        [Header("Scene")]
        public M8.SceneAssetPath introScene;
        public M8.SceneAssetPath endScene;

        [Header("Levels")]
        public LevelData[] levels;

        [Header("Cell Components")]
        public CellComponent[] cellComponents;

        private List<CellTemplate> mCellTemplateList; //saved cells made by player, count is based on (current progress + 1)
        private List<EnvironmentStat> mEnvironmentCompleteList; //saved stats for completed environment;        

        public CellComponent GetCellComponent(int id) {
            for(int i = 0; i < cellComponents.Length; i++) {
                var comp = cellComponents[i];
                if(comp && comp.ID == id)
                    return comp;
            }

            return null;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "gameData", menuName = "Game/Game Data", order = 0)]
    public class GameData : M8.SingletonScriptableObject<GameData> {
        public struct EnvironmentStat {
            public string levelName; //name of LevelData
            public int index; //which environment index
            public int organismTemplateID; //cell template used for this environment
            public int count; //propagation count during play (cell or organism)
        }

        [Header("Scene")]
        public M8.SceneAssetPath introScene;
        public M8.SceneAssetPath endScene;

        [Header("Levels")]
        public LevelData[] levels;

        [Header("Cell Components")]
        public OrganismComponent[] organismComponents;

        public bool isGameStarted { get; private set; } //true: we got through start normally, false: debug

        private List<OrganismTemplate> mOrganismTemplateList; //saved organisms made by player
        private List<EnvironmentStat> mEnvironmentStateList; //saved stats for environment

        /// <summary>
        /// Called in start scene
        /// </summary>
        public void Begin(bool isRestart) {
            if(isRestart) {
                ResetUserData();

                LoLManager.instance.ApplyProgress(0, 0);
            }
            else
                LoadUserData();

            isGameStarted = true;

            Current();
        }

        /// <summary>
        /// Update level index based on current progress, and load scene
        /// </summary>
        public void Current() {
            var lolMgr = LoLManager.instance;

            var curProgress = lolMgr.curProgress;

            if(curProgress <= 0) { //intro
                introScene.Load();
            }
            else { //grab level index, and load level scene
                int levelIndex = 0;

                int levelProgressCount = 0;
                for(int i = 0; i < levels.Length; i++) {
                    levelProgressCount += levels[i].progressCount;
                    if(curProgress < levelProgressCount)
                        break;

                    levelIndex++;
                }

                if(levelIndex < levels.Length) {
                    levels[levelIndex].scene.Load();
                }
                else { //end
                    endScene.Load();
                }
            }
        }

        public void Progress() {
            int curProgress = 0;

            if(isGameStarted) {
                if(LoLManager.isInstantiated)
                    curProgress = LoLManager.instance.curProgress;
            }
            else {
                //determine our progress based on current scene
                var curScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                int levelProgressCount = 0;
                for(int i = 0; i < levels.Length; i++) {
                    var lvl = levels[i];

                    if(lvl.scene == curScene) {
                        curProgress = levelProgressCount;
                        break;
                    }

                    levelProgressCount += lvl.progressCount;
                }

                isGameStarted = true;
            }

            if(LoLManager.isInstantiated)
                LoLManager.instance.ApplyProgress(curProgress + 1);

            //load to new scene
            Current();
        }

        public OrganismComponent GetCellComponent(int id) {
            for(int i = 0; i < organismComponents.Length; i++) {
                var comp = organismComponents[i];
                if(comp && comp.ID == id)
                    return comp;
            }

            return null;
        }

        protected override void OnInstanceInit() {
            //compute max progress
            if(LoLManager.isInstantiated) {
                int progressCount = 0;

                for(int i = 0; i < levels.Length; i++)
                    progressCount += levels[i].progressCount;

                LoLManager.instance.progressMax = progressCount;
            }

            isGameStarted = false;
        }

        private void LoadUserData() {

        }

        private void ResetUserData() {

        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "gameData", menuName = "Game/Game Data", order = 0)]
    public class GameData : M8.SingletonScriptableObject<GameData> {
        [Header("Scene")]
        public M8.SceneAssetPath endScene;

        [Header("Levels")]
        public LevelData[] levels;

        [Header("Cell Components")]
        public OrganismComponent[] organismComponents;

        public bool isGameStarted { get; private set; } //true: we got through start normally, false: debug

        private const string userDataKeyOrganismTemplateCount = "organismCount";
        private const string userDataKeyOrganismTemplate = "organism";

        private List<OrganismTemplate> mOrganismTemplateList; //saved organisms made by player

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

            //grab level index, and load level scene
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

            if(LoLManager.isInstantiated) {
                SaveUserData();

                LoLManager.instance.ApplyProgress(curProgress + 1);
            }

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
            if(!LoLManager.isInstantiated)
                return;

            var usrData = LoLManager.instance.userData;
            if(!usrData)
                return;

            //load level stats
            for(int i = 0; i < levels.Length; i++) {
                if(levels[i])
                    levels[i].LoadStatsFrom(usrData);
            }

            //load organism templates
            ClearOrganismTemplates();

            int organismCount = usrData.GetInt(userDataKeyOrganismTemplateCount);
            if(organismCount == 0)
                return;

            mOrganismTemplateList = new List<OrganismTemplate>(organismCount);

            for(int i = 0; i < organismCount; i++)
                mOrganismTemplateList.Add(OrganismTemplate.LoadFrom(usrData, userDataKeyOrganismTemplate + i));
        }

        private void SaveUserData() {
            if(!LoLManager.isInstantiated)
                return;

            var usrData = LoLManager.instance.userData;
            if(!usrData)
                return;

            //save level stats
            for(int i = 0; i < levels.Length; i++) {
                if(levels[i])
                    levels[i].SaveStatsTo(usrData);
            }

            //save organism templates
            int organismCount = mOrganismTemplateList != null ? mOrganismTemplateList.Count : 0;

            usrData.SetInt(userDataKeyOrganismTemplateCount, organismCount);

            int curInd = 0;
            for(int i = 0; i < organismCount; i++) {
                var organismTemplate = mOrganismTemplateList[i];
                if(organismTemplate) {
                    organismTemplate.SaveTo(usrData, userDataKeyOrganismTemplate + curInd);
                    curInd++;
                }
            }
        }

        private void ResetUserData() {
            if(!LoLManager.isInstantiated)
                return;

            var usrData = LoLManager.instance.userData;
            if(!usrData)
                return;

            //clear level stats
            for(int i = 0; i < levels.Length; i++) {
                if(levels[i])
                    levels[i].ResetStatsFrom(usrData);
            }

            //clear organism templates
            usrData.Remove(userDataKeyOrganismTemplateCount);
            usrData.RemoveAllByNameContain(userDataKeyOrganismTemplate);

            ClearOrganismTemplates();
        }

        private void ClearOrganismTemplates() {
            if(mOrganismTemplateList != null) {
                for(int i = 0; i < mOrganismTemplateList.Count; i++) {
                    if(mOrganismTemplateList[i])
                        Destroy(mOrganismTemplateList[i]);
                }

                mOrganismTemplateList = null;
            }
        }
    }
}
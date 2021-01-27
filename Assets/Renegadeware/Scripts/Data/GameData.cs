using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "gameData", menuName = "Game/Game Data", order = 0)]
    public class GameData : M8.SingletonScriptableObject<GameData> {
        public const int organismComponentCapacity = 8;

        [Header("Scene")]
        public M8.SceneAssetPath endScene;

        [Header("Levels")]
        public LevelData[] levels;

        [Header("Cell Components")]
        public OrganismComponent[] organismComponents;

        [Header("Signals")]
        public M8.Signal signalOrganismBodyChanged;
        public M8.SignalInteger signalOrganismComponentEssentialChanged; //component index
        public M8.SignalInteger signalOrganismComponentChanged; //component index

        public bool isGameStarted { get; private set; } //true: we got through start normally, false: debug

        public OrganismTemplate organismTemplateCurrent {
            get {
                if(!mOrganismTemplateCurrent) {
                    var usrData = LoLManager.isInstantiated ? LoLManager.instance.userData : null;

                    if(usrData)
                        mOrganismTemplateCurrent = OrganismTemplate.LoadFrom(usrData, userDataKeyOrganismTemplateCurrent);
                    else {
                        mOrganismTemplateCurrent = OrganismTemplate.CreateEmpty();
                    }
                }

                return mOrganismTemplateCurrent;
            }
        }

        private const string userDataKeyOrganismTemplateCount = "organismCount";
        private const string userDataKeyOrganismTemplate = "organism";
        private const string userDataKeyOrganismTemplateCurrent = "organismCurrent";
        private const string userDataKeyOrganismTemplateIDCounter = "organismIDCounter";

        private OrganismTemplate mOrganismTemplateCurrent; //currently editting organism
        private List<OrganismTemplate> mOrganismTemplateList; //saved organisms made by player

        private int mOrganismTemplateIDCounter = 1;

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
                        curProgress += lvl.GetProgressCount();
                        break;
                    }

                    levelProgressCount += lvl.progressCount;
                }

                isGameStarted = true;
            }

            if(LoLManager.isInstantiated) {
                if(mOrganismTemplateCurrent)
                    mOrganismTemplateCurrent.Reset();

                SaveUserData();

                LoLManager.instance.ApplyProgress(curProgress + 1);
            }

            //load to new scene
            Current();
        }

        public T GetOrganismComponent<T>(int id) where T : OrganismComponent {
            if(id == OrganismTemplate.invalidID)
                return null;

            for(int i = 0; i < organismComponents.Length; i++) {
                var comp = organismComponents[i];
                if(comp && comp.ID == id)
                    return comp as T;
            }

            return null;
        }

        public void AddOrganismTemplateCurrentToList() {
            if(!mOrganismTemplateCurrent)
                return;

            if(mOrganismTemplateList == null)
                mOrganismTemplateList = new List<OrganismTemplate>();

            if(mOrganismTemplateCurrent.ID != OrganismTemplate.invalidID) {
                //check if already in list
                OrganismTemplate template = null;
                for(int i = 0; i < mOrganismTemplateList.Count; i++) {
                    if(mOrganismTemplateList[i].ID == mOrganismTemplateCurrent.ID) {
                        template = mOrganismTemplateList[i];
                        break;
                    }
                }

                if(template)
                    template.CopyFrom(mOrganismTemplateCurrent);
                else
                    mOrganismTemplateList.Add(OrganismTemplate.Clone(mOrganismTemplateCurrent));

                //TODO: invalidate current's ID?
            }
            else {
                //generate ID, then add
                mOrganismTemplateCurrent.ID = mOrganismTemplateIDCounter;

                mOrganismTemplateIDCounter++;

                mOrganismTemplateList.Add(OrganismTemplate.Clone(mOrganismTemplateCurrent));
            }
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

        protected override void OnDestroy() {
            if(mOrganismTemplateCurrent) {
                Destroy(mOrganismTemplateCurrent);
                mOrganismTemplateCurrent = null;
            }

            ClearOrganismTemplates();

            base.OnDestroy();
        }

        private void LoadUserData() {
            if(!isGameStarted)
                return;

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

            //load organism ID Counter
            mOrganismTemplateIDCounter = usrData.GetInt(userDataKeyOrganismTemplateIDCounter, 1);

            //load organism template current
            if(mOrganismTemplateCurrent)
                mOrganismTemplateCurrent.Load(usrData, userDataKeyOrganismTemplateCurrent);
            else
                mOrganismTemplateCurrent = OrganismTemplate.LoadFrom(usrData, userDataKeyOrganismTemplateCurrent);

            //load organism templates
            ClearOrganismTemplates();

            int organismCount = usrData.GetInt(userDataKeyOrganismTemplateCount);

            mOrganismTemplateList = new List<OrganismTemplate>(organismCount);

            for(int i = 0; i < organismCount; i++)
                mOrganismTemplateList.Add(OrganismTemplate.LoadFrom(usrData, userDataKeyOrganismTemplate + i));
        }

        private void SaveUserData() {
            if(!isGameStarted)
                return;

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

            //save organism template ID Counter
            usrData.SetInt(userDataKeyOrganismTemplateIDCounter, mOrganismTemplateIDCounter);

            //save organism template current
            if(mOrganismTemplateCurrent)
                mOrganismTemplateCurrent.SaveTo(usrData, userDataKeyOrganismTemplateCurrent);

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

            //clear organism template ID counter
            mOrganismTemplateIDCounter = 1;
            usrData.Remove(userDataKeyOrganismTemplateIDCounter);

            //clear organism template current
            if(mOrganismTemplateCurrent) {
                mOrganismTemplateCurrent.Reset();

                usrData.RemoveAllByNameContain(userDataKeyOrganismTemplateCurrent);
            }

            //clear organism templates
            ClearOrganismTemplates();

            usrData.Remove(userDataKeyOrganismTemplateCount);
            usrData.RemoveAllByNameContain(userDataKeyOrganismTemplate);
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "gameData", menuName = "Game/Game Data", order = 0)]
    public class GameData : M8.SingletonScriptableObject<GameData> {
        public const int invalidID = 0;

        public const int organismComponentCapacity = 8;

        public const string speechGroupInfo = "info";

        [Header("Levels")]
        public LevelData[] levels;

        [Header("Scene")]
        public M8.SceneAssetPath endScene;

        [Header("Modals")]
        public string modalEnvironmentSelect = "environmentSelect";
        public string modalOrganismEdit = "organismEdit";
        public string modalRetry = "retry";
        public string modalVictory = "victory";

        [Header("Scoring")]
        public int scoreBase = 1000;
        public int scoreBonus = 500;

        [Header("Environment Settings")]
        [M8.TagSelector]
        public string environmentSolidTag;
        public float environmentDepth = 0f;
        public float environmentInputDragScale = 0.03f;

        [Header("Organism Settings")]
        [M8.TagSelector]
        public string organismPlayerTag;

        public string organismPlayerSpawnName = "player";

        public float organismDepth = -0.2f;

        public float organismContactsUpdateDelay = 0.1f;

        public float organismSeparateSpeed = 1f;

        /// <summary>
        /// How far apart can an organism be considered 'sticky' to another organism/solid. If distance between reaches above this threshold, detach.
        /// </summary>
        public float organismStickyDistanceThreshold = 0.25f;

        public float organismStickySpeed = 1f;

        /// <summary>
        /// how long to stay dead once an organism's life expired.
        /// </summary>
        public float organismDeathDelay = 2f;

        /// <summary>
        /// To what angle do we need to keep turning when going towards/against a target
        /// </summary>
        public float organismSeekTurnAngleThreshold = 2f;

        public float organismSeekTurnAngleDampenScale = 4f;

        /// <summary>
        /// To what angle do we need to turn towards/against a target before moving
        /// </summary>
        public float organismSeekAngleThreshold = 30f;

        /// <summary>
        /// Anchor on entity to attach endobiotics
        /// </summary>
        public string organismAnchorEndobiotic = "endobiotic";

        [Header("Organism Filter Settings")]
        [M8.TagSelector]
        public string[] organismEntityTags;

        [Header("Organism Animation Settings")]
        public string organismTakeSpawn = "spawn";
        public string organismTakeReproduce = "reproduce";
        public string organismTakeDeath = "death";

        [Header("Energy Settings")]
        [M8.TagSelector]
        public string energyTag;
        public float energyDepth = -0.15f;

        [Header("Time Settings")]
        public float[] timeScales;

        //[Header("Input Settings")]

        [Header("Text Refs")]
        [M8.Localize]
        public string textPlayLeaveTitleRef;
        [M8.Localize]
        public string textPlayLeaveDescRef;

        [Header("Organism Components")]
        public OrganismComponent[] organismComponents;

        [Header("Signals")]
        public M8.SignalInteger signalEnvironmentChanged;

        public SignalModeSelectChange signalModeSelectChange;

        public M8.Signal signalEnvironmentDragBegin;
        public M8.SignalVector2 signalEnvironmentDrag; //receive delta
        public M8.Signal signalEnvironmentDragEnd;
        public M8.SignalVector2 signalEnvironmentClick;

        public M8.SignalInteger signalEditBodyPreview; //body component id
        public M8.SignalInteger signalEditComponentEssentialPreview; //component essential index, component id
        public SignalIntegerPair signalEditComponentPreview; //component index, component id
        public M8.Signal signalEditRefresh; //used when backing out without changes

        public M8.Signal signalOrganismBodyChanged;
        public M8.SignalInteger signalOrganismComponentEssentialChanged; //component index
        public M8.SignalInteger signalOrganismComponentChanged; //component index

        public M8.SignalInteger signalCameraZoom; //zoom index

        public bool isGameStarted { get; private set; } //true: we got through start normally, false: debug

        public LevelData currentLevelData {
            get {
                var curScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                for(int i = 0; i < levels.Length; i++) {
                    if(levels[i].scene == curScene)
                        return levels[i];
                }

                return null;
            }
        }

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

            set {
                if(value) {
                    if(mOrganismTemplateCurrent != value) {
                        if(mOrganismTemplateCurrent)
                            mOrganismTemplateCurrent.CopyFrom(value);
                        else
                            mOrganismTemplateCurrent = OrganismTemplate.Clone(value);
                    }
                }
                else if(mOrganismTemplateCurrent) {
                    Destroy(mOrganismTemplateCurrent);
                    mOrganismTemplateCurrent = null;
                }
            }
        }

        public ContactFilter2D organismSpawnContactFilter {
            get {
                if(!mOrganismSpawnContactFilterInit) {
                    var min = Mathf.Min(environmentDepth, organismDepth);
                    var max = Mathf.Max(environmentDepth, organismDepth);

                    mOrganismSpawnContactFilter.SetDepth(min, max);
                    mOrganismSpawnContactFilter.useTriggers = false;

                    mOrganismSpawnContactFilterInit = true;
                }

                return mOrganismSpawnContactFilter;
            }
        }

        public ContactFilter2D organismContactFilter {
            get {
                if(!mOrganismContactFilterInit) {
                    var min = Mathf.Min(organismDepth, energyDepth, environmentDepth);
                    var max = Mathf.Max(organismDepth, energyDepth, environmentDepth);

                    mOrganismContactFilter.SetDepth(min, max);
                    mOrganismContactFilter.useTriggers = true;

                    mOrganismContactFilterInit = true;
                }

                return mOrganismContactFilter;
            }
        }

        /// <summary>
        /// Filter for organisms and energy sources
        /// </summary>
        public ContactFilter2D organismSensorContactFilter {
            get {
                if(!mOrganismSensorContactFilterInit) {
                    var min = Mathf.Min(organismDepth, energyDepth);
                    var max = Mathf.Max(organismDepth, energyDepth);

                    mOrganismSensorContactFilter.SetDepth(min, max);
                    mOrganismSensorContactFilter.useTriggers = true;

                    mOrganismSensorContactFilterInit = true;
                }

                return mOrganismSensorContactFilter;
            }
        }

        public ContactFilter2D organismSolidContactFilter {
            get {
                if(!mOrganismSolidContactFilterInit) {
                    mOrganismSolidContactFilter.SetDepth(environmentDepth, environmentDepth);
                    mOrganismSolidContactFilter.useTriggers = false;

                    mOrganismSolidContactFilterInit = true;
                }

                return mOrganismSolidContactFilter;
            }
        }

        public ContactFilter2D organismEntityContactFilter {
            get {
                if(!mOrganismEntityContactFilterInit) {
                    mOrganismEntityContactFilter.SetDepth(organismDepth, organismDepth);
                    mOrganismEntityContactFilter.useTriggers = false;

                    mOrganismEntityContactFilterInit = true;
                }

                return mOrganismEntityContactFilter;
            }
        }

        private const string userDataKeyOrganismTemplateCount = "organismCount";
        private const string userDataKeyOrganismTemplate = "organism";
        private const string userDataKeyOrganismTemplateCurrent = "organismCurrent";
        private const string userDataKeyOrganismTemplateIDCounter = "organismIDCounter";

        private OrganismTemplate mOrganismTemplateCurrent; //currently editting organism
        private List<OrganismTemplate> mOrganismTemplateList; //saved organisms made by player

        private int mOrganismTemplateIDCounter = 1;

        private Dictionary<int, OrganismComponent> mOrganismLookup;

        private ContactFilter2D mOrganismSpawnContactFilter = new ContactFilter2D();
        private bool mOrganismSpawnContactFilterInit = false;

        private ContactFilter2D mOrganismContactFilter = new ContactFilter2D();
        private bool mOrganismContactFilterInit = false;

        private ContactFilter2D mOrganismSensorContactFilter = new ContactFilter2D();
        private bool mOrganismSensorContactFilterInit = false;

        private ContactFilter2D mOrganismSolidContactFilter = new ContactFilter2D();
        private bool mOrganismSolidContactFilterInit = false;

        private ContactFilter2D mOrganismEntityContactFilter = new ContactFilter2D();
        private bool mOrganismEntityContactFilterInit = false;

        public int GetScore(int organismCount, int organismCriteriaCount, int organismBonusCount) {
            int score = 0;

            if(organismCount >= organismCriteriaCount) {
                score = scoreBase;

                int bonusCount = organismCount - organismCriteriaCount;
                float bonusScale = (float)bonusCount / organismBonusCount;

                score += Mathf.FloorToInt(scoreBonus * bonusScale);
            }

            return score;
        }

        /// <summary>
        /// Return index medal based on organism count. -1 if no valid medal.
        /// </summary>
        public int GetMedalIndex(int medalCount, int organismCount, int organismCriteriaCount, int organismBonusCount) {
            if(organismCount >= organismCriteriaCount) {
                if(organismBonusCount > 0)
                    return Mathf.FloorToInt(Mathf.Clamp01((float)(organismCount - organismCriteriaCount) / organismBonusCount) * (medalCount - 1));
                else
                    return 0;
            }

            return -1;
        }

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

        /// <summary>
        /// Apply new progress, call Current afterwards to load scene
        /// </summary>
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

            if(mOrganismTemplateCurrent)
                mOrganismTemplateCurrent.Reset();

            if(isGameStarted) {
                SaveUserData();

                LoLManager.instance.ApplyProgress(curProgress + 1);
            }
        }

        public T GetOrganismComponent<T>(int id) where T : OrganismComponent {
            if(id == invalidID)
                return null;

            OrganismComponent ret;
            mOrganismLookup.TryGetValue(id, out ret);

            return ret as T;
        }

        public void AddOrganismTemplateCurrentToList() {
            if(!mOrganismTemplateCurrent)
                return;

            if(mOrganismTemplateList == null)
                mOrganismTemplateList = new List<OrganismTemplate>();

            if(mOrganismTemplateCurrent.ID != invalidID) {
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

        /// <summary>
        /// Used in editor to check if components need to be refreshed. Returns true if everything checks out.
        /// </summary>
        public bool VerifyOrganismComponents() {
            if(organismComponents == null)
                return false;

            for(int i = 0; i < organismComponents.Length; i++) {
                if(organismComponents[i] == null)
                    return false;
            }

            return true;
        }

        protected override void OnInstanceInit() {
            //editor clear
            organismTemplateCurrent = null;
            ClearOrganismTemplates();

            mOrganismTemplateIDCounter = 1;

            mOrganismSpawnContactFilterInit = false;
            mOrganismContactFilterInit = false;
            mOrganismSensorContactFilterInit = false;
            mOrganismSolidContactFilterInit = false;
            //

            //generate organism component look-up
            mOrganismLookup = new Dictionary<int, OrganismComponent>(organismComponents.Length);
            for(int i = 0; i < organismComponents.Length; i++) {
                var comp = organismComponents[i];

                if(mOrganismLookup.ContainsKey(comp.ID)) {
                    Debug.LogWarning("ID: " + comp.ID + " already exists: " + mOrganismLookup[comp.ID].name);
                    continue;
                }

                mOrganismLookup.Add(comp.ID, comp);
            }

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
            organismTemplateCurrent = null;            

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
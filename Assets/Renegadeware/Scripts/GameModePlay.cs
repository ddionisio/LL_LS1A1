using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class GameModePlay : GameModeController<GameModePlay> {
        public enum TransitionState {
            Shown,
            Hidden,
            Enter,
            Exit
        }

        [Header("Environments")]
        public GameObject environmentRootGO; //should have Environment components and CameraControl here

        [Header("Organism Edit")]
        public GameObject editRootGO; //should include a camera and OrganismEditMode
        
        [Header("Game")]
        public GameObject gameRootGO; //should include OrganismTemplateSpawner
        public GamePlayFlow gameFlow; //use for tutorial, explanations

        [Header("Transition")]
        public AnimatorEnterExit transition; //this is also treated as the root GO of transition

        [Header("Debug")]
        public LevelData debugLevel;

        public bool debugPlay; //set to true to directly go to Play with given env. index and template
        public int debugPlayEnvIndex;
        public OrganismTemplate debugPlayOrganism;
        public int debugPlaySpawnCountOverride = 0; //set to > 0 to override spawnable count
        public bool debugPlayTimeUnlimited = false;
        public bool debugWin = false; //set to true to win right away

        /////////////////////////////

        public LevelData level { get { return mLevelData; } }

        public EnvironmentControl[] environments { get; private set; }
        public int environmentCurrentIndex { get; private set; }

        public ModeSelect modeSelect { get; private set; }

        /////////////////
        //Transition
        public bool transitionIsBusy { get { return mTransitionState == TransitionState.Enter || mTransitionState == TransitionState.Exit; } }

        /////////////////
        //Environment
        public EnvironmentControl environmentCurrentControl { get { return environments[environmentCurrentIndex]; } }

        public LevelData.EnvironmentInfo environmentCurrentInfo { get { return level.environments[environmentCurrentIndex]; } }

        public bool environmentIsDragging { get { return environments[environmentCurrentIndex].isDragging; } }

        public int environmentSpawnableCount { get { return debugPlay && debugPlaySpawnCountOverride > 0 ? debugPlaySpawnCountOverride : environmentCurrentInfo.spawnableCount; } }

        /////////////////
        //Edit
        public OrganismDisplayEdit editDisplay { get { return mOrganismEdit; } }

        /////////////////
        //Game
        public bool gameIsInputEnabled {
            get { return mGameInputEnabled; }
            set {
                if(mGameInputEnabled != value) {
                    mGameInputEnabled = value;

                    if(GameData.isInstantiated) {
                        var gameDat = GameData.instance;

                        if(mGameInputEnabled) {
                            gameDat.signalEnvironmentDrag.callback += OnEnvironmentDrag;
                            gameDat.signalEnvironmentClick.callback += OnEnvironmentClick;
                        }
                        else {
                            gameDat.signalEnvironmentDrag.callback -= OnEnvironmentDrag;
                            gameDat.signalEnvironmentClick.callback -= OnEnvironmentClick;
                        }
                    }

                    if(CameraControl.isInstantiated)
                        CameraControl.instance.inputEnabled = value;

                    if(HUD.isInstantiated)
                        RefreshSpawnPlacementActive();
                }
            }
        }

        public OrganismTemplateSpawner gameSpawner { get { return mOrganismSpawner; } }

        public ModeSelect currentModeSelect { get; private set; }

        public int timeIndex { get { return mGameTimeIndex; } }

        public bool isGameflowAvailable { get { return gameFlow && gameFlow.gameObject.activeSelf; } }

        /////////////////////////////

        private LevelData mLevelData;

        //general
        private M8.GenericParams mModalParms = new M8.GenericParams();

        private ModeSelect mModeSelectNext;

        //edit stuff
        private OrganismDisplayEdit mOrganismEdit;

        //game stuff
        private const float pixelRes = 32f;

        private OrganismTemplateSpawner mOrganismSpawner;
        private bool mGameInputEnabled;

        private Collider2D[] mGameSpawnCheckOverlaps = new Collider2D[8];
        private int mGameSpawnCheckOverlapCount;
        private float mGameSpawnRadius;

        private int mGameTimeIndex;

        private int mGameSpawnCount;

        //transition stuff
        private TransitionState mTransitionState;

        protected override void OnInstanceDeinit() {
            if(GameData.isInstantiated) {
                var gameDat = GameData.instance;

                gameDat.signalEnvironmentChanged.callback -= OnEnvironmentChanged;

                gameDat.signalCameraZoom.callback -= OnCameraZoom;

                gameDat.signalOrganismBodyChanged.callback -= OnOrganismBodyChanged;
                gameDat.signalOrganismComponentEssentialChanged.callback -= OnOrganismComponentEssentialChanged;
                gameDat.signalOrganismComponentChanged.callback -= OnOrganismComponentChanged;
            }

            if(mOrganismSpawner) {
                mOrganismSpawner.spawnCallback -= OnOrganismSpawnerSpawn;
                mOrganismSpawner.releaseCallback -= OnOrganismSpawnerRelease;
            }

            if(HUD.isInstantiated) {
                var hud = HUD.instance;

                hud.modeSelectClickCallback -= OnModeSelect;

                hud.zoomCallback -= OnHUDZoom;
                hud.timePlayCallback -= SetTimeIndex;

                hud.HideAll();
            }

            gameIsInputEnabled = false;

            base.OnInstanceDeinit();
        }

        protected override void OnInstanceInit() {
            base.OnInstanceInit();

            
        }

        void OnApplicationFocus(bool focus) {
            if(!focus) {
                if(HUD.isInstantiated) {
                    HUD.instance.spawnPlacementIsActive = false;
                }
            }
        }

        protected override IEnumerator Start() {
            yield return null;

            var gameDat = GameData.instance;

            if(debugLevel)
                mLevelData = debugLevel;
            else
                mLevelData = gameDat.currentLevelData;

            /////////////////////////////
            //initialize environment
            var envRootTrans = environmentRootGO.transform;
            var envList = new List<EnvironmentControl>();
            for(int i = 0; i < envRootTrans.childCount; i++) {
                var child = envRootTrans.GetChild(i);

                var envCtrl = child.GetComponent<EnvironmentControl>();
                if(envCtrl) {
                    envCtrl.gameObject.SetActive(false);
                    envList.Add(envCtrl);
                }
            }

            environments = envList.ToArray();

            //set current environment
            if(debugPlay)
                environmentCurrentIndex = debugPlayEnvIndex;
            else {
                environmentCurrentIndex = Random.Range(0, level.environments.Length);

                //ensure it is not complete
                for(int i = 0; i < level.environments.Length; i++) {
                    if(!level.IsEnvironmentComplete(environmentCurrentIndex))
                        break;

                    environmentCurrentIndex++;
                    if(environmentCurrentIndex == level.environments.Length)
                        environmentCurrentIndex = 0;
                }
            }

            EnvironmentInitCurrent();

            environmentRootGO.SetActive(true);

            /////////////////////////////
            //initialize edit

            //grab organism edit
            mOrganismEdit = editRootGO.GetComponentInChildren<OrganismDisplayEdit>();

            editRootGO.SetActive(false);

            /////////////////////////////
            //initialize simulation

            //grab organism spawner
            mOrganismSpawner = gameRootGO.GetComponent<OrganismTemplateSpawner>();

            mOrganismSpawner.spawnCallback += OnOrganismSpawnerSpawn;
            mOrganismSpawner.releaseCallback += OnOrganismSpawnerRelease;

            gameRootGO.SetActive(false);

            /////////////////////////////
            //initialize transition
            if(transition) transition.gameObject.SetActive(false);

            mTransitionState = TransitionState.Shown;

            /////////////////////////////
            //initialize signals
            gameDat.signalEnvironmentChanged.callback += OnEnvironmentChanged;

            gameDat.signalCameraZoom.callback += OnCameraZoom;

            gameDat.signalOrganismBodyChanged.callback += OnOrganismBodyChanged;
            gameDat.signalOrganismComponentEssentialChanged.callback += OnOrganismComponentEssentialChanged;
            gameDat.signalOrganismComponentChanged.callback += OnOrganismComponentChanged;

            //Loading
            yield return base.Start();

            if(LoLManager.isInstantiated) {
                while(!LoLManager.instance.isReady)
                    yield return null;
            }

            //hook-up hud interaction
            var hud = HUD.instance;

            hud.modeSelectClickCallback += OnModeSelect;

            hud.timePlayCallback += OnHUDTimeIndexChange;
            hud.zoomCallback += OnHUDZoom;            

            //game flow
            if(debugPlay) {
                GameData.instance.organismTemplateCurrent = debugPlayOrganism;

                ChangeToMode(ModeSelect.Play);
            }
            else
                ChangeToMode(ModeSelect.Environment);
        }

        IEnumerator DoEnvironmentSelect() {
            currentModeSelect = ModeSelect.Environment;

            var hud = HUD.instance;

            //initialize environment
            EnvironmentInitCurrent();

            environmentRootGO.SetActive(true);

            yield return DoTransitionShow();

            //open environment modal
            mModalParms[ModalEnvironmentSelect.parmEnvironmentInfos] = level.environments;
            mModalParms[ModalEnvironmentSelect.parmEnvironmentIndex] = environmentCurrentIndex;

            M8.ModalManager.main.Open(GameData.instance.modalEnvironmentSelect, mModalParms);

            //don't show mode select if no buttons available
            if(hud.modeSelectFlags != ModeSelectFlags.None)
                hud.ElementShow(HUD.Element.ModeSelect);

            if(isGameflowAvailable)
                yield return gameFlow.EnvironmentStart();

            //wait for mode select
            mModeSelectNext = ModeSelect.None;
            while(mModeSelectNext == ModeSelect.None)
                yield return null;

            GameData.instance.signalModeSelectChange.Invoke(ModeSelect.Environment, mModeSelectNext);

            //hide environment if we are editing
            if(mModeSelectNext == ModeSelect.Edit) {
                StartCoroutine(DoTransitionHide());
            }

            //hide hud
            if(hud.modeSelectFlags != ModeSelectFlags.None)
                hud.ElementHide(HUD.Element.ModeSelect);

            //close environment modal
            M8.ModalManager.main.CloseUpTo(GameData.instance.modalEnvironmentSelect, true);

            //wait for transitions
            while(M8.ModalManager.main.isBusy || hud.isBusy || transitionIsBusy)
                yield return null;

            if(mModeSelectNext == ModeSelect.Edit)
                environmentRootGO.SetActive(false);

            ChangeToMode(mModeSelectNext);
        }

        IEnumerator DoEnvironmentChange(int toEnvInd) {
            var hud = HUD.instance;

            hud.modeSelectInteractive = false;

            yield return DoTransitionHide();

            EnvironmentDenitCurrent();

            environmentCurrentIndex = toEnvInd;

            EnvironmentInitCurrent();

            //show/hide hud?
            var modeSelectFlags = HUDGetModeSelectFlags();

            if(modeSelectFlags == ModeSelectFlags.None) {
                if(hud.ElementIsVisible(HUD.Element.ModeSelect))
                    hud.ElementHide(HUD.Element.ModeSelect);
            }
            else if(!hud.ElementIsVisible(HUD.Element.ModeSelect)) {
                hud.ModeSelectSetVisible(modeSelectFlags);
                hud.ElementShow(HUD.Element.ModeSelect);
            }

            yield return DoTransitionShow();

            while(hud.isBusy)
                yield return null;

            hud.modeSelectInteractive = true;
        }

        IEnumerator DoOrganismEdit() {
            currentModeSelect = ModeSelect.Edit;

            var gameDat = GameData.instance;

            editRootGO.SetActive(true);

            //setup organism edit display
            mOrganismEdit.Setup(gameDat.organismTemplateCurrent);

            yield return DoTransitionShow();

            //open organism edit modal
            mModalParms[ModalOrganismEditor.parmTitleRef] = level.titleRef;
            mModalParms[ModalOrganismEditor.parmOrganismBodyGroup] = level.organismBodyGroup;
            mModalParms[ModalOrganismEditor.parmOrganismTemplate] = gameDat.organismTemplateCurrent;

            M8.ModalManager.main.Open(gameDat.modalOrganismEdit, mModalParms);

            HUD.instance.ElementShow(HUD.Element.ModeSelect);

            if(isGameflowAvailable)
                yield return gameFlow.EditStart();

            //wait for mode select
            mModeSelectNext = ModeSelect.None;
            while(mModeSelectNext == ModeSelect.None)
                yield return null;

            //clear body if essentials are not filled
            if(!gameDat.organismTemplateCurrent.isEssentialComponentsFilled)
                gameDat.organismTemplateCurrent.body = null;

            gameDat.signalModeSelectChange.Invoke(ModeSelect.Edit, mModeSelectNext);

            //hide organism edit
            StartCoroutine(DoTransitionHide());

            //hide hud
            HUD.instance.ElementHide(HUD.Element.ModeSelect);

            //close organism edit modal
            M8.ModalManager.main.CloseUpTo(gameDat.modalOrganismEdit, true);

            //wait for transitions
            while(M8.ModalManager.main.isBusy || HUD.instance.isBusy || transitionIsBusy)
                yield return null;

            editRootGO.SetActive(false);

            ChangeToMode(mModeSelectNext);
        }

        IEnumerator DoSimulation(bool isRestart) {
            currentModeSelect = ModeSelect.Play;

            var gameDat = GameData.instance;

            var hud = HUD.instance;

            var camCtrl = CameraControl.instance;

            var envInfo = environmentCurrentInfo;

            //initialize game states
            SetTimeIndex(1);

            mGameSpawnCount = environmentSpawnableCount;

            //start up entities
            if(!isRestart) {
                mOrganismSpawner.Setup(gameDat.organismTemplateCurrent, gameDat.organismPlayerSpawnName, gameDat.organismPlayerTag, envInfo.capacity);

                var organismSize = mOrganismSpawner.template.size;
                mGameSpawnRadius = Mathf.Max(organismSize.x, organismSize.y) * 0.5f;
            }

            environmentRootGO.SetActive(true);

            gameRootGO.SetActive(true);

            yield return DoTransitionShow();

            //setup hud
            RefreshSpawnPlacementSize();

            hud.SpawnPlacementSetCount(mGameSpawnCount);

            hud.TimePlaySetIndex(mGameTimeIndex);
            hud.TimeUpdate(0f, level.duration);

            hud.ZoomSetup(camCtrl.zoomIndex, camCtrl.zoomLevels);

            hud.OrganismProgressApply(0, envInfo.criteriaCount, envInfo.bonusCount);

            hud.ResetMedalCounter();

            hud.ElementShow(HUD.Element.Gameplay);
            hud.ElementShow(HUD.Element.ModeSelect);

            while(hud.isBusy)
                yield return null;

            if(isGameflowAvailable)
                yield return gameFlow.GameStart();

            gameIsInputEnabled = true;
                        
            var gameStartTime = Time.time;

            var isTimeExpired = false;

            mModeSelectNext = ModeSelect.None;
            while(mModeSelectNext == ModeSelect.None) {
                //update spawn placement display
                if(hud.spawnPlacementIsActive) {
                    bool isValid = false;

                    var mousePos = Input.mousePosition;
                    if(mousePos.x >= 0f && mousePos.x < Screen.width && mousePos.y >= 0f && mousePos.y < Screen.height) {
                        Vector2 pos = CameraControl.instance.cameraSource.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -CameraControl.instance.cameraSource.transform.position.z));

                        //ensure there are no overlaps
                        mGameSpawnCheckOverlapCount = Physics2D.OverlapCircle(pos, mGameSpawnRadius, gameDat.organismSpawnContactFilter, mGameSpawnCheckOverlaps);

                        //Debug.Log("pos: " + pos);

                        isValid = mGameSpawnCheckOverlapCount == 0;
                    }

                    hud.spawnPlacementPointer.position = new Vector3(Mathf.Clamp(mousePos.x, 0f, Screen.width), Mathf.Clamp(mousePos.y, 0f, Screen.height), 0f);
                    hud.spawnPlacementIsValid = isValid;
                }

                //refresh time
                var curGameTime = Time.time - gameStartTime;

                hud.TimeUpdate(curGameTime, level.duration);

                if(!(debugPlay && debugPlayTimeUnlimited) && curGameTime >= level.duration) {
                    isTimeExpired = true;
                    break;
                }

                if(debugWin) {
                    isTimeExpired = true;
                    break;
                }

                //some simulation update
                //if(M8.Util.CheckTag(gameObject, GameData.instance.inputSpawnTagFilter))
                yield return null;
            }

            //hide hud
            HUD.instance.ElementHide(HUD.Element.Gameplay);
            HUD.instance.ElementHide(HUD.Element.ModeSelect);

            gameIsInputEnabled = false;

            //revert time
            SetTimeIndex(1);

            //determine next mode if time expired
            if(isTimeExpired) {
                int entityCount;

                if(debugWin) {
                    entityCount = environmentCurrentInfo.criteriaCount;
                    debugWin = false;
                }
                else
                    entityCount = mOrganismSpawner.entityCount;

                //met criteria?
                if(entityCount >= environmentCurrentInfo.criteriaCount) { //victory
                    //save progress
                    level.ApplyStats(environmentCurrentIndex, gameDat.organismTemplateCurrent.ID, entityCount);
                    gameDat.Progress();

                    //setup stats for victory display
                    mModalParms[ModalVictory.parmCount] = entityCount;
                    mModalParms[ModalVictory.parmCriteriaCount] = environmentCurrentInfo.criteriaCount;
                    mModalParms[ModalVictory.parmBonusCount] = environmentCurrentInfo.bonusCount;

                    M8.ModalManager.main.Open(gameDat.modalVictory, mModalParms);

                    while(M8.ModalManager.main.isBusy || M8.ModalManager.main.IsInStack(gameDat.modalVictory))
                        yield return null;

                    //check if we are ready to move on
                    if(level.IsComplete())
                        mModeSelectNext = ModeSelect.NextLevel;
                    else {
                        mModeSelectNext = ModeSelect.Environment;
                    }

                    if(isGameflowAvailable)
                        yield return gameFlow.Victory();
                }
                else { //retry
                    mModeSelectNext = ModeSelect.None;

                    mModalParms[ModalRetry.parmCurCount] = entityCount;
                    mModalParms[ModalRetry.parmCount] = environmentCurrentInfo.criteriaCount;
                    mModalParms[ModalRetry.parmHintTextRef] = environmentCurrentInfo.hintRef;
                    mModalParms[ModalRetry.parmCallback] = (System.Action<ModeSelect>)OnModeSelectRetry;

                    M8.ModalManager.main.Open(gameDat.modalRetry, mModalParms);

                    while(mModeSelectNext == ModeSelect.None)
                        yield return null;
                }
            }

            //purge player entities
            if(mModeSelectNext == ModeSelect.Retry)
                mOrganismSpawner.Clear();
            else
                mOrganismSpawner.Destroy();

            gameRootGO.SetActive(false);

            GameData.instance.signalModeSelectChange.Invoke(ModeSelect.Play, mModeSelectNext);

            //hide environment if we are editing
            if(mModeSelectNext == ModeSelect.Edit)
                StartCoroutine(DoTransitionHide());

            //wait for transitions
            while(M8.ModalManager.main.isBusy || HUD.instance.isBusy || transitionIsBusy)
                yield return null;

            if(mTransitionState == TransitionState.Hidden)
                environmentRootGO.SetActive(false);

            ChangeToMode(mModeSelectNext);
        }

        IEnumerator DoTransitionShow() {
            if(mTransitionState == TransitionState.Shown || mTransitionState == TransitionState.Enter)
                yield break;

            if(transition) {
                mTransitionState = TransitionState.Enter;

                yield return transition.PlayEnterWait();

                transition.gameObject.SetActive(false);
            }

            mTransitionState = TransitionState.Shown;
        }

        IEnumerator DoTransitionHide() {
            if(mTransitionState == TransitionState.Hidden || mTransitionState == TransitionState.Exit)
                yield break;

            if(transition) {
                transition.gameObject.SetActive(true);

                mTransitionState = TransitionState.Exit;

                yield return transition.PlayExitWait();
            }

            mTransitionState = TransitionState.Hidden;
        }

        void OnModeSelectRetry(ModeSelect toMode) {
            mModeSelectNext = toMode;
        }

        void OnModeSelect(ModeSelect toMode) {
            //confirm
            if(modeSelect == ModeSelect.Play && mOrganismSpawner.entityCount > 0) {
                ModalConfirm.Open(
                    GameData.instance.textPlayLeaveTitleRef, GameData.instance.textPlayLeaveDescRef, 
                    confirm => {
                        if(confirm)
                            mModeSelectNext = toMode;
                    });
            }
            else
                mModeSelectNext = toMode;
        }

        void OnHUDTimeIndexChange(int timeIndex) {
            var hud = HUD.instance;

            //if(hud.spawnPlacementIsActive)
                //hud.spawnPlacementIsActive = false;

            SetTimeIndex(timeIndex);
        }

        void OnHUDZoom(int zoomIndex) {
            CameraControl.instance.ZoomTo(zoomIndex);
        }

        void OnCameraZoom(int zoomIndex) {
            var hud = HUD.instance;

            hud.ZoomApply(zoomIndex);

            RefreshSpawnPlacementSize();
        }

        void OnEnvironmentChanged(int envInd) {
            StartCoroutine(DoEnvironmentChange(envInd));
        }

        void OnEnvironmentDrag(Vector2 delta) {
            var camCtrl = CameraControl.instance;

            /*Vector2 pos;
            if(camCtrl.isMoving)
                pos = camCtrl.positionMoveTo;
            else
                pos = camCtrl.position;*/

            camCtrl.position -= delta * GameData.instance.environmentInputDragScale;
        }

        void OnEnvironmentClick(Vector2 pos) {
            var hud = HUD.instance;
            if(hud.spawnPlacementIsActive) {
                var screenPos = CameraControl.instance.cameraSource.WorldToScreenPoint(pos);
                hud.spawnPlacementPointer.position = new Vector3(Mathf.Clamp(screenPos.x, 0f, Screen.width), Mathf.Clamp(screenPos.y, 0f, Screen.height), 0f);

                //ensure there are no overlaps
                if(mGameSpawnCheckOverlapCount == 0) {
                    OrganismEntity newEnt;

                    if(level.spawnIsRandomDir)
                        newEnt = mOrganismSpawner.SpawnAtRandomDir(pos);
                    else
                        newEnt = mOrganismSpawner.SpawnAt(pos);

                    if(newEnt && mGameSpawnCount > 0) {
                        mGameSpawnCount--;
                        HUD.instance.SpawnPlacementSetCount(mGameSpawnCount);

                        RefreshSpawnPlacementActive();

                        if(!string.IsNullOrEmpty(hud.sfxSpawnPlacement))
                            M8.SoundPlaylist.instance.Play(hud.sfxSpawnPlacement, false);
                    }
                }
                else {
                    if(!string.IsNullOrEmpty(hud.sfxSpawnPlacement))
                        M8.SoundPlaylist.instance.Play(hud.sfxSpawnPlacementInvalid, false);
                }
            }
            else
                RefreshSpawnPlacementActive();
        }

        void OnOrganismBodyChanged() {
            HUD.instance.ModeSelectSetVisible(HUDGetModeSelectFlags());
        }

        void OnOrganismComponentEssentialChanged(int ind) {
            HUD.instance.ModeSelectSetVisible(HUDGetModeSelectFlags());
        }

        void OnOrganismComponentChanged(int ind) {
            HUD.instance.ModeSelectSetVisible(HUDGetModeSelectFlags());
        }

        void OnOrganismSpawnerSpawn(OrganismEntity ent) {
            var envInfo = environmentCurrentInfo;

            HUD.instance.OrganismProgressApply(mOrganismSpawner.entityCount, envInfo.criteriaCount, envInfo.bonusCount);

            RefreshSpawnPlacementActive();
        }

        void OnOrganismSpawnerRelease(OrganismEntity ent) {
            var envInfo = environmentCurrentInfo;

            HUD.instance.OrganismProgressApply(mOrganismSpawner.entityCount, envInfo.criteriaCount, envInfo.bonusCount);

            //reset spawnable if there are no player entities left
            if(mOrganismSpawner.entityCount == 0) {
                mGameSpawnCount = environmentSpawnableCount;
                HUD.instance.SpawnPlacementSetCount(mGameSpawnCount);
            }

            RefreshSpawnPlacementActive();
        }

        private void RefreshSpawnPlacementActive() {
            if(mGameInputEnabled && mGameSpawnCount > 0) {
                HUD.instance.spawnPlacementIsActive = true;

                //if(mGameTimeIndex > 1) {
                //SetTimeIndex(1);
                //HUD.instance.TimePlaySetIndex(mGameTimeIndex);
                //}
            }
            else {
                HUD.instance.spawnPlacementIsActive = false;
            }
        }

        private void SetTimeIndex(int timeIndex) {
            var gameDat = GameData.instance;

            mGameTimeIndex = Mathf.Clamp(timeIndex, 0, gameDat.timeScales.Length - 1);

            M8.SceneManager.instance.timeScale = gameDat.timeScales[mGameTimeIndex];
        }

        private ModeSelectFlags HUDGetModeSelectFlags() {
            //check if current organism template is valid
            var organismTemplate = GameData.instance.organismTemplateCurrent;
            var organismTemplateValid = organismTemplate.isValid;

            ModeSelectFlags modeFlags = ModeSelectFlags.None;

            switch(modeSelect) {
                case ModeSelect.Environment:
                    //check if environment is already completed. if it is, then no mode change is shown
                    if(!level.IsEnvironmentComplete(environmentCurrentIndex)) {
                        if(organismTemplateValid)
                            modeFlags |= ModeSelectFlags.Play;

                        modeFlags |= ModeSelectFlags.Edit;
                    }
                    break;
                case ModeSelect.Edit:
                    if(organismTemplateValid)
                        modeFlags |= ModeSelectFlags.Play;

                    modeFlags |= ModeSelectFlags.Environment;
                    break;
                case ModeSelect.Play:
                case ModeSelect.Retry:
                    modeFlags = ModeSelectFlags.Environment | ModeSelectFlags.Edit;
                    break;
            }

            return modeFlags;
        }

        private void ChangeToMode(ModeSelect toMode) {
            modeSelect = toMode;

            HUD.instance.ModeSelectSetVisible(HUDGetModeSelectFlags());

            switch(toMode) {
                case ModeSelect.Environment:
                    StartCoroutine(DoEnvironmentSelect());
                    break;
                case ModeSelect.Edit:
                    StartCoroutine(DoOrganismEdit());
                    break;
                case ModeSelect.Play:
                    StartCoroutine(DoSimulation(false));
                    break;
                case ModeSelect.Retry:
                    modeSelect = ModeSelect.Play;
                    StartCoroutine(DoSimulation(true));
                    break;
                case ModeSelect.NextLevel:                    
                    GameData.instance.Current(); //this should load the next scene since this level's progress is fully complete
                    break;
            }
        }

        private void EnvironmentInitCurrent() {
            var env = environments[environmentCurrentIndex];
            if(env.isActive) //already initialized
                return;

            //init stuff
            env.ApplyToCamera(CameraControl.instance);
            env.isActive = true;
        }

        private void EnvironmentDenitCurrent() {
            var env = environments[environmentCurrentIndex];
            if(!env.isActive) //already deinitialized
                return;

            env.isActive = false;
        }

        private void RefreshSpawnPlacementSize() {
            var camCtrl = CameraControl.instance;

            var cursorSize = (mGameSpawnRadius * pixelRes + 4f) * camCtrl.zoomScale;
            HUD.instance.spawnPlacementPointer.sizeDelta = new Vector2(cursorSize, cursorSize);
        }
    }
}
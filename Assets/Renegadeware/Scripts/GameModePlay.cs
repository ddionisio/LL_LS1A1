using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class GameModePlay : GameModeController<GameModePlay> {
        public enum TransitionState {
            Shown,
            Hidden,
            Enter,
            Exit
        }

        [Header("Level Data")]
        public LevelData level;

        [Header("Environments")]
        public GameObject environmentRootGO; //should have Environment components and CameraControl here

        [Header("Organism Edit")]
        public GameObject editRootGO; //should include a camera and OrganismEditMode
        
        [Header("Game")]
        public GameObject gameRootGO;

        [Header("Transition")]
        public AnimatorEnterExit transition; //this is also treated as the root GO of transition

        [Header("Debug")]
        public bool debugPlay; //set to true to directly go to Play with given env. index and template
        public int debugPlayEnvIndex;
        public OrganismTemplate debugPlayOrganism;

        /////////////////////////////

        public EnvironmentControl[] environments { get; private set; }
        public int environmentCurrentIndex { get; private set; }

        public CameraControl environmentCamera { get; private set; }

        public ModeSelect modeSelect { get; private set; }

        //Transition
        public bool isTransitioning { get { return mTransitionState == TransitionState.Enter || mTransitionState == TransitionState.Exit; } }

        /////////////////////////////

        //general
        private M8.GenericParams mModalParms = new M8.GenericParams();

        private ModeSelect mModeSelectNext;

        //edit stuff
        private OrganismDisplayEdit mOrganismEdit;

        private TransitionState mTransitionState;

        protected override void OnInstanceDeinit() {            
            if(GameData.isInstantiated) {
                var gameDat = GameData.instance;

                gameDat.signalEnvironmentChanged.callback -= OnEnvironmentChanged;

                gameDat.signalOrganismBodyChanged.callback -= OnOrganismBodyChanged;
                gameDat.signalOrganismComponentEssentialChanged.callback -= OnOrganismComponentEssentialChanged;
                gameDat.signalOrganismComponentChanged.callback -= OnOrganismComponentChanged;
            }

            if(HUD.isInstantiated) {
                var hud = HUD.instance;

                hud.modeSelectClickCallback -= OnModeSelect;

                hud.HideAll();
            }

            base.OnInstanceDeinit();
        }

        protected override void OnInstanceInit() {
            base.OnInstanceInit();

            var gameDat = GameData.instance;

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
                else if(!environmentCamera) {
                    environmentCamera = child.GetComponent<CameraControl>();
                }
            }

            environments = envList.ToArray();

            //set current environment
            environmentCurrentIndex = debugPlay ? debugPlayEnvIndex : 0;
            EnvironmentInitCurrent();

            environmentRootGO.SetActive(true);

            /////////////////////////////
            //initialize edit

            //grab organism edit
            mOrganismEdit = editRootGO.GetComponentInChildren<OrganismDisplayEdit>();

            editRootGO.SetActive(false);

            /////////////////////////////
            //initialize simulation

            gameRootGO.SetActive(false);

            /////////////////////////////
            //initialize transition
            if(transition) transition.gameObject.SetActive(false);

            mTransitionState = TransitionState.Shown;

            /////////////////////////////
            //initialize signals
            gameDat.signalEnvironmentChanged.callback += OnEnvironmentChanged;

            gameDat.signalOrganismBodyChanged.callback += OnOrganismBodyChanged;
            gameDat.signalOrganismComponentEssentialChanged.callback += OnOrganismComponentEssentialChanged;
            gameDat.signalOrganismComponentChanged.callback += OnOrganismComponentChanged;
        }

        protected override IEnumerator Start() {
            //Loading
            yield return base.Start();

            //hook-up hud interaction
            HUD.instance.modeSelectClickCallback += OnModeSelect;

            //game flow
            if(debugPlay) {
                GameData.instance.organismTemplateCurrent = debugPlayOrganism;

                ChangeToMode(ModeSelect.Play);
            }
            else
                ChangeToMode(ModeSelect.Environment);
        }

        IEnumerator DoEnvironmentSelect() {
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

            //wait for mode select
            mModeSelectNext = ModeSelect.None;
            while(mModeSelectNext == ModeSelect.None)
                yield return null;

            //hide environment if we are editing
            if(mModeSelectNext == ModeSelect.Edit)
                StartCoroutine(DoTransitionHide());

            //hide hud
            if(hud.modeSelectFlags != ModeSelectFlags.None)
                hud.ElementHide(HUD.Element.ModeSelect);

            //close environment modal
            M8.ModalManager.main.CloseUpTo(GameData.instance.modalEnvironmentSelect, true);

            //wait for transitions
            while(M8.ModalManager.main.isBusy || hud.isBusy || isTransitioning)
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
            if(modeSelectFlags != ModeSelectFlags.None) {
                if(hud.ElementIsVisible(HUD.Element.ModeSelect))
                    hud.ElementHide(HUD.Element.ModeSelect);
            }
            else if(!hud.ElementIsVisible(HUD.Element.ModeSelect))
                hud.ElementShow(HUD.Element.ModeSelect);

            yield return DoTransitionShow();

            while(hud.isBusy)
                yield return null;

            hud.modeSelectInteractive = true;
        }

        IEnumerator DoOrganismEdit() {
            editRootGO.SetActive(true);

            //setup organism edit display
            mOrganismEdit.Setup(GameData.instance.organismTemplateCurrent);

            yield return DoTransitionShow();

            //open organism edit modal
            mModalParms[ModalOrganismEditor.parmOrganismBodyGroup] = level.organismBodyGroup;
            mModalParms[ModalOrganismEditor.parmOrganismTemplate] = GameData.instance.organismTemplateCurrent;

            M8.ModalManager.main.Open(GameData.instance.modalOrganismEdit, mModalParms);

            HUD.instance.ElementShow(HUD.Element.ModeSelect);

            //wait for mode select
            mModeSelectNext = ModeSelect.None;
            while(mModeSelectNext == ModeSelect.None)
                yield return null;

            //hide organism edit
            StartCoroutine(DoTransitionHide());

            //hide hud
            HUD.instance.ElementHide(HUD.Element.ModeSelect);

            //close organism edit modal
            M8.ModalManager.main.CloseUpTo(GameData.instance.modalOrganismEdit, true);

            //wait for transitions
            while(M8.ModalManager.main.isBusy || HUD.instance.isBusy || isTransitioning)
                yield return null;

            editRootGO.SetActive(false);

            ChangeToMode(mModeSelectNext);
        }

        IEnumerator DoSimulation() {
            //start up entities

            //

            environmentRootGO.SetActive(true);

            gameRootGO.SetActive(true);

            yield return DoTransitionShow();

            //setup hud

            mModeSelectNext = ModeSelect.None;

            yield return null;

            //clear out entities
            

            //hide environment if we are editing
            if(mModeSelectNext == ModeSelect.Edit)
                StartCoroutine(DoTransitionHide());

            //wait for transitions
            while(M8.ModalManager.main.isBusy || HUD.instance.isBusy || isTransitioning)
                yield return null;

            if(mModeSelectNext == ModeSelect.Edit)
                environmentRootGO.SetActive(false);

            gameRootGO.SetActive(false);

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

        void OnModeSelect(ModeSelect toMode) {
            mModeSelectNext = toMode;
        }

        void OnEnvironmentChanged(int envInd) {
            StartCoroutine(DoEnvironmentChange(envInd));
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

        private ModeSelectFlags HUDGetModeSelectFlags() {
            var hud = HUD.instance;

            //check if current organism template is valid
            var organismTemplate = GameData.instance.organismTemplateCurrent;
            var organismTemplateValid = organismTemplate.isEssentialComponentsFilled;

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
                    StartCoroutine(DoSimulation());
                    break;
            }
        }

        private void EnvironmentInitCurrent() {
            var env = environments[environmentCurrentIndex];
            if(env.isActive) //already initialized
                return;

            //init stuff
            env.ApplyBoundsToCamera(environmentCamera);
            env.isActive = true;
        }

        private void EnvironmentDenitCurrent() {
            var env = environments[environmentCurrentIndex];
            if(!env.isActive) //already deinitialized
                return;

            env.isActive = false;
        }
    }
}
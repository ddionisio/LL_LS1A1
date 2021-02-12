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

        [System.Serializable]
        public class Environment {
            public GameObject rootGO;
            public GameBounds2D bounds;

            public bool isActive {
                get { return rootGO ? rootGO.activeSelf : false; }
                set { if(rootGO) rootGO.SetActive(value); }
            }

            public void ApplyBoundsToCamera(GameCamera cam) {
                var boundRect = bounds.rect;

                cam.SetBounds(boundRect, false);
                cam.SetPosition(boundRect.center);
            }
        }

        [Header("Level Data")]
        public LevelData level;

        [Header("Environments")]
        public GameObject environmentRootGO; //should have Environment components here
        public Environment[] environments; //corrolates to level.environments

        [Header("Organism Edit")]
        public GameObject editRootGO; //should include a camera and OrganismEditMode

        [Header("Game Camera")]
        public GameCamera cameraControl;
        public float cameraMoveSpeed = 1f;
        public float cameraMoveSmoothDelay = 0.05f;

        [Header("Game")]
        public GameObject gameRootGO;

        [Header("Transition")]
        public AnimatorEnterExit transition; //this is also treated as the root GO of transition

        /////////////////////////////

        public int environmentCurrentIndex { get; private set; }
        public ModeSelect modeSelect { get; private set; }

        //Transition
        public bool isTransitioning { get { return mTransitionState == TransitionState.Enter || mTransitionState == TransitionState.Exit; } }

        /////////////////////////////

        //general
        private M8.GenericParams mModalParms = new M8.GenericParams();

        private ModeSelect mModeSelectNext;

        //edit stuff
        private OrganismDisplayEdit mOrganismEdit;

        private TransitionState mTransitionState = TransitionState.Shown;

        protected override void OnInstanceDeinit() {            
            if(GameData.isInstantiated) {
                var gameDat = GameData.instance;

                gameDat.signalEnvironmentChanged.callback -= OnEnvironmentChanged;
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
            for(int i = 0; i < environments.Length; i++)
                environments[i].isActive = false;

            environmentCurrentIndex = 0;
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

            /////////////////////////////
            //initialize signals
            gameDat.signalEnvironmentChanged.callback += OnEnvironmentChanged;
        }

        protected override IEnumerator Start() {
            //Loading
            yield return base.Start();

            //hook-up hud interaction
            HUD.instance.modeSelectClickCallback += OnModeSelect;

            //game flow
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
            //setup organism edit display

            editRootGO.SetActive(true);

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

                mTransitionState = TransitionState.Enter;

                yield return transition.PlayExitWait();
            }

            mTransitionState = TransitionState.Shown;
        }

        void OnModeSelect(ModeSelect toMode) {
            mModeSelectNext = toMode;
        }

        void OnEnvironmentChanged(int envInd) {
            StartCoroutine(DoEnvironmentChange(envInd));
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
            env.ApplyBoundsToCamera(cameraControl);

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
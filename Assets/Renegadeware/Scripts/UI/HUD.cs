using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class HUD : M8.SingletonBehaviour<HUD> {
        public enum Element {
            ModeSelect,
            Gameplay
        }

        [Header("Mode")]
        public GameObject modeSelectRootGO;
        public CanvasGroup modeSelectCanvasGroup; //use to enable/disable mode select
        public AnimatorEnterExit modeSelectTransition;
        public Button[] modeSelectButtons; //corresponds to ModeSelect

        [Header("Gameplay")]
        public GameObject gameplayRootGO;
        public AnimatorEnterExit gameplayTransition;

        public bool isBusy { 
            get {
                return (modeSelectTransition && modeSelectTransition.isPlaying) || (gameplayTransition && gameplayTransition.isPlaying);
            } 
        }

        public bool modeSelectInteractive { 
            get { return mModeSelectInteractive; } 
            set { 
                mModeSelectInteractive = value;
                ApplyModeSelectInteractive();
            } 
        }

        public ModeSelectFlags modeSelectFlags { get; private set; }

        public event System.Action<ModeSelect> modeSelectClickCallback;

        private bool mModeSelectInteractive = true;

        public bool ElementIsVisible(Element elem) {
            switch(elem) {
                case Element.ModeSelect:
                    return modeSelectRootGO && modeSelectRootGO.activeSelf;
                case Element.Gameplay:
                    return gameplayRootGO && gameplayRootGO.activeSelf;
                default:
                    return false;
            }
        }

        public void ElementShow(Element elem) {
            StartCoroutine(DoTransitionEnter(elem));
        }

        public void ElementHide(Element elem) {
            StartCoroutine(DoTransitionExit(elem));
        }

        public void ModeSelectSetVisible(ModeSelectFlags flags) {
            modeSelectFlags = flags;
            ApplyModeSelectVisible();
        }

        public void HideAll() {
            StopAllCoroutines();

            if(modeSelectRootGO) modeSelectRootGO.SetActive(false);
            if(modeSelectTransition) modeSelectTransition.Stop();

            if(gameplayRootGO) gameplayRootGO.SetActive(false);
            if(gameplayTransition) gameplayTransition.Stop();

            modeSelectFlags = ModeSelectFlags.None;
            ApplyModeSelectVisible();
        }

        void Awake() {
            HideAll();

            if(modeSelectCanvasGroup)
                mModeSelectInteractive = modeSelectCanvasGroup.interactable;

            //hook up calls
            for(int i = 0; i < modeSelectButtons.Length; i++) {
                var mode = (ModeSelect)i;
                modeSelectButtons[i].onClick.AddListener(delegate () { modeSelectClickCallback.Invoke(mode); });
            }
        }

        IEnumerator DoTransitionEnter(Element elem) {
            switch(elem) {
                case Element.ModeSelect:
                    if(modeSelectRootGO) modeSelectRootGO.SetActive(true);

                    if(modeSelectTransition) {
                        modeSelectTransition.PlayEnter();
                        
                        ApplyModeSelectInteractive();

                        while(modeSelectTransition.isPlaying)
                            yield return null;

                        ApplyModeSelectInteractive();
                    }
                    break;

                case Element.Gameplay:
                    if(gameplayRootGO) gameplayRootGO.SetActive(true);

                    gameplayTransition.PlayEnter();
                    break;
            }
        }

        IEnumerator DoTransitionExit(Element elem) {
            switch(elem) {
                case Element.ModeSelect:
                    if(modeSelectTransition) {
                        modeSelectTransition.PlayExit();

                        ApplyModeSelectInteractive();

                        while(modeSelectTransition.isPlaying)
                            yield return null;
                    }

                    if(modeSelectRootGO) modeSelectRootGO.SetActive(false);
                    break;

                case Element.Gameplay:
                    if(gameplayTransition)
                        yield return gameplayTransition.PlayExitWait();

                    if(gameplayRootGO) gameplayRootGO.SetActive(false);
                    break;
            }
        }

        private void ApplyModeSelectVisible() {
            for(int i = 0; i < modeSelectButtons.Length; i++) {
                var go = modeSelectButtons[i].gameObject;

                var mode = (ModeSelect)i;
                var flag = ModeSelectFlags.None;

                switch(mode) {
                    case ModeSelect.Environment:
                        flag = ModeSelectFlags.Environment;
                        break;
                    case ModeSelect.Edit:
                        flag = ModeSelectFlags.Edit;
                        break;
                    case ModeSelect.Play:
                        flag = ModeSelectFlags.Play;
                        break;
                }

                go.SetActive((modeSelectFlags & flag) == flag);
            }
        }

        private void ApplyModeSelectInteractive() {
            if(modeSelectCanvasGroup)
                modeSelectCanvasGroup.interactable = mModeSelectInteractive && !(modeSelectTransition && modeSelectTransition.isPlaying);
        }
    }
}
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

        public struct TransitionQueue {
            public Element elem;
            public bool isEnter;
        }

        [Header("Mode")]
        public GameObject modeSelectRootGO;
        public GameObject modeSelectDisableGO; //active when mode select is disabled
        public AnimatorEnterExit modeSelectTransition;
        public Button[] modeSelectButtons; //corresponds to GameMode

        [Header("Gameplay")]
        public GameObject gameplayRootGO;
        public AnimatorEnterExit gameplayTransition;

        public bool isBusy { get { return mRout != null; } }

        public bool modeSelectInteractive { get { return mModeSelectInteractive; } set { mModeSelectInteractive = value; ApplyModeSelectInteractive(); } }

        public ModeSelectFlags modeSelectFlags { get; private set; }

        public event System.Action<ModeSelect> modeSelectClickCallback;

        private M8.CacheList<TransitionQueue> mTransitionQueue = new M8.CacheList<TransitionQueue>(4);

        private bool mModeSelectInteractive = true;
        
        private Coroutine mRout;        

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
            mTransitionQueue.Add(new TransitionQueue { elem = elem, isEnter = true });

            if(mRout == null) {
                mRout = StartCoroutine(DoTransitions());
                ApplyModeSelectInteractive();
            }
        }

        public void ElementHide(Element elem) {
            mTransitionQueue.Add(new TransitionQueue { elem = elem, isEnter = false });

            if(mRout == null) {
                mRout = StartCoroutine(DoTransitions());
                ApplyModeSelectInteractive();
            }
        }

        public void ModeSelectSetVisible(ModeSelectFlags flags) {
            modeSelectFlags = flags;
            ApplyModeSelectVisible();
        }

        public void HideAll() {
            if(modeSelectRootGO) modeSelectRootGO.SetActive(false);
            if(gameplayRootGO) gameplayRootGO.SetActive(false);

            modeSelectFlags = ModeSelectFlags.None;
            ApplyModeSelectVisible();

            mTransitionQueue.Clear();

            if(mRout != null) {
                StopCoroutine(mRout);
                mRout = null;
            }
        }

        void Awake() {
            HideAll();

            if(modeSelectDisableGO) modeSelectDisableGO.SetActive(false);

            //hook up calls
            for(int i = 0; i < modeSelectButtons.Length; i++) {
                var mode = (ModeSelect)i;
                modeSelectButtons[i].onClick.AddListener(delegate () { modeSelectClickCallback.Invoke(mode); });
            }
        }

        IEnumerator DoTransitions() {
            while(mTransitionQueue.Count > 0) {
                var itm = mTransitionQueue.Remove();

                GameObject rootGO = null;
                AnimatorEnterExit trans = null;

                switch(itm.elem) {
                    case Element.ModeSelect:
                        rootGO = modeSelectRootGO;
                        trans = modeSelectTransition;
                        break;

                    case Element.Gameplay:
                        rootGO = gameplayRootGO;
                        trans = gameplayTransition;
                        break;
                }

                if(itm.isEnter) {
                    if(rootGO) rootGO.SetActive(true);

                    if(trans) yield return trans.PlayEnterWait();
                }
                else {
                    if(trans) yield return trans.PlayExitWait();

                    if(rootGO) rootGO.SetActive(false);
                }
            }

            mRout = null;

            ApplyModeSelectInteractive();
        }

        private void ApplyModeSelectVisible() {
            for(int i = 0; i < modeSelectButtons.Length; i++) {
                var go = modeSelectButtons[i].gameObject;

                var mode = (ModeSelect)i;
                var flag = ModeSelectFlags.None;

                switch(mode) {
                    case ModeSelect.Environment:
                        flag = ModeSelectFlags.Edit;
                        break;
                    case ModeSelect.Edit:
                        flag = ModeSelectFlags.Environment;
                        break;
                    case ModeSelect.Play:
                        flag = ModeSelectFlags.Play;
                        break;
                }

                go.SetActive((modeSelectFlags & flag) == flag);
            }
        }

        private void ApplyModeSelectInteractive() {
            if(modeSelectDisableGO)
                modeSelectDisableGO.SetActive(!mModeSelectInteractive || mRout != null);
        }
    }
}
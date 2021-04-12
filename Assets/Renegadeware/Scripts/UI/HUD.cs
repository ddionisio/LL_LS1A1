using System.Collections;
using System.Collections.Generic;
using TMPro;
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
        public CanvasGroup gameplayCanvasGroup;
        public AnimatorEnterExit gameplayTransition;

        [Header("Gameplay Organism Group")]
        public TMP_Text organismSpawnCountLabel;

        public Image organismProgress;
        public Image organismProgressBonus;
        public GameObject[] organismProgressMedalActives;

        [Header("Gameplay Time Group")]
        public Button timePlayButton;
        public GameObject[] timePlayStateActives;

        public Image timeProgress;

        [Header("Gameplay View Group")]
        public TMP_Text zoomLabel;
        public Button zoomInButton;
        public Button zoomOutButton;

        [Header("Spawn Placement")]
        public GameObject spawnPlacementRootGO;

        public RectTransform spawnPlacementPointer;
        public M8.UI.Graphics.ColorGroup spawnPlacementColorGroup;
        public Color spawnPlacementColorValid = Color.green;
        public Color spawnPlacementColorInvalid = Color.red;

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

        public bool spawnPlacementIsActive {
            get { return spawnPlacementRootGO.activeSelf; }
            set {
                if(spawnPlacementRootGO.activeSelf != value)
                    spawnPlacementRootGO.SetActive(value);
            }
        }

        public bool spawnPlacementIsValid {
            get { return spawnPlacementColorGroup.applyColor == spawnPlacementColorValid; }
            set {
                spawnPlacementColorGroup.ApplyColor(value ? spawnPlacementColorValid : spawnPlacementColorInvalid);
            }
        }

        public ModeSelectFlags modeSelectFlags { get; private set; }

        public event System.Action<ModeSelect> modeSelectClickCallback;

        public event System.Action<int> timePlayCallback; //time index: 0 - stop, 1 - 1x speed, 2 - 2x speed, etc.
        public event System.Action<int> zoomCallback;

        private bool mModeSelectInteractive = true;

        private int mTimeIndex;

        private int mZoomIndex;
        private CameraControl.ZoomLevelInfo[] mZoomInfos;

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

            if(spawnPlacementRootGO) spawnPlacementRootGO.SetActive(false);

            modeSelectFlags = ModeSelectFlags.None;
            ApplyModeSelectVisible();
        }

        public void OrganismProgressApply(int currentCount, int goalCount, int goalBonusCount) {
            organismProgress.fillAmount = Mathf.Clamp01((float)currentCount / goalCount);

            if(currentCount > goalCount) {
                organismProgressBonus.gameObject.SetActive(true);

                organismProgressBonus.fillAmount = Mathf.Clamp01((float)(currentCount - goalCount) / goalBonusCount);
            }
            else
                organismProgressBonus.gameObject.SetActive(false);

            if(currentCount >= goalCount) {
                int medalInd = Mathf.FloorToInt(Mathf.Clamp01((float)(currentCount - goalCount) / goalBonusCount) * (organismProgressMedalActives.Length - 1));

                for(int i = 0; i < organismProgressMedalActives.Length; i++)
                    organismProgressMedalActives[i].SetActive(i <= medalInd);
            }
            else {
                for(int i = 0; i < organismProgressMedalActives.Length; i++)
                    organismProgressMedalActives[i].SetActive(false);
            }

            organismSpawnCountLabel.text = currentCount.ToString();
        }

        public void TimePlaySetIndex(int timeIndex) {
            mTimeIndex = timeIndex;
            TimeDisplayRefresh();
        }

        public void TimeUpdate(float time, float duration) {
            timeProgress.fillAmount = time / duration;
        }

        public void ZoomSetup(int startIndex, CameraControl.ZoomLevelInfo[] infos) {
            mZoomIndex = startIndex;
            mZoomInfos = infos;

            ZoomDisplayRefresh();
        }

        public void ZoomApply(int index) {
            mZoomIndex = index;
            ZoomDisplayRefresh();
        }

        void Awake() {
            HideAll();

            if(modeSelectCanvasGroup)
                mModeSelectInteractive = modeSelectCanvasGroup.interactable;

            spawnPlacementIsActive = false;

            //hook up calls
            for(int i = 0; i < modeSelectButtons.Length; i++) {
                var mode = (ModeSelect)i;
                modeSelectButtons[i].onClick.AddListener(delegate () { modeSelectClickCallback?.Invoke(mode); });
            }

            timePlayButton.onClick.AddListener(OnTimePlayClick);

            zoomInButton.onClick.AddListener(OnZoomInClick);
            zoomOutButton.onClick.AddListener(OnZoomOutClick);
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

                    if(gameplayTransition) {
                        if(gameplayCanvasGroup) gameplayCanvasGroup.interactable = false;

                        yield return gameplayTransition.PlayEnterWait();

                        if(gameplayCanvasGroup) gameplayCanvasGroup.interactable = true;
                    }
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
                    spawnPlacementIsActive = false;

                    if(gameplayTransition) {
                        if(gameplayCanvasGroup) gameplayCanvasGroup.interactable = false;

                        yield return gameplayTransition.PlayExitWait();
                    }

                    if(gameplayRootGO) gameplayRootGO.SetActive(false);
                    break;
            }
        }

        void OnTimePlayClick() {
            if(mTimeIndex < timePlayStateActives.Length - 1)
                mTimeIndex++;
            else
                mTimeIndex = 1;

            TimeDisplayRefresh();

            timePlayCallback?.Invoke(mTimeIndex);
        }

        void OnZoomInClick() {
            if(mZoomIndex > 0) {
                mZoomIndex--;
                ZoomDisplayRefresh();

                zoomCallback?.Invoke(mZoomIndex);
            }
        }

        void OnZoomOutClick() {
            if(mZoomIndex < mZoomInfos.Length - 1) {
                mZoomIndex++;
                ZoomDisplayRefresh();

                zoomCallback?.Invoke(mZoomIndex);
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

        private void TimeDisplayRefresh() {
            for(int i = 0; i < timePlayStateActives.Length; i++)
                timePlayStateActives[i].SetActive(mTimeIndex == i);
        }

        private void ZoomDisplayRefresh() {
            zoomLabel.text = mZoomInfos[mZoomIndex].label;

            zoomInButton.interactable = mZoomIndex > 0;
            zoomOutButton.interactable = mZoomIndex < mZoomInfos.Length - 1;
        }
    }
}
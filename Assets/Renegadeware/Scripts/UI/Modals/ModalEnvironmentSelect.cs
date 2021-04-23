using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class ModalEnvironmentSelect : M8.ModalController, M8.IModalPush, M8.IModalPop {
        public const string parmEnvironmentIndex = "eind"; //current index selected
        public const string parmEnvironmentInfos = "einf"; //LevelData.EnvironmentInfo[]

        public const string speakGroup = "envSelect";

        [Header("Display")]
        public TMP_Text titleText;
        public TMP_Text descText;
        public AttributeGroupWidget attributeGroup;
        public GameObject completedGO;

        [Header("Animation")]
        public M8.Animator.Animate selectAnimator;
        [M8.Animator.TakeSelector(animatorField = "selectAnimator")]
        public string selectTakeEnter;
        [M8.Animator.TakeSelector(animatorField = "selectAnimator")]
        public string selectTakeExit;

        private int mEnvCurInd;
        private LevelData.EnvironmentInfo[] mEnvInfos;

        private Coroutine mRout;

        public void EnvironmentNext() {
            if(mRout != null || mEnvInfos.Length <= 1) //fail-safe: transition in process, only one environment?
                return;

            mEnvCurInd++;
            if(mEnvCurInd == mEnvInfos.Length)
                mEnvCurInd = 0;

            GameData.instance.signalEnvironmentChanged.Invoke(mEnvCurInd);

            mRout = StartCoroutine(DoEnvironmentSelect());
        }

        public void EnvironmentPrev() {
            if(mRout != null || mEnvInfos.Length <= 1) //fail-safe: transition in process, only one environment?
                return;

            mEnvCurInd--;
            if(mEnvCurInd < 0)
                mEnvCurInd = mEnvInfos.Length - 1;

            GameData.instance.signalEnvironmentChanged.Invoke(mEnvCurInd);

            mRout = StartCoroutine(DoEnvironmentSelect());
        }

        public void Speak() {
            var envInf = mEnvInfos[mEnvCurInd];

            LoLManager.instance.StopSpeakQueue();
            
            LoLManager.instance.SpeakText(envInf.descRef);
        }

        void M8.IModalPop.Pop() {
            ClearRout();
        }

        void M8.IModalPush.Push(M8.GenericParams parms) {
            mEnvCurInd = 0;
            mEnvInfos = null;

            if(parms != null) {
                if(parms.ContainsKey(parmEnvironmentIndex))
                    mEnvCurInd = parms.GetValue<int>(parmEnvironmentIndex);

                if(parms.ContainsKey(parmEnvironmentInfos))
                    mEnvInfos = parms.GetValue<LevelData.EnvironmentInfo[]>(parmEnvironmentInfos);
            }

            RefreshDisplay();

            SpeakTitle();
        }

        IEnumerator DoEnvironmentSelect() {
            if(completedGO)
                completedGO.SetActive(false);

            if(selectAnimator) {
                if(!string.IsNullOrEmpty(selectTakeExit))
                    yield return selectAnimator.PlayWait(selectTakeExit);

                RefreshDisplay();

                if(!string.IsNullOrEmpty(selectTakeEnter))
                    yield return selectAnimator.PlayWait(selectTakeEnter);

                SpeakTitle();
            }
            else {
                yield return null;
                RefreshDisplay();
            }

            mRout = null;
        }

        private void RefreshDisplay() {
            var envInf = mEnvInfos[mEnvCurInd];

            if(titleText) titleText.text = M8.Localize.Get(envInf.nameRef);
            if(descText) descText.text = M8.Localize.Get(envInf.descRef);

            if(attributeGroup)
                attributeGroup.Setup(envInf.attributes);

            if(completedGO)
                completedGO.SetActive(GameModePlay.instance.level.IsEnvironmentComplete(mEnvCurInd));
        }

        private void SpeakTitle() {
            var envInf = mEnvInfos[mEnvCurInd];
            LoLManager.instance.StopSpeakQueue();
            LoLManager.instance.SpeakText(envInf.nameRef);
        }

        private void ClearRout() {
            if(mRout != null) {
                StopCoroutine(mRout);
                mRout = null;
            }
        }
    }
}
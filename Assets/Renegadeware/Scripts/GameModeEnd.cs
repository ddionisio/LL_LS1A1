using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class GameModeEnd : GameModeController<GameModeEnd> {
        [Header("Animation")]
        public M8.Animator.Animate animator;
        [M8.Animator.TakeSelector(animatorField = "animator")]
        public string takePlay;

        [Header("UI")]
        public GameObject uiRootGO;
        public M8.Animator.Animate uiAnimator;
        [M8.Animator.TakeSelector(animatorField = "uiAnimator")]
        public string takeUIPlay;

        [Header("Display")]
        public M8.TextMeshPro.TextMeshProCounter scoreLabel;

        protected override void OnInstanceInit() {
            base.OnInstanceInit();

            uiRootGO.SetActive(false);
        }

        protected override IEnumerator Start() {
            yield return base.Start();

            if(LoLManager.isInstantiated) {
                while(!LoLManager.instance.isReady)
                    yield return null;
            }

            scoreLabel.SetCountImmediate(0);

            if(LoLManager.isInstantiated)
                scoreLabel.count = LoLManager.instance.curScore;

            yield return animator.PlayWait(takePlay);

            uiRootGO.SetActive(true);

            yield return uiAnimator.PlayWait(takeUIPlay);

            yield return new WaitForSeconds(0.5f);

            if(LoLManager.isInstantiated)
                LoLManager.instance.Complete();
        }
    }
}
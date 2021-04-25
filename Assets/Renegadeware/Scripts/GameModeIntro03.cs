using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class GameModeIntro03 : GameModeController<GameModeIntro03> {
        public M8.Animator.Animate animator;

        [Header("Sequence")]

        [M8.Animator.TakeSelector(animatorField = "animator")]
        public string takeEnter;

        protected override void OnInstanceInit() {
            base.OnInstanceInit();
        }

        protected override IEnumerator Start() {
            yield return base.Start();

            while(!LoLManager.instance.isReady)
                yield return null;
        }
    }
}
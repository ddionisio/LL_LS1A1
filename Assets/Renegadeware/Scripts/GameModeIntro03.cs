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

        public ModalDialogFlow dialogIntro;

        public ModalDialogFlow dialogForward;

        public ModalDialogFlow dialogProtis;

        protected override void OnInstanceInit() {
            base.OnInstanceInit();
        }

        protected override IEnumerator Start() {
            yield return base.Start();

            if(LoLManager.isInstantiated) {
                while(!LoLManager.instance.isReady)
                    yield return null;
            }

            yield return dialogIntro.Play();

            yield return animator.PlayWait(takeEnter);

            yield return dialogForward.Play();

            //classification
            var classificationParms = new M8.GenericParams();
            classificationParms[ModalCellClassification.parmIndex] = 2;

            M8.ModalManager.main.Open(GameData.instance.modalCellClassification, classificationParms);

            //bacteria dialog
            yield return dialogProtis.Play();

            M8.ModalManager.main.CloseAll();

            while(M8.ModalManager.main.isBusy)
                yield return null;

            //proceed
            GameData.instance.Progress();

            yield return new WaitForSeconds(0.3f);

            GameData.instance.Current();
        }
    }
}
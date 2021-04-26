using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class GameModeIntro02 : GameModeController<GameModeIntro02> {
        public M8.Animator.Animate animator;

        [Header("Sequence")]

        [M8.Animator.TakeSelector(animatorField = "animator")]
        public string takeEnter;

        public ModalDialogFlow dialogIntro;

        public ModalDialogFlow dialogBacteria;

        protected override void OnInstanceInit() {
            base.OnInstanceInit();
        }

        protected override IEnumerator Start() {
            //Loading
            yield return base.Start();

            if(LoLManager.isInstantiated) {
                while(!LoLManager.instance.isReady)
                    yield return null;
            }

            yield return animator.PlayWait(takeEnter);

            //intro dialog
            yield return dialogIntro.Play();

            //classification
            var classificationParms = new M8.GenericParams();
            classificationParms[ModalCellClassification.parmIndex] = 1;

            M8.ModalManager.main.Open(GameData.instance.modalCellClassification, classificationParms);

            //bacteria dialog
            yield return dialogBacteria.Play();

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
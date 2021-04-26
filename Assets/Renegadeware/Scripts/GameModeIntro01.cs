using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class GameModeIntro01 : GameModeController<GameModeIntro01> {
        public M8.Animator.Animate animator;

        [Header("Sequence")]

        public ModalDialogFlow dialogIntro;

        [M8.Animator.TakeSelector(animatorField = "animator")]
        public string takeSpecEnter;

        public ModalDialogFlow dialogSpec;

        [M8.Animator.TakeSelector(animatorField = "animator")]
        public string takeBlobForm;

        public ModalDialogFlow dialogBlobForm;

        public ModalDialogFlow dialogStart;

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

            //intro
            yield return dialogIntro.Play();

            //spec enter
            animator.Play(takeSpecEnter);

            yield return new WaitForSeconds(0.5f);

            //spec dialog
            yield return dialogSpec.Play();

            while(animator.isPlaying)
                yield return null;

            //blob form
            animator.Play(takeBlobForm);

            //blob dialog
            yield return dialogBlobForm.Play();

            while(animator.isPlaying)
                yield return null;

            //classification
            var classificationParms = new M8.GenericParams();
            classificationParms[ModalCellClassification.parmIndex] = 0;

            M8.ModalManager.main.Open(GameData.instance.modalCellClassification, classificationParms);

            //start dialog
            yield return dialogStart.Play();

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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class GamePlayFlowStage01 : GamePlayFlow {
        [Header("Sequence")]

        public ModalDialogFlow dialogEnvironment;
        public ModalDialogFlow dialogEdit;

        public ModalDialogFlow dialogBody;

        public AnimatorEnterExit bodyDiagram;
        public GameObject bodyPlasmaMembraneGO;
        public GameObject bodyCytoplasmGO;

        public ModalDialogFlow dialogPlasmaMembrane;
        public ModalDialogFlow dialogCytoplasm;
        public ModalDialogFlow dialogBodyEnd;

        public ModalDialogFlow dialogEssentialComplete;

        public ModalDialogFlow dialogGame;

        public ModalDialogFlow dialogVictory;

        private bool mIsEnvironmentStart = false;
        private bool mIsEditStart = false;
        private bool mIsBodySelected = false;
        private bool mIsEssentialComplete = false;
        private bool mIsGameStart = false;
        private bool mIsGameTimeHint = false;
        private bool mIsVictory = false;

        public override IEnumerator EnvironmentStart() {
            var gamePlay = GameModePlay.instance;

            if(gamePlay.level.GetProgressCount() == 0) {
                if(!mIsEnvironmentStart) {
                    mIsEnvironmentStart = true;

                    yield return dialogEnvironment.Play();
                }
            }
        }

        public override IEnumerator EditStart() {
            var gamePlay = GameModePlay.instance;

            if(gamePlay.level.GetProgressCount() == 0) {
                if(!mIsEditStart) {
                    mIsEditStart = true;

                    yield return dialogEdit.Play();
                }
            }
        }

        public override IEnumerator GameStart() {
            var gamePlay = GameModePlay.instance;

            if(gamePlay.level.GetProgressCount() == 0) {
                if(!mIsGameStart) {
                    mIsGameStart = true;

                    yield return dialogGame.Play();

                    while(M8.ModalManager.main.isBusy)
                        yield return null;
                }

                if(!mIsGameTimeHint) {
                    mIsGameTimeHint = true;
                    StartCoroutine(DoTimeHint());
                }
            }
        }

        public override IEnumerator Victory() {
            if(!mIsVictory) {
                mIsVictory = true;

                yield return dialogVictory.Play();
            }
        }

        void OnBodyChanged() {
            mIsBodySelected = true; //disabled

            if(mIsBodySelected)
                return;

            var gamePlay = GameModePlay.instance;

            if(gamePlay.level.GetProgressCount() == 0) {
                mIsBodySelected = true;
                StartCoroutine(DoBodyDialog());
            }
        }

        void OnEssentialChanged(int ind) {
            if(mIsEssentialComplete)
                return;

            var gamePlay = GameModePlay.instance;

            if(gamePlay.level.GetProgressCount() == 0) {
                var template = GameData.instance.organismTemplateCurrent;
                if(template.isEssentialComponentsFilled) {
                    mIsEssentialComplete = true;
                    StartCoroutine(DoEssentialComplete());
                }
            }
        }

        void OnDestroy() {
            if(GameData.isInstantiated) {
                GameData.instance.signalOrganismBodyChanged.callback -= OnBodyChanged;
                GameData.instance.signalOrganismComponentEssentialChanged.callback -= OnEssentialChanged;
            }
        }

        void Awake() {
            GameData.instance.signalOrganismBodyChanged.callback += OnBodyChanged;
            GameData.instance.signalOrganismComponentEssentialChanged.callback += OnEssentialChanged;

            bodyDiagram.gameObject.SetActive(false);

            bodyPlasmaMembraneGO.SetActive(false);
            bodyCytoplasmGO.SetActive(false);
        }

        IEnumerator DoBodyDialog() {
            var gamePlay = GameModePlay.instance;

            //hide some elements
            var editModal = M8.ModalManager.main.GetBehaviour<ModalOrganismEditor>(GameData.instance.modalOrganismEdit);
            if(editModal && editModal.canvasGroup)
                editModal.canvasGroup.alpha = 0f;

            if(gamePlay.editDisplay)
                gamePlay.editDisplay.gameObject.SetActive(false);

            yield return dialogBody.Play();

            //show diagram
            bodyDiagram.gameObject.SetActive(true);
            yield return bodyDiagram.PlayEnterWait();

            //plasma membrane
            bodyPlasmaMembraneGO.SetActive(true);
            yield return dialogPlasmaMembrane.Play();

            //cytoplasm
            bodyCytoplasmGO.SetActive(true);
            yield return dialogCytoplasm.Play();

            yield return bodyDiagram.PlayExitWait();
            bodyDiagram.gameObject.SetActive(false);

            yield return dialogBodyEnd.Play();

            //revert hidden elements
            if(editModal && editModal.canvasGroup)
                editModal.canvasGroup.alpha = 1f;

            if(gamePlay.editDisplay)
                gamePlay.editDisplay.gameObject.SetActive(true);
        }

        IEnumerator DoEssentialComplete() {
            yield return dialogEssentialComplete.Play();
        }

        IEnumerator DoTimeHint() {
            yield return new WaitForSeconds(30f);

            if(GameModePlay.instance.timeIndex == 1) {
                var hud = HUD.instance;
                if(hud.timeHintGO)
                    hud.timeHintGO.SetActive(true);
            }
        }
    }
}
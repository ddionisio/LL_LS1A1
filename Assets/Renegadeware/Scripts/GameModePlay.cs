using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class GameModePlay : GameModeController<GameModePlay> {
        [Header("Level Data")]
        public LevelData level;

        public int environmentCurrent { get; private set; }
        public ModeSelect modeSelect { get; private set; }

        private M8.GenericParams mModalParms = new M8.GenericParams();

        private ModeSelect mModeSelectNext;

        protected override void OnInstanceDeinit() {
            if(HUD.isInstantiated) {
                var hud = HUD.instance;

                hud.modeSelectClickCallback -= OnModeSelect;

                hud.HideAll();
            }

            base.OnInstanceDeinit();
        }

        protected override void OnInstanceInit() {
            base.OnInstanceInit();

            //initialize some data
            environmentCurrent = 0;
        }

        protected override IEnumerator Start() {
            //Loading
            yield return base.Start();

            //hook-up hud interaction
            HUD.instance.modeSelectClickCallback += OnModeSelect;

            //game flow
            ChangeToMode(ModeSelect.Environment);
        }

        IEnumerator DoEnvironmentSelect() {
            //show initial environment

            //open environment modal
            mModalParms[ModalEnvironmentSelect.parmEnvironmentInfos] = level.environments;
            mModalParms[ModalEnvironmentSelect.parmEnvironmentIndex] = environmentCurrent;

            M8.ModalManager.main.Open(GameData.instance.modalEnvironmentSelect, mModalParms);

            HUD.instance.ElementShow(HUD.Element.ModeSelect);
            

            //wait for mode select
            mModeSelectNext = ModeSelect.None;
            while(mModeSelectNext == ModeSelect.None)
                yield return null;

            //hide environment

            //hide hud
            HUD.instance.ElementHide(HUD.Element.ModeSelect);

            //close environment modal
            M8.ModalManager.main.CloseUpTo(GameData.instance.modalEnvironmentSelect, true);

            //wait for transitions
            while(M8.ModalManager.main.isBusy || HUD.instance.isBusy)
                yield return null;

            ChangeToMode(mModeSelectNext);
        }

        IEnumerator DoOrganismEdit() {
            //show organism edit

            //open organism edit modal
            mModalParms[ModalOrganismEditor.parmOrganismBodyGroup] = level.organismBodyGroup;
            mModalParms[ModalOrganismEditor.parmOrganismTemplate] = GameData.instance.organismTemplateCurrent;

            M8.ModalManager.main.Open(GameData.instance.modalOrganismEdit, mModalParms);

            HUD.instance.ElementShow(HUD.Element.ModeSelect);

            //wait for mode select
            mModeSelectNext = ModeSelect.None;
            while(mModeSelectNext == ModeSelect.None)
                yield return null;

            //hide organism edit

            //hide hud
            HUD.instance.ElementHide(HUD.Element.ModeSelect);

            //close organism edit modal
            M8.ModalManager.main.CloseUpTo(GameData.instance.modalOrganismEdit, true);

            //wait for transitions
            while(M8.ModalManager.main.isBusy || HUD.instance.isBusy)
                yield return null;

            ChangeToMode(mModeSelectNext);
        }

        IEnumerator DoSimulation() {
            //setup hud
            yield return null;
        }

        void OnModeSelect(ModeSelect toMode) {
            mModeSelectNext = toMode;
        }

        private void HUDApplyModeSelect() {
            var hud = HUD.instance;

            //check if current organism template is valid
            var organismTemplate = GameData.instance.organismTemplateCurrent;
            var organismTemplateValid = organismTemplate.IsEssentialComponentsFilled();

            ModeSelectFlags modeFlags = ModeSelectFlags.None;

            switch(modeSelect) {
                case ModeSelect.Environment:
                    if(organismTemplateValid)
                        modeFlags |= ModeSelectFlags.Play;

                    modeFlags |= ModeSelectFlags.Edit;
                    break;
                case ModeSelect.Edit:
                    if(organismTemplateValid)
                        modeFlags |= ModeSelectFlags.Play;

                    modeFlags |= ModeSelectFlags.Environment;
                    break;
                case ModeSelect.Play:
                    modeFlags = ModeSelectFlags.Environment | ModeSelectFlags.Edit;
                    break;
            }

            hud.ModeSelectSetVisible(modeFlags);
        }

        private void ChangeToMode(ModeSelect toMode) {
            modeSelect = toMode;

            HUDApplyModeSelect();

            switch(toMode) {
                case ModeSelect.Environment:
                    StartCoroutine(DoEnvironmentSelect());
                    break;
                case ModeSelect.Edit:
                    StartCoroutine(DoOrganismEdit());
                    break;
                case ModeSelect.Play:
                    StartCoroutine(DoSimulation());
                    break;
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class GameModePlay : GameModeController<GameModePlay> {
        [Header("Level Data")]
        public LevelData level;

        protected override void OnInstanceDeinit() {
            HUD.instance.HideAll();

            base.OnInstanceDeinit();
        }

        protected override void OnInstanceInit() {
            base.OnInstanceInit();

            //initialize some data
        }

        protected override IEnumerator Start() {
            //Loading
            yield return base.Start();

            //game flow
            StartCoroutine(DoEnvironmentSelect());
        }

        IEnumerator DoEnvironmentSelect() {
            //open environment modal

            //setup hud

            //close environment modal

            yield return null;
        }

        IEnumerator DoOrganismEdit() {
            //open organism modal

            //setup hud

            //close organism modal

            yield return null;
        }

        IEnumerator DoSimulation() {
            //setup hud
            yield return null;
        }
    }
}
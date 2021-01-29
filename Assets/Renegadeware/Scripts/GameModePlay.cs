using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class GameModePlay : GameModeController<GameModePlay> {
        [Header("Level Data")]
        public LevelData level;

        protected override IEnumerator Start() {
            //Loading
            yield return base.Start();

            //game flow
        }

        IEnumerator DoEnvironmentSelect() {
            yield return null;
        }

        IEnumerator DoOrganismEdit() {
            yield return null;
        }

        IEnumerator DoSimulation() {
            yield return null;
        }
    }
}
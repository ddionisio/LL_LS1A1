using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class GameModePlay : GameModeController<GameModePlay> {
        [Header("Level Data")]
        public LevelData level;

#if UNITY_EDITOR
        [Header("Debug")]
        public bool isResetUserData;
#endif

        protected override IEnumerator Start() {
            //Loading
            yield return base.Start();

            //we need user data to be loaded
            if(!GameData.instance.isGameStarted) {                
                while(!LoLManager.instance.isReady)
                    yield return null;

#if UNITY_EDITOR
                GameData.instance.GameStart(isResetUserData);
#else
                GameData.instance.GameStart(false);
#endif
            }

            //game flow
        }
    }
}
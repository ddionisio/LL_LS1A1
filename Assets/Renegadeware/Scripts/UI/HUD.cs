using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class HUD : M8.SingletonBehaviour<HUD> {
        [System.Flags]
        public enum GameModeFlags {
            None = 0x0,
            Environment = 0x1,
            Edit = 0x2,
            Simulate = 0x4
        }

        [Header("Mode")]
        public GameObject gameModeRootGO;
        public AnimatorEnterExit gameModeTransition;
        public Button[] gameModeButtons; //corresponds to GameMode flags

        public event System.Action<GameModeFlags> gameModeClickCallback;

        private Coroutine mRout;

        void Awake() {
            HideAll();

            //hook up calls
        }

        private void HideAll() {

        }
    }
}
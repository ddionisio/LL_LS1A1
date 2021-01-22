using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    /// <summary>
    /// General use for info
    /// </summary>
    [CreateAssetMenu(fileName = "info", menuName = "Game/Info")]
    public class InfoData : ScriptableObject {
        [Header("Info")]
        public Sprite icon;
        [M8.Localize]
        public string nameRef;
        [M8.Localize]
        public string descRef;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    /// <summary>
    /// Used for stats display during edit.
    /// </summary>
    [System.Serializable]
    public class AttributeInfo {
        public Sprite icon;

        [M8.Localize]
        public string categoryRef;
        [M8.Localize]
        public string nameRef;
    }
}
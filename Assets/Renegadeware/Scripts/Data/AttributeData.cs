using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    /// <summary>
    /// Used for stats display during edit.
    /// </summary>
    [CreateAssetMenu(fileName = "attribute", menuName = "Game/Attribute Info")]
    public class AttributeData : ScriptableObject {
        public Sprite icon;
        public Sprite descImage;
        [M8.Localize]
        public string categoryRef;
        [M8.Localize]
        public string nameRef;
        [M8.Localize]
        public string descRef;
    }
}
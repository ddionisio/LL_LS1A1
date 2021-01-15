using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    /// <summary>
    /// General use for categorization.
    /// </summary>
    [CreateAssetMenu(fileName = "category", menuName = "Game/Category")]
    public class CategoryData : ScriptableObject {
        [Header("Info")]
        public Sprite icon;
        [M8.Localize]
        public string nameRef;
        [M8.Localize]
        public string descRef;
    }
}
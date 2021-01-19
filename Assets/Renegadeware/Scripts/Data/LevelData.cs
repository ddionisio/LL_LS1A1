using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "level", menuName = "Game/Level")]
    public class LevelData : ScriptableObject {
        [System.Serializable]
        public struct EnvironmentInfo {
            [M8.Localize]
            public string nameRef;
            [M8.Localize]
            public string descRef;

            public Sprite previewImage;
        }

        [Header("Data")]
        public M8.SceneAssetPath scene;
        public int progressCount; //number of progress for this particular level before going to next (use LoL cur. progress)
        public EnvironmentInfo[] environments; //usually 4

        [Header("Cell Bodies")]
        public CategoryData bodyCategory;
        public OrganismComponent[] bodyComponents;

        //environment selections

        //cell spawn restriction, etc.
    }
}
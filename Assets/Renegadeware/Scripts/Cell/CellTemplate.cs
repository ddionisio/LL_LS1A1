using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "cellTemplate", menuName = "Game/Cell Template")]
    public class CellTemplate : ScriptableObject {
        public int[] componentIDs;

        /// <summary>
        /// Instantiate a template from user data. Remember to delete this when done.
        /// </summary>
        public static CellTemplate LoadFrom(string key) {
            return null;
        }
    }
}
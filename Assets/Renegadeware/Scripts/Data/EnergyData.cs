using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "energy", menuName = "Game/Environment/Energy")]
    public class EnergyData : ScriptableObject {
        [Tooltip("Used for testing, set to true to always include in filters (sensor/contact)")]
        public bool ignoreMatch;

        [Tooltip("If true, organisms will not stay to receive energy.")]
        public bool ethereal;
    }
}
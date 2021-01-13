using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class EnergySource : MonoBehaviour {
        [Header("Data")]
        public EnergyData data;
        public float amount;
        public bool isAbsorb; //if true, this will be absorbed by the cell's energy gatherer organelle

        [Header("Animation")]
        public M8.Animator.Animate animator;
        [M8.Animator.TakeSelector(animatorField = "animator")]
        public string takeSpawn;
        [M8.Animator.TakeSelector(animatorField = "animator")]
        public string takeActive;
        [M8.Animator.TakeSelector(animatorField = "animator")]
        public string takeDespawn;

        public bool isSpawn { get; private set; }
        

    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class EnvironmentHazard : MonoBehaviour {
        public HazardData hazard;

        /// <summary>
        /// Reduce an organism's energy based on a percentage of its energy capacity. (per second)
        /// </summary>
        public float energyDrainScale;

        public void Apply(OrganismStats stats) {
            if(!stats.HazardMatch(hazard))
                stats.energy -= stats.energyCapacity * energyDrainScale * Time.deltaTime;
        }
    }
}
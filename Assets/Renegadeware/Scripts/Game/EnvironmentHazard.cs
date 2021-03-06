﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class EnvironmentHazard : MonoBehaviour {
        public HazardData hazard;

        /// <summary>
        /// Reduce an organism's energy based on a percentage of its energy capacity. (per second)
        /// </summary>
        public float energyDrainScale;

        public float moveScale = 0.5f;
        public float turnScale = 0.5f;

        public bool isActive { get { return gameObject.activeSelf; } }
    }
}
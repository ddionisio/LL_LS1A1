using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    /// <summary>
    /// Provide energy to all organisms that matches its energy source.
    /// </summary>
    public class EnvironmentEnergy : MonoBehaviour {
        public EnergyData energySource;

        public float energyRate; //amount of energy per second

        public bool isActive { get { return gameObject.activeSelf; } }
    }
}
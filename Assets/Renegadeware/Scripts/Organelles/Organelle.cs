using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public abstract class Organelle : ScriptableObject {
        public int id;
        public GameObject prefab;

        [Header("Info")]
        public Sprite icon;
        [M8.Localize]
        public string nameRef;
        [M8.Localize]
        public string descRef;

        [Header("General")]
        public float energy; //initial energy value upon spawn
        public float energyRate; //determines amount of energy consumption/regeneration
        public float energyCapacity; //determines energy cap, when reached, allow division
    }
}
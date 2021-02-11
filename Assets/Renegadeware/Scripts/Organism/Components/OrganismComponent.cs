using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public abstract class OrganismComponent : InfoData {
        [Header("Component Info")]
        [ID(group = "organismComponent", invalidID = GameData.invalidID)]
        public int ID;
        public string anchorName;

        [Header("Templates")]
        public GameObject editPrefab; //use during edit mode
        public GameObject gamePrefab; //use for spawned, added to cell's template

        [Header("General")]
        public float energy; //initial energy value upon spawn
        public float energyRate; //determines amount of energy consumption/regeneration
        public float energyCapacity; //determines energy cap, when reached, allow division
    }
}
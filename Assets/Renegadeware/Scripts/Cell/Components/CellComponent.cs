using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public abstract class CellComponent : ScriptableObject {
        public int id;

        [Header("Info")]
        public Sprite icon;
        [M8.Localize]
        public string nameRef;
        [M8.Localize]
        public string descRef;
        public AnchorData anchorId;

        [Header("Templates")]
        public GameObject editPrefab; //use during edit mode
        public GameObject gamePrefab; //use for spawned, added to cell's template

        [Header("General")]
        public float energy; //initial energy value upon spawn
        public float energyRate; //determines amount of energy consumption/regeneration
        public float energyCapacity; //determines energy cap, when reached, allow division        
    }
}
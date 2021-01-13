using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "organelle", menuName = "Game/Organelle")]
    public class OrganelleData : ScriptableObject {
        public int id;
        public GameObject prefab;

        [Header("Info Data")]
        public Sprite icon;
        [M8.Localize]
        public string nameRef;
        [M8.Localize]
        public string descRef;

        [Header("Game Data")]        
        public float energyRate; //determines amount of energy consumption/regeneration
        public float mass; //determines energy capacity for growth
    }
}
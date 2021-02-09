using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismBodyDisplay : MonoBehaviour {
        [Header("Display")]
        public SpriteRenderer render;

        [Header("Attachments")]
        public Transform anchorRoot; //holds all anchors

        private Dictionary<string, List<Transform>> mAnchors = new Dictionary<string, List<Transform>>();

        void Awake() {
            
        }
    }
}
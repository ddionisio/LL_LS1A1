using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismEntity : MonoBehaviour {
        public OrganismBodyDisplay bodyDisplay { get; private set; }

        public M8.SpriteColorGroup color { get; private set; }

        /// <summary>
        /// Call this after creating the prefab, before generating the pool.
        /// </summary>
        public void SetupTemplate(OrganismTemplate organismTemplate) {
            //go through and initialize organelles
        }

        void OnTriggerEnter2D(Collider2D collision) {
            
        }

        void OnTriggerExit2D(Collider2D collision) {
            
        }
    }
}
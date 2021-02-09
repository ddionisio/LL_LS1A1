using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismEditMode : MonoBehaviour {

        [Header("Cache Info")]
        public Transform cacheRoot; //move unused instances here

        private OrganismBodyDisplay mBodyDisplay;

        private Dictionary<GameObject, GameObject[]> mComponentInstances = new Dictionary<GameObject, GameObject[]>();

        public void Setup(OrganismTemplate organismTemplate) {

        }

        void OnDisable() {
            
        }

        void OnEnable() {
            //hook up signals
        }

        void OnDestroy() {
            
        }

        void Awake() {
            //generate anchor look-ups
        }

        private void ClearComponents() {

        }
    }
}
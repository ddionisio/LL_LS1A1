using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismBodyDisplay : MonoBehaviour {
        [Header("Attachments")]
        public Transform anchorRoot; //holds all anchors

        private Dictionary<string, List<Transform>> mAnchors = new Dictionary<string, List<Transform>>();

        public List<Transform> GetAnchors(string anchorName) {
            List<Transform> anchors;
            mAnchors.TryGetValue(anchorName, out anchors);
            return anchors;
        }

        void Awake() {
            //initialize anchor look-ups
            GenerateAnchors(anchorRoot);
        }

        private void GenerateAnchors(Transform parent) {
            for(int i = 0; i < parent.childCount; i++) {
                var t = parent.GetChild(i);

                List<Transform> anchors;
                if(!mAnchors.TryGetValue(t.name, out anchors)) {
                    anchors = new List<Transform>();
                    mAnchors.Add(t.name, anchors);
                }

                anchors.Add(t);

                if(t.childCount > 0)
                    GenerateAnchors(t);
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismDisplayBody : MonoBehaviour {
        [Header("Display")]
        [SerializeField]
        SpriteRenderer _spriteRender = null;
        [SerializeField]
        M8.SpriteColorGroup _colorGroup = null;

        [Header("Attachments")]
        public Transform anchorRoot; //holds all anchors

        public SpriteRenderer spriteRender {
            get {
                if(!_spriteRender)
                    _spriteRender = GetComponent<SpriteRenderer>();

                return _spriteRender;
            }
        }

        public M8.SpriteColorGroup colorGroup { 
            get {
                if(!_colorGroup)
                    _colorGroup = GetComponent<M8.SpriteColorGroup>();

                return _colorGroup;
            }
        }

        private Dictionary<string, List<Transform>> mAnchors = null;

        public List<Transform> GetAnchors(string anchorName) {
            if(mAnchors == null) {
                mAnchors = new Dictionary<string, List<Transform>>();

                if(anchorRoot)
                    GenerateAnchors(anchorRoot);
            }

            List<Transform> anchors;
            mAnchors.TryGetValue(anchorName, out anchors);
            return anchors;
        }

        private void GenerateAnchors(Transform parent) {
            for(int i = 0; i < parent.childCount; i++) {
                var t = parent.GetChild(i);
                if(!t)
                    continue;

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
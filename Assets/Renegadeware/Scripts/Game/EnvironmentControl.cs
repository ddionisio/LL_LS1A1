using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class EnvironmentControl : MonoBehaviour {

        public bool isActive {
            get {
                return gameObject.activeSelf;
            }

            set {
                gameObject.SetActive(value);
            }
        }

        public GameBounds2D bounds {
            get {
                if(!mBounds)
                    mBounds = GetComponent<GameBounds2D>();
                return mBounds;
            }
        }

        private GameBounds2D mBounds;

        public void ApplyBoundsToCamera(GameCamera cam) {
            var boundRect = bounds.rect;

            cam.SetBounds(boundRect, false);
            cam.SetPosition(boundRect.center);
        }
    }
}
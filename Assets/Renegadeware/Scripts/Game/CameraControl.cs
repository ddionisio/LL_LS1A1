using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class CameraControl : MonoBehaviour {
        public Camera cameraSource { get; private set; }

        private Rect mBoundsRect;

        void Awake() {
            cameraSource = GetComponentInChildren<Camera>();
        }
    }
}
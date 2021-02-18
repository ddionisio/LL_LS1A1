using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class EnvironmentControl : MonoBehaviour {

        public Rect bounds;

        [Header("Editor")]
        public float editBoundsSteps = 1.0f;
        public bool editSyncBoxCollider = true;
        public Color editBoundsColor = Color.cyan;

        public bool isActive {
            get {
                return gameObject.activeSelf;
            }

            set {
                gameObject.SetActive(value);
            }
        }

        public Vector2 Clamp(Vector2 center, Vector2 ext) {
            Vector2 min = (Vector2)bounds.min + ext;
            Vector2 max = (Vector2)bounds.max - ext;

            float extX = bounds.width * 0.5f;
            float extY = bounds.height * 0.5f;

            if(extX > ext.x)
                center.x = Mathf.Clamp(center.x, min.x, max.x);
            else
                center.x = bounds.center.x;

            if(extY > ext.y)
                center.y = Mathf.Clamp(center.y, min.y, max.y);
            else
                center.y = bounds.center.y;

            return center;
        }

        /// <summary>
        /// Set camera's bounds, camera's position to bounds center, and zoom out to last level.
        /// </summary>
        public void ApplyBoundsToCamera(CameraControl camCtrl) {
            camCtrl.SetBounds(bounds, camCtrl.zoomLevels.Length - 1, true);
        }

        void OnDrawGizmos() {
            Gizmos.color = editBoundsColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
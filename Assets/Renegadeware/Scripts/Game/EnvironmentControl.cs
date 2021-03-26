using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class EnvironmentControl : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler {

        public Rect bounds;

        [Header("Physics")]
        public float linearDrag = 1f;
        public float angularDrag = 5f;

        public EnvironmentVelocity velocityControl;

        [Header("Attributes")]
        public EnvironmentHazard[] hazards;
        public EnvironmentEnergy[] energySources;

        [Header("Editor")]
        public float editBoundsSteps = 1.0f;
        public bool editSyncBoxCollider = true;
        public Color editBoundsColor = Color.cyan;

        public bool isDragging { get; private set; }

        public bool isActive {
            get {
                return gameObject.activeSelf;
            }

            set {
                gameObject.SetActive(value);
            }
        }

        public Vector2 Clamp(Vector2 center, Vector2 ext) {
            Vector2 min = bounds.min + ext;
            Vector2 max = bounds.max - ext;

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

        void OnApplicationFocus(bool focus) {
            if(!focus) {
                if(isDragging)
                    ((IEndDragHandler)this).OnEndDrag(null);
            }
        }

        void OnDisable() {
            if(isDragging)
                ((IEndDragHandler)this).OnEndDrag(null);
        }

        void OnDrawGizmos() {
            Gizmos.color = editBoundsColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
            isDragging = true;
            GameData.instance.signalEnvironmentDragBegin.Invoke();
        }

        void IDragHandler.OnDrag(PointerEventData eventData) {
            if(isDragging)
                GameData.instance.signalEnvironmentDrag.Invoke(eventData.delta);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
            if(isDragging) {
                isDragging = false;
                GameData.instance.signalEnvironmentDragEnd.Invoke();
            }
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            if(!isDragging && eventData.pointerCurrentRaycast.isValid)
                GameData.instance.signalEnvironmentClick.Invoke(eventData.pointerCurrentRaycast.worldPosition);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class EnvironmentControl : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler {

        public Rect bounds;

        public GameObject controlRoot; //grab velocity, hazards, energies here

        [Header("Display")]
        public Color backgroundColor = Color.black;

        [Header("Physics")]
        public float linearDrag = 1f;
        public float angularDrag = 5f;

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

        public EnvironmentVelocity velocityControl { get { return mVelocityCtrl; } }

        public EnvironmentHazard[] hazards { get { return mHazards; } }
        public EnvironmentEnergy[] energySources { get { return mEnergySrcs; } }

        private EnvironmentVelocity mVelocityCtrl;
        private EnvironmentHazard[] mHazards;
        private EnvironmentEnergy[] mEnergySrcs;

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
        /// Set camera's bounds, camera's position to bounds center, and zoom out to last level. Also apply background color.
        /// </summary>
        public void ApplyToCamera(CameraControl camCtrl) {
            camCtrl.SetBounds(bounds, camCtrl.zoomLevels.Length - 1, true);
            camCtrl.cameraSource.backgroundColor = backgroundColor;
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

        void Awake() {
            mVelocityCtrl = controlRoot.GetComponentInChildren<EnvironmentVelocity>(true);
            mHazards = controlRoot.GetComponentsInChildren<EnvironmentHazard>(true);
            mEnergySrcs = controlRoot.GetComponentsInChildren<EnvironmentEnergy>(true);
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
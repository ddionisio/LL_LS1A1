using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class CameraControl : M8.SingletonBehaviour<CameraControl> {
        [System.Serializable]
        public struct ZoomLevelInfo {
            public string label;
            public float level;
        }

        [Header("Move Info")]
        public float moveToDelay = 0.01f;        

        [Header("Zoom Info")]
        public ZoomLevelInfo[] zoomLevels;
        public float zoomDelay = 0.1f;

        [Header("Input")]
        public float inputMoveSpeed = 0.5f;

        public M8.InputAction inputZoom;
        public M8.InputAction inputMoveX;
        public M8.InputAction inputMoveY;

        public Camera cameraSource { 
            get {
                if(!mCamSrc)
                    mCamSrc = GetComponentInChildren<Camera>();
                return mCamSrc; 
            }
        }

        public Vector2 position { 
            get { return transform.position; } 
            set {
                transform.position = ClampCenterWorldToBounds(value);
                mIsMove = false;
            }
        }

        public Vector2 positionMoveTo { get { return mMoveToPos; } }

        public bool isMoving { get { return mIsMove; } }

        public int zoomIndex {
            get { return mZoomIndex; }
            set {
                if(mZoomIndex != value) {
                    mZoomIndex = value;

                    var camPos = cameraSource.transform.localPosition;
                    camPos.z = zoomLevels[mZoomIndex].level;

                    cameraSource.transform.localPosition = camPos;

                    mIsZoom = false;

                    RefreshCameraViewSize();
                    ApplyBounds();

                    GameData.instance.signalCameraZoom.Invoke(mZoomIndex);
                }
            }
        }

        public bool isZooming { get { return mIsZoom; } }

        public float zoomScale {
            get {
                if(zoomLevels.Length == 0 || zoomLevels[mZoomIndex].level == 0f)
                    return 1f;

                var levelBase = zoomLevels[zoomLevels.Length - 1].level;

                return Mathf.Abs(levelBase / zoomLevels[mZoomIndex].level);
            }
        }

        public bool inputEnabled { 
            get { return mInputEnabled; }
            set {
                if(mInputEnabled != value) {
                    mInputEnabled = true;
                    mInputMoveAxis = Vector2.zero;
                    mInputZoomAxis = 0f;
                    mInputLastTime = Time.realtimeSinceStartup;
                }
            }
        }

        public Rect bounds { get; private set; }

        private Vector2 mMoveToPos;
        private Vector2 mMoveToVel;
        private float mMoveLastTime;
        private bool mIsMove;

        private int mZoomIndex;
        private float mZoomToVel;
        private float mZoomLastTime;
        private bool mIsZoom;

        private Camera mCamSrc;
        private Vector3[] mCamFrustumCorners = new Vector3[4];

        private Vector2 mCamCurSize;

        private bool mInputEnabled;
        private Vector2 mInputMoveAxis;
        private float mInputZoomAxis;
        private float mInputLastTime;

        public void SetBounds(Rect newBounds, int toZoomIndex, bool setPositionToBounds) {
            bounds = newBounds;

            mZoomIndex = toZoomIndex;
            var camPos = cameraSource.transform.localPosition;
            camPos.z = zoomLevels[mZoomIndex].level;
            cameraSource.transform.localPosition = camPos;

            if(setPositionToBounds) {
                transform.position = newBounds.center;
                mIsMove = false;
            }

            RefreshCameraViewSize();
            ApplyBounds();
        }

        public void MoveTo(Vector2 toPos) {
            mMoveToPos = ClampCenterWorldToBounds(toPos);
            mMoveToVel = Vector2.zero;

            if(!mIsMove) {
                mIsMove = true;
                mMoveLastTime = Time.realtimeSinceStartup;
            }
        }

        public void ZoomTo(int zoomIndex) {
            if(mZoomIndex == zoomIndex)
                return;

            mZoomIndex = zoomIndex;

            mIsZoom = true;
            mZoomLastTime = Time.realtimeSinceStartup;

            GameData.instance.signalCameraZoom.Invoke(mZoomIndex);
        }

        void OnEnable() {
            mIsMove = false;
            mIsZoom = false;

            inputEnabled = false;

            RefreshCameraViewSize();
        }

        void Update() {
            if(mIsMove) {
                Vector2 curPos = transform.position;
                if(curPos != mMoveToPos) {
                    var curTime = Time.realtimeSinceStartup;

                    curPos = Vector2.SmoothDamp(curPos, mMoveToPos, ref mMoveToVel, moveToDelay, Mathf.Infinity, curTime - mMoveLastTime);

                    mMoveLastTime = curTime;

                    transform.position = curPos;
                }
                else
                    mIsMove = false;
            }

            if(mIsZoom) {
                var camPos = cameraSource.transform.localPosition;
                var curZoom = zoomLevels[mZoomIndex];
                if(camPos.z != curZoom.level) {
                    var curTime = Time.realtimeSinceStartup;

                    camPos.z = Mathf.SmoothDamp(camPos.z, curZoom.level, ref mZoomToVel, zoomDelay, Mathf.Infinity, curTime - mZoomLastTime);

                    mZoomLastTime = curTime;

                    cameraSource.transform.localPosition = camPos;

                    RefreshCameraViewSize();
                    ApplyBounds();
                }
                else
                    mIsZoom = false;
            }

            if(inputEnabled && !M8.SceneManager.instance.isPaused) {
                float curTime = Time.realtimeSinceStartup;
                float dt = curTime - mInputLastTime;
                mInputLastTime = curTime;

                //move
                mInputMoveAxis.x = inputMoveX.GetAxis();
                mInputMoveAxis.y = inputMoveY.GetAxis();

                if(mInputMoveAxis != Vector2.zero)
                    position += mInputMoveAxis * dt * inputMoveSpeed;

                //zoom
                if(inputZoom.IsPressed()) {
                    mInputZoomAxis = inputZoom.GetAxis();

                    int toZoomInd = mZoomIndex;

                    if(mInputZoomAxis < 0f) {
                        if(toZoomInd > 0)
                            toZoomInd--;
                    }
                    else if(mInputZoomAxis > 0f) {
                        if(toZoomInd < zoomLevels.Length - 1)
                            toZoomInd++;
                    }

                    ZoomTo(toZoomInd);
                }
            }
        }

        void OnDrawGizmos() {
            //display camera rect
            var cam = cameraSource;
            if(cam) {
                cam.CalculateFrustumCorners(cam.rect, -cam.transform.localPosition.z, Camera.MonoOrStereoscopicEye.Mono, mCamFrustumCorners);

                var camT = cam.transform;
                var pos = camT.position;

                Vector2 min = pos + mCamFrustumCorners[0];
                Vector2 max = pos + mCamFrustumCorners[2];

                Gizmos.color = new Color(0.75f, 0f, 0.75f);

                Gizmos.DrawLine(new Vector3(min.x, min.y, 0f), new Vector3(max.x, min.y, 0f));
                Gizmos.DrawLine(new Vector3(max.x, min.y, 0f), new Vector3(max.x, max.y, 0f));
                Gizmos.DrawLine(new Vector3(max.x, max.y, 0f), new Vector3(min.x, max.y, 0f));
                Gizmos.DrawLine(new Vector3(min.x, max.y, 0f), new Vector3(min.x, min.y, 0f));
            }
        }

        private void RefreshCameraViewSize() {
            var cam = cameraSource;
            if(cam) {
                cam.CalculateFrustumCorners(cam.rect, -cam.transform.localPosition.z, Camera.MonoOrStereoscopicEye.Mono, mCamFrustumCorners);

                mCamCurSize = new Vector2(Mathf.Abs(mCamFrustumCorners[2].x - mCamFrustumCorners[0].x), Mathf.Abs(mCamFrustumCorners[2].y - mCamFrustumCorners[0].y));
            }
        }

        private void ApplyBounds() {
            //clamp position
            var camRect = new Rect();
            camRect.size = mCamCurSize;
            camRect.center = position;

            transform.position = ClampCenterWorldRectToBounds(camRect);

            //clamp moveTo
            if(mIsMove)
                mMoveToPos = ClampCenterWorldToBounds(mMoveToPos);
        }

        private Vector2 ClampCenterWorldToBounds(Vector2 center) {
            var r = new Rect();
            r.size = mCamCurSize;
            r.center = center;

            return ClampCenterWorldRectToBounds(r);
        }

        private Vector2 ClampCenterWorldRectToBounds(Rect worldRect) {
            var center = worldRect.center;

            //adjust X
            if(worldRect.width >= bounds.width)
                center.x = bounds.center.x;
            else if(worldRect.xMin < bounds.xMin)
                center.x = bounds.xMin + (worldRect.width * 0.5f);
            else if(worldRect.xMax > bounds.xMax)
                center.x = bounds.xMax - (worldRect.width * 0.5f);

            //adjust Y
            if(worldRect.height >= bounds.height)
                center.y = bounds.center.y;
            else if(worldRect.yMin < bounds.yMin)
                center.y = bounds.yMin + (worldRect.height * 0.5f);
            else if(worldRect.yMax > bounds.yMax)
                center.y = bounds.yMax - (worldRect.height * 0.5f);

            return center;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class TransformRadiusWander : MonoBehaviour {
        public float delay;
        public float radius;

        public Color previewColor = new Color(0.75f, 0f, 0f, 0.5f);

        private Vector2 mOriginPoint;

        private Vector2 mEndPos;

        private Vector2 mVel;
        private float mLastTime;

        private bool mIsStarted;

        void OnEnable() {
            mVel = Vector2.zero;

            if(mIsStarted)
                SetDest();
        }

        void Start() {
            mOriginPoint = transform.localPosition;
            mVel = Vector2.zero;

            SetDest();
            mIsStarted = true;
        }

        void Update() {
            Vector2 pos = transform.localPosition;
            pos = Vector2.SmoothDamp(pos, mEndPos, ref mVel, delay);
            transform.localPosition = new Vector3(pos.x, pos.y, transform.localPosition.z);

            if(Time.time - mLastTime >= delay)
                SetDest();
        }

        private void SetDest() {
            mEndPos = mOriginPoint + (M8.MathUtil.Rotate(Vector2.up, Random.Range(0f, M8.MathUtil.TwoPI)) * radius);
            mLastTime = Time.time;
        }

        void OnDrawGizmos() {
            if(radius > 0f) {
                Gizmos.color = previewColor;
                Gizmos.DrawWireSphere(transform.position, radius);
            }
        }
    }
}
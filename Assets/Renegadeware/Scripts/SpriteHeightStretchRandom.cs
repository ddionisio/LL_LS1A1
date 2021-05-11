using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    /// <summary>
    /// Ensure sprite anchor is bottom, renderer set to tile or sliced
    /// </summary>
    public class SpriteHeightStretchRandom : MonoBehaviour {
        public SpriteRenderer spriteRender;

        public M8.RangeFloat heightRange;
        public M8.RangeFloat delayRange;

        public AnimationCurve curve; //set to bell-curve, ideally: 0: 0, 0.5: 1, 1: 0

        private float mHeight;
        private float mDelay;
        private float mLastTime;

        void OnEnable() {
            Begin();
        }

        void Update() {
            float curTime = Time.time - mLastTime;
            if(curTime <= mDelay) {
                float t = Mathf.Clamp01(curTime / mDelay);
                float h = curve.Evaluate(t) * mHeight;

                var s = spriteRender.size;
                s.y = h;
                spriteRender.size = s;
            }
            else
                Begin();
        }

        private void Begin() {
            mHeight = heightRange.random;
            mDelay = delayRange.random;
            mLastTime = Time.time;
        }
    }
}
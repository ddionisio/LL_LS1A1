using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    /// <summary>
    /// Move towards/against a polar point.
    /// </summary>
    public class EnvironmentVelocityPolar : EnvironmentVelocity {
        public Transform polarRoot;

        public float accel;

        public override Vector2 GetVelocity(Vector2 pos, Vector2 forward, float deltaTime) {
            Vector2 polarPos = polarRoot.position;

            var moveDir = (polarPos - pos).normalized;

            return moveDir * accel * deltaTime;
        }

        void Awake() {
            if(!polarRoot)
                polarRoot = transform;
        }
    }
}
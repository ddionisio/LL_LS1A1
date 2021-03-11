using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class EnvironmentVelocityDir : EnvironmentVelocity {
        [SerializeField]
        float _angle;

        public float speed;
        public bool isAccelerate;

        public float angle {
            get { return _angle; }
            set {
                if(_angle != value) {
                    _angle = value;
                    ApplyDir();
                }
            }
        }

        private Vector2 mDir;

        public override Vector2 GetVelocity(Vector2 pos, Vector2 forward, float deltaTime) {
            return isAccelerate ? mDir * (speed * deltaTime) : mDir * speed;
        }

        void Awake() {
            ApplyDir();
        }

        private void ApplyDir() {
            mDir = M8.MathUtil.RotateAngle(Vector2.up, _angle);
        }

        void OnDrawGizmos() {
            ApplyDir();

            Vector2 pos = transform.position;

            Gizmos.color = Color.white;

            M8.Gizmo.ArrowLine2D(pos, pos + mDir * 2f);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismFieldVelocityDir : OrganismField {
        [Header("Velocity Settings")]
        [SerializeField]
        float _angle;

        public float accel;

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

        protected override void UpdateEntity(OrganismEntity ent, float timeDelta) {
            ent.velocity += mDir * accel * timeDelta;
        }

        protected override void Awake() {
            base.Awake();

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
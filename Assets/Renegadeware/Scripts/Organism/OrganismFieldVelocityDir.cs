using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismFieldVelocityDir : OrganismField {
        [Header("Velocity Settings")]
        [SerializeField]
        float _angle;

        public float accel;
        public bool entityApplyVelocityScale; //if true, apply velocityReceiveScale

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
            if(entityApplyVelocityScale)
                ent.velocity += mDir * accel * ent.stats.velocityReceiveScale * timeDelta;
            else
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
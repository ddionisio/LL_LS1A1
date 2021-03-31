using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class SpawnPoint : MonoBehaviour {
        public float radius;
        public int count;

        public Color color = Color.yellow;

        public Vector2 position { get { return transform.position; } }

        public Vector2 GetPoint() {
            return position + (Random.insideUnitCircle * radius);
        }

        void OnDrawGizmos() {
            if(radius > 0f) {
                Gizmos.color = color;
                Gizmos.DrawWireSphere(transform.position, radius);
            }
        }
    }
}
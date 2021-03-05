using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    /// <summary>
    /// General animation for organism reproduction (mostly for binary-fission)
    /// </summary>
    public class OrganismDisplaySpawn : MonoBehaviour {
        public const float gizmoPointRadius = 0.15f;
        public static readonly Color gizmoColor = new Color(0.5f, 0.5f, 0f);

        [Header("Display")]
        public SpriteRenderer spriteRender;
        public Sprite[] spriteFrames;
        public float spriteAnimationDuration;

        [Header("Spawn Info")]
        [SerializeField]
        Vector2[] _spawnPoints;

        public int spawnPointCount { get { return _spawnPoints.Length; } }

        public Vector3 GetSpawnPoint(int index) {
            return transform.TransformPoint(_spawnPoints[index]);
        }

        public IEnumerator PlayWait() {
            yield return null;
        }

        void OnDrawGizmos() {
            Gizmos.color = gizmoColor;

            for(int i = 0; i < spawnPointCount; i++)
                Gizmos.DrawSphere(GetSpawnPoint(i), gizmoPointRadius);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismTemplateSpawner : MonoBehaviour {
        public const string poolType = "organismTemplate";

        public Transform spawnRoot;

        public int spawnCount { get { return mPool ? mPool.GetActiveCount(poolType) : 0; } }

        private M8.PoolController mPool;

        public void Setup(OrganismTemplate organismTemplate, int capacity) {
            Destroy();

            //create a GameObject template

            //generate pool
        }

        public void Destroy() {
            if(mPool)
                mPool.RemoveType(poolType);
        }

        public void Clear() {
            if(mPool)
                mPool.ReleaseAll();
        }

        public OrganismEntity SpawnAt(Vector2 pt) {
            return null;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismTemplateSpawner : MonoBehaviour {
        public const string poolType = "organismTemplate";
        public const string poolSpawn = "organismSpawn";

        public Transform spawnRoot;

        public M8.CacheList<OrganismEntity> entities { get; private set; }
        public int entityCount { get { return entities != null ? entities.Count : 0; } }

        public int capacity { get { return entities.Capacity; } }

        private M8.PoolController mPool;

        private OrganismEntity mTemplate;

        private M8.GenericParams mParms = new M8.GenericParams();

        public void Setup(OrganismTemplate organismTemplate, int capacity) {
            Destroy();

            //generate pool
            if(!mPool) {
                mPool = M8.PoolController.CreatePool(name, transform);
                mPool.despawnCallback += OnDespawn;
            }

            //setup entity active list
            if(entities == null)
                entities = new M8.CacheList<OrganismEntity>(capacity);
            else if(entities.Capacity != capacity)
                entities.Resize(capacity);

            //create a GameObject template
            mTemplate = OrganismEntity.CreateTemplate(poolSpawn, organismTemplate, transform);
            mTemplate.gameObject.SetActive(false);

            //setup pool type
            mPool.AddType(poolType, mTemplate.gameObject, capacity, capacity, spawnRoot);
        }

        public void Destroy() {
            if(entities != null)
                entities.Clear();

            if(mPool)
                mPool.RemoveType(poolType);

            if(mTemplate) {
                DestroyImmediate(mTemplate.gameObject);
                mTemplate = null;
            }
        }

        public void Clear() {
            if(mPool)
                mPool.ReleaseAll();
        }

        public OrganismEntity SpawnAtRandomDir(Vector2 pt) {
            if(entities.IsFull)
                return null;

            var spawnPt = new Vector3(pt.x, pt.y, spawnRoot.position.z);

            mParms[OrganismEntity.parmForwardRandom] = true;

            var ent = mPool.Spawn<OrganismEntity>(poolSpawn, null, spawnPt, mParms);

            entities.Add(ent);

            return ent;
        }

        public OrganismEntity SpawnAt(Vector2 pt) {
            return SpawnAt(pt, Vector2.up);
        }

        public OrganismEntity SpawnAt(Vector2 pt, Vector2 forward) {
            if(entities.IsFull)
                return null;

            var spawnPt = new Vector3(pt.x, pt.y, spawnRoot.position.z);

            mParms[OrganismEntity.parmForwardRandom] = false;
            mParms[OrganismEntity.parmForward] = forward;

            var ent = mPool.Spawn<OrganismEntity>(poolSpawn, null, spawnPt, mParms);

            entities.Add(ent);

            return ent;
        }

        void OnDespawn(M8.PoolDataController pdc) {
            for(int i = 0; i < entities.Count; i++) {
                if(entities[i].poolControl == pdc) {
                    entities.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
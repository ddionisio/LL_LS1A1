using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismTemplateSpawner : MonoBehaviour {
        public M8.PoolController pool;
        public Transform spawnTo;

        public M8.CacheList<OrganismEntity> entities { get; private set; }
        public int entityCount { get { return entities != null ? entities.Count : 0; } }

        public int capacity { get { return entities.Capacity; } }

        public OrganismEntity template { get { return mTemplate; } }

        private OrganismEntity mTemplate;

        private M8.GenericParams mParms = new M8.GenericParams();

        private bool mIsPoolInit;

        public void Setup(OrganismTemplate organismTemplate, string templateName, string tag, int capacity) {
            if(!mIsPoolInit) {
                //generate pool
                if(!pool) {
                    pool = GetComponent<M8.PoolController>();
                    if(!pool)
                        pool = M8.PoolController.CreatePool(name, transform);
                }

                pool.despawnCallback += OnDespawn;

                mIsPoolInit = true;
            }
            else
                Destroy();

            //setup entity active list
            if(entities == null)
                entities = new M8.CacheList<OrganismEntity>(capacity);
            else if(entities.Capacity != capacity)
                entities.Resize(capacity);

            //create a GameObject template
            mTemplate = OrganismEntity.CreateTemplate(organismTemplate, templateName, tag, transform);
            mTemplate.gameObject.SetActive(false);

            //setup pool type
            pool.AddType(mTemplate.gameObject, capacity, capacity, spawnTo);
        }

        public void Destroy() {
            if(entities != null)
                entities.Clear();

            if(pool)
                pool.RemoveType(mTemplate.name);

            if(mTemplate) {
                DestroyImmediate(mTemplate.gameObject);
                mTemplate = null;
            }
        }

        public void Clear() {
            if(pool)
                pool.ReleaseAllByType(mTemplate.name);
        }

        public OrganismEntity SpawnAtRandomDir(Vector2 pt) {
            if(entities.IsFull)
                return null;

            var spawnPt = new Vector3(pt.x, pt.y, GameData.instance.organismDepth);

            mParms[OrganismEntity.parmForwardRandom] = true;

            var ent = pool.Spawn<OrganismEntity>(mTemplate.name, mTemplate.name, null, spawnPt, mParms);

            entities.Add(ent);

            return ent;
        }

        public OrganismEntity SpawnAt(Vector2 pt) {
            return SpawnAt(pt, Vector2.up);
        }

        public OrganismEntity SpawnAt(Vector2 pt, Vector2 forward) {
            if(entities.IsFull)
                return null;

            var spawnPt = new Vector3(pt.x, pt.y, GameData.instance.organismDepth);

            mParms[OrganismEntity.parmForwardRandom] = false;
            mParms[OrganismEntity.parmForward] = forward;

            var ent = pool.Spawn<OrganismEntity>(mTemplate.name, mTemplate.name, null, spawnPt, mParms);

            entities.Add(ent);

            return ent;
        }

        void OnDespawn(M8.PoolDataController pdc) {
            if(pdc.factoryKey == mTemplate.name) {
                for(int i = 0; i < entities.Count; i++) {
                    if(entities[i].poolControl == pdc) {
                        entities.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}
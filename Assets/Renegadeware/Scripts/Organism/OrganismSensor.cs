using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismSensor : MonoBehaviour {
        public const int cacheCapacity = 8;

        public float radius;

        public float delay = 0.15f;

        public M8.CacheList<OrganismEntity> organisms { get { return mOrganisms; } }
        public M8.CacheList<EnergySource> energies { get { return mEnergies; } }

        public event System.Action<OrganismSensor> refreshCallback;

        private OrganismStats mStats;
        private float mLastTime;

        private M8.CacheList<OrganismEntity> mOrganisms = new M8.CacheList<OrganismEntity>(cacheCapacity);
        private M8.CacheList<EnergySource> mEnergies = new M8.CacheList<EnergySource>(cacheCapacity);

        private Collider2D[] mCollCache = new Collider2D[cacheCapacity];
        private int mCollCount;

        public void Setup(OrganismStats stats) {
            mStats = stats;
        }

        void OnDisable() {
            mEnergies.Clear();
            mOrganisms.Clear();
        }

        void OnEnable() {
            mLastTime = Time.time;
            mCollCount = 0;
        }

        void Update() {
            var time = Time.time;
            if(time - mLastTime < delay)
                return;

            Vector2 pos = transform.position;

            var gameDat = GameData.instance;

            mEnergies.Clear();
            mOrganisms.Clear();

            mCollCount = Physics2D.OverlapCircle(pos, radius, gameDat.organismSensorContactFilter, mCollCache);
            for(int i = 0; i < mCollCount; i++) {
                var coll = mCollCache[i];

                if(coll.gameObject == gameObject)
                    continue;

                if(coll.CompareTag(gameDat.energyTag)) {
                    var energySrc = coll.GetComponent<EnergySource>();
                    if(energySrc && energySrc.isActive && mStats.EnergyMatch(energySrc.data))
                        mEnergies.Add(energySrc);
                }
                else if(M8.Util.CheckTag(coll, gameDat.organismEntityTags)) {
                    var ent = coll.GetComponent<OrganismEntity>();
                    if(ent)
                        mOrganisms.Add(ent);
                }
            }

            if(refreshCallback != null)
                refreshCallback.Invoke(this);

            mLastTime = time;
        }

        void OnDrawGizmos() {
            var pos = transform.position;

            if(radius > 0f) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(pos, radius);
            }
        }
    }
}
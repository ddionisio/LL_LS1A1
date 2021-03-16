using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismSensor : MonoBehaviour {
        public enum CheckMode {
            None,
            Energy,
            Organism
        }

        public const int cacheCapacity = 4;

        public float energyCheckRadius;
        public float organismCheckRadius;

        public float delay = 0.15f;

        private OrganismStats mStats;
        private float mLastTime;

        private M8.CacheList<OrganismEntity> mOrganisms;
        private M8.CacheList<EnergySource> mEnergies;

        private Collider2D[] mCollCache = new Collider2D[cacheCapacity];
        private int mCollCount;

        private CheckMode mCheckMode;

        public void Setup(OrganismStats stats) {
            mStats = stats;

            if(energyCheckRadius > 0f)
                mEnergies = new M8.CacheList<EnergySource>(cacheCapacity);

            if(organismCheckRadius > 0f)
                mOrganisms = new M8.CacheList<OrganismEntity>(cacheCapacity);
        }

        void OnDisable() {
            if(mEnergies != null)
                mEnergies.Clear();
            if(mOrganisms != null)
                mOrganisms.Clear();
        }

        void OnEnable() {
            mLastTime = Time.time;
            mCollCount = 0;

            mCheckMode = CheckMode.None;
            ApplyNextMode();
        }

        void Update() {
            if(mCheckMode == CheckMode.None)
                return;

            var time = Time.time;
            if(time - mLastTime < delay)
                return;

            Vector2 pos = transform.position;

            var gameDat = GameData.instance;

            //alternate between checking for energy and organisms
            switch(mCheckMode) {
                case CheckMode.Energy:
                    mEnergies.Clear();

                    mCollCount = Physics2D.OverlapCircle(pos, energyCheckRadius, gameDat.organismEnergyFilter, mCollCache);
                    for(int i = 0; i < mCollCount; i++) {
                        var coll = mCollCache[i];

                        if(!coll.CompareTag(gameDat.energyTag))
                            continue;

                        var energySrc = coll.GetComponent<EnergySource>();
                        if(!energySrc)
                            continue;

                        if(!mStats.EnergyMatch(energySrc.data))
                            continue;

                        mEnergies.Add(energySrc);
                    }
                    break;

                case CheckMode.Organism:
                    mOrganisms.Clear();

                    mCollCount = Physics2D.OverlapCircle(pos, organismCheckRadius, gameDat.organismContactFilter, mCollCache);
                    for(int i = 0; i < mCollCount; i++) {
                        var coll = mCollCache[i];

                        if(coll.gameObject == gameObject)
                            continue;

                        if(!M8.Util.CheckTag(coll, gameDat.organismEntityTags))
                            continue;

                        var ent = coll.GetComponent<OrganismEntity>();
                        if(!ent)
                            continue;

                        mOrganisms.Add(ent);
                    }
                    break;
            }

            mLastTime = time;
            ApplyNextMode();
        }

        private void ApplyNextMode() {
            switch(mCheckMode) {
                case CheckMode.None:
                    if(energyCheckRadius > 0f)
                        mCheckMode = CheckMode.Energy;
                    else if(organismCheckRadius > 0f)
                        mCheckMode = CheckMode.Organism;
                    break;

                case CheckMode.Energy:
                    if(organismCheckRadius > 0f)
                        mCheckMode = CheckMode.Organism;
                    break;

                case CheckMode.Organism:
                    if(energyCheckRadius > 0f)
                        mCheckMode = CheckMode.Energy;
                    break;
            }
        }

        void OnDrawGizmos() {
            var pos = transform.position;

            if(organismCheckRadius > 0f) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pos, organismCheckRadius);
            }

            if(energyCheckRadius > 0f) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(pos, energyCheckRadius);
            }
        }
    }
}
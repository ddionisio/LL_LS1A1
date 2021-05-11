using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public abstract class OrganismField : MonoBehaviour {
        [Header("General")]
        public int capacity = 32;
        public float refreshDelay = 0.3f;

        public Collider2D fieldCollider { get; private set; }
        public M8.CacheList<OrganismEntity> entities { get { return mEntities; } }

        private Collider2D[] mColls;
        private M8.CacheList<OrganismEntity> mEntities;

        private float mLastTime;

        protected abstract void UpdateEntity(OrganismEntity ent, float timeDelta);

        protected virtual void OnDisable() {
            if(mEntities != null)
                mEntities.Clear();
        }

        protected virtual void OnEnable() {
            mLastTime = Time.time;
        }

        protected virtual void Awake() {
            fieldCollider = GetComponent<Collider2D>();

            mColls = new Collider2D[capacity];
            mEntities = new M8.CacheList<OrganismEntity>(capacity);
        }

        void Update() {
            var gameDat = GameData.instance;

            var time = Time.time;
            var dt = Time.deltaTime;

            if(time - mLastTime >= refreshDelay) {
                mLastTime = time;

                mEntities.Clear();

                var collCount = fieldCollider.OverlapCollider(gameDat.organismEntityContactFilter, mColls);
                for(int i = 0; i < collCount; i++) {
                    var ent = mColls[i].GetComponent<OrganismEntity>();
                    if(ent)
                        mEntities.Add(ent);
                }
            }

            for(int i = mEntities.Count - 1; i >= 0; i--) {
                var ent = mEntities[i];
                if(ent && !ent.isReleased && !ent.physicsLocked)
                    UpdateEntity(ent, dt);
                else
                    mEntities.RemoveAt(i);
            }
        }
    }
}
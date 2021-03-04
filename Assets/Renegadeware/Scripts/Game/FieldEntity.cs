using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public abstract class FieldEntity : MonoBehaviour {
        public const int contactCapacity = 512;

        [Header("General Settings")]
        public float updateDelay = 0.3f;

        private static Collider2D[] mContacts = new Collider2D[contactCapacity];
        private static bool mContactsLocked;

        private Collider2D mColl;

        /// <summary>
        /// Do general update, return true if we want to process contacts
        /// </summary>
        protected abstract bool Update();

        protected abstract void UpdateEntity(OrganismEntity ent);

        protected virtual void OnEnable() {
            StartCoroutine(DoUpdate());
        }

        protected virtual void Awake() {
            mColl = GetComponent<Collider2D>();
        }

        IEnumerator DoUpdate() {
            YieldInstruction wait = updateDelay > 0f ? new WaitForSeconds(updateDelay) : null;

            while(true) {
                yield return wait;

                if(!Update())
                    continue;

                //NOTE: assume asynchronous, or if we ever need this to be asynchronous
                while(mContactsLocked)
                    yield return null;

                mContactsLocked = true;

                int contactCount = mColl.GetContacts(GameData.instance.organismContactFilter, mContacts);
                for(int i = 0; i < contactCount; i++) {
                    var coll = mContacts[i];

                    var ent = coll.GetComponent<OrganismEntity>();
                    if(ent)
                        UpdateEntity(ent);
                }

                mContactsLocked = false;
            }
        }
    }
}
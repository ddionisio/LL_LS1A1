using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public abstract class OrganismHunter : OrganismComponent {
        [Header("Hunter Info")]
        public string anchorContainer = "hunt"; //anchors where to store consumed endobiotics


    }

    public abstract class OrganismHunterControl : OrganismComponentControl {

        private List<Transform> mEntityAnchors;
        private M8.CacheList<OrganismEntity> mEntityEndobiotics;

        public bool isFull {
            get {
                return mEntityEndobiotics != null && mEntityEndobiotics.IsFull;
            }
        }

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            base.Init(ent, owner);

            var hunterComp = owner as OrganismHunter;

            mEntityAnchors = ent.bodyDisplay.GetAnchors(hunterComp.anchorContainer);

            if(mEntityAnchors != null)
                mEntityEndobiotics = new M8.CacheList<OrganismEntity>(mEntityAnchors.Count);
        }

        public override void Spawn(M8.GenericParams parms) {
            
        }

        public override void Despawn() {
            //clear out and release consumed entities
            if(mEntityEndobiotics != null) {
                for(int i = 0; i < mEntityEndobiotics.Count; i++) {
                    var ent = mEntityEndobiotics[i];
                    if(ent && !ent.isReleased)
                        ent.Release();
                }

                mEntityEndobiotics.Clear();
            }
        }

        public override void Update() {
            //feed consumed endobiotics
            //if()
        }
    }
}
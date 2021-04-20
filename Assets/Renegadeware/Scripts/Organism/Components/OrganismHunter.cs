using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public abstract class OrganismHunter : OrganismComponent {
    }

    public abstract class OrganismHunterControl : OrganismComponentControl {
        private List<Transform> mEndobioticAnchors;
        private int mEndobioticAnchorIndex; //current available index

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            base.Init(ent, owner);

            mEndobioticAnchors = ent.bodyDisplay.GetAnchors(GameData.instance.organismAnchorEndobiotic);
        }

        public override void Spawn(M8.GenericParams parms) {
            mEndobioticAnchorIndex = 0;            
        }

        public void Eat(OrganismEntity target) {
            if(entity.isReleased || entity.stats.energyLocked || entity.stats.energy == 0f) //dead, don't eat
                return;

            if(target.isReleased) //fail-safe
                return;

            var targetStats = target.stats;

            if((targetStats.flags & OrganismFlag.Endobiotic) == OrganismFlag.Endobiotic) {
                var anchor = mEndobioticAnchors[mEndobioticAnchorIndex];
                mEndobioticAnchorIndex++;
                if(mEndobioticAnchorIndex == mEndobioticAnchors.Count)
                    mEndobioticAnchorIndex = 0;

                var tgtBodyCtrl = target.GetComponentControl<OrganismBodySingleCellControl>();
                if(tgtBodyCtrl != null) {
                    tgtBodyCtrl.EndobioticAttach(entity, anchor);
                }
                else { //fail-safe
                    target.Release();
                }
            }
            else if(targetStats.toxic > 0f && (entity.stats.flags & OrganismFlag.ToxicImmunity) == 0) { //poison
                entity.stats.energy -= targetStats.toxic;
                target.Release();
            }
            else {
                entity.stats.energyConsume += targetStats.energy;
                target.Release();
            }
        }
    }
}
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

        public void Eat(OrganismEntity ent) {
            var entStats = ent.stats;

            if((entStats.flags & OrganismFlag.Endobiotic) == OrganismFlag.Endobiotic) {
                var anchor = mEndobioticAnchors[mEndobioticAnchorIndex];
                mEndobioticAnchorIndex++;
                if(mEndobioticAnchorIndex == mEndobioticAnchors.Count)
                    mEndobioticAnchorIndex = 0;

                var entBodyCtrl = ent.GetComponentControl<OrganismBodySingleCellControl>();
                if(entBodyCtrl != null) {
                    entBodyCtrl.EndobioticAttach(entity, anchor);
                }
                else { //just eat it
                    ent.stats.energyConsume += entStats.energy;
                    ent.Release();
                }
            }
            else {
                ent.stats.energyConsume += entStats.energy;
                ent.Release();
            }
        }
    }
}
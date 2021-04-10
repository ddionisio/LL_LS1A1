using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismFieldKill : OrganismField {

        protected override void UpdateEntity(OrganismEntity ent, float timeDelta) {
            if(!ent.stats.isLifeExpired)
                ent.stats.ForcedLifeExpire();
        }
    }
}
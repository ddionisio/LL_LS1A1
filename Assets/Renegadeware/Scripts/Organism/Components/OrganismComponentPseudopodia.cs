using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "pseudopodia", menuName = "Game/Organism/Component/Pseudopodia")]
    public class OrganismComponentPseudopodia : OrganismComponent {
        public override OrganismComponentControl GenerateControl(OrganismEntity organismEntity) {
            return new OrganismComponentPseudopodiaControl();
        }
    }

    public class OrganismComponentPseudopodiaControl : OrganismComponentControl {

        public override void Update() {
            
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "cellbody", menuName = "Game/Organism Component/Body")]
    public class OrganismBody : OrganismComponent {
        [Header("Body Info")]
        public OrganismComponentGroup[] componentGroups;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "organismComponentGroup", menuName = "Game/Organism Component Group")]
    public class OrganismComponentGroup : InfoData {
        [Header("Components")]
        public OrganismComponent[] components;
    }
}
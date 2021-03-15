using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "motility", menuName = "Game/Organism/Component/Motility")]
    public class OrganismComponentMotility : OrganismComponent {
        [Header("Motility")]
        public float forwardSpeed = 1f;
        public float turnSpeed = 5f;
        public float changeDelay = 1f;

        public override OrganismComponentControl GenerateControl(OrganismEntity organismEntity) {
            return new OrganismComponentMotilityControl();
        }
    }

    public class OrganismComponentMotilityControl : OrganismComponentControl {
        public override void Despawn(OrganismEntity ent) {
        }

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
        }

        public override void Spawn(OrganismEntity ent, M8.GenericParams parms) {
        }

        public override void Update(OrganismEntity ent) {
        }
    }
}
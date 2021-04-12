using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "bodyMagnoliophyta", menuName = "Game/Organism/Component/Body Magnoliophyta")]
    public class OrganismBodyMagnoliophyta : OrganismBody {
        [Header("Magnoliophyta")]
        public float waterCapacity;
        public float lightCapacity;

        public override OrganismComponentControl GenerateControl(OrganismEntity organismEntity) {
            return new OrganismBodyMagnoliophytaControl();
        }
    }

    public class OrganismBodyMagnoliophytaControl : OrganismComponentControl {
        public float nutrient;

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            base.Init(ent, owner);


        }

        public override void Update() {
            
        }
    }
}
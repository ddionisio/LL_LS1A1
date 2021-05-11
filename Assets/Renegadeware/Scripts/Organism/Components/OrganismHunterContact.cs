using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "hunterContact", menuName = "Game/Organism/Component/Hunter Contact")]
    public class OrganismHunterContact : OrganismHunter {
        [Header("Hunter Contact")]
        public float contactAngle = 35f; //set to 360 for no restriction

        public override OrganismComponentControl GenerateControl(OrganismEntity organismEntity) {
            return new OrganismHunterContactControl();
        }
    }

    public class OrganismHunterContactControl : OrganismHunterControl {
        private OrganismHunterContact mComp;

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            base.Init(ent, owner);

            mComp = owner as OrganismHunterContact;
        }

        public override void Update() {
            //check for contacts, eat anything within angle range based on forward
            if(!entity.stats.energyLocked && entity.contactOrganisms.Count > 0) {
                var pos = entity.position;
                var fwd = entity.forward;

                for(int i = 0; i < entity.contactOrganisms.Count; i++) {
                    var contactEnt = entity.contactOrganisms[i];

                    if(contactEnt.isReleased || contactEnt.physicsLocked || contactEnt.stats.energy == 0f || !entity.stats.CanEat(contactEnt.stats) || entity.IsMatchTemplate(contactEnt))
                        continue;

                    if(mComp.contactAngle < 360f) {
                        var dpos = contactEnt.position - pos;
                        var dir = dpos.normalized;

                        if(Vector2.Angle(fwd, dir) <= mComp.contactAngle)
                            Eat(contactEnt);
                    }
                    else
                        Eat(contactEnt);
                }
            }
        }
    }
}
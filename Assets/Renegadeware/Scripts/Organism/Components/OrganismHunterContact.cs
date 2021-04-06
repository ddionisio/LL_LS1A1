using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "hunter", menuName = "Game/Organism/Component/Hunter Contact")]
    public class OrganismHunterContact : OrganismHunter {
        [Header("Hunter Contact")]
        public float contactAngle = 35f;

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
                    var ent = entity.contactOrganisms[i];

                    if(ent.isReleased || ent.physicsLocked || ent.stats.energy == 0f || entity.stats.danger < ent.stats.danger || entity.IsMatchTemplate(ent))
                        continue;

                    var dpos = ent.position - pos;
                    var dir = dpos.normalized;

                    if(Vector2.Angle(fwd, dir) <= mComp.contactAngle)
                        Eat(ent);
                }
            }
        }
    }
}
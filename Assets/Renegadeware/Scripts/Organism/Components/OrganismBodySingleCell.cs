using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "body", menuName = "Game/Organism/Component/Body SingleCell")]
    public class OrganismBodySingleCell : OrganismBody {
        public override OrganismComponentControl GenerateControl(OrganismEntity organismEntity) {
            return new OrganismBodySingleCellControl();
        }
    }

    public class OrganismBodySingleCellControl : OrganismComponentControl {
        //private OrganismBodySingleCell mComp;

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            //mComp = owner as OrganismBodySingleCell;
        }

        public override void Spawn(OrganismEntity ent, M8.GenericParams parms) {
            
        }

        public override void Update(OrganismEntity ent) {
            //update velocity

            //do separation
            var separateVel = Vector2.zero;

            var pos = ent.position;

            for(int i = 0; i < ent.contactOrganisms.Count; i++) {
                var otherEnt = ent.contactOrganisms[i];

                if(otherEnt.bodyComponent == null || ent.stats.mass <= otherEnt.stats.mass) //exclude organisms with lesser mass
                    separateVel += pos - otherEnt.position;
            }

            ent.velocity += separateVel;

            //bounce from solid?
            //TODO: stick to solid? (e.g. philli hooks)
            if(ent.solidHitCount > 0) {
                var moveDir = ent.velocityDir;

                for(int i = 0; i < ent.solidHitCount; i++) {
                    var solidHit = ent.solidHits[i];
                    moveDir = Vector2.Reflect(moveDir, solidHit.normal);
                }

                ent.velocity += moveDir * ent.speed;
            }
        }

        public override void Despawn(OrganismEntity ent) {
            
        }
    }
}
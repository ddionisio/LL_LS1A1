using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "body", menuName = "Game/Organism/Component/Body")]
    public class OrganismBody : OrganismComponent, IVelocityAdd {
        [Header("Templates")]
        [SerializeField]
        GameObject _editPrefab = null;
        [SerializeField]
        GameObject _gamePrefab = null;

        [Header("Body Info")]
        public OrganismComponent[] componentEssentials; //essential organelles for this body (used after picking body the first time)
        public OrganismComponentGroup[] componentGroups;

        public override GameObject editPrefab => _editPrefab;
        public override GameObject gamePrefab => _gamePrefab;

        public int GetComponentEssentialIndex(int id) {
            for(int i = 0; i < componentEssentials.Length; i++) {
                if(componentEssentials[i].ID == id)
                    return i;
            }

            return -1;
        }

        public virtual Vector2 OnAddVelocity(OrganismEntity entity) {
            //do separation
            var separateVel = Vector2.zero;

            var pos = entity.position;
            
            for(int i = 0; i < entity.contactOrganisms.Count; i++) {
                var otherEnt = entity.contactOrganisms[i];

                if(otherEnt.bodyComponent == null || entity.stats.mass <= otherEnt.stats.mass) //exclude organisms with lesser mass
                    separateVel += pos - otherEnt.position;
            }

            //bounce from solid?
            //TODO: stick to solid? (e.g. philli hooks)
            var solidVel = Vector2.zero;

            if(entity.solidHitCount > 0) {
                var moveDir = entity.velocityDir;

                for(int i = 0; i < entity.solidHitCount; i++) {
                    var solidHit = entity.solidHits[i];
                    moveDir = Vector2.Reflect(moveDir, solidHit.normal);
                }

                solidVel = moveDir * entity.speed;
            }

            return separateVel + solidVel;
        }
    }
}
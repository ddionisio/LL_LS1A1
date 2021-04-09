using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "hunterVacuum", menuName = "Game/Organism/Component/Hunter Vacuum")]
    public class OrganismHunterVacuum : OrganismHunter {
        [Header("Vacuum Settings")]
        public float vacuumAccel;
        public float vacuumRadius;
        public float vacuumAngle;

        public override OrganismComponentControl GenerateControl(OrganismEntity organismEntity) {
            return new OrganismHunterVacuumControl();
        }
    }

    public class OrganismHunterVacuumControl : OrganismHunterControl {
        private OrganismHunterVacuum mComp;

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            base.Init(ent, owner);

            mComp = owner as OrganismHunterVacuum;
        }

        public override void Spawn(M8.GenericParams parms) {
            base.Spawn(parms);
        }

        public override void Despawn() {
        }

        public override void Update() {
            if(entity.stats.energyLocked || entity.physicsLocked)
                return;

            var fwd = entity.forward;
            var pos = entity.position;
            var vacuumAngle = mComp.vacuumAngle;

            //eat any in contact
            for(int i = 0; i < entity.contactOrganisms.Count; i++) {
                var contactEnt = entity.contactOrganisms[i];

                if(contactEnt.isReleased || contactEnt.physicsLocked || contactEnt.stats.energy == 0f || !entity.stats.CanEat(contactEnt.stats) || entity.IsMatchTemplate(contactEnt))
                    continue;

                //check vacuum angle range
                var dpos = contactEnt.position - pos;
                var dir = dpos.normalized;

                if(Vector2.Angle(fwd, dir) <= vacuumAngle)
                    Eat(contactEnt);
            }

            //suck in entities within sensor
            var sensor = entity.sensor;
            if(sensor) {
                var dt = Time.deltaTime;

                var vacuumAccel = mComp.vacuumAccel;
                var vacuumDistSqr = mComp.vacuumRadius * mComp.vacuumRadius;

                Vector2 accel = Vector2.zero;

                for(int i = 0; i < sensor.organisms.Count; i++) {
                    var sensorEnt = sensor.organisms[i];

                    if(sensorEnt.isReleased || sensorEnt.physicsLocked || !entity.stats.CanEat(sensorEnt.stats) || entity.IsMatchTemplate(sensorEnt))
                        continue;

                    var dpos = sensorEnt.position - pos;
                    var distSqr = dpos.sqrMagnitude;

                    //check range
                    if(distSqr > 0f && distSqr <= vacuumDistSqr) {
                        var dist = Mathf.Sqrt(distSqr);
                        var dir = dpos / dist;

                        //check vacuum angle range
                        if(Vector2.Angle(fwd, dir) <= vacuumAngle) {
                            //suck in
                            sensorEnt.velocity -= dir * (vacuumAccel * dt);

                            //move towards
                            accel += dir * entity.stats.forwardAccel;
                        }
                    }
                }

                if(accel != Vector2.zero)
                    entity.velocity += accel * dt;
            }
        }
    }
}
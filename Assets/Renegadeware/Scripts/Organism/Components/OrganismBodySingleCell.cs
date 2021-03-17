using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "body", menuName = "Game/Organism/Component/Body SingleCell")]
    public class OrganismBodySingleCell : OrganismBody {
        [Header("Single Cell Settings")]
        public float energyConsumeRate = 1f; //energy consumed from sources per second

        [Header("Death Settings")]
        public Color deathTint = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        public override OrganismComponentControl GenerateControl(OrganismEntity organismEntity) {
            return new OrganismBodySingleCellControl();
        }
    }

    public class OrganismBodySingleCellControl : OrganismComponentControl {
        public enum State {
            Normal,

            DeathWait,
            Death,

            Divide
        }

        private OrganismBodySingleCell mComp;

        private State mState;

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            mComp = owner as OrganismBodySingleCell;
        }

        public override void Spawn(OrganismEntity ent, M8.GenericParams parms) {
            mState = State.Normal;
        }

        public override void Despawn(OrganismEntity ent) {
        }

        public override void Update(OrganismEntity ent) {
            var gameDat = GameData.instance;

            switch(mState) {
                case State.Normal:
                    //check for death
                    if(ent.stats.isLifeExpired) {
                        if(ent.animator)
                            ent.animator.Stop();

                        if(ent.bodyDisplay.colorGroup)
                            ent.bodyDisplay.colorGroup.color *= mComp.deathTint;

                        ent.stats.energyLocked = true;

                        mState = State.DeathWait;
                    }
                    //ran out of energy?
                    else if(ent.stats.energy == 0f) {
                        if(ent.animator && !string.IsNullOrEmpty(gameDat.organismTakeDeath))
                            ent.animator.Play(gameDat.organismTakeDeath);

                        ent.stats.energyLocked = true;

                        mState = State.Death;
                    }
                    //ready to divide?
                    else if(ent.stats.isEnergyFull) {
                        if(!GameModePlay.instance.gameSpawner.entities.IsFull) {//don't allow divide if capacity is full
                            if(ent.animator && !string.IsNullOrEmpty(gameDat.organismTakeReproduce))
                                ent.animator.Play(gameDat.organismTakeReproduce);

                            ent.stats.energyLocked = true;

                            mState = State.Divide;
                        }
                    }
                    else {
                        var energyConsume = mComp.energyConsumeRate * Time.deltaTime;

                        //check for energy source contacts and absorb its energy
                        for(int i = 0; i < ent.contactEnergies.Count; i++) {
                            var energySrc = ent.contactEnergies[i];

                            float energyAmt;
                            if(ent.stats.energy + energyConsume < ent.stats.energyCapacity)
                                energyAmt = energyConsume;
                            else //cap
                                energyAmt = ent.stats.energyCapacity - (ent.stats.energy + energyConsume);

                            energySrc.energy -= energyAmt;
                            ent.stats.energy += energyAmt;

                            if(ent.stats.isEnergyFull)
                                break;
                        }
                    }
                    break;

                case State.DeathWait:
                    if(Time.time - ent.stats.spawnTimeElapsed >= ent.stats.lifespan + gameDat.organismDeathDelay) {
                        if(ent.animator && !string.IsNullOrEmpty(gameDat.organismTakeDeath))
                            ent.animator.Play(gameDat.organismTakeDeath);

                        mState = State.Death;
                    }
                    break;

                case State.Death:
                    if(!ent.animator || !ent.animator.isPlaying) {
                        ent.Release();
                        return;
                    }
                    break;

                case State.Divide:
                    if(!ent.animator || !ent.animator.isPlaying) {
                        var spawner = GameModePlay.instance.gameSpawner;

                        var forward = ent.forward;

                        //split horizontally
                        var dist = ent.size.x * 0.3f;

                        //get two spawn point
                        var pt1 = ent.SolidClip(ent.left, dist);
                        var pt2 = ent.SolidClip(ent.right, dist);

                        //release this
                        ent.Release();

                        //spawn two
                        spawner.SpawnAt(pt1, forward);
                        spawner.SpawnAt(pt2, forward);

                        return;
                    }
                    break;
            }

            //update velocity
            if(!ent.physicsLocked) {
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
                    /*var moveDir = ent.velocityDir;

                    for(int i = 0; i < ent.solidHitCount; i++) {
                        var solidHit = ent.solidHits[i];
                        moveDir = Vector2.Reflect(moveDir, solidHit.normal);
                    }

                    ent.velocity += moveDir * ent.speed;*/
                    var spd = ent.speed;
                    if(spd > 0f) {
                        Vector2 normalSum = Vector2.zero;
                        for(int i = 0; i < ent.solidHitCount; i++)
                            normalSum += ent.solidHits[i].normal;

                        ent.velocity += normalSum * spd * ent.stats.velocityReceiveScale;
                    }
                }
            }
        }
    }
}
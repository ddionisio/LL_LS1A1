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

        public override void Spawn(M8.GenericParams parms) {
            mState = State.Normal;
        }

        public override void Despawn() {
        }

        public override void Update() {
            var gameDat = GameData.instance;

            switch(mState) {
                case State.Normal:
                    //check for death
                    if(entity.stats.isLifeExpired) {
                        if(entity.animator)
                            entity.animator.Stop();

                        if(entity.bodyDisplay.colorGroup)
                            entity.bodyDisplay.colorGroup.color *= mComp.deathTint;

                        entity.stats.energyLocked = true;

                        mState = State.DeathWait;
                    }
                    //ran out of energy?
                    else if(entity.stats.energy == 0f) {
                        if(entity.animator && !string.IsNullOrEmpty(gameDat.organismTakeDeath))
                            entity.animator.Play(gameDat.organismTakeDeath);

                        entity.stats.energyLocked = true;

                        mState = State.Death;
                    }
                    //ready to divide?
                    else if(entity.stats.isEnergyFull) {
                        if(!GameModePlay.instance.gameSpawner.entities.IsFull) {//don't allow divide if capacity is full
                            if(entity.animator && !string.IsNullOrEmpty(gameDat.organismTakeReproduce))
                                entity.animator.Play(gameDat.organismTakeReproduce);

                            entity.stats.energyLocked = true;

                            mState = State.Divide;
                        }
                    }
                    else {
                        var energyConsume = mComp.energyConsumeRate * Time.deltaTime;

                        //check for energy source contacts and absorb its energy
                        for(int i = 0; i < entity.contactEnergies.Count; i++) {
                            var energySrc = entity.contactEnergies[i];

                            float energyAmt;
                            if(entity.stats.energy + energyConsume < entity.stats.energyCapacity)
                                energyAmt = energyConsume;
                            else //cap
                                energyAmt = entity.stats.energyCapacity - (entity.stats.energy + energyConsume);

                            energySrc.energy -= energyAmt;
                            entity.stats.energy += energyAmt;

                            if(entity.stats.isEnergyFull)
                                break;
                        }
                    }
                    break;

                case State.DeathWait:
                    if(Time.time - entity.stats.spawnTimeElapsed >= entity.stats.lifespan + gameDat.organismDeathDelay) {
                        if(entity.animator && !string.IsNullOrEmpty(gameDat.organismTakeDeath))
                            entity.animator.Play(gameDat.organismTakeDeath);

                        mState = State.Death;
                    }
                    break;

                case State.Death:
                    if(!entity.animator || !entity.animator.isPlaying) {
                        entity.Release();
                        return;
                    }
                    break;

                case State.Divide:
                    if(!entity.animator || !entity.animator.isPlaying) {
                        var spawner = GameModePlay.instance.gameSpawner;

                        var forward = entity.forward;

                        //split horizontally
                        var dist = entity.size.x * 0.3f;

                        //get two spawn point
                        var pt1 = entity.SolidClip(entity.left, dist);
                        var pt2 = entity.SolidClip(entity.right, dist);

                        //release this
                        entity.Release();

                        //spawn two
                        spawner.SpawnAt(pt1, forward);
                        spawner.SpawnAt(pt2, forward);

                        return;
                    }
                    break;
            }

            //update velocity
            if(!entity.physicsLocked) {
                //do separation
                var separateVel = Vector2.zero;

                var pos = entity.position;

                for(int i = 0; i < entity.contactOrganisms.Count; i++) {
                    var otherEnt = entity.contactOrganisms[i];

                    if(otherEnt.bodyComponent == null || entity.stats.mass <= otherEnt.stats.mass) //exclude organisms with lesser mass
                        separateVel += pos - otherEnt.position;
                }

                entity.velocity += separateVel;

                //bounce from solid?
                //TODO: stick to solid? (e.g. philli hooks)
                if(entity.solidHitCount > 0) {
                    /*var moveDir = ent.velocityDir;

                    for(int i = 0; i < ent.solidHitCount; i++) {
                        var solidHit = ent.solidHits[i];
                        moveDir = Vector2.Reflect(moveDir, solidHit.normal);
                    }

                    ent.velocity += moveDir * ent.speed;*/
                    var spd = entity.speed;
                    if(spd > 0f) {
                        Vector2 normalSum = Vector2.zero;
                        for(int i = 0; i < entity.solidHitCount; i++)
                            normalSum += entity.solidHits[i].normal;

                        entity.velocity += normalSum * spd * entity.stats.velocityReceiveScale;
                    }
                }
            }
        }
    }
}
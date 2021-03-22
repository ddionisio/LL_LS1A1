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
        public const int stickyCapacity = 4;

        public enum State {
            Normal,

            DeathWait,
            Death,

            Divide
        }

        private OrganismBodySingleCell mComp;

        private M8.CacheList<Collider2D> mStickies;

        private State mState;

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            base.Init(ent, owner);

            mComp = owner as OrganismBodySingleCell;

            if((ent.stats.flags & OrganismFlag.Sticky) == OrganismFlag.Sticky)
                mStickies = new M8.CacheList<Collider2D>(stickyCapacity);
        }

        public override void Spawn(M8.GenericParams parms) {
            mState = State.Normal;
        }

        public override void Despawn() {
            if(mStickies != null)
                mStickies.Clear();
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

                        entity.velocity = Vector2.zero;
                        entity.angularVelocity = 0f;
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

                            entity.velocity = Vector2.zero;
                            entity.angularVelocity = 0f;
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

                        if(mStickies != null) {
                            if(!entity.physicsLocked) {
                                //update stickies
                                for(int i = mStickies.Count - 1; i >= 0; i--) {
                                    var sticky = mStickies[i];

                                    //check if invalid
                                    if(!sticky) {
                                        mStickies.RemoveLast();
                                        continue;
                                    }

                                    var distInfo = entity.bodyCollider.Distance(sticky);
                                    if(!distInfo.isValid) {
                                        mStickies.RemoveLast();
                                        continue;
                                    }

                                    if(distInfo.distance <= 0f)
                                        continue;

                                    //disconnect if distance is too far
                                    if(distInfo.distance >= gameDat.organismStickyDistanceThreshold) {
                                        mStickies.RemoveLast();
                                        continue;
                                    }

                                    //move towards
                                    entity.velocity += distInfo.normal * gameDat.organismSeparateSpeed;//distInfo.distance;
                                }

                                //check for new stickies
                                for(int i = 0; !mStickies.IsFull && i < entity.contactCount; i++) {
                                    var coll = entity.contactColliders[i];
                                    if(coll && coll.enabled && !coll.isTrigger && !mStickies.Exists(coll))
                                        mStickies.Add(coll);
                                }
                            }
                            else
                                mStickies.Clear();
                        }
                    }
                    break;

                case State.DeathWait:
                    if(entity.stats.spawnTimeElapsed >= entity.stats.lifespan + gameDat.organismDeathDelay) {
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
                        var pt1 = entity.left * dist;
                        var pt2 = entity.right * dist;

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
            if(!entity.physicsLocked && entity.contactCount > 0) {
                //do separation
                var separateVel = Vector2.zero;

                for(int i = 0; i < entity.contactCount; i++) {
                    var distInfo = entity.contactDistances[i];
                    if(distInfo.isValid && distInfo.distance < 0f)
                        separateVel -= distInfo.normal * gameDat.organismSeparateSpeed;//distInfo.distance;
                }

                entity.velocity += separateVel;
            }
        }
    }
}
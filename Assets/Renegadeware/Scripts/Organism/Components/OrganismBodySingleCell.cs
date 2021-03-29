using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "body", menuName = "Game/Organism/Component/Body SingleCell")]
    public class OrganismBodySingleCell : OrganismBody {
        public enum SplitMode {
            Horizontal,
            Vertical
        }

        [Header("Split Settings")]
        public SplitMode splitMode = SplitMode.Horizontal;

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

        public bool isStickied { get { return mStickies != null && mStickies.Count > 0; } }

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
                    else if(mStickies != null) {
                        if(!entity.physicsLocked) {
                            //update stickies
                            for(int i = mStickies.Count - 1; i >= 0; i--) {
                                var sticky = mStickies[i];

                                //check if invalid
                                if(!(sticky && sticky.enabled)) {
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
                                entity.velocity += distInfo.normal * gameDat.organismStickySpeed;//distInfo.distance;
                            }

                            //check for new stickies

                            //only stick to solid energy compatibles
                            for(int i = 0; !mStickies.IsFull && i < entity.contactEnergies.Count; i++) {
                                var energySrc = entity.contactEnergies[i];

                                if(energySrc.isActive && energySrc.isSolid && !mStickies.Exists(energySrc.bodyCollider))
                                    mStickies.Add(energySrc.bodyCollider);
                            }

                            //only stick to same organisms
                            for(int i = 0; !mStickies.IsFull && i < entity.contactOrganismMatches.Count; i++) {
                                var contactEntity = entity.contactOrganismMatches[i];
                                if(!contactEntity.isReleased && !contactEntity.physicsLocked) {
                                    //ensure contacted is not already stuck to us
                                    var bodySingleCellCtrl = contactEntity.GetComponentControl<OrganismBodySingleCellControl>();
                                    if(bodySingleCellCtrl != null && !bodySingleCellCtrl.mStickies.Exists(entity.bodyCollider))
                                        mStickies.Add(contactEntity.bodyCollider);
                                }
                            }
                        }
                        else
                            mStickies.Clear();
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

                        var entPt = entity.position;

                        var forward = entity.forward;

                        Vector2 pt1, pt2;
                        float dist;

                        switch(mComp.splitMode) {
                            case OrganismBodySingleCell.SplitMode.Horizontal:
                                dist = entity.size.x * 0.25f;

                                pt1 = entPt + entity.left * dist;
                                pt2 = entPt + entity.right * dist;
                                break;

                            default:
                                dist = entity.size.y * 0.25f;

                                pt1 = entPt + entity.forward * dist;
                                pt2 = entPt - entity.forward * dist;
                                break;
                        }

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
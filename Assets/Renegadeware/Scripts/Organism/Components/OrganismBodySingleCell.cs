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

        public struct StickyInfo {
            public OrganismEntity entity;
            public Collider2D coll;

            public bool isValid {
                get {
                    if(coll && coll.enabled) {
                        if(entity) {
                            if(!entity.stats.energyLocked)// && entity.stats.energyDelta > 0f)
                                return true;
                        }
                        else
                            return true;
                    }

                    return false;
                }
            }

            public bool isSolid { get { return coll && !entity; } }
        }

        public bool isStickied { get { return mStickies != null && mStickies.Count > 0; } }

        private OrganismBodySingleCell mComp;

        private M8.CacheList<StickyInfo> mStickies;

        private OrganismEntity mEndobioticHost;

        private Transform mParentSpawned;
        private Vector2 mScale;

        private State mState;

        private int mTakeDeathInd;
        private int mTakeDivideInd;

        public void EndobioticAttach(OrganismEntity host, Transform anchor) {
            mEndobioticHost = host;

            entity.transform.SetParent(anchor, true);
            entity.transform.localScale = mScale;
            entity.transform.localPosition = Vector3.zero;

            entity.physicsLocked = true;
        }

        public void EndobioticDetach() {
            entity.transform.SetParent(mParentSpawned, true);
            entity.transform.localScale = mScale;

            entity.physicsLocked = false;

            mEndobioticHost = null;
        }

        public bool IsStickTo(Collider2D coll) {
            if(mStickies != null) {
                for(int i = 0; i < mStickies.Count; i++) {
                    if(mStickies[i].coll == coll)
                        return true;
                }
            }
            return false;
        }

        public bool IsStickTo(OrganismEntity ent) {
            if(mStickies != null) {
                for(int i = 0; i < mStickies.Count; i++) {
                    if(mStickies[i].entity == ent)
                        return true;
                }
            }
            return false;
        }

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            base.Init(ent, owner);

            mComp = owner as OrganismBodySingleCell;

            mScale = ent.transform.localScale;

            if((ent.stats.flags & (OrganismFlag.Sticky | OrganismFlag.StickySolid)) != 0)
                mStickies = new M8.CacheList<StickyInfo>(stickyCapacity);

            //animation
            if(ent.animator) {
                mTakeDeathInd = ent.animator.GetTakeIndex(GameData.instance.organismTakeDeath);
                mTakeDivideInd = ent.animator.GetTakeIndex(GameData.instance.organismTakeReproduce);
            }
            else {
                mTakeDeathInd = -1;
                mTakeDivideInd = -1;
            }
        }

        public override void Spawn(M8.GenericParams parms) {
            mParentSpawned = entity.transform.parent;

            mState = State.Normal;
        }

        public override void Despawn() {
            if(mStickies != null)
                mStickies.Clear();

            mParentSpawned = null;

            mEndobioticHost = null;
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
                        if(mTakeDeathInd != -1)
                            entity.animator.Play(mTakeDeathInd);

                        entity.stats.energyLocked = true;

                        mState = State.Death;
                    }
                    //ready to divide?
                    else if(entity.stats.isEnergyFull) {
                        if((entity.stats.flags & OrganismFlag.DivideLocked) == 0 && !GameModePlay.instance.gameSpawner.entities.IsFull) {//don't allow divide if capacity is full
                            if(mTakeDivideInd != -1)
                                entity.animator.Play(mTakeDivideInd);

                            entity.velocity = Vector2.zero;
                            entity.angularVelocity = 0f;
                            entity.stats.energyLocked = true;

                            mState = State.Divide;
                        }
                    }
                    else if(mEndobioticHost != null) { //absorb energy from host
                        if(!mEndobioticHost.isReleased && mEndobioticHost.stats.energy > 0f) {
                            var energyAmt = entity.stats.energyConsumeRate * Time.deltaTime;

                            mEndobioticHost.stats.energy -= energyAmt;
                            entity.stats.energy += energyAmt;
                        }
                        else //detach from host
                            EndobioticDetach();
                    }
                    else if(mStickies != null) {
                        if(!entity.physicsLocked) {
                            var isEnergyRateUp = entity.stats.energyDelta > 0f;

                            //update stickies
                            for(int i = mStickies.Count - 1; i >= 0; i--) {
                                var sticky = mStickies[i];

                                //check if invalid
                                if(!sticky.isValid) {
                                    mStickies.RemoveLast();
                                    continue;
                                }

                                //unstick to solids if we are not getting any energy
                                if(!isEnergyRateUp && sticky.isSolid) {
                                    mStickies.RemoveLast();
                                    continue;
                                }

                                var distInfo = entity.bodyCollider.Distance(sticky.coll);
                                if(!distInfo.isValid) {
                                    mStickies.RemoveLast();
                                    continue;
                                }

                                if(distInfo.distance <= 0f) //separate a bit
                                    //entity.velocity += distInfo.normal * gameDat.organismSeparateSpeed;
                                    continue;
                                else if(distInfo.distance >= gameDat.organismStickyDistanceThreshold) //disconnect if distance is too far
                                    mStickies.RemoveLast();
                                else //move towards
                                    entity.velocity -= distInfo.normal * gameDat.organismStickySpeed;//distInfo.distance;
                            }

                            //check for new stickies

                            //stick to solid if we are generating energy
                            if(isEnergyRateUp && (entity.stats.flags & OrganismFlag.StickySolid) == OrganismFlag.StickySolid) {
                                for(int i = 0; !mStickies.IsFull && i < entity.contactSolids.Count; i++) {
                                    var coll = entity.contactSolids[i];

                                    if(coll.enabled && !IsStickTo(coll)) {
                                        mStickies.Add(new StickyInfo { coll = coll });

                                        entity.velocity = Vector2.zero;
                                    }
                                }
                            }

                            //only stick to same organisms
                            for(int i = 0; !mStickies.IsFull && i < entity.contactOrganismMatches.Count; i++) {
                                var contactEntity = entity.contactOrganismMatches[i];
                                if(!contactEntity.isReleased && !contactEntity.physicsLocked && !IsStickTo(contactEntity)) {
                                    //ensure contacted is not already stuck to us
                                    var bodySingleCellCtrl = contactEntity.GetComponentControl<OrganismBodySingleCellControl>();
                                    if(bodySingleCellCtrl == null || !bodySingleCellCtrl.IsStickTo(entity))
                                        mStickies.Add(new StickyInfo { entity = contactEntity, coll = contactEntity.bodyCollider });
                                }
                            }

                            //only stick to solid energy compatibles
                            for(int i = 0; !mStickies.IsFull && i < entity.contactEnergies.Count; i++) {
                                var energySrc = entity.contactEnergies[i];

                                if(energySrc.isActive && energySrc.isSolid && !IsStickTo(energySrc.bodyCollider))
                                    mStickies.Add(new StickyInfo { coll = energySrc.bodyCollider });
                            }
                        }
                        else
                            mStickies.Clear();
                    }

                    //check if we are toxic, and kamikazi on contact
                    if(entity.stats.toxic > 0f && entity.contactOrganisms.Count > 0) {
                        int toxicCount = 0;

                        for(int i = 0; i < entity.contactOrganisms.Count; i++) {
                            var contactEnt = entity.contactOrganisms[i];
                            if(!contactEnt.isReleased && !contactEnt.stats.energyLocked && contactEnt.stats.energy > 0f && (contactEnt.stats.flags & OrganismFlag.ToxicImmunity) == 0) {
                                contactEnt.stats.energy -= entity.stats.toxic;
                                toxicCount++;
                            }
                        }

                        if(toxicCount > 0)
                            entity.stats.ForcedLifeExpire();
                    }
                    break;

                case State.DeathWait:
                    if(entity.stats.spawnTimeElapsed >= entity.stats.lifespan + gameDat.organismDeathDelay) {
                        if(mTakeDeathInd != -1)
                            entity.animator.Play(mTakeDeathInd);

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
                        Vector2 fwd1, fwd2;

                        var entParent = entity.transform.parent;
                        var entEndobioticHost = mEndobioticHost;

                        if(entEndobioticHost) {
                            pt1 = pt2 = entParent.position;

                            fwd1 = M8.MathUtil.Rotate(forward, Random.Range(0f, M8.MathUtil.TwoPI));
                            fwd2 = M8.MathUtil.Rotate(forward, Random.Range(0f, M8.MathUtil.TwoPI));
                        }
                        else {
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

                            fwd1 = fwd2 = forward;
                        }

                        //release this
                        entity.Release();

                        //spawn two
                        var ent1 = spawner.SpawnAt(pt1, fwd1);
                        var ent2 = spawner.SpawnAt(pt2, fwd2);

                        //attach to previous host
                        if(entEndobioticHost) {
                            if(ent1) {
                                var ent1BodyCtrl = ent1.GetComponentControl<OrganismBodySingleCellControl>();
                                ent1BodyCtrl.EndobioticAttach(entEndobioticHost, entParent);
                                ent1.position += Random.insideUnitCircle * entity.radius;
                            }

                            if(ent2) {
                                var ent2BodyCtrl = ent2.GetComponentControl<OrganismBodySingleCellControl>();
                                ent2BodyCtrl.EndobioticAttach(entEndobioticHost, entParent);
                                ent2.position += Random.insideUnitCircle * entity.radius;
                            }
                        }
                        return;
                    }
                    break;
            }

            //update velocity
            if(!entity.physicsLocked) {
                if(entity.contactCount > 0) {
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
}
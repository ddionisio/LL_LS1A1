using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "hunterTentacle", menuName = "Game/Organism/Component/Hunter Tentacle")]
    public class OrganismHunterTentacle : OrganismHunter {
        [Header("Tentacle Settings")]
        public GameObject tentacleTemplate; //ensure pivot is bottom-center
        public int tentacleCount;
        public float tentacleTractDelay;
        public DG.Tweening.Ease grabEaseStart = DG.Tweening.Ease.OutSine;
        public DG.Tweening.Ease grabEaseEnd = DG.Tweening.Ease.InSine;
        public M8.RangeFloat tentacleRange; //range to grab from sensor, and max distance

        public override OrganismComponentControl GenerateControl(OrganismEntity organismEntity) {
            return new OrganismHunterTentacleControl();
        }
    }

    public class OrganismHunterTentacleControl : OrganismHunterControl {
        public class GrabDisplay {
            public enum State {
                Tract,
                Absorb,
                Retract
            }

            public OrganismEntity target { get { return mTarget; } }

            private SpriteRenderer mRender;

            private OrganismEntity mTarget;

            private State mState;

            private float mTime;
            private float mDist;

            public GrabDisplay(SpriteRenderer aRender) {
                mRender = aRender;

                mRender.gameObject.SetActive(false);
            }

            public void Start(OrganismEntity aTarget, float time) {
                mTarget = aTarget;
                mTime = time;

                mRender.gameObject.SetActive(true);

                mState = State.Tract;
            }

            public void End() {
                mTarget = null;

                mRender.gameObject.SetActive(false);
            }

            /// <summary>
            /// Return true if finish.
            /// </summary>
            public bool Update(OrganismEntity root, M8.RangeFloat range, float time, float delay, DG.Tweening.EaseFunction easeStart, DG.Tweening.EaseFunction easeEnd) {
                var curTime = time - mTime;

                var renderSize = mRender.size;

                switch(mState) {
                    case State.Tract:
                        if(!mTarget.isReleased && !mTarget.stats.isLifeExpired && mTarget.stats.energy > 0f) {
                            //stretch towards
                            if(curTime < delay) {
                                Vector2 dpos = mTarget.position - root.position;
                                mDist = dpos.magnitude;

                                if(mDist > 0f) {
                                    Vector2 dir = dpos / mDist;

                                    mRender.transform.up = dir;

                                    float t = easeStart(curTime, delay, 0f, 0f);

                                    renderSize.y = mDist * t;
                                }
                                else
                                    ChangeState(State.Absorb);
                            }
                            else
                                ChangeState(State.Absorb);
                        }
                        else
                            ChangeState(State.Retract);
                        break;

                    case State.Absorb:
                        if(!mTarget.isReleased && !mTarget.stats.isLifeExpired && mTarget.stats.energy > 0f) {
                            Vector2 dpos = mTarget.position - root.position;
                            mDist = dpos.magnitude;

                            Vector2 dir;

                            if(mDist > 0f)
                                dir = dpos / mDist;
                            else
                                dir = Vector2.zero;

                            var dt = Time.deltaTime;

                            //maintain distance
                            if(mDist < range.max) {
                                if(mDist > range.min)
                                    root.velocity += dir * (root.stats.forwardAccel * dt);

                                //absorb from target
                                var energyAmt = root.stats.energyConsumeRate * dt;

                                root.stats.energy += energyAmt;
                                mTarget.stats.energy -= energyAmt;

                                //update tentacle render
                                mRender.transform.up = dir;

                                renderSize.y = mDist;
                            }
                            else
                                ChangeState(State.Retract);
                        }
                        else
                            ChangeState(State.Retract);
                        break;

                    case State.Retract:
                        if(curTime < delay) {
                            float t = easeEnd(curTime, delay, 0f, 0f);

                            renderSize.y = mDist * (1f - t);
                        }
                        else
                            return true;
                        break;
                }

                mRender.size = renderSize;

                return false;
            }

            private void ChangeState(State toState) {
                mState = toState;
                mTime = Time.time;
            }
        }

        private OrganismHunterTentacle mComp;

        private M8.CacheList<GrabDisplay> mGrabActives;
        private M8.CacheList<GrabDisplay> mGrabRenderCache;

        private DG.Tweening.EaseFunction mEaseStart;
        private DG.Tweening.EaseFunction mEaseEnd;

        private OrganismComponentMotilityControl mMotilityCtrl;

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            base.Init(ent, owner);

            mComp = owner as OrganismHunterTentacle;

            mMotilityCtrl = ent.GetComponentControl<OrganismComponentMotilityControl>();

            //generate grab renders
            var root = entity.transform;

            var grabCount = mComp.tentacleCount;

            mGrabActives = new M8.CacheList<GrabDisplay>(grabCount);
            mGrabRenderCache = new M8.CacheList<GrabDisplay>(grabCount);

            for(int i = 0; i < grabCount; i++) {
                var go = Object.Instantiate(mComp.tentacleTemplate, root);

                var spriteRender = go.GetComponent<SpriteRenderer>();

                var t = go.transform;
                t.localPosition = Vector3.zero;
                t.localScale = new Vector3(1f / entity.transform.localScale.x, 1f / entity.transform.localScale.y, 1f);

                go.SetActive(false);

                mGrabRenderCache.Add(new GrabDisplay(spriteRender));
            }

            mEaseStart = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(mComp.grabEaseStart);
            mEaseEnd = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(mComp.grabEaseEnd);

            if(entity.sensor)
                entity.sensor.refreshCallback += OnSensorUpdate;
        }

        public override void Spawn(M8.GenericParams parms) {
            base.Spawn(parms);
        }

        public override void Despawn() {
            ClearGrabs();
        }

        public override void Update() {
            if(entity.stats.energyLocked || entity.physicsLocked) {
                ClearGrabs();
                return;
            }

            //update grabs
            var time = Time.time;
            var delay = mComp.tentacleTractDelay;
            var range = mComp.tentacleRange;

            for(int i = mGrabActives.Count - 1; i >= 0; i--) {
                var display = mGrabActives[i];

                if(display.Update(entity, range, time, delay, mEaseStart, mEaseEnd)) {
                    mGrabActives.RemoveAt(i);

                    display.End();

                    mGrabRenderCache.Add(display);

                    RefreshMotilityLock();
                }
            }
        }

        void OnSensorUpdate(OrganismSensor sensor) {
            if(entity.stats.energyLocked || entity.physicsLocked)
                return;

            var minRangeSqr = mComp.tentacleRange.min * mComp.tentacleRange.min;

            for(int i = 0; i < sensor.organisms.Count; i++) {
                var sensorEnt = sensor.organisms[i];

                if(sensorEnt.isReleased
                    || sensorEnt.physicsLocked
                    || sensorEnt.stats.energy == 0f
                    || !entity.stats.CanEat(sensorEnt.stats)
                    || entity.IsMatchTemplate(sensorEnt)
                    || IsGrabbed(sensorEnt))
                    continue;

                //grab if available
                if(mGrabRenderCache.Count > 0) {
                    //check distance, start a new grab display
                    var distSqr = (sensorEnt.position - entity.position).sqrMagnitude;
                    if(distSqr <= minRangeSqr) {
                        var display = mGrabRenderCache.RemoveLast();

                        display.Start(sensorEnt, Time.time);

                        mGrabActives.Add(display);
                        RefreshMotilityLock();
                        break;
                    }
                }
            }
        }

        void RefreshMotilityLock() {
            var isLocked = mGrabActives.Count > 0;

            if(mMotilityCtrl != null)
                mMotilityCtrl.isLocked = isLocked;
        }

        private bool IsGrabbed(OrganismEntity ent) {
            for(int i = 0; i < mGrabActives.Count; i++) {
                var grab = mGrabActives[i];
                if(grab.target == ent)
                    return true;
            }

            return false;
        }

        private void ClearGrabs() {
            if(mGrabActives.Count > 0) {
                for(int i = 0; i < mGrabActives.Count; i++) {
                    var display = mGrabActives[i];

                    var ent = display.target;
                    if(ent && !ent.isReleased)
                        ent.physicsLocked = false;

                    display.End();

                    mGrabRenderCache.Add(display);
                }

                mGrabActives.Clear();
            }
        }
    }
}
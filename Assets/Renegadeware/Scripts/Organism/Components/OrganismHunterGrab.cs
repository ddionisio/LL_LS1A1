using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "hunterGrab", menuName = "Game/Organism/Component/Hunter Grab")]
    public class OrganismHunterGrab : OrganismHunter {
        public GameObject grabTemplate; //ensure pivot is bottom-center
        public int grabCount;
        public float grabDelay;
        public DG.Tweening.Ease grabEaseStart = DG.Tweening.Ease.OutSine;
        public DG.Tweening.Ease grabEaseEnd = DG.Tweening.Ease.InSine;
        public float grabRadius; //range to grab from sensor

        public override OrganismComponentControl GenerateControl(OrganismEntity organismEntity) {
            return new OrganismHunterGrabControl();
        }
    }

    public class OrganismHunterGrabControl : OrganismHunterControl {
        public class GrabDisplay {
            public OrganismEntity target { get { return mTarget; } }

            private SpriteRenderer mRender;

            private OrganismEntity mTarget;

            private float mStartTime;
            private float mDist;

            public GrabDisplay(SpriteRenderer aRender) {
                mRender = aRender;

                mRender.gameObject.SetActive(false);
            }

            public void Start(OrganismEntity aTarget, float time) {
                mTarget = aTarget;
                mStartTime = time;

                mRender.gameObject.SetActive(true);
            }

            public void End() {
                mTarget = null;

                mRender.gameObject.SetActive(false);
            }

            /// <summary>
            /// Return true if finish.
            /// </summary>
            public bool Update(OrganismEntity root, float time, float delay, DG.Tweening.EaseFunction easeStart, DG.Tweening.EaseFunction easeEnd) {
                var curTime = time - mStartTime;
                var hDelay = delay * 0.5f;

                var renderSize = mRender.size;

                //stretch towards
                if(curTime < hDelay) {
                    Vector2 dpos = mTarget.position - root.position;
                    mDist = dpos.magnitude;

                    if(mDist > 0f) {
                        Vector2 dir = dpos / mDist;

                        mRender.transform.up = dir;

                        float t = easeStart(curTime, hDelay, 0f, 0f);

                        renderSize.y = mDist * t;
                    }
                    else
                        return true;
                }
                //retract
                else if(curTime < delay) {
                    float t = easeEnd(curTime - hDelay, hDelay, 0f, 0f);

                    float curDist = mDist * (1f - t);

                    renderSize.y = curDist;

                    Vector2 dir = mRender.transform.up;

                    mTarget.position = root.position + dir * curDist;
                }
                else
                    return true;

                mRender.size = renderSize;

                return false;
            }
        }

        private OrganismHunterGrab mComp;

        private M8.CacheList<GrabDisplay> mGrabActives;
        private M8.CacheList<GrabDisplay> mGrabRenderCache;

        private DG.Tweening.EaseFunction mEaseStart;
        private DG.Tweening.EaseFunction mEaseEnd;

        public override void Init(OrganismEntity ent, OrganismComponent owner) {
            base.Init(ent, owner);

            mComp = owner as OrganismHunterGrab;

            //generate grab renders
            var root = entity.transform;

            var grabCount = mComp.grabCount;

            mGrabActives = new M8.CacheList<GrabDisplay>(grabCount);
            mGrabRenderCache = new M8.CacheList<GrabDisplay>(grabCount);

            for(int i = 0; i < grabCount; i++) {
                var go = Object.Instantiate(mComp.grabTemplate, root);

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
            //clear out grabs
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

        public override void Update() {
            //eat any in contact
            for(int i = 0; i < entity.contactOrganisms.Count; i++) {
                var contactEnt = entity.contactOrganisms[i];

                if(contactEnt.isReleased || contactEnt.physicsLocked || contactEnt.stats.energy == 0f || entity.stats.danger <= contactEnt.stats.danger || entity.IsMatchTemplate(contactEnt))
                    continue;

                Eat(contactEnt);
            }

            //update grabs
            var time = Time.time;
            var delay = mComp.grabDelay;

            for(int i = mGrabActives.Count - 1; i >= 0; i--) {
                var display = mGrabActives[i];

                if(display.Update(entity, time, delay, mEaseStart, mEaseEnd)) {
                    mGrabActives.RemoveAt(i);

                    Eat(display.target);

                    display.End();

                    mGrabRenderCache.Add(display);
                }
            }
        }

        void OnSensorUpdate(OrganismSensor sensor) {
            for(int i = 0; i < sensor.organisms.Count; i++) {
                var sensorEnt = sensor.organisms[i];

                if(sensorEnt.isReleased || sensorEnt.physicsLocked || sensorEnt.stats.energy == 0f || entity.stats.danger <= sensorEnt.stats.danger || entity.IsMatchTemplate(sensorEnt))
                    continue;

                //grab if available
                if(mGrabRenderCache.Count > 0) {
                    //check distance, lock entity and start a new grab display
                    var distSqr = (sensorEnt.position - entity.position).sqrMagnitude;
                    if(distSqr <= mComp.grabRadius * mComp.grabRadius) {
                        sensorEnt.physicsLocked = true;

                        var display = mGrabRenderCache.RemoveLast();

                        display.Start(sensorEnt, Time.time);

                        mGrabActives.Add(display);
                        break;
                    }
                }
            }
        }
    }
}
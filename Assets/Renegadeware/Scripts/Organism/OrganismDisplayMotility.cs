using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismDisplayMotility : MonoBehaviour {
        public SpriteRenderer spriteRenderer;
        public M8.RangeFloat delayRange;

        private OrganismComponentMotilityControl mMotilityCtrl;

        private float mDelay;
        private float mLastTime;

        void OnEnable() {
            mDelay = delayRange.random;
            mLastTime = Time.time;
        }

        void Start() {
            var organismEnt = GetComponentInParent<OrganismEntity>();
            if(organismEnt) {
                mMotilityCtrl = organismEnt.GetComponentControl<OrganismComponentMotilityControl>();
            }
        }

        void Update() {
            if(IsMoving()) {
                var t = Time.time;

                if(t - mLastTime >= mDelay) {
                    spriteRenderer.flipX = !spriteRenderer.flipX;
                    mLastTime = Time.time;
                }
            }
        }

        private bool IsMoving() {
            if(mMotilityCtrl != null) {
                if(mMotilityCtrl.entity.physicsLocked || mMotilityCtrl.entity.stats.isLifeExpired)
                    return false;

                switch(mMotilityCtrl.state) {
                    case OrganismComponentMotilityControl.State.Explore:
                        switch(mMotilityCtrl.stateExplore) {
                            case OrganismComponentMotilityControl.ExploreState.Forward:
                            case OrganismComponentMotilityControl.ExploreState.Turn:
                            case OrganismComponentMotilityControl.ExploreState.TurnAway:
                                return true;
                        }
                        break;

                    case OrganismComponentMotilityControl.State.Seek:
                    case OrganismComponentMotilityControl.State.Retreat:
                        return true;
                }
            }

            return false;
        }
    }
}
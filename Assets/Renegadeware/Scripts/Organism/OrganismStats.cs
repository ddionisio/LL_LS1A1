using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [System.Serializable]
    public class OrganismStats {
        [Header("Physics")]
        public float mass;
        public float speedLimit; //set to 0 for no limit
        public float velocityReceiveScale; //influence amount of velocity received (environment velocity, fields)

        [Header("Energy")]
        public float energyInitial;
        public float energyCapacity;
        public float energyDeathDelay; //how long before dying when energy is 0
        public float energyCloneDelay; //how long to wait for cloning when energy is full

        [Header("Life")]
        public float lifespan; //how long does this cell live, set to 0 for immortality

        public float energy { 
            get { return mEnergy; }
            set {
                var _val = Mathf.Clamp(value, 0f, energyCapacity);
                if(mEnergy != _val) {
                    mEnergy = _val;

                    if(mEnergy == 0f || mEnergy == energyCapacity)
                        mLastEnergyCheckTime = Time.time;
                }
            }
        }

        /// <summary>
        /// Time since energy was either empty or full
        /// </summary>
        public float energyCheckTimeElapsed { get { return Time.time - mLastEnergyCheckTime; } }

        /// <summary>
        /// Time since organism spawned
        /// </summary>
        public float spawnTimeElapsed { get { return Time.time - mLastResetTime; } }

        private float mEnergy;

        private float mLastEnergyCheckTime; //last time energy was either empty or full
        private float mLastResetTime;

        public void Reset() {
            mEnergy = energyInitial;

            mLastResetTime = Time.time;
        }

        public void Append(OrganismStats otherStats) {
            mass += otherStats.mass;
            speedLimit += otherStats.speedLimit;

            energyInitial += otherStats.energyInitial;
            energyCapacity += otherStats.energyCapacity;
        }
    }
}
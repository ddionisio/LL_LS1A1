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
        public float energyCapacity;

        [Header("Life")]
        public float lifespan; //how long does this cell live, set to 0 for immortality. Once expired, energy will drain and not regenerate.

        public float energy { 
            get { return mEnergy; }
            set { mEnergy = Mathf.Clamp(value, 0f, energyCapacity); }
        }

        /// <summary>
        /// Initial energy when spawned, this is half of energy capacity.
        /// </summary>
        public float energyInitial { get { return energyCapacity * 0.5f; } }

        /// <summary>
        /// Time since organism spawned
        /// </summary>
        public float spawnTimeElapsed { get { return Time.time - mLastResetTime; } }

        public bool isLifeExpired { get { return lifespan > 0f && spawnTimeElapsed >= lifespan; } }

        public bool isEnergyFull { get { return mEnergy == energyCapacity; } }

        private float mEnergy;

        private float mLastResetTime;

        public void Reset() {
            mEnergy = energyInitial;

            mLastResetTime = Time.time;
        }

        public void Append(OrganismStats otherStats) {
            mass += otherStats.mass;
            speedLimit += otherStats.speedLimit;
            velocityReceiveScale += otherStats.velocityReceiveScale;

            energyCapacity += otherStats.energyCapacity;

            lifespan += otherStats.lifespan;
        }
    }
}
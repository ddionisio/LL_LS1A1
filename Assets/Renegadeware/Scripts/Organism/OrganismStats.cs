using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [System.Flags]
    public enum OrganismFlag {
        Sticky = 0x1,
        Parasitic = 0x2 //eat cell from within after being eaten
    }

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

        [Header("Environment")]
        [SerializeField]
        List<HazardData> _hazardResistances;
        [SerializeField]
        List<EnergyData> _energySources;

        [Header("Specifics")]
        public OrganismFlag flags;
        public float danger; //used for avoidance (danger < other.danger), also determines if this organism is a hunter (danger > 0)

        public float energy { 
            get { return mEnergy; }
            set {
                if(energyLocked)
                    return;

                mEnergy = Mathf.Clamp(value, 0f, energyCapacity); 
            }
        }

        /// <summary>
        /// Energy value normalized relative to energyCapacity.
        /// </summary>
        public float energyScale { get { return mEnergy / energyCapacity; } }

        /// <summary>
        /// Energy change since last update.
        /// </summary>
        public float energyDelta { get { return mEnergy - mEnergyLastUpdate; } }

        /// <summary>
        /// Initial energy when spawned, this is half of energy capacity.
        /// </summary>
        public float energyInitial { get { return energyCapacity * 0.5f; } }

        /// <summary>
        /// Set to true to lock the energy value. Used when dying/reproducing
        /// </summary>
        public bool energyLocked { get; set; }

        public List<EnergyData> energySources { get { return _energySources; } }

        public List<HazardData> hazardResistances { get { return _hazardResistances; } }

        /// <summary>
        /// Time since organism spawned
        /// </summary>
        public float spawnTimeElapsed { get { return Time.time - mLastResetTime; } }

        public bool isLifeExpired { get { return lifespan > 0f && spawnTimeElapsed >= lifespan; } }

        public bool isEnergyFull { get { return mEnergy == energyCapacity; } }

        private float mEnergy;
        private float mEnergyLastUpdate;

        private float mLastResetTime;

        public void Reset() {
            mEnergy = energyInitial;
            mEnergyLastUpdate = mEnergy;

            energyLocked = false;

            mLastResetTime = Time.time;
        }

        public bool HazardMatch(HazardData hazardData) {
            if(_hazardResistances == null)
                return false;

            return _hazardResistances.Contains(hazardData);
        }

        public bool EnergyMatch(EnergyData energyData) {
            if(_energySources == null)
                return false;

            return _energySources.Contains(energyData);
        }

        /// <summary>
        /// Check if another entity's attributes allows us to eat it.
        /// </summary>
        public bool CanEat(OrganismStats otherStats) {
            return danger > otherStats.danger && mass > otherStats.mass; //if edible, make sure it can be swallowed
        }

        public void EnergyUpdateLast() {
            mEnergyLastUpdate = mEnergy;
        }

        public void Copy(OrganismStats otherStats) {
            mass = otherStats.mass;
            speedLimit = otherStats.speedLimit;
            velocityReceiveScale = otherStats.velocityReceiveScale;

            energyCapacity = otherStats.energyCapacity;

            lifespan = otherStats.lifespan;

            if(otherStats._hazardResistances != null)
                _hazardResistances = new List<HazardData>(otherStats._hazardResistances);
            else
                _hazardResistances = new List<HazardData>();

            if(otherStats._energySources != null)
                _energySources = new List<EnergyData>(otherStats._energySources);
            else
                _energySources = new List<EnergyData>();

            flags = otherStats.flags;

            danger = otherStats.danger;
        }

        public void Append(OrganismStats otherStats) {
            mass += otherStats.mass;
            speedLimit += otherStats.speedLimit;
            velocityReceiveScale += otherStats.velocityReceiveScale;

            energyCapacity += otherStats.energyCapacity;

            lifespan += otherStats.lifespan;

            if(otherStats._hazardResistances != null) {
                if(_hazardResistances == null)
                    _hazardResistances = new List<HazardData>(otherStats._hazardResistances);
                else
                    _hazardResistances.AddRange(otherStats._hazardResistances);
            }

            if(otherStats._energySources != null) {
                if(_energySources == null)
                    _energySources = new List<EnergyData>(otherStats._energySources);
                else
                    _energySources.AddRange(otherStats._energySources);
            }

            flags |= otherStats.flags;

            danger += otherStats.danger;
        }
    }
}
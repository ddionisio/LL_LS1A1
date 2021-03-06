﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [System.Flags]
    public enum OrganismFlag {
        Sticky = 0x1,
        StickySolid = 0x2,
        DivideLocked = 0x4, //don't allow cell division
        Endobiotic = 0x8, //will not be killed when consumed by a hunter
        ToxicImmunity = 0x10, //immune to toxic
        ToxicKamikazi = 0x20, //for toxic organisms, expire on contact, and secrete toxins to them
    }

    [System.Serializable]
    public class OrganismStats {
        [Header("Physics")]
        public float mass;
        public float forwardAccel; //used by motility control
        public float turnAccel; //used by motility control
        public float speedLimit; //set to 0 for no limit
        public float velocityReceiveScale; //influence amount of velocity received (environment velocity, fields)

        [Header("Energy")]
        public float energyCapacity;
        [Tooltip("Energy per second to gain from energyConsume.")]
        public float energyConsumeRate;

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
        public float toxic; //when eaten, energy is reduced by this value.
        public float seekMassMin;

        public float energy { 
            get { return mEnergy; }
            set {
                if(mEnergyLocked)
                    return;

                mEnergy = Mathf.Clamp(value, 0f, energyCapacity); 
            }
        }

        /// <summary>
        /// Energy to consume, this will be transfered to energy based on energySourceConsumeRate
        /// </summary>
        public float energyConsume {
            get { return mEnergyConsume; }
            set {
                mEnergyConsume = value;
                if(mEnergyConsume < 0f)
                    mEnergyConsume = 0f;
            }
        }

        /// <summary>
        /// Used for distributing energy when in contact with similar organisms
        /// </summary>
        public float energyShare { get; set; }

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
        public bool energyLocked { 
            get { return mEnergyLocked; }
            set {
                if(mEnergyLocked != value) {
                    mEnergyLocked = value;

                    if(mEnergyLocked)
                        mEnergyLastUpdate = mEnergy;
                }
            }
        }

        public List<EnergyData> energySources { get { return _energySources; } }

        public List<HazardData> hazardResistances { get { return _hazardResistances; } }

        /// <summary>
        /// Time since organism spawned
        /// </summary>
        public float spawnTimeElapsed { get { return Time.time - mLastResetTime; } }

        public bool isLifeExpired { get { return mIsForcedLifeExpire || (lifespan > 0f && spawnTimeElapsed >= lifespan); } }

        public bool isEnergyFull { get { return mEnergy == energyCapacity; } }

        private float mEnergy;
        private float mEnergyConsume;
        private float mEnergyLastUpdate;

        private bool mEnergyLocked;

        private float mLastResetTime;

        private bool mIsForcedLifeExpire;

        public void Reset() {
            mEnergy = energyInitial;
            mEnergyLastUpdate = mEnergy;

            energyShare = 0f;
            mEnergyLocked = false;

            mLastResetTime = Time.time;

            mIsForcedLifeExpire = false;
        }

        /// <summary>
        /// Force this organism to die via life expiration.
        /// </summary>
        public void ForcedLifeExpire() {
            mIsForcedLifeExpire = true;
            mLastResetTime = Time.time - lifespan;
        }

        public bool HazardMatch(HazardData hazardData) {
            if(_hazardResistances == null)
                return false;

            return _hazardResistances.Contains(hazardData);
        }

        public bool EnergyMatch(EnergyData energyData) {
            if(energyData.ignoreMatch)
                return true;

            if(_energySources == null)
                return false;

            return _energySources.Contains(energyData);
        }

        /// <summary>
        /// Check if another entity's attributes allows us to eat it.
        /// </summary>
        public bool CanEat(OrganismStats otherStats) {
            return danger > otherStats.danger && mass > otherStats.mass && otherStats.mass >= seekMassMin; //if edible, make sure it can be swallowed
        }

        public void EnergyUpdate(float deltaTime) {
            mEnergyLastUpdate = mEnergy;

            if(!mEnergyLocked && !isEnergyFull && energyConsume > 0f) {
                var energyConsume = energyConsumeRate * deltaTime;

                energy += energyConsume;
                this.energyConsume -= energyConsume;
            }
        }

        public void Copy(OrganismStats otherStats) {
            mass = otherStats.mass;
            forwardAccel = otherStats.forwardAccel;
            turnAccel = otherStats.turnAccel;
            speedLimit = otherStats.speedLimit;
            velocityReceiveScale = otherStats.velocityReceiveScale;

            energyCapacity = otherStats.energyCapacity;
            energyConsumeRate = otherStats.energyConsumeRate;

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
            toxic = otherStats.toxic;
            seekMassMin = otherStats.seekMassMin;
        }

        public void Append(OrganismStats otherStats) {
            mass += otherStats.mass;
            forwardAccel += otherStats.forwardAccel;
            turnAccel += otherStats.turnAccel;
            speedLimit += otherStats.speedLimit;
            velocityReceiveScale += otherStats.velocityReceiveScale;

            energyCapacity += otherStats.energyCapacity;
            energyConsumeRate += otherStats.energyConsumeRate;

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
            toxic += otherStats.toxic;
            seekMassMin += otherStats.seekMassMin;
        }
    }
}
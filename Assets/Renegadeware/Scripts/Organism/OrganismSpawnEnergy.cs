using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class OrganismSpawnEnergy : MonoBehaviour, M8.IPoolInit, M8.IPoolSpawn, M8.IPoolDespawn {
        public EnergySource template;

        public string poolGroup = "energySource";
        public int poolCapacity = 100;

        [Tooltip("Amount of energy needed to spawn.")]
        public float energyRequired;

        public float spawnRadius;
        
        
        private M8.PoolController mEnergyPoolCtrl;

        private OrganismEntity mEntity;

        private float mEnergyCurrent;

        private EnergySource mEnergySource;

        private M8.GenericParams mEnergySpawnParms = new M8.GenericParams();

        void M8.IPoolInit.OnInit() {
            mEnergyPoolCtrl = M8.PoolController.CreatePool(poolGroup);
            mEnergyPoolCtrl.AddType(template.gameObject, poolCapacity, poolCapacity);

            mEntity = GetComponent<OrganismEntity>();
        }

        void M8.IPoolDespawn.OnDespawned() {
            if(mEnergySource) {
                if(mEnergySource.poolData)
                    mEnergySource.poolData.despawnCallback -= OnEnergyDespawn;

                mEnergySource.Release();
                mEnergySource = null;
            }
        }

        void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
            mEnergyCurrent = 0f;
        }

        void Update() {
            if(mEnergyCurrent < energyRequired) {
                var energyDelta = mEntity.stats.energyDelta;
                if(energyDelta > 0f)
                    mEnergyCurrent += energyDelta;
            }
            else if(!mEnergySource) {
                EnergySpawn();
                mEnergyCurrent = 0f;
            }
        }

        private void EnergySpawn() {
            var pos = mEntity.position;            

            mEnergySpawnParms[EnergySource.parmAnchorPos] = pos;
            mEnergySpawnParms[EnergySource.parmAnchorRadius] = spawnRadius;

            var delta = Random.insideUnitCircle * spawnRadius;

            var spawnPt = new Vector3(pos.x + delta.x, pos.y + delta.y, GameData.instance.energyDepth);

            mEnergySource = mEnergyPoolCtrl.Spawn<EnergySource>(template.name, template.name, transform.parent, spawnPt, mEnergySpawnParms);
            mEnergySource.poolData.despawnCallback += OnEnergyDespawn;
        }

        void OnEnergyDespawn(M8.PoolDataController pdc) {
            pdc.despawnCallback -= OnEnergyDespawn;
            mEnergySource = null;
        }

        void OnDrawGizmos() {
            if(spawnRadius > 0f) {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, spawnRadius);
            }
        }
    }
}
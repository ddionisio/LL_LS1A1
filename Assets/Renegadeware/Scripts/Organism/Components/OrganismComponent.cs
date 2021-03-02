﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public interface ISpawn {
        void OnSpawn(OrganismEntity entity);
    }

    public interface IDespawn {
        void OnDespawn(OrganismEntity entity);
    }

    public interface IUpdate {
        float delay { get; }

        void OnUpdate(OrganismEntity entity);
    }

    public interface IVelocityAdd {
        Vector2 OnAddVelocity(OrganismEntity entity);
    }

    public abstract class OrganismComponent : InfoData {
        [ID(group = "organismComponent", invalidID = GameData.invalidID)]
        public int ID;        

        //[Header("General")]
        //public float energy; //initial energy value upon spawn
        //public float energyRate; //determines amount of energy consumption/regeneration
        //public float energyCapacity; //determines energy cap, when reached, allow division

        public virtual string anchorName { get { return ""; } } //used for attaching component to body

        public virtual GameObject editPrefab { get { return null; } } //used during edit mode
        public virtual GameObject gamePrefab { get { return null; } } //used during simulation mode

        /// <summary>
        /// This is called during prefab generation for OrganismTemplate used during simulation mode.
        /// </summary>
        public virtual void SetupTemplate(OrganismEntity organismEntity) { }
    }
}
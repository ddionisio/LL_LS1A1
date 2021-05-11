using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public abstract class OrganismComponent : InfoData {
        [ID(group = "organismComponent", invalidID = GameData.invalidID)]
        public int ID;

        public AttributeInfo[] attributeInfos;

        public OrganismStats stats;

        public virtual string anchorName { get { return ""; } } //used for attaching component to body

        public virtual GameObject editPrefab { get { return null; } } //used during edit mode
        public virtual GameObject gamePrefab { get { return null; } } //used during simulation mode

        /// <summary>
        /// This is for components that need a controller during runtime. E.g. body components (manage spawning/death), motility components
        /// </summary>
        public virtual OrganismComponentControl GenerateControl(OrganismEntity organismEntity) { return null; }

        /// <summary>
        /// This is called during prefab generation for OrganismTemplate used during simulation mode.
        /// </summary>
        public virtual void SetupTemplate(OrganismEntity organismEntity) { }

        /// <summary>
        /// This is called during edit
        /// </summary>
        public virtual void SetupEditBody(OrganismDisplayBody displayBody) { }
    }
}
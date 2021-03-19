using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public abstract class OrganismComponentControl  {
        public OrganismEntity entity { get; private set; }

        public virtual void Init(OrganismEntity ent, OrganismComponent owner) {
            entity = ent;
        }

        public abstract void Spawn(M8.GenericParams parms);

        public abstract void Despawn();

        public abstract void Update();
    }
}
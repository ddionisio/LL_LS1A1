using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public abstract class OrganismComponentControl  {
        public abstract void Init(OrganismEntity ent, OrganismComponent owner);

        public abstract void Spawn(OrganismEntity ent, M8.GenericParams parms);

        public abstract void Update(OrganismEntity ent);

        public abstract void Despawn(OrganismEntity ent);
    }
}
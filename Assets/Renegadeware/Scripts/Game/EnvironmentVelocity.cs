using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public abstract class EnvironmentVelocity : MonoBehaviour {

        public abstract Vector2 GetVelocity(Vector2 pos, Vector2 forward, float deltaTime);
    }
}
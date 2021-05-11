using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public abstract class GamePlayFlow : MonoBehaviour {

        public virtual IEnumerator EnvironmentStart() { yield return null; }

        public virtual IEnumerator EditStart() { yield return null; }

        public virtual IEnumerator GameStart() { yield return null; }

        public virtual IEnumerator Victory() { yield return null; }
    }
}
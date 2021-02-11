using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class IDAttribute : PropertyAttribute {
        public string group = "general";
        public int invalidID = 0;
        public int startID = 1;
    }
}
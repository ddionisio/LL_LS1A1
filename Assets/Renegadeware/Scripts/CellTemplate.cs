using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class CellTemplate {
        public struct OrganelleData {
            public int id;
            public Vector2 position;
        }

        public string name;
        public OrganelleData[] organelles;

        public static CellTemplate LoadFromLevel() {
            //use active scene's name as base key

            return null;
        }
    }
}
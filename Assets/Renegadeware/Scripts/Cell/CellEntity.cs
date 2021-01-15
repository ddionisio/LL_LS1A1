using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class CellEntity : MonoBehaviour {

        public Rigidbody2D body { get { return mBody; } }
        public M8.SpriteColorGroup color { get { return mColor; } }

        private Rigidbody2D mBody;
        private M8.SpriteColorGroup mColor;

        /// <summary>
        /// Call this after creating the prefab, before generating the pool.
        /// </summary>
        /// <param name="cellTemplate"></param>
        public void SetupTemplate(CellTemplate cellTemplate) {
            //go through and initialize organelles
        }

        void Awake() {
            mBody = GetComponent<Rigidbody2D>();
            mColor = GetComponent<M8.SpriteColorGroup>();
        }



        void OnTriggerEnter2D(Collider2D collision) {
            
        }

        void OnTriggerExit2D(Collider2D collision) {
            
        }
    }
}
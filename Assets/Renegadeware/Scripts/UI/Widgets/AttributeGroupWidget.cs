using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class AttributeGroupWidget : MonoBehaviour {
        public const int itemCapacity = 4;

        public AttributeWidget template;

        public Transform root;

        private Dictionary<string, AttributeWidget> mItemActives = new Dictionary<string, AttributeWidget>(itemCapacity);
        private M8.CacheList<AttributeWidget> mItemCache = new M8.CacheList<AttributeWidget>(itemCapacity);

        public void Setup(AttributeInfo[] infos) {
            Clear();

            for(int i = 0; i < infos.Length; i++) {
                var inf = infos[i];

                var itm = GetWidget(inf.categoryRef);
                if(itm)
                    itm.Add(inf);
            }
        }

        void Awake() {
            template.gameObject.SetActive(false);
        }

        private void Clear() {
            foreach(var pair in mItemActives) {
                var itm = pair.Value;
                itm.Clear();
                itm.gameObject.SetActive(false);
                mItemCache.Add(itm);
            }

            mItemActives.Clear();
        }

        private AttributeWidget GetWidget(string categoryRef) {
            AttributeWidget itm = null;

            if(!mItemActives.TryGetValue(categoryRef, out itm)) {
                //create new
                if(mItemCache.Count == 0) {
                    if(mItemActives.Count < itemCapacity)
                        itm = Instantiate(template, root);
                }
                else
                    itm = mItemCache.RemoveLast();

                if(itm) {
                    itm.SetTitle(M8.Localize.Get(categoryRef));
                    itm.transform.SetAsLastSibling();
                    itm.gameObject.SetActive(true);                    

                    mItemActives.Add(categoryRef, itm);
                }
            }

            return itm;
        }
    }
}
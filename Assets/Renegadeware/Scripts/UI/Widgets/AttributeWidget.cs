using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Renegadeware.LL_LS1A1 {
    public class AttributeWidget : MonoBehaviour {
        public const int itemCapacity = 4;

        public AttributeItemWidget template;

        public Transform root;

        public TMP_Text titleLabel;

        private M8.CacheList<AttributeItemWidget> mItemActive = new M8.CacheList<AttributeItemWidget>(itemCapacity);
        private M8.CacheList<AttributeItemWidget> mItemCache = new M8.CacheList<AttributeItemWidget>(itemCapacity);

        public void SetTitle(string aTitle) {
            titleLabel.text = aTitle;
        }

        public void Add(AttributeInfo info) {
            AttributeItemWidget newItem = null;

            //expand cache?
            if(mItemCache.Count == 0) {
                if(mItemActive.Count < itemCapacity)
                    newItem = Instantiate(template, root);
            }
            else
                newItem = mItemCache.RemoveLast();

            if(newItem) {
                newItem.Setup(info);

                newItem.transform.SetAsLastSibling();
                newItem.gameObject.SetActive(true);

                mItemActive.Add(newItem);
            }
        }

        public void Clear() {
            for(int i = 0; i < mItemActive.Count; i++) {
                var itm = mItemActive[i];
                itm.gameObject.SetActive(false);
                mItemCache.Add(itm);
            }

            mItemActive.Clear();
        }

        void Awake() {
            template.gameObject.SetActive(false);
        }
    }
}
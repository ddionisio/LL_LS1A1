using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Renegadeware.LL_LS1A1 {
    public class InfoGroupWidget : MonoBehaviour {
        [Header("Display")]
        [SerializeField]
        Selectable _selectableNavigationDown;

        [Header("Info Data")]
        public Transform infoRoot;
        public InfoWidget infoTemplate;

        /// <summary>
        /// Set this up to allow downward selection from items
        /// </summary>
        public Selectable selectableNavigationDown {
            get { return _selectableNavigationDown; }
            set {
                if(_selectableNavigationDown != value) {
                    _selectableNavigationDown = value;
                    RefreshNavigation();
                }
            }
        }

        private M8.CacheList<InfoWidget> mItems = new M8.CacheList<InfoWidget>(GameData.organismComponentCapacity);
        private M8.CacheList<InfoWidget> mItemCache = new M8.CacheList<InfoWidget>(GameData.organismComponentCapacity);

        public int itemCount { get { return mItems.Count; } }

        public event System.Action<int, InfoData> selectCallback;
        public event System.Action<int, InfoData> clickCallback;

        public int GetIndex(InfoData info) {
            for(int i = 0; i < mItems.Count; i++) {
                if(mItems[i].data == info)
                    return i;
            }

            return -1;
        }

        public InfoData GetItem(int index) {
            if(index >= mItems.Count)
                return null;

            return mItems[index].data;
        }

        public void SetSelect(int index) {
            index = Mathf.Clamp(index, 0, mItems.Count - 1);

            var selectable = mItems[index].selectable;
            if(selectable)
                selectable.Select();
        }

        public void Add(InfoData[] infos) {
            for(int i = 0; i < infos.Length; i++) {
                var itm = AllocateItem();
                itm.Setup(infos[i]);
            }

            RefreshNavigation();
        }

        public void Add(InfoData info) {
            var itm = AllocateItem();

            itm.Setup(info);

            RefreshNavigation();
        }

        public void Remove(int index) {
            if(index >= mItems.Count)
                return;

            var itm = mItems[index];
            mItems.RemoveAt(index);

            itm.gameObject.SetActive(false);

            mItemCache.Add(itm);

            RefreshNavigation();
        }

        public void Remove(InfoData info) {
            for(int i = 0; i < mItems.Count; i++) {
                if(mItems[i].data == info) {
                    Remove(i);
                    break;
                }
            }
        }

        public void Clear() {
            for(int i = 0; i < mItems.Count; i++) {
                var itm = mItems[i];
                itm.gameObject.SetActive(false);

                mItemCache.Add(itm);
            }

            mItems.Clear();
        }

        void ItemSelect(InfoWidget itm) {
            var dat = itm.data;

            var ind = GetIndex(dat);

            selectCallback?.Invoke(ind, dat);
        }

        void ItemClick(InfoWidget itm) {
            var dat = itm.data;

            var ind = GetIndex(dat);

            clickCallback?.Invoke(ind, dat);
        }

        private InfoWidget AllocateItem() {
            InfoWidget newItem;

            if(mItemCache.Count == 0) {
                newItem = Instantiate(infoTemplate);
                newItem.transform.SetParent(infoRoot, false);

                newItem.selectCallback += ItemSelect;
                newItem.clickCallback += ItemClick;
            }
            else
                newItem = mItemCache.RemoveLast();
            
            newItem.gameObject.SetActive(true);

            mItems.Add(newItem);

            return newItem;
        }

        private void RefreshNavigation() {

        }
    }
}
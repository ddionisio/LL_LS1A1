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

        public int selectIndex { 
            get { return mSelectIndex; }
            set {
                if(mSelectIndex == value)
                    return;

                if(mSelectIndex != -1) {
                    if(mSelectIndex < mItems.Count)
                        mItems[mSelectIndex].isSelected = false;
                }

                mSelectIndex = value;

                if(mSelectIndex != -1) {
                    var selectItm = mItems[mSelectIndex];

                    selectItm.isSelected = true;

                    selectCallback?.Invoke(mSelectIndex, selectItm.data);
                }
            }
        }

        public int itemCount { get { return mItems.Count; } }

        public event System.Action<int, InfoData> selectCallback;
        public event System.Action<int, InfoData> clickCallback;

        private M8.CacheList<InfoWidget> mItems = new M8.CacheList<InfoWidget>(GameData.organismComponentCapacity);
        private M8.CacheList<InfoWidget> mItemCache = new M8.CacheList<InfoWidget>(GameData.organismComponentCapacity);

        private int mSelectIndex;

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

        public void Add(InfoData[] infos) {
            for(int i = 0; i < infos.Length; i++) {
                int ind = mItems.Count;

                var itm = AllocateItem();
                itm.Setup(ind, infos[i]);
            }

            RefreshNavigation();
        }

        public void Add(InfoData info) {
            int ind = mItems.Count;

            var itm = AllocateItem();

            itm.Setup(ind, info);

            RefreshNavigation();
        }

        public void Remove(int index) {
            if(index >= mItems.Count)
                return;

            var itm = mItems[index];
            mItems.RemoveAt(index);

            itm.gameObject.SetActive(false);

            mItemCache.Add(itm);

            if(mSelectIndex == index)
                mSelectIndex = -1;

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

            mSelectIndex = -1;
        }

        void ItemClick(InfoWidget itm) {
            var dat = itm.data;

            var ind = GetIndex(dat);

            selectIndex = ind;

            clickCallback?.Invoke(ind, dat);
        }

        private InfoWidget AllocateItem() {
            InfoWidget newItem;

            if(mItemCache.Count == 0) {
                newItem = Instantiate(infoTemplate);
                newItem.transform.SetParent(infoRoot, false);

                newItem.clickCallback += ItemClick;
            }
            else
                newItem = mItemCache.RemoveLast();

            newItem.transform.SetAsLastSibling();
            newItem.gameObject.SetActive(true);            

            mItems.Add(newItem);

            return newItem;
        }

        private void RefreshNavigation() {

        }
    }
}
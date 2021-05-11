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
                    var itm = GetItemWidget(mSelectIndex);
                    if(itm)
                        itm.isSelected = false;
                }

                mSelectIndex = value;

                if(mSelectIndex != -1) {
                    var selectItm = GetItemWidget(mSelectIndex);
                    if(selectItm) {
                        selectItm.isSelected = true;

                        selectCallback?.Invoke(mSelectIndex, selectItm.data);
                    }
                }
            }
        }

        public M8.CacheList<InfoWidget> items { get { return mItems; } }

        public int itemCount { get { return mItems.Count; } }

        public event System.Action<int, InfoData> selectCallback;
        public event System.Action<int, InfoData> clickCallback;

        private M8.CacheList<InfoWidget> mItems = new M8.CacheList<InfoWidget>(GameData.organismComponentCapacity);
        private M8.CacheList<InfoWidget> mItemCache = new M8.CacheList<InfoWidget>(GameData.organismComponentCapacity);

        private int mSelectIndex;

        public void SelectFirstItem() {
            if(mItems.Count > 0)
                selectIndex = mItems[0].index;
        }

        public int GetIndex(InfoData info) {
            for(int i = 0; i < mItems.Count; i++) {
                if(mItems[i].data == info)
                    return mItems[i].index;
            }

            return -1;
        }

        public InfoData GetItem(int index) {
            for(int i = 0; i < mItems.Count; i++) {
                if(mItems[i].index == index)
                    return mItems[i].data;
            }

            return null;
        }

        public InfoWidget GetItemWidget(int index) {
            for(int i = 0; i < mItems.Count; i++) {
                if(mItems[i].index == index)
                    return mItems[i];
            }

            return null;
        }

        public InfoWidget Add(InfoData info, int index) {
            var itm = AllocateItem();

            itm.Setup(index, info);

            RefreshNavigation();

            return itm;
        }

        public void Remove(int index) {
            if(index >= mItems.Count)
                return;

            InfoWidget itm = GetItemWidget(index);
            if(itm) {
                mItems.Remove(itm);

                itm.gameObject.SetActive(false);

                mItemCache.Add(itm);
            }

            if(mSelectIndex == index)
                mSelectIndex = -1;

            RefreshNavigation();
        }

        public void Remove(InfoData info) {
            for(int i = 0; i < mItems.Count; i++) {
                if(mItems[i].data == info) {
                    var itm = mItems[i];

                    mItems.RemoveAt(i);

                    itm.gameObject.SetActive(false);

                    mItemCache.Add(itm);
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

            var ind = itm.index;

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
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class ModalOrganismEditor : M8.ModalController, M8.IModalPush, M8.IModalPop, M8.IModalOpening, M8.IModalClosing {
        public const string parmTitleRef = "ott";
        public const string parmOrganismBodyGroup = "obg"; //OrganismComponentGroup
        public const string parmOrganismTemplate = "ot"; //OrganismTemplate

        public enum Mode {
            Category,
            ComponentBody, //select body
            ComponentEssential, //fill out essentials
            Component //select component for current category
        }

        [Header("Display")]
        public TMP_Text titleText;
        public InfoWidget infoWidget;
        public AttributeGroupWidget attrWidget;

        [Header("Category")]
        public GameObject categoryRootGO;
        public AnimatorEnterExit categoryTransition;
        public InfoGroupWidget categoryWidget;

        [Header("Component")]
        public GameObject componentRootGO;
        public AnimatorEnterExit componentTransition;
        public InfoGroupWidget componentWidget;

        public Button componentAcceptButton;
        public Button componentBackButton;

        private Mode mCurMode;

        private OrganismComponentGroup mBodyGroup;
        private OrganismTemplate mOrganismTemplate;

        private M8.CacheList<int> mComponentIds = new M8.CacheList<int>(GameData.organismComponentCapacity); //components of the organism template, first should be the body

        private int mCategoryIndex;

        private int mCompIndex;

        void M8.IModalPop.Pop() {
            ResetData();
            HideAll();
        }

        void M8.IModalPush.Push(M8.GenericParams parms) {
            ResetData();
            HideAll();

            mCurMode = Mode.Category;

            var titleString = "";

            if(parms != null) {
                parms.TryGetValue(parmOrganismBodyGroup, out mBodyGroup);

                //setup component ids
                parms.TryGetValue(parmOrganismTemplate, out mOrganismTemplate);
                RefreshComponentIds();

                if(parms.ContainsKey(parmTitleRef))
                    titleString = M8.Localize.Get(parms.GetValue<string>(parmTitleRef));

                //setup categories
                RefreshCategoryWidget();
            }

            if(titleText)
                titleText.text = titleString;
        }

        IEnumerator M8.IModalOpening.Opening() {
            yield return DoEnter();

            RefreshAttributeInfo(-1, 0);
        }

        IEnumerator M8.IModalClosing.Closing() {
            yield return DoExit();
        }

        void OnDestroy() {
            if(categoryWidget)
                categoryWidget.clickCallback -= CategoryClick;

            if(componentWidget)
                componentWidget.selectCallback -= ComponentSelect;
        }

        void Awake() {
            //setup calls
            categoryWidget.clickCallback += CategoryClick;

            componentWidget.selectCallback += ComponentSelect;

            componentAcceptButton.onClick.AddListener(ComponentConfirm);
            componentBackButton.onClick.AddListener(ComponentCancel);
        }

        IEnumerator DoEnter() {
            switch(mCurMode) {
                case Mode.Category:
                    if(categoryRootGO) categoryRootGO.SetActive(true);

                    if(categoryTransition)
                        yield return categoryTransition.PlayEnterWait();
                    break;

                case Mode.ComponentBody:
                case Mode.ComponentEssential:
                case Mode.Component:
                    if(componentRootGO) componentRootGO.SetActive(true);

                    if(componentAcceptButton) componentAcceptButton.interactable = mCompIndex != -1;
                    if(componentBackButton) componentBackButton.gameObject.SetActive(mCurMode != Mode.ComponentEssential && GetBodyComp()); //don't allow going back in essential mode, or if there's no body

                    if(componentTransition)
                        yield return componentTransition.PlayEnterWait();
                    break;
            }
        }

        IEnumerator DoExit() {
            switch(mCurMode) {
                case Mode.Category:
                    if(categoryTransition)
                        yield return categoryTransition.PlayExitWait();

                    if(categoryRootGO) categoryRootGO.SetActive(false);
                    break;

                case Mode.ComponentBody:
                case Mode.ComponentEssential:
                case Mode.Component:
                    if(componentTransition)
                        yield return componentTransition.PlayExitWait();

                    if(componentRootGO) componentRootGO.SetActive(false);
                    break;
            }
        }

        IEnumerator DoTransition(Mode toMode) {
            HideInfo();

            yield return DoExit();

            mCurMode = toMode;

            switch(mCurMode) {
                case Mode.Category:
                    categoryWidget.selectIndex = -1;
                    break;

                case Mode.ComponentEssential:
                    var bodyComp = GetBodyComp();

                    //add components
                    componentWidget.Clear();

                    for(int i = 0; i < bodyComp.componentEssentials.Length; i++) {
                        var comp = bodyComp.componentEssentials[i];
                        if(mOrganismTemplate.GetComponentEssentialIndex(comp.ID) == -1) {
                            var widget = componentWidget.Add(comp, i);
                            widget.selectable.interactable = i == 0;
                        }
                    }

                    componentWidget.SelectFirstItem();
                    break;
            }

            yield return DoEnter();

            switch(mCurMode) {
                case Mode.Component:
                    if(componentWidget.selectIndex != -1) {
                        var itm = componentWidget.GetItem(componentWidget.selectIndex);
                        if(itm)
                            RefreshInfo(itm);
                    }
                    break;
            }
        }

        void CategoryClick(int index, InfoData data) {
            mCategoryIndex = index;

            OrganismComponent[] comps = null;

            var bodyComp = GetBodyComp();

            Mode toMode;

            if(index == 0) { //body selected
                comps = mBodyGroup.components;

                //setup current component index
                if(bodyComp)
                    mCompIndex = mBodyGroup.GetIndex(bodyComp);
                else
                    mCompIndex = -1;

                toMode = Mode.ComponentBody;
            }
            else {
                int subCatInd = index - 1;

                if(bodyComp) {
                    var subCategory = bodyComp.componentGroups[subCatInd];
                    comps = subCategory.components;

                    //setup current component index
                    if(subCatInd < mComponentIds.Count)
                        mCompIndex = subCategory.GetIndex(mComponentIds[index]);
                    else
                        mCompIndex = -1;
                }

                toMode = Mode.Component;
            }

            if(comps != null) {
                componentWidget.Clear();

                for(int i = 0; i < comps.Length; i++)
                    componentWidget.Add(comps[i], i);

                componentWidget.selectIndex = mCompIndex;

                StartCoroutine(DoTransition(toMode));
            }
        }

        void ComponentSelect(int index, InfoData data) {
            if(mCompIndex == index)
                return;

            var gameDat = GameData.instance;

            mCompIndex = index;

            switch(mCurMode) {
                case Mode.ComponentBody:
                    var bodyComp = data as OrganismComponent;
                    gameDat.signalEditBodyPreview.Invoke(bodyComp.ID);
                    break;

                case Mode.ComponentEssential:
                    gameDat.signalEditComponentEssentialPreview.Invoke((data as OrganismComponent).ID);
                    break;

                case Mode.Component:
                    var compID = (data as OrganismComponent).ID;

                    gameDat.signalEditComponentPreview.Invoke(mCategoryIndex, compID);

                    RefreshAttributeInfo(mCategoryIndex, compID);
                    break;
            }

            if(componentAcceptButton) componentAcceptButton.interactable = mCompIndex != -1;

            //update info window
            RefreshInfo(data);
        }

        void ComponentConfirm() {
            switch(mCurMode) {
                case Mode.ComponentBody: //body apply
                    mOrganismTemplate.body = mBodyGroup.components[mCompIndex] as OrganismBody;

                    RefreshComponentIds();

                    RefreshCategoryWidget();

                    //check if essential components are filled
                    if(mOrganismTemplate.isEssentialComponentsFilled) {
                        //transition back to categories
                        StartCoroutine(DoTransition(Mode.Category));
                    }
                    else { //enter essential components mode
                        mCompIndex = -1;

                        StartCoroutine(DoTransition(Mode.ComponentEssential));
                    }
                    break;

                case Mode.ComponentEssential:
                    var compItemWidget = componentWidget.GetItemWidget(mCompIndex);
                    var itm = compItemWidget.data as OrganismComponent;
                    if(!itm)
                        return;

                    var ind = GetBodyComp().GetComponentEssentialIndex(itm.ID);

                    mOrganismTemplate.SetComponentEssentialID(ind, itm.ID);

                    //remove item
                    componentWidget.Remove(itm);

                    mCompIndex = -1;

                    int nextInd = compItemWidget.index + 1;
                    var widget = componentWidget.GetItemWidget(nextInd);
                    if(widget) { //prep for next component
                        componentWidget.selectIndex = widget.index;
                        widget.selectable.interactable = true;
                    }
                    else {
                        //transition back to categories
                        StartCoroutine(DoTransition(Mode.Category));
                    }
                    break;

                case Mode.Component:
                    int compId = GetBodyComp().componentGroups[mCategoryIndex - 1].components[mCompIndex].ID;

                    mComponentIds[mCategoryIndex] = compId;

                    mOrganismTemplate.SetComponentID(mCategoryIndex, compId);

                    //refresh category highlight
                    bool isHighlight = false;
                    for(int i = 0; i < categoryWidget.itemCount; i++) {
                        var categoryItemWidget = categoryWidget.items[i];
                        if(!isHighlight) {
                            if(mComponentIds[i] == GameData.invalidID) {
                                categoryItemWidget.highlightGO.SetActive(true);
                                isHighlight = true;
                            }
                            else
                                categoryItemWidget.highlightGO.SetActive(false);
                        }
                        else
                            categoryItemWidget.highlightGO.SetActive(false);
                    }

                    //transition back to categories
                    StartCoroutine(DoTransition(Mode.Category));
                    break;
            }
        }

        void ComponentCancel() {
            var gameDat = GameData.instance;

            //refresh (undo preview)
            gameDat.signalEditRefresh.Invoke();

            RefreshAttributeInfo(-1, 0);

            //transition back to categories
            StartCoroutine(DoTransition(Mode.Category));
        }

        private OrganismBody GetBodyComp() {
            if(mComponentIds.Count == 0)
                return null;

            return GameData.instance.GetOrganismComponent<OrganismBody>(mComponentIds[0]);
        }

        private void ResetData() {
            mBodyGroup = null;
            mOrganismTemplate = null;
            mComponentIds.Clear();

            mCategoryIndex = 0;
            mCompIndex = -1;
        }

        private void RefreshCategoryWidget() {
            categoryWidget.Clear();

            if(mComponentIds.Count <= 0 || mComponentIds[0] == GameData.invalidID) {
                //only put body
                var widget = categoryWidget.Add(mBodyGroup, 0);
                widget.highlightGO.SetActive(true);
            }
            else {
                //get current body selected
                var bodyComp = GetBodyComp();
                if(bodyComp) {
                    //put in other categories based on body
                    categoryWidget.Add(mBodyGroup, 0);

                    bool isHighlight = false;

                    for(int i = 0; i < bodyComp.componentGroups.Length; i++) {
                        int ind = i + 1;

                        var grp = bodyComp.componentGroups[i];
                        if(!grp.isHidden) {
                            var widget = categoryWidget.Add(grp, ind);

                            //highlight if no component has been picked yet
                            if(!isHighlight) {
                                if(mComponentIds[ind] == GameData.invalidID) {
                                    widget.highlightGO.SetActive(true);
                                    isHighlight = true;
                                }
                            }
                        }
                    }
                }
                else {
                    //only put body
                    var widget = categoryWidget.Add(mBodyGroup, 0);
                    widget.highlightGO.SetActive(true);
                }
            }
        }

        private void RefreshComponentIds() {
            mComponentIds.Clear();

            if(mOrganismTemplate.componentIDs != null) {
                for(int i = 0; i < mOrganismTemplate.componentIDs.Length; i++)
                    mComponentIds.Add(mOrganismTemplate.componentIDs[i]);
            }
        }

        private void RefreshInfo(InfoData info) {
            if(!string.IsNullOrEmpty(info.descRef)) {
                if(infoWidget) {
                    infoWidget.Setup(0, info);
                    infoWidget.gameObject.SetActive(true);
                }
            }
            else
                HideInfo();
        }

        /// <summary>
        /// Set compModifiedInd to a valid index to apply modified ID
        /// </summary>
        private void RefreshAttributeInfo(int compModifiedInd, int compModifiedID) {
            if(!attrWidget)
                return;

            var gameDat = GameData.instance;

            var attrList = new List<AttributeInfo>();

            for(int i = 0; i < mComponentIds.Count; i++) {
                int id;
                if(i == compModifiedInd)
                    id = compModifiedID;
                else
                    id = mComponentIds[i];

                var compDat = gameDat.GetOrganismComponent<OrganismComponent>(id);
                if(compDat)
                    attrList.AddRange(compDat.attributeInfos);
            }

            if(attrList.Count > 0) {
                attrWidget.Setup(attrList.ToArray());
                attrWidget.gameObject.SetActive(true);
            }
            else
                attrWidget.gameObject.SetActive(false);
        }

        private void HideInfo() {
            if(infoWidget) infoWidget.gameObject.SetActive(false);
        }

        private void HideAll() {
            if(categoryRootGO) categoryRootGO.SetActive(false);
            if(componentRootGO) componentRootGO.SetActive(false);
            if(infoWidget) infoWidget.gameObject.SetActive(false);
            if(attrWidget) attrWidget.gameObject.SetActive(false);
        }
    }
}
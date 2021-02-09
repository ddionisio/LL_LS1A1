using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using LoLExt;

namespace Renegadeware.LL_LS1A1 {
    public class ModalOrganismEditor : M8.ModalController, M8.IModalPush, M8.IModalPop, M8.IModalOpening, M8.IModalClosing {
        public const string parmOrganismBodyGroup = "obg"; //OrganismComponentGroup
        public const string parmOrganismTemplate = "ot"; //OrganismTemplate

        public enum Mode {
            Category,
            ComponentEssential,
            Component
        }

        [Header("Display")]
        public TMP_Text titleText;

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
        private int mCompLastIndex;

        void M8.IModalPop.Pop() {
            ResetData();
            HideAll();
        }

        void M8.IModalPush.Push(M8.GenericParams parms) {
            ResetData();
            HideAll();

            mCurMode = Mode.Category;

            if(parms != null) {
                parms.TryGetValue(parmOrganismBodyGroup, out mBodyGroup);

                //setup component ids
                parms.TryGetValue(parmOrganismTemplate, out mOrganismTemplate);
                RefreshComponentIds();

                //setup categories
                RefreshCategoryWidget();
            }
        }

        IEnumerator M8.IModalOpening.Opening() {
            yield return DoEnter();
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

                case Mode.ComponentEssential:
                case Mode.Component:
                    if(componentRootGO) componentRootGO.SetActive(true);

                    if(componentBackButton) componentBackButton.gameObject.SetActive(mCurMode == Mode.Component); //don't allow going back in essential mode

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

                case Mode.ComponentEssential:
                case Mode.Component:
                    if(componentTransition)
                        yield return componentTransition.PlayExitWait();

                    if(componentRootGO) componentRootGO.SetActive(false);
                    break;
            }
        }

        IEnumerator DoTransition(Mode toMode) {
            yield return DoExit();

            mCurMode = toMode;

            yield return DoEnter();
        }

        void CategoryClick(int index, InfoData data) {
            if(mCategoryIndex == index)
                return;

            mCategoryIndex = index;

            OrganismComponent[] comps = null;

            var bodyComp = GetBodyComp();

            if(index == 0) { //body selected
                comps = mBodyGroup.components;

                //setup current component index
                if(bodyComp)
                    mCompIndex = mBodyGroup.GetIndex(bodyComp);
                else
                    mCompIndex = -1;
            }
            else {
                int subCatInd = index - 1;

                if(bodyComp) {
                    var subCategory = bodyComp.componentGroups[subCatInd];
                    comps = subCategory.components;

                    //setup current component index
                    if(subCatInd < mComponentIds.Count)
                        mCompIndex = subCategory.GetIndex(mComponentIds[subCatInd]);
                    else
                        mCompIndex = -1;
                }
            }

            mCompLastIndex = mCompIndex;

            if(comps != null) {
                componentWidget.Clear();

                componentWidget.Add(comps);

                componentWidget.SetSelect(mCompIndex);

                StartCoroutine(DoTransition(Mode.Component));
            }
        }

        void ComponentSelect(int index, InfoData data) {
            if(mCompIndex == index)
                return;

            var gameDat = GameData.instance;

            mCompIndex = index;

            if(mCategoryIndex == 0) { //body preview
                var bodyComp = data as OrganismComponent;
                gameDat.signalEditBodyPreview.Invoke(bodyComp.ID);
            }
            else { //component preview
                var comp = data as OrganismComponent;

                if(mCurMode == Mode.Component)
                    gameDat.signalEditComponentPreview.Invoke(mCategoryIndex - 1, comp.ID);
                else if(mCurMode == Mode.ComponentEssential)
                    gameDat.signalEditComponentEssentialPreview.Invoke(mCompIndex, comp.ID);
            }
        }

        void ComponentConfirm() {
            if(mCategoryIndex == 0) { //body apply
                mOrganismTemplate.SetBody(mBodyGroup.components[mCompIndex] as OrganismBody);

                RefreshComponentIds();

                RefreshCategoryWidget();

                //check if essential components are filled
                if(mOrganismTemplate.IsEssentialComponentsFilled()) {
                    //transition back to categories
                    categoryWidget.SetSelect(1);
                    StartCoroutine(DoTransition(Mode.Category));
                }
                else { //enter essential components mode
                    var bodyComp = GetBodyComp();

                    //add components
                    componentWidget.Clear();

                    for(int i = 0; i < bodyComp.componentEssentials.Length; i++) {
                        var comp = bodyComp.componentEssentials[i];
                        if(mOrganismTemplate.GetComponentEssentialIndex(comp.ID) == -1)
                            componentWidget.Add(comp);
                    }

                    StartCoroutine(DoTransition(Mode.ComponentEssential));
                }
            }
            else { //component apply
                var bodyComp = GetBodyComp();

                if(mCurMode == Mode.Component) {
                    int subCatInd = mCategoryIndex - 1;
                    int compId = bodyComp.componentGroups[subCatInd].components[mCompIndex].ID;

                    mComponentIds[subCatInd] = compId;

                    mOrganismTemplate.SetComponentID(subCatInd, compId);

                    //transition back to categories
                    categoryWidget.SetSelect(mCategoryIndex);
                    StartCoroutine(DoTransition(Mode.Category));
                }
                else if(mCurMode == Mode.ComponentEssential) {
                    var itm = componentWidget.GetItem(mCompIndex) as OrganismComponent;

                    var ind = bodyComp.GetComponentEssentialIndex(itm.ID);

                    mOrganismTemplate.SetComponentEssentialID(ind, itm.ID);

                    //remove item
                    componentWidget.Remove(itm);

                    if(componentWidget.itemCount > 0) //prep for next component
                        componentWidget.SetSelect(0);
                    else {
                        //transition back to categories
                        categoryWidget.SetSelect(1);
                        StartCoroutine(DoTransition(Mode.Category));
                    }
                }
            }
        }

        void ComponentCancel() {
            var gameDat = GameData.instance;

            //refresh (undo preview)
            gameDat.signalEditRefresh.Invoke();

            //transition back to categories
            categoryWidget.SetSelect(mCategoryIndex);
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

            if(mComponentIds.Count <= 0 || mComponentIds[0] == OrganismTemplate.invalidID) {
                //only put body
                categoryWidget.Add(mBodyGroup);
            }
            else {
                //get current body selected
                var bodyComp = GetBodyComp();
                if(bodyComp) {
                    //put in other categories based on body
                    categoryWidget.Add(mBodyGroup);
                    categoryWidget.Add(bodyComp.componentGroups);
                }
                else {
                    //only put body
                    categoryWidget.Add(mBodyGroup);
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

        private void HideAll() {
            if(categoryRootGO) categoryRootGO.SetActive(false);
            if(componentRootGO) componentRootGO.SetActive(false);
        }
    }
}
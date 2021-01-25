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

        [Header("Signals")]
        public M8.SignalInteger signalInvokeBodyChange; //new body component id
        public M8.SignalInteger signalInvokeBodyPreview; //body component id
        public SignalOrganismComponent signalInvokeComponentChange; //component index, component id

        private Mode mCurMode;

        private OrganismComponentGroup mBodyGroup;
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

            if(parms != null) {
                parms.TryGetValue(parmOrganismBodyGroup, out mBodyGroup);

                //setup component ids
                OrganismTemplate orgTemplate = null;
                if(parms.TryGetValue(parmOrganismTemplate, out orgTemplate)) {
                    if(orgTemplate.componentIDs != null) {
                        for(int i = 0; i < orgTemplate.componentIDs.Length; i++)
                            mComponentIds.Add(orgTemplate.componentIDs[i]);
                    }
                }

                //setup categories
                if(mComponentIds.Count <= 0 || mComponentIds[0] == OrganismTemplate.invalidID) {
                    //only put body
                    categoryWidget.Setup(mBodyGroup);
                }
                else {
                    //get current body selected
                    var bodyComp = GetBodyComp();
                    if(bodyComp) {
                        //put in other categories based on body
                        categoryWidget.Setup(mBodyGroup, bodyComp.componentGroups);
                    }
                    else {
                        //only put body
                        categoryWidget.Setup(mBodyGroup);
                    }
                }
            }
        }

        IEnumerator M8.IModalOpening.Opening() {
            yield return DoEnter();
        }

        IEnumerator M8.IModalClosing.Closing() {
            yield return DoExit();
        }

        IEnumerator DoEnter() {
            switch(mCurMode) {
                case Mode.Category:
                    if(categoryRootGO) categoryRootGO.SetActive(true);

                    if(categoryTransition)
                        yield return categoryTransition.PlayEnterWait();
                    break;

                case Mode.Component:
                    if(componentRootGO) componentRootGO.SetActive(true);

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

        void CategorySelect(int index, InfoData data) {
            if(mCategoryIndex == index)
                return;

            mCategoryIndex = index;

            OrganismComponent[] comps = null;

            if(index == 0) { //body selected
                comps = mBodyGroup.components;

                //setup component index
            }
            else {
                int subCatInd = index - 1;

                var bodyComp = GetBodyComp();
                if(bodyComp) {
                    var subCategory = bodyComp.componentGroups[subCatInd];
                    comps = subCategory.components;

                    //setup component index
                }
            }

            if(comps != null) {
                componentWidget.Setup(comps);

                componentWidget.SetSelect(mCompIndex);

                StartCoroutine(DoTransition(Mode.Component));
            }
        }

        private OrganismBody GetBodyComp() {
            if(mComponentIds.Count == 0)
                return null;

            return GameData.instance.GetOrganismComponent<OrganismBody>(mComponentIds[0]);
        }

        private void ResetData() {
            mBodyGroup = null;
            mComponentIds.Clear();

            mCategoryIndex = 0;
            mCompIndex = -1;
        }

        private void HideAll() {
            if(categoryRootGO) categoryRootGO.SetActive(false);
            if(componentRootGO) componentRootGO.SetActive(false);
        }
    }
}
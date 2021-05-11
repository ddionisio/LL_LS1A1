using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Renegadeware.LL_LS1A1 {
    public class InfoWidget : MonoBehaviour, IPointerClickHandler {
        [Header("Display")]
        public TMP_Text titleText;
        public TMP_Text descText;
        public Image iconImage;
        public GameObject selectGO;
        public GameObject highlightGO;

        public int index { get; private set; }

        public InfoData data { get; private set; }

        public bool isSelected {
            get { return selectGO ? selectGO.activeSelf : false; }
            set { if(selectGO) selectGO.SetActive(value); }
        }

        public Selectable selectable {
            get {
                if(!mSelectable)
                    mSelectable = GetComponent<Selectable>();
                return mSelectable;
            }
        }

        public event System.Action<InfoWidget> clickCallback;

        private Selectable mSelectable;

        public void Setup(int index, InfoData aData) {
            this.index = index;
            data = aData;

            if(titleText) titleText.text = M8.Localize.Get(data.nameRef);
            if(descText) descText.text = M8.Localize.Get(data.descRef);

            if(iconImage) iconImage.sprite = data.icon;

            if(selectable)
                selectable.interactable = true;

            if(highlightGO)
                highlightGO.SetActive(false);

            isSelected = false;
        }

        public void PlaySpeech() {
            if(!data || !LoLExt.LoLManager.isInstantiated)
                return;

            var lolMgr = LoLExt.LoLManager.instance;

            lolMgr.StopSpeakQueue();

            if(!string.IsNullOrEmpty(data.nameRef))
                lolMgr.SpeakTextQueue(data.nameRef, GameData.speechGroupInfo, 0);
            if(!string.IsNullOrEmpty(data.descRef))
                lolMgr.SpeakTextQueue(data.descRef, GameData.speechGroupInfo, 1);
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            if(selectable && selectable.interactable)
                clickCallback?.Invoke(this);
        }
    }
}
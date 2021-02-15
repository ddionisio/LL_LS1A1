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

        public InfoData data { get; private set; }

        public bool isSelected {
            get { return selectGO ? selectGO.activeSelf : false; }
            set { if(selectGO) selectGO.SetActive(value); }
        }

        public event System.Action<InfoWidget> clickCallback;

        public void Setup(InfoData aData) {
            data = aData;

            if(titleText) titleText.text = M8.Localize.Get(data.nameRef);
            if(descText) descText.text = M8.Localize.Get(data.descRef);

            if(iconImage) iconImage.sprite = data.icon;

            isSelected = false;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            clickCallback?.Invoke(this);
        }
    }
}
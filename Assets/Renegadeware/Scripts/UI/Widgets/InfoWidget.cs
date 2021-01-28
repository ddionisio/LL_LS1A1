using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Renegadeware.LL_LS1A1 {
    public class InfoWidget : MonoBehaviour, IPointerClickHandler, ISelectHandler {
        [Header("Display")]
        public TMP_Text titleText;
        public TMP_Text descText;
        public Image iconImage;

        public InfoData data { get; private set; }

        public Selectable selectable { get; private set; }

        public event System.Action<InfoWidget> clickCallback;
        public event System.Action<InfoWidget> selectCallback;

        public void Setup(InfoData aData) {
            data = aData;

            if(titleText) titleText.text = M8.Localize.Get(data.nameRef);
            if(descText) descText.text = M8.Localize.Get(data.descRef);

            if(iconImage) iconImage.sprite = data.icon;
        }

        void Awake() {
            selectable = GetComponent<Selectable>();
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            clickCallback?.Invoke(this);
        }

        void ISelectHandler.OnSelect(BaseEventData eventData) {
            selectCallback?.Invoke(this);
        }
    }
}
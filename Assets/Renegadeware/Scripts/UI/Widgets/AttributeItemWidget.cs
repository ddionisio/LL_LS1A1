﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Renegadeware.LL_LS1A1 {
    public class AttributeItemWidget : MonoBehaviour {
        public Image icon;
        public TMP_Text textLabel;

        public void Setup(AttributeInfo info) {
            if(info.icon) {
                icon.gameObject.SetActive(true);
                icon.sprite = info.icon;
            }
            else
                icon.gameObject.SetActive(false);

            textLabel.text = M8.Localize.Get(info.nameRef);
        }
    }
}
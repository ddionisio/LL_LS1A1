using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Renegadeware.LL_LS1A1 {
    public class ModalRetry : M8.ModalController, M8.IModalPush, M8.IModalPop {
        public const string parmCurCount = "retryCurC";
        public const string parmCount = "retryC";
        public const string parmCallback = "retryCB";

        [Header("Info")]
        [M8.Localize]
        public string descRef;

        [Header("Display")]
        public TMP_Text descLabel;

        private System.Action<ModeSelect> mCallback;

        void M8.IModalPop.Pop() {
            mCallback = null;
        }

        void M8.IModalPush.Push(M8.GenericParams parms) {
            int curCount = 0, count = 0;

            if(parms != null) {
                if(parms.ContainsKey(parmCurCount))
                    curCount = parms.GetValue<int>(parmCurCount);

                if(parms.ContainsKey(parmCount))
                    count = parms.GetValue<int>(parmCount);

                if(parms.ContainsKey(parmCallback))
                    mCallback = parms.GetValue<System.Action<ModeSelect>>(parmCallback);
            }

            descLabel.text = string.Format(M8.Localize.Get(descRef), curCount, count);
        }

        public void GotoEnvironmentClick() {
            mCallback?.Invoke(ModeSelect.Environment);

            Close();
        }

        public void GotoEditClick() {
            mCallback?.Invoke(ModeSelect.Edit);

            Close();
        }

        public void GotoRetryClick() {
            mCallback?.Invoke(ModeSelect.Retry);

            Close();
        }
    }
}
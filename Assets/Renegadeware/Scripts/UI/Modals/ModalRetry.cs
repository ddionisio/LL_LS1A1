using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class ModalRetry : M8.ModalController, M8.IModalPush, M8.IModalPop {
        public const string parmCallback = "retryCB";

        private System.Action<ModeSelect> mCallback;

        void M8.IModalPop.Pop() {
            mCallback = null;
        }

        void M8.IModalPush.Push(M8.GenericParams parms) {
            if(parms != null) {
                if(parms.ContainsKey(parmCallback))
                    mCallback = parms.GetValue<System.Action<ModeSelect>>(parmCallback);
            }
        }

        public void GotoEnvironmentClick() {
            mCallback?.Invoke(ModeSelect.Environment);

            Close();
        }

        public void GotoEditClick() {
            mCallback?.Invoke(ModeSelect.Edit);

            Close();
        }
    }
}
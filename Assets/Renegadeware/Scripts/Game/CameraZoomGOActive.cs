using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class CameraZoomGOActive : MonoBehaviour {
        public GameObject[] GOLevels; //corresponds to zoom levels

        void OnDisable() {
            GameData.instance.signalCameraZoom.callback -= OnCameraZoom;
        }

        void OnEnable() {
            GameData.instance.signalCameraZoom.callback += OnCameraZoom;

            //update current zoom level
            var camCtrl = CameraControl.instance;
            OnCameraZoom(camCtrl.zoomIndex);
        }

        void OnCameraZoom(int level) {
            GameObject prevGO = null;

            for(int i = 0; i < GOLevels.Length; i++) {
                var go = GOLevels[i];
                if(go && go != prevGO)
                    go.SetActive(i >= level);

                prevGO = go;
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Renegadeware.LL_LS1A1 {
    public struct GameDataEdit {
        public static GameData gameData {
            get {
                if(!mGameData)
                    mGameData = Resources.Load<GameData>(GameData.resourcePath);

                return mGameData;
            }
        }

        public static void RefreshOrganismComponents(GameData gameData) {
            //grab all OrganismComponent
            var guids = AssetDatabase.FindAssets("t:" + typeof(OrganismComponent).Name);

            var comps = new List<OrganismComponent>(guids.Length);

            for(int i = 0; i < guids.Length; i++) {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);

                var comp = AssetDatabase.LoadAssetAtPath<OrganismComponent>(path);
                if(comp)
                    comps.Add(comp);
            }

            gameData.organismComponents = comps.ToArray();

            EditorUtility.SetDirty(gameData);
        }

        private static GameData mGameData;
    }
}
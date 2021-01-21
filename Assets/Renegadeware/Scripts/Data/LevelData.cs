using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "level", menuName = "Game/Level")]
    public class LevelData : ScriptableObject {
        [System.Serializable]
        public struct EnvironmentInfo {
            [M8.Localize]
            public string nameRef;
            [M8.Localize]
            public string descRef;
        }

        public class EnvironmentStat {
            public int organismTemplateID = OrganismTemplate.invalidID; //cell template used for this environment
            public int count = 0; //organism count made after play, if >= criteriaCount, then this environment is complete

            private const string userDataKeySubOrganismID = "_id";
            private const string userDataKeySubOrganismCount = "_count";

            public EnvironmentStat(M8.UserData usrData, string key) {
                LoadFrom(usrData, key);
            }

            public void LoadFrom(M8.UserData usrData, string key) {
                organismTemplateID = usrData.GetInt(key + userDataKeySubOrganismID, OrganismTemplate.invalidID);
                count = usrData.GetInt(key + userDataKeySubOrganismCount);
            }

            public void SaveTo(M8.UserData usrData, string key) {
                usrData.SetInt(key + userDataKeySubOrganismID, organismTemplateID);
                usrData.SetInt(key + userDataKeySubOrganismCount, count);
            }

            public void Reset() {
                organismTemplateID = OrganismTemplate.invalidID;
                count = 0;
            }
        }

        [Header("Data")]
        public M8.SceneAssetPath scene;
        public int progressCount = 2; //number of progress for this particular level before going to next (use LoL cur. progress)
        public int criteriaCount; //number of organisms to grow to complete an environment
        public EnvironmentInfo[] environments; //usually 4

        [Header("Cell Bodies")]
        public CategoryData bodyCategory;
        public OrganismComponent[] bodyComponents;

        private const string userDataKeyEnvironmentStat = "envStat";

        private EnvironmentStat[] mStats;

        //environment selections

        //cell spawn restriction, etc.

        public int GetProgressCount() {
            if(mStats == null)
                return 0;

            int progressCount = 0;

            for(int i = 0; i < mStats.Length; i++) {
                if(mStats[i].count >= criteriaCount)
                    progressCount++;
            }

            //fail-safe clamp count
            progressCount = Mathf.Clamp(progressCount, 0, progressCount);

            return progressCount;
        }

        public void LoadStatsFrom(M8.UserData usrData) {
            var envCount = environments.Length;

            mStats = new EnvironmentStat[envCount];

            for(int i = 0; i < envCount; i++)
                mStats[i] = new EnvironmentStat(usrData, name + i);
        }

        public void SaveStatsTo(M8.UserData usrData) {
            if(mStats == null)
                return;

            var envCount = environments.Length;

            for(int i = 0; i < envCount; i++)
                mStats[i].SaveTo(usrData, name + i);
        }

        public void ResetStatsFrom(M8.UserData usrData) {
            usrData.RemoveAllByNameContain(name);

            if(mStats != null) {
                for(int i = 0; i < mStats.Length; i++)
                    mStats[i].Reset();
            }
        }
    }
}
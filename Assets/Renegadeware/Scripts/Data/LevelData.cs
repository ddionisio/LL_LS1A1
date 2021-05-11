using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "level", menuName = "Game/Level")]
    public class LevelData : ScriptableObject {
        [System.Serializable]
        public struct EnvironmentInfo {
            [Header("Info")]
            [M8.Localize]
            public string nameRef;
            [M8.Localize]
            public string descRef;

            [M8.Localize]
            public string hintRef;

            public AttributeInfo[] attributes;

            [Header("Spawn Settings")]
            public int spawnableCount; //threshold count to allow spawning (active count < spawnable count)
            public int criteriaCount; //number of organisms to grow to complete an environment
            public int bonusCount; //number of count outside criteria to gain maximum bonus
            public int capacity; //max spawn in the world
        }

        public class EnvironmentStat {
            public int organismTemplateID; //cell template used for this environment
            public int count; //organism count made after play, if >= criteriaCount, then this environment is complete

            private const string userDataKeySubOrganismID = "_id";
            private const string userDataKeySubOrganismCount = "_count";

            public EnvironmentStat() {
                Reset();
            }

            public EnvironmentStat(M8.UserData usrData, string key) {
                LoadFrom(usrData, key);
            }

            public void LoadFrom(M8.UserData usrData, string key) {
                organismTemplateID = usrData.GetInt(key + userDataKeySubOrganismID, GameData.invalidID);
                count = usrData.GetInt(key + userDataKeySubOrganismCount);
            }

            public void SaveTo(M8.UserData usrData, string key) {
                usrData.SetInt(key + userDataKeySubOrganismID, organismTemplateID);
                usrData.SetInt(key + userDataKeySubOrganismCount, count);
            }

            public void Reset() {
                organismTemplateID = GameData.invalidID;
                count = 0;
            }
        }

        [Header("Data")]
        public M8.SceneAssetPath introScene;
        public M8.SceneAssetPath scene;

        [M8.Localize]
        public string titleRef;

        public int progressCount = 2; //number of progress for this particular level before going to next (use LoL cur. progress)
        public float duration = 60f; //play duration

        public bool spawnIsRandomDir;
        
        public EnvironmentInfo[] environments; //usually 4

        public OrganismComponentGroup organismBodyGroup;

        public EnvironmentStat[] stats {
            get {
                if(mStats == null) {
                    mStats = new EnvironmentStat[environments.Length];
                    for(int i = 0; i < mStats.Length; i++)
                        mStats[i] = new EnvironmentStat();
                }

                return mStats;
            }
        }

        private const string userDataKeyEnvironmentStat = "envStat";

        private EnvironmentStat[] mStats;

        //environment selections

        //cell spawn restriction, etc.

        public bool IsEnvironmentComplete(int envInd) {
            return stats[envInd].count >= environments[envInd].criteriaCount;
        }

        public bool IsComplete() {
            return GetProgressCount() >= progressCount;
        }

        public int GetProgressCount() {
            int progressCount = 0;

            for(int i = 0; i < stats.Length; i++) {
                if(stats[i].count >= environments[i].criteriaCount)
                    progressCount++;
            }

            //fail-safe clamp count
            progressCount = Mathf.Clamp(progressCount, 0, progressCount);

            return progressCount;
        }

        public int GetScore() {
            var gameDat = GameData.instance;

            int score = 0;

            for(int i = 0; i < stats.Length; i++) {
                var env = environments[i];

                var count = stats[i].count;
                if(count > 0)
                    score += gameDat.GetScore(count, env.criteriaCount, env.bonusCount);
            }

            return score;
        }

        public void ApplyStats(int envInd, int templateID, int spawnCount) {
            stats[envInd].organismTemplateID = templateID;
            stats[envInd].count = spawnCount;
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
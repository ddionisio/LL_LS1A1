using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class ModalVictory : M8.ModalController, M8.IModalPush {
        public const string parmCount = "victoryCount";
        public const string parmCriteriaCount = "victoryCriteriaCount";
        public const string parmBonusCount = "victoryBonusCount";

        [Header("Display")]
        public M8.TextMeshPro.TextMeshProCounter organismCountLabel;
        public M8.TextMeshPro.TextMeshProCounter scoreLabel;
        public GameObject[] medals;

        [Header("SFX")]
        [M8.SoundPlaylist]
        public string sfxSuccess;

        void M8.IModalPush.Push(M8.GenericParams parms) {
            int count = 0, criteriaCount = 0, bonusCount = 0;

            if(parms != null) {
                if(parms.ContainsKey(parmCount))
                    count = parms.GetValue<int>(parmCount);

                if(parms.ContainsKey(parmCriteriaCount))
                    criteriaCount = parms.GetValue<int>(parmCriteriaCount);

                if(parms.ContainsKey(parmBonusCount))
                    bonusCount = parms.GetValue<int>(parmBonusCount);
            }

            int score = GameData.instance.GetScore(count, criteriaCount, bonusCount);
            int medalInd = GameData.instance.GetMedalIndex(medals.Length, count, criteriaCount, bonusCount);

            organismCountLabel.SetCountImmediate(0);
            organismCountLabel.count = count;

            scoreLabel.SetCountImmediate(0);
            scoreLabel.count = score;

            if(medalInd != -1) {
                for(int i = 0; i < medals.Length; i++) {
                    if(medals[i])
                        medals[i].SetActive(i <= medalInd);
                }
            }
            else {
                for(int i = 0; i < medals.Length; i++) {
                    if(medals[i])
                        medals[i].SetActive(false);
                }
            }

            if(!string.IsNullOrEmpty(sfxSuccess))
                M8.SoundPlaylist.instance.Play(sfxSuccess, false);
        }
    }
}
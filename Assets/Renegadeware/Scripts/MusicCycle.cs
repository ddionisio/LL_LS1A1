using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class MusicCycle : M8.SingletonBehaviour<MusicCycle> {
        [System.Serializable]
        public struct Info {
            [M8.MusicPlaylist]
            public string music;
            public float duration;
        }

        public Info[] info;

        private Coroutine mRout;

        public void Play() {
            if(mRout != null)
                StopCoroutine(mRout);

            mRout = StartCoroutine(DoPlay());
        }

        public void Stop() {
            if(mRout != null) {
                M8.MusicPlaylist.instance.Stop(false);
                StopCoroutine(mRout);

                mRout = null;
            }
        }

        void OnDisable() {
            mRout = null;
        }

        IEnumerator DoPlay() {
            M8.MusicPlaylist.instance.Stop(false);

            int curInd = 0;

            while(true) {
                var inf = info[curInd];

                M8.MusicPlaylist.instance.Play(inf.music, true, false);
                
                yield return new WaitForSecondsRealtime(inf.duration);

                M8.MusicPlaylist.instance.Stop(false);

                curInd++;
                if(curInd == info.Length) {
                    M8.ArrayUtil.Shuffle(info);
                    curInd = 0;
                }
            }
        }
    }
}
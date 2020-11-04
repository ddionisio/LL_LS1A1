using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using UnityEngine;
using System.Runtime.InteropServices;
using SimpleJSON;
/**
	Version 2.0.0 RC-10
*/
namespace LoLSDK
{
    public class WebGL : ILOLSDK
    {
        // *************************************
        // PLUMBING
        // *************************************

        [DllImport("__Internal")]
        public static extern void _PostWindowMessage(string msgName, string jsonPayload);
        public void PostWindowMessage(string msgName, string jsonPayload)
        {
            Debug.Log("PostWindowMessage " + msgName);

            _PostWindowMessage(msgName, jsonPayload);
        }

        public void LogMessage(string msg)
        {
            JSONObject payload = new JSONObject()
            {
                ["msg"] = msg
            };

            PostWindowMessage("logMessage", payload.ToString());
        }

        // *************************************
        // GAME LIFECYCLE
        // *************************************

        [DllImport("__Internal")]
        private static extern string _GameIsReady(string gameName, string callbackGameObject, string aspectRatio, string resolution, string sdkVersion);
        public void GameIsReady(string gameName, string callbackGameObject, string aspectRatio, string resolution)
        {
            _GameIsReady(gameName, callbackGameObject, aspectRatio, resolution, "V4");
        }

        public void CompleteGame()
        {
            JSONObject payload = new JSONObject();
            PostWindowMessage("complete", payload.ToString());
        }

        // *************************************
        // ANSWER SUBMISSION
        // *************************************

        public void SubmitProgress(int score, int currentProgress, int maximumProgress = -1)
        {
            JSONObject payload = new JSONObject()
            {

                ["score"] = score,
                ["currentProgress"] = currentProgress,
                ["maximumProgress"] = maximumProgress
            };

            PostWindowMessage("progress", payload.ToString());
        }

        public void SubmitAnswer(int questionId, int alternativeId)
        {
            JSONObject payload = new JSONObject()
            {
                ["questionId"] = questionId,
                ["alternativeId"] = alternativeId
            };

            PostWindowMessage("answer", payload.ToString());
        }


        // *************************************
        // SPEECH
        // *************************************

        public void SpeakText(string key)
        {
            JSONObject payload = new JSONObject()
            {
                ["key"] = key
            };

            PostWindowMessage("speakText", payload.ToString());
        }

        public void SpeakQuestion(int questionId)
        {
            Debug.Log("SpeakQuestion");

            JSONObject payload = new JSONObject()
            {
                ["questionId"] = questionId
            };

            PostWindowMessage("speakQuestion", payload.ToString());
        }

        public void SpeakAlternative(int alternativeId)
        {
            JSONObject payload = new JSONObject()
            {
                ["alternativeId"] = alternativeId
            };

            PostWindowMessage("speakAlternative", payload.ToString());
        }

        public void SpeakQuestionAndAlternatives(int questionId)
        {
            JSONObject payload = new JSONObject()
            {
                ["questionId"] = questionId
            };

            PostWindowMessage("speakQuestionAndAlternatives", payload.ToString());
        }

        public void Error(string msg)
        {
            JSONObject payload = new JSONObject()
            {
                ["msg"] = msg
            };

            PostWindowMessage("error", payload.ToString());
        }

        public void ShowQuestion()
        {
            Debug.Log("ShowQuestion");

            PostWindowMessage("showQuestion", "{}");
        }

        // *************************************
        // PLAY, STOP, and CONFIGURE SOUNDS
        // *************************************

        public void PlaySound(string file, bool background = false, bool loop = false)
        {
            JSONObject payload = new JSONObject()
            {

                ["file"] = file,
                ["background"] = background,
                ["loop"] = loop
            };

            PostWindowMessage("playSound", payload.ToString());
        }

        public void ConfigureSound(float foreground, float background, float fade)
        {
            JSONObject payload = new JSONObject()
            {
                ["foreground"] = foreground,
                ["background"] = background,
                ["fade"] = fade
            };

            PostWindowMessage("configureSound", payload.ToString());
        }

        public void StopSound(string file)
        {
            JSONObject payload = new JSONObject()
            {
                ["file"] = file
            };

            PostWindowMessage("stopSound", payload.ToString());
        }

        public void SaveState(string data)
        {
            PostWindowMessage("saveState", data);
        }

        public void LoadState()
        {
            PostWindowMessage("loadState", "{}");
        }

        public void GetPlayerActivityId()
        {
            PostWindowMessage("getPlayerActivityId", "{}");
        }
    }

    public class MockWebGL : ILOLSDK
    {

        /* *********************************************
 		 * MOCK Messages for Local Development
		 * *********************************************

		/* *********************************************
 		 * PLUMBING
		 ********************************************** */

        public void PostWindowMessage(string msgName, string jsonPayload)
        {
            Debug.Log("PostWindowMessage: " + msgName);
            Debug.Log("JSON: " + jsonPayload);
        }

        public void LogMessage(string msg)
        {
            Debug.Log("LogMessage");
        }
        /* *********************************************
 		 * ERROR
		 ********************************************** */

        public void sError(string msg)
        {
            Debug.Log("Error");
        }

        /* *********************************************
 		 * GAME LIFECYCLE
		 ********************************************** */

        public void CompleteGame()
        {
            Debug.Log("CompleteGame");
        }

        public void GameIsReady(string gameName, string callbackGameObject, string aspectRatio, string resolution)
        {
            Debug.Log("GameIsReady Editor");
            Debug.Log("GameIsReady gameName" + gameName);
            Debug.Log("GameIsReady callbackGameObject" + callbackGameObject);
        }

        /* *********************************************
 		 * SUBMIT PROGRESS AND ANSWERS
		 ********************************************** */

        public void SubmitProgress(int score, int currentProgress, int maximumProgress = -1)
        {
            Debug.Log("SubmitProgress");
        }

        public void SubmitAnswer(int questionId, int alternativeId)
        {
            Debug.Log("SubmitAnswer");
        }


        /* *********************************************
 		 * QUESTIONS
		 ********************************************** */

        public void ShowQuestion()
        {
            Debug.Log("ShowQuestion");
        }
        /* *********************************************
 		 * SPEECH
		 ********************************************** */

        public void SpeakText(string key)
        {
            Debug.Log("SpeakText");
        }

        public void SpeakQuestion(int questionId)
        {
            Debug.Log("SpeakQuestion");
        }

        public void SpeakQuestionAndAlternatives(int questionId)
        {
            Debug.Log("SpeakQuestionAndAlternatives");
        }

        public void SpeakAlternative(int alternativeId)
        {
            Debug.Log("SpeakAlternative");
        }

        /* *********************************************
 		 * SOUND
		 ********************************************** */
        public void ConfigureSound(float a, float b, float c)
        {
            Debug.Log("ConfigureSound");
        }

        public void PlaySound(string path, bool background, bool loop)
        {
            Debug.Log("PlaySound");
        }

        public void StopSound(string path)
        {
            Debug.Log("StopSound");
        }

        public void SaveState(string data)
        {
            Debug.Log("SaveData");
        }

        public void LoadState()
        {
            Debug.Log("LoadData");
        }

        public void GetPlayerActivityId()
        {
            Debug.Log("GetPlayerActivityId");
        }
    }
}

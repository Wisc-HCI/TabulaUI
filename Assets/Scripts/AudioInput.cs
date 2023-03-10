using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEngine.Windows.Speech;
#elif UNITY_ANDROID
using TextSpeech;
using UnityEngine.Android;
#endif

/// <summary>
/// Class <c>AudioInput</c> listens for speech from a microphone and
///  converts it to text.
/// 
/// The code in this file was heavily drawn from the Speech-Recognition-Unity
///  project on GitHub: https://github.com/LightBuzz/Speech-Recognition-Unity
///  written by LightBuzz
///
///  The URL to the repository is https://github.com/LightBuzz/Speech-Recognition-Unity
///  The URL to the code below is https://github.com/LightBuzz/Speech-Recognition-Unity/blob/master/SpeechRecognitionUnity/Assets/Open%20Dictation%20Mode/DictationEngine.cs
///  The relevant commit hash is 7456d38973eb4a67c1b59d7aecccc17b69effbfd
///  
///  The original code is licensed under the MIT license. The license can be
///  found in the third_party_licenses.txt under "Speech-Recognition-Unity".
/// </summary>
public class AudioInput : MonoBehaviour
{
#if UNITY_EDITOR
    protected DictationRecognizer dictationRecognizer;
#elif UNITY_ANDROID
    GameObject dialog = null;
#endif

    private string final_result;
    public GameObject speechFeedback;

#if UNITY_EDITOR
    void Start()
    {
        speechFeedback.GetComponent<Text>().text = "";
        final_result = "";
    }

    private void DictationRecognizer_OnDictationHypothesis(string text)
    {
        Debug.Log("Dictation hypothesis: " + text);
        speechFeedback.GetComponent<Text>().text = text;
    }
    private void DictationRecognizer_OnDictationComplete(DictationCompletionCause completionCause)
    {
        switch (completionCause)
        {
            case DictationCompletionCause.TimeoutExceeded:
            case DictationCompletionCause.PauseLimitExceeded:
            case DictationCompletionCause.Canceled:
            case DictationCompletionCause.Complete:
                // Restart required
                Stop();
                break;
            case DictationCompletionCause.UnknownError:
            case DictationCompletionCause.AudioQualityFailure:
            case DictationCompletionCause.MicrophoneUnavailable:
            case DictationCompletionCause.NetworkFailure:
                // Error
                Stop();
                break;
        }
    }
    private void DictationRecognizer_OnDictationResult(string text, ConfidenceLevel confidence)
    {
        Debug.Log("Dictation result: " + text);
        speechFeedback.GetComponent<Text>().text = text;
        final_result = text;
    }
    private void DictationRecognizer_OnDictationError(string error, int hresult)
    {
        Debug.Log("Dictation error: " + error);
    }
    private void OnApplicationQuit()
    {
        Stop();
    }
    public void Record()
    {
        final_result = "";
        speechFeedback.GetComponent<Text>().text = "";
        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationHypothesis += DictationRecognizer_OnDictationHypothesis;
        dictationRecognizer.DictationResult += DictationRecognizer_OnDictationResult;
        dictationRecognizer.DictationComplete += DictationRecognizer_OnDictationComplete;
        dictationRecognizer.DictationError += DictationRecognizer_OnDictationError;
        dictationRecognizer.Start();
    }
    public string Stop()
    {
        if (dictationRecognizer != null)
        {
            string toReturn = final_result;
            dictationRecognizer.DictationHypothesis -= DictationRecognizer_OnDictationHypothesis;
            dictationRecognizer.DictationComplete -= DictationRecognizer_OnDictationComplete;
            dictationRecognizer.DictationResult -= DictationRecognizer_OnDictationResult;
            dictationRecognizer.DictationError -= DictationRecognizer_OnDictationError;
            if (dictationRecognizer.Status == SpeechSystemStatus.Running)
            {
                dictationRecognizer.Stop();
            }
            dictationRecognizer.Dispose();
            return toReturn;
        }
        return "";
    }
#elif UNITY_ANDROID

    void Start()
    {
        Setting("en-US");
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            dialog = new GameObject();
        }
    }
    

    public void Record()
    {
        Debug.Log("starting to record...");
        speechFeedback.GetComponent<Text>().text = "";
        final_result = "";
        SpeechToText.instance.onResultCallback = OnResultSpeech;
        SpeechToText.instance.StartRecording("Speak any");
    }

    public string Stop()
    {
        Debug.Log("stopping record");
        SpeechToText.instance.StopRecording();
        return final_result;
    }
    void OnResultSpeech(string _data)
    {
        Debug.Log(_data);
        speechFeedback.GetComponent<Text>().text = _data;
        final_result = _data;
    }
    public void OnClickSpeak()
    {
        //TextToSpeech.instance.StartSpeak(inputText.text);
    }
    public void  OnClickStopSpeak()
    {
        //TextToSpeech.instance.StopSpeak();
    }
    public void Setting(string code)
    {
        SpeechToText.instance.Setting(code);
    }
#endif
}
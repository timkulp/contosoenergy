using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class QandAManager : MonoBehaviour {
    // Support classes
    [Serializable]
    public struct Question
    {
        public string question;
    }

    [Serializable]
    public struct Answer
    {
        public string answer;
        public double score;
    }

    public string KnowledgeBaseId;
    public string SubscriptionKey;
    public AudioSource Listening;
    public AudioSource ListeningEnd;
    public string EndCommandText;

    private DictationRecognizer recognizer;
    private bool isListening;
    private string currentQuestion;


	public void StartListening()
	{
		if (!isListening)
		{
            PhraseRecognitionSystem.Shutdown();
            recognizer = new DictationRecognizer();
            recognizer.DictationError += Recognizer_DictationError;
            recognizer.DictationComplete += Recognizer_DictationComplete;
            recognizer.DictationResult += Recognizer_DictationResult;
            recognizer.Start();
			isListening = true;
            Listening.Play(0);
		}
	}

	public void StopLisenting()
	{
		recognizer.Stop();
		isListening = false;
        ListeningEnd.Play(0);
        recognizer.DictationError -= Recognizer_DictationError;
        recognizer.DictationComplete -= Recognizer_DictationComplete;
        recognizer.DictationResult -= Recognizer_DictationResult;
        recognizer.Dispose();
        PhraseRecognitionSystem.Restart();
	}

	private void Recognizer_DictationResult(string text, ConfidenceLevel confidence)
	{
        if (confidence == ConfidenceLevel.Medium || confidence == ConfidenceLevel.High){
            currentQuestion = text;
            if (currentQuestion.ToLower() == EndCommandText.ToLower()) {
                StopLisenting();
            }
            else {
                GetResponse();
            }
        }
	}
	private void Recognizer_DictationError(string error, int hresult)
	{
        throw new Exception(error);
	}
	private void Recognizer_DictationComplete(DictationCompletionCause cause)
	{
        if (cause != DictationCompletionCause.Complete) {
            ShowError();
        }
	}

    private void GetResponse(){
        StartCoroutine(getResponse());
    }

    private IEnumerator getResponse(){
		string url = "https://westus.api.cognitive.microsoft.com/" + 
            "qnamaker/v1.0/knowledgebases/{0}/generateAnswer";
		url = string.Format(url, KnowledgeBaseId);

		Question q = new Question() { question = currentQuestion };
		string questionJson = JsonUtility.ToJson(q);
		byte[] questionBytes = System.Text.UTF8Encoding.UTF8.GetBytes(questionJson);

        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
        headers.Add("Content-Type", "application/json");

        WWW w = new WWW(url, questionBytes, headers);

		yield return w;

		if (w.isDone)
		{
			if (string.IsNullOrEmpty(w.error))
			{
                Answer answer = JsonUtility.FromJson<Answer>(w.text);

                // TODO: Read the answer
                TextToSpeechManager tts = GetComponent<TextToSpeechManager>();
                tts.Say(answer.answer);
			}
		}
	}

    private void ShowError(){
        
    }


    
}

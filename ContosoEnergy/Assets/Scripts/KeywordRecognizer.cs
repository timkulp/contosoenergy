using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Windows.Speech;

public class KeywordRecognizer : MonoBehaviour {

    [System.Serializable]
	public struct KeywordAndResponse {
		public string Keyword;
		public UnityEvent Response;
	}


	[Tooltip("The keywords to listen for from the user")]
	public KeywordAndResponse[] Keywords;
	
	[Tooltip("Whether or not to start the Keyword Recognizer automatically")]
	public bool StartAutomatically;

	private UnityEngine.Windows.Speech.KeywordRecognizer recognizer;

	// Use this for initialization
	void Start () {

		List<string> keywords = new List<string>();
		foreach(var keyword in Keywords)
			keywords.Add(keyword.Keyword);

        this.recognizer = new UnityEngine.Windows.Speech.KeywordRecognizer(keywords.ToArray());
		this.recognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
		
		if(this.StartAutomatically)
			this.recognizer.Start();
		
	}

    /// <summary>
    /// When a word is recognized, loop through the keywords to find the 
    /// appropriate action
    /// </summary>
    /// <param name="args">PhraseRecognizedEventArgs to determine what was said
    /// and what to do</param>
	private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args){
		foreach(var keyword in Keywords){
			if(keyword.Keyword == args.text){
				keyword.Response.Invoke();
				return;
			}
		}
	}
}

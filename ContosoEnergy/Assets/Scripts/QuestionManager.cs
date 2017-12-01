using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;

using UnityEngine;
using UnityEngine.Windows.Speech;

public class QuestionManager : MonoBehaviour
{

    public string ApiSecret;
    public bool IsListening;
    public bool IsSpeaking;
    public float WaitTime;

    private Dictionary<string, string> apiUrls;
    private DictationRecognizer recognizer;
    private AuthToken token;
    private ConversationStream currentConversation;
    private bool isMessageSent;
    private string userId;
    private string lastWatermark;
    private float currentWaitTime = 0.0f;

    #region Manage Conversation
    public void StartListening(){
        if(!IsListening){
            recognizer.Start();
            IsListening = true;
        }
    }

    public void StopLisenting(){
        if(recognizer.Status == SpeechSystemStatus.Running){
			recognizer.Stop();
			IsListening = false;    
        }
    }

    public void StartConversations()
    {
        string responseFromServer = sendEmptyPost(apiUrls["ConversationStart"], token.token);
        currentConversation = JsonUtility.FromJson<ConversationStream>(responseFromServer);
        userId = string.Format("user_{0}", currentConversation.conversationId);        
    }
    public void EndConversation()
    {
        SendActivity("endOfConversation", "Good bye!");
    }
    public void SendActivity(string activityType, string text)
    {
        Activity activity = new Activity();
        activity.type = activityType;
        activity.text = text;
        activity.from = new ChannelAccount();
        activity.from.id = userId;

        StartCoroutine(_sendActivity(activity));
    }
    public void GetActivity()
    {
        StartCoroutine(_getActivity());
    }
	#endregion

	#region Certificate Management
	public bool certificateValidationCallback(System.Object sender,
	X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
		bool isOk = true;
		// If there are errors in the certificate chain, 
		// look at each error to determine the cause. 
		if (sslPolicyErrors != SslPolicyErrors.None)
		{
			for (int i = 0; i < chain.ChainStatus.Length; i++)
			{
				if (chain.ChainStatus[i].Status == X509ChainStatusFlags.RevocationStatusUnknown)
				{
					continue;
				}
				chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
				chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
				chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
				chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
				bool chainIsValid = chain.Build((X509Certificate2)certificate);
				if (!chainIsValid)
				{
					isOk = false;
					break;
				}
			}
		}
		return isOk;
	}
	#endregion

	#region Display Conversation

	#endregion


	// Use this for initialization
	void Start()
    {
        apiUrls = new Dictionary<string, string>();
        apiUrls.Add("TokenGenerate", "https://directline.botframework.com/v3/directline/tokens/generate");
        apiUrls.Add("TokenRefresh", "https://directline.botframework.com/v3/directline/tokens/refresh");
        apiUrls.Add("ConversationStart", "https://directline.botframework.com/v3/directline/conversations");
        apiUrls.Add("Activity", "https://directline.botframework.com/v3/directline/conversations/{0}/activities");

		recognizer = new DictationRecognizer();
		recognizer.DictationError += Recognizer_DictationError;
		recognizer.DictationComplete += Recognizer_DictationComplete;
		recognizer.DictationResult += Recognizer_DictationResult;

        Authenticate();

        currentWaitTime = WaitTime;
    }

    void Update(){
        if (isMessageSent && currentWaitTime <= 0.0f){
            currentWaitTime = WaitTime;
            GetActivity();
        }
        else if(isMessageSent){
            currentWaitTime -= Time.deltaTime;
        }
    }

    void OnDestroy(){
        if (IsListening)
            recognizer.Dispose();

        if(IsSpeaking){
            
        }
    }

    private string sendEmptyPost(string url, string token){
        ServicePointManager.ServerCertificateValidationCallback = certificateValidationCallback;
		string postData = string.Empty;
		byte[] postDataBytes = System.Text.Encoding.UTF8.GetBytes(postData);

		WebRequest request = WebRequest.Create(url);
        request.Method = "POST";
        request.Headers.Add("Authorization", "Bearer " + token);
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = postDataBytes.Length;

        using (System.IO.Stream requestStream = request.GetRequestStream())
        {
            requestStream.Write(postDataBytes, 0, postDataBytes.Length);
            requestStream.Close();

            WebResponse response = request.GetResponse();
            System.IO.Stream responseStream = response.GetResponseStream();
            System.IO.StreamReader reader = new System.IO.StreamReader(responseStream);
            return reader.ReadToEnd();
        }
    }

    private void Authenticate(){
        // TODO: Add Logic to detect refresh here

        string responseFromServer = sendEmptyPost(apiUrls["TokenGenerate"], ApiSecret);
        token = JsonUtility.FromJson<AuthToken>(responseFromServer);
    }

    private IEnumerator _sendActivity(Activity activity)
    {
        string json = JsonUtility.ToJson(activity);
        byte[] buffer = System.Text.UTF8Encoding.UTF8.GetBytes(json);
        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Authorization", "Bearer " + token.token);
        headers.Add("Content-Type", "application/json");

        string url = string.Format(apiUrls["Activity"], currentConversation.conversationId);
        WWW w = new WWW(url, buffer, headers);
        yield return w;

        if(w.isDone){
            if(string.IsNullOrEmpty(w.error)){
                var activityResponse = JsonUtility.FromJson<ActivityResponse>(w.text);
                activity.id = activityResponse.id;
                isMessageSent = true;
            }
        }
    }
    private IEnumerator _getActivity()
    {
        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Authorization", "Bearer " + currentConversation.token);

        string url = string.Format(apiUrls["Activity"], currentConversation.conversationId);
        WWW w = new WWW(url, null, headers);
        yield return w;

        if(w.isDone){
            if(string.IsNullOrEmpty(w.error)){
                ActivitySet activitySet = JsonUtility.FromJson<ActivitySet>(w.text);
                lastWatermark = activitySet.watermark;
                isMessageSent = false;

                //TODO: Add Speaking Here

                //TODO: Add UI Output Here
            }
        }
    }

    private void Recognizer_DictationResult(string text, ConfidenceLevel confidence){
        
    }
    private void Recognizer_DictationError(string error, int hresult){
        
    }
    private void Recognizer_DictationComplete(DictationCompletionCause cause){
        
    }


	#region Support Classes for Conversation

    [Serializable]
    public class AuthToken {
        public string conversationId;
        public string token;
        public int expires_in;
    }
    [Serializable]
    public class ConversationStream : AuthToken {
        public string streamUrl;
    }

	/// <summary> 
	/// Activity object used to communicate to and from bots 
	/// </summary> 
	[Serializable]
	public class Activity
	{
		[DataMember(EmitDefaultValue = false)]
		public string action;
		[DataMember(EmitDefaultValue = false)]
		public Attachment[] attachments;
		[DataMember(EmitDefaultValue = false)]
		public string attachmentLayout;
		[DataMember(EmitDefaultValue = false)]
		public object channelData;
		[DataMember(EmitDefaultValue = false)]
		public string channelId;
		[DataMember(EmitDefaultValue = false)]
		public ConversationAccount conversation;
		[DataMember(EmitDefaultValue = false)]
		public object[] entities;
		[DataMember(EmitDefaultValue = false)]
		public ChannelAccount from = new ChannelAccount();
		[DataMember(EmitDefaultValue = false)]
		public bool historyDisclosed;
		[DataMember(EmitDefaultValue = false)]
		public string id;
		[DataMember(EmitDefaultValue = false)]
		public string inputHint;
		[DataMember(EmitDefaultValue = false)]
		public string locale;
		[DataMember(EmitDefaultValue = false)]
		public string localTimestamp;
		[DataMember(EmitDefaultValue = false)]
		public ChannelAccount[] membersAdded;
		[DataMember(EmitDefaultValue = false)]
		public ChannelAccount[] membersRemoved;
		[DataMember(EmitDefaultValue = false)]
		public ChannelAccount recipient;
		[DataMember(EmitDefaultValue = false)]
		public ConversationReference relatesTo;
		[DataMember(EmitDefaultValue = false)]
		public string replyTo;
		[DataMember(EmitDefaultValue = false)]
		public string serviceUrl;
		[DataMember(EmitDefaultValue = false)]
		public string speak;
		[DataMember(EmitDefaultValue = false)]
		public SuggestedActions suggestedActions;
		[DataMember(EmitDefaultValue = false)]
		public string summary;
		[DataMember(EmitDefaultValue = false)]
		public string text;
		[DataMember(EmitDefaultValue = false)]
		public string textFormat;
		[DataMember(EmitDefaultValue = false)]
		public string tTimeStamp;
		[DataMember(EmitDefaultValue = false)]
		public string topicName;
		[DataMember(EmitDefaultValue = false)]
		public string type;
	}
	[Serializable]
	public class ActivityResponse
	{
		public string id;
	}
	/// <summary> 
	/// See https://docs.microsoft.com/en-us/bot-framework/rest-api/bot-framework-rest-connector-api-reference#attachment-object 
	/// </summary> 
	[Serializable]
	public class Attachment
	{
		public string contentType;
		public string contentUrl;
		public object content;
		public string name;
		public string thumbNailUrl;
	}
	/// <summary> 
	/// See https://docs.microsoft.com/en-us/bot-framework/rest-api/bot-framework-rest-connector-api-reference#conversationaccount-object 
	/// </summary> 
	[Serializable]
	public class ConversationAccount
	{
		public string id;
		public bool isGroup;
		public string name;
	}
	/// <summary> 
	/// See https://docs.microsoft.com/en-us/bot-framework/rest-api/bot-framework-rest-connector-api-reference#channelaccount-object 
	/// </summary> 
	[Serializable]
	public class ChannelAccount
	{
		public string id;
		[DataMember(EmitDefaultValue = false)]
		public string name;
	}
	/// <summary> 
	/// See https://docs.microsoft.com/en-us/bot-framework/rest-api/bot-framework-rest-connector-api-reference#conversationreference-object 
	/// </summary> 
	[Serializable]
	public class ConversationReference
	{
		public string activityId;
		public ChannelAccount bot;
		public string channelId;
		public ConversationAccount conversation;
		public string serviceUrl;
		public ChannelAccount user;
	}
	/// <summary> 
	/// See https://docs.microsoft.com/en-us/bot-framework/rest-api/bot-framework-rest-connector-api-reference#suggestedactions-object 
	/// </summary> 
	[Serializable]
	public class SuggestedActions
	{
		public string[] to;
		public CardAction[] actions;
	}
	/// <summary> 
	/// See https://docs.microsoft.com/en-us/bot-framework/rest-api/bot-framework-rest-connector-api-reference#cardaction-object 
	/// </summary> 
	[Serializable]
	public class CardAction
	{
		public string type;
		public string title;
		public string image;
		public string value;
	}
	/// <summary> 
	/// See https://docs.microsoft.com/en-us/bot-framework/rest-api/bot-framework-rest-direct-line-3-0-api-reference#activityset-object 
	/// </summary> 
	[Serializable]
	public class ActivitySet
	{
		public Activity[] activities;
		public string watermark;
	}

	#endregion

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Networking;

public class TextToSpeechManager : MonoBehaviour {

    public string SubscriptionKey = "1fa98c8ad11b420e9bbf47c0cc8f66ed";
    public string SSMLMarkup = "<speak version='1.0' xml:lang='en-US'><voice xml:lang='en-US' xml:gender='{0}' name='Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)'>{1}</voice></speak>";
    public Genders Gender = Genders.Female;
    public AudioSource audioSource;

    private string token;
    private string ssml;

    public void Say(string text){
        ssml = string.Format(SSMLMarkup, System.Enum.GetName(typeof(Genders), Gender), text);
        byte[] ssmlBytes = System.Text.UTF8Encoding.UTF8.GetBytes(ssml);

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://speech.platform.bing.com/synthesize");
        request.Method = "POST";
        request.Headers.Add("X-Microsoft-OutputFormat", "riff-16khz-16bit-mono-pcm");
        request.Headers.Add("Authorization", "Bearer " + token);
        request.ContentType = "application/ssml+xml";
        request.SendChunked = false;
        request.ContentLength = ssmlBytes.Length;
        request.UserAgent = "ContosoEnergy";    

        System.IO.Stream postData = request.GetRequestStream();
        postData.Write(ssmlBytes, 0, ssmlBytes.Length);
        postData.Close();

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        string path = string.Format("{0}\\assets\\tmp\\{1}.wav", 
            System.IO.Directory.GetCurrentDirectory(), 
            DateTime.Now.ToString("yyyy_mm_dd_HH_nn_ss"));

        using (Stream fs = File.OpenWrite(path))
        using (Stream responseStream = response.GetResponseStream())
        {
            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fs.Write(buffer, 0, bytesRead);
            }
        }

        StartCoroutine(say(path));
        

    }

    private IEnumerator say(string path)
    {
        WWW w = new WWW(path);
        yield return w;

        if (w.isDone)
        {
            if (string.IsNullOrEmpty(w.error))
            {
                audioSource.clip = w.GetAudioClip(false, true, AudioType.WAV);
                audioSource.Play();
            }
            else
            {
                throw new System.Exception(w.error);
            }
        }        
    }

    void Start(){
        ServicePointManager.ServerCertificateValidationCallback = remoteCertificateValidationCallback;
        token = getToken();
    }

    void Update()
    {
        
    }

    private string getToken(){
        WebRequest request = WebRequest.Create("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");
        request.Headers.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
        request.Method = "POST";

        WebResponse response = request.GetResponse();
		System.IO.Stream responseStream = response.GetResponseStream();
		System.IO.StreamReader reader = new System.IO.StreamReader(responseStream);
		return reader.ReadToEnd();
	}

    private bool remoteCertificateValidationCallback(System.Object sender,
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

    public enum Genders {
        Male,
        Female
    }

}

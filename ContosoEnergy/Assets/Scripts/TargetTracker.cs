using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class TargetTracker : MonoBehaviour, ITrackableEventHandler {

    private TrackableBehaviour mTrackableBehaviour;
    private QandAManager qnaManager;

    public string KnowledgeBaseId;

    public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus){
        if (newStatus == TrackableBehaviour.Status.DETECTED ||
            newStatus == TrackableBehaviour.Status.TRACKED ||
            newStatus == TrackableBehaviour.Status.EXTENDED_TRACKED){
            qnaManager.KnowledgeBaseId = KnowledgeBaseId;
        }
    }

    // Use this for initialization
    void Start () {
        mTrackableBehaviour = GetComponent<TrackableBehaviour>();
        if (mTrackableBehaviour) {
            mTrackableBehaviour.RegisterTrackableEventHandler(this);
        }
        qnaManager = GetComponent<QandAManager>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}

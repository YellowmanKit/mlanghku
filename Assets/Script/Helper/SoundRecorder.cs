using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class SoundRecorder : MonoBehaviour {
	
	public static SoundRecorder _instance = null;
	internal static SoundRecorder Instance { get { return _instance; } }
	
	public delegate void EventHandler(AudioClip clip);
	public event EventHandler		_responseHandler = null;

	public Text						_txtLength = null;

	// recording
	private bool					_dynIsRecording = false;

	private AudioSource 			_audioSource;
	private int 					minFreq;
	private int 					maxFreq;
	private float					timeSinceRecordStarted = 0;
	//private float					maxRecordingLength;
	
	void Awake () {
		_instance = this;
	}
	
	void OnDestroy () {
		if (_audioSource.clip != null) {
			Destroy(_audioSource.clip);
		}
	}

	// Use this for initialization
	void Start () {
		_audioSource = this.GetComponent<AudioSource>();
		
		RectTransform t = GetComponent<RectTransform>();
		t.anchoredPosition = new Vector2(0, 0);
		
		gameObject.SetActive(false);
		/*
		#if UNITY_WEBPLAYER
		RequestAuthorize();
#elif UNITY_WEBGL
		RequestAuthorize();
		#endif
*/
	}

	void Update () {
		if (_dynIsRecording) {
			timeSinceRecordStarted += Time.deltaTime;
			//_txtLength.text = Mathf.Floor(timeSinceRecordStarted / 60f).ToString("00") + " : " + ((int)Mathf.Floor(timeSinceRecordStarted) % 60).ToString("00");
			_txtLength.text = "Time left : " + Mathf.Clamp((60 - Mathf.Floor(timeSinceRecordStarted)),0,60).ToString("F0");
			if (timeSinceRecordStarted > 61f) {
				_txtLength.text = "Stop!";
			}
		}
	}
	
	public void ShowSoundRecording (EventHandler handler) {
		_responseHandler = handler;
		
		gameObject.SetActive (true);

		// reset parameter
		_dynIsRecording = false;
		_txtLength.text = "60";
		if (_audioSource.clip != null) {
			Destroy(_audioSource.clip);
			_audioSource.clip = null;
		}
		timeSinceRecordStarted = 0;

		EventRecordingBegin();
	}

	private void EventRecordingBegin () {
		
		if(Microphone.devices.Length <= 0) {
			AlertTask fail = new AlertTask(
				GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
				, GameManager.Instance.Language.getString("no-microphone", CaseHandle.FirstCharUpper)
				, TextAnchor.MiddleLeft);
			fail.buttonCompletedEvent_0 += EventButtonCancel;
			GameManager.Instance.AlertTasks.Add (fail);

			EventButtonCancel();
		} else {
			//Set 'micConnected' to true
			//Get the default microphone recording capabilities
			Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);
			
			//According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...
			if(minFreq == 0 && maxFreq == 0) {
				maxFreq = 44100;
			}

			// record begin
			_dynIsRecording = true;

			if (_audioSource.clip != null) {
				Destroy(_audioSource.clip);
			}
			_audioSource.clip = Microphone.Start(null, true, 60, maxFreq);
			timeSinceRecordStarted = 0;
		}
	}

	private void EventSoundRecordEnd () {
		
		Microphone.End(null);
		_dynIsRecording = false;
		
		//timeSinceRecordStarted += 1;
		
		// shorten
		AudioClip ac = _audioSource.clip;
		float lengthL = ac.length;
		float samplesL = ac.samples;
		float samplesPerSec = (float)samplesL/lengthL;
		float[] samples = new float[(int)(samplesPerSec * timeSinceRecordStarted)];
		ac.GetData(samples,0);
		Destroy(_audioSource.clip);
		_audioSource.clip = AudioClip.Create("RecordedSound",(int)(timeSinceRecordStarted*samplesPerSec),1,maxFreq,false);
		_audioSource.clip.SetData(samples,0);

	}
	
	public void EventButtonCancel () {
		_responseHandler (null);
		gameObject.SetActive(false);
	}
	
	public void EventButtonDone () {
		EventSoundRecordEnd ();
		_responseHandler (_audioSource.clip);
		_audioSource.clip = null;

		gameObject.SetActive(false);
	}
}

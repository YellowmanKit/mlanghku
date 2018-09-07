using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using SimpleJSON;
using WWUtils.Audio;

public class AddCard_LangRow : MonoBehaviour
{
//	private bool					isSetup = false;

	public Card						card = null;
	internal string					langKey {
		get {
			if (cardLang != null)
				return cardLang.langKey;
			else
				return "";
		}
	}
	public CardLang					cardLang = null;

	/*
	public string					langKey = "";
	internal string					langName {
		get { return _inputName.text; }
	}
	*/

	public Text						_txtLangName = null;
	public InputField				_inputName = null;

	public Button					_btnRecord = null;
	public Button					_btnPlay = null;
	public GameObject				_btnDelete = null;
	public Sprite[]					_spritePlayButton = null;
	public Text						_txtRecordingLength = null;
	public Image					_imgPlayProgessFill = null;
	
	private bool					_isSoundChanged = false;
	internal bool					isSoundChanged { get { return _isSoundChanged; } }
	private bool					_isTextChanged = false;
	internal bool					isTextChanged { get { return _isTextChanged; } }
	private AudioSource 			_audioSource;

	// Use this for initialization
	void Start () {
		if (_audioSource == null) {	
			_audioSource = this.GetComponent<AudioSource>();
			
			_btnPlay.gameObject.SetActive(false);
			_btnPlay.transform.FindChild("Image").GetComponent<Image>().sprite = _spritePlayButton[0];
		}
		//_txtRecordingLength.text = "00:00";
		_imgPlayProgessFill.fillAmount = 0;
	}
	
	// Update is called once per frame
	void Update () {
		if (_audioSource != null && _audioSource.isPlaying) {
			_imgPlayProgessFill.fillAmount = _audioSource.time / _audioSource.clip.length;
		}

	}

	void OnDestroy() {
		if (_audioSource.clip != null) {
			Destroy(_audioSource.clip);
		}
	}

	public void Init (Card _card, CardLang _lang) {

		gameObject.name = _lang.langKey;

		_txtLangName.text = GameManager.Instance.Language.getString("lang-" + _lang.langKey, CaseHandle.FirstCharUpper);
		_audioSource = this.GetComponent<AudioSource>();

//		if (_audioSource.clip != null) {
//			Destroy(_audioSource.clip);
//		}
		_audioSource.clip = null;

		_btnPlay.gameObject.SetActive(false);
		_btnPlay.transform.FindChild("Image").GetComponent<Image>().sprite = _spritePlayButton[0];

		//If lang hv sound, put it


		//
		_txtRecordingLength.text = "00:00";

		card = _card;
		cardLang = _lang;
		_inputName.text = cardLang.getName();
		
		if (cardLang.isSoundExist) {
			try {
				WAV wav = new WAV(cardLang.SoundPath);
				
				AudioClip audioClip = AudioClip.Create("temp", wav.SampleCount, 1, wav.Frequency,false);
				audioClip.SetData(wav.LeftChannel, 0);

				_audioSource.clip = audioClip;
				
				_btnPlay.transform.FindChild("Image").GetComponent<Image>().sprite = _spritePlayButton[0];
				_btnPlay.gameObject.SetActive(true);
			} catch (Exception e) {
				Debug.LogError(e.Message);
			}
		}

		if (_lang.langKey.Equals("zh_w")) {	// default lang
			_btnDelete.SetActive(false);
		} else {
			_btnDelete.SetActive(true);
		}

		RenewSoundButton();
	}
	
	public void EventButtonRecording () {
		_imgPlayProgessFill.fillAmount = 0;
		_audioSource.Stop();
		SoundRecorder.Instance.ShowSoundRecording(SoundRecordCompletedHandler);
	}
	
	private void SoundRecordCompletedHandler (AudioClip clip) {
		if (clip != null) {
			// new clip received
			_imgPlayProgessFill.fillAmount = 0;
			_audioSource.Stop();
			if (_audioSource.clip != null) {
				Destroy(_audioSource.clip);
			}
			_audioSource.clip = clip;
			_isSoundChanged = true;
		}
		
		RenewSoundButton();
	}
	
	private void RenewSoundButton () {
		//Debug.Log ("Renew Soundbtn!!");
		if (_audioSource == null){
			//Debug.Log ("No audiosource!");
			return;
		} else if (_audioSource.clip == null) {
			//Debug.Log ("No audioclip!");
			_txtRecordingLength.text = "00:00";
			_btnPlay.gameObject.SetActive(false);
		} else {
			float length = Mathf.Clamp(_audioSource.clip.length,0f,60f);
			//Debug.Log ("Yup! Clip length :" + length);
			length = Mathf.Ceil (length);
			//Debug.Log ((length % 60f).ToString("00"));
			_txtRecordingLength.text = Mathf.Floor(length / 59f).ToString("00") + ":" + (length % 60f).ToString("00");
			_btnPlay.gameObject.SetActive(true);
		}
	}
	
	public void EventButtonPlaySound () {
		if (_audioSource.clip != null) {
			_audioSource.Stop ();
			_imgPlayProgessFill.fillAmount = _audioSource.time / _audioSource.clip.length;
			_audioSource.Play();
		}
	}

	public void ApplyToCardLang () {
		if (cardLang == null) {
			// featured? !!!
			cardLang = new CardLang(card.cardId, langKey, null);
		}

		if (!cardLang.getName().Equals(_inputName.text)) {
			_isTextChanged = true;
			cardLang.name = _inputName.text;
		}
		
		// sound managerment
		if (_isSoundChanged) {
			cardLang.soundLastUpdatedAt = GameManager.GetCurrentTimestemp();
		}

	}

	public void WriteToFile () {
		if (_audioSource != null && _audioSource.clip != null && _isSoundChanged) {
			Debug.Log ("write sound " + langKey);
			string soundPath = cardLang.SoundPath;
			SavWav.Save (soundPath, _audioSource.clip);
		} else if (cardLang.isDeleted) {
			LocalData.Instance.SafelyDelete(cardLang.SoundPath);
		} else {
			//Debug.Log ("no change");
		}
	}

	public bool IsCompleted () {
		if (_audioSource.clip == null)
			return false;

		if (string.IsNullOrEmpty(_inputName.text))
		    return false;

		return true;
	}
}

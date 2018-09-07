//----------------------------------------------------------------
// mLang v1.3
//----------------------------------------------------------------
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Parse;
using SimpleJSON;
using WWUtils.Audio;

public class ViewCard_LangRow : MonoBehaviour
{
	private CardLang 	   	lang;
	private CardController 	controller;

	public Image		   	_imgLang = null;
	public Text				_txtName = null;
	public Text				_txtLangName = null;
	public Button			_btnPlay = null;

	private AudioClip 		audioClip = null;

	/*
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	*/

	void OnDestroy() {
		if (audioClip != null) {
			Destroy(audioClip);
			audioClip = null;
		}
	}

	public void Init (string cardId, CardLang _lang, CardController _controller)
	{
		Debug.Log ("[ViewCard_LangRow] Init()");
		name = _lang.langKey;
		lang = _lang;
		controller = _controller;

		_txtName.text = lang.getName();

		//_imgLang.sprite = DownloadManager.GetNationFlagByLangKey(lang.langKey); // v1.3 useless?
	
		if (lang.isSoundExist) {
			Debug.Log ("[ViewCard_LangRow] Init() lang.isSoundExist == true");
			_btnPlay.interactable = true;
		} else {
			Debug.Log ("[ViewCard_LangRow] Init() lang.isSoundExist == false");
			_btnPlay.interactable = false;
			//Debug.Log ("[ViewCard_LangRow] Init(): sound-not-exist: SoundName=" + lang.SoundName + " SoundPath=" + lang.SoundPath + " soundUrl=" + lang.soundUrl);
			//if (!string.IsNullOrEmpty(lang.soundUrl)) { // v1.3
				DownloadManager.Instance.AddLinkage(lang.SoundPath, lang.soundUrl, _btnPlay, true);
			//}
		}
		_txtLangName.text = GameManager.Instance.Language.getString("lang-" + _lang.langKey, CaseHandle.FirstCharUpper);

		//----------------------------------------------------------------
		// v1.3 show "..." if the text is too long on a book page view
		//----------------------------------------------------------------
		Canvas.ForceUpdateCanvases ();
		GameManager.Instance.CheckTextLength (_txtName);
		//----------------------------------------------------------------
	}
	
	//----------------------------------------------------------------
	// v1.3 added - click the row will show the TextView
	//----------------------------------------------------------------
	public void EventButtonShowText ()
	{
		TextViewer.Instance.showText (lang);
	}

	public bool TryToPlaySound () // v1.3 set to return bool instead of throw exception
	{
		if (audioClip == null) {
			if (lang.isSoundExist) {
				try {
					Debug.Log ("[ViewCard_LangRow] TryToPlaySound(): sound-exist: SoundPath=" + lang.SoundPath);

					WAV wav = new WAV (lang.SoundPath);
					audioClip = AudioClip.Create ("temp", wav.SampleCount, 1, wav.Frequency, false);
					audioClip.SetData (wav.LeftChannel, 0);
					controller.EventPlaySound (audioClip);
					return true;
				} catch (Exception e) {
					Debug.Log("TryToPlaySound catch exception:" + e.ToString());
					return false;
				} 
			} else {
				Debug.LogWarning ("[ViewCard_LangRow] TryToPlaySound(): sound-not-exist: " + lang.SoundPath);
				return true; // v1.3
			}
		} else {
			try {
				controller.EventPlaySound (audioClip);
				return true;
			} catch (Exception e) {
				Debug.Log("TryToPlaySound catch exception:" + e.ToString());
				return false;
			}
		} 
	}
	 
	public void EventButtonPlaySound () // v1.3 set to return bool instead of throw exception
	{
		if (TryToPlaySound () == false) {
			GameManager.Instance.AlertTasks.Add (
				new AlertTask(
					GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
					, GameManager.Instance.Language.getString("sound-file-not-ready", CaseHandle.FirstCharUpper)
					, TextAnchor.MiddleLeft));
		}

		/*
		if (audioClip == null) {
			if (lang.isSoundExist) {
				try {
					Debug.Log ("[ViewCard_LangRow] EventButtonPlaySound(): sound-exist: SoundPath=" + lang.SoundPath);

					WAV wav = new WAV(lang.SoundPath);
					
					audioClip = AudioClip.Create("temp", wav.SampleCount, 1, wav.Frequency,false);
					audioClip.SetData(wav.LeftChannel, 0);
					
					controller.EventPlaySound(audioClip);
				} catch (Exception e) {
					Debug.LogError(e.Message);
					GameManager.Instance.AlertTasks.Add (
						new AlertTask(
						GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
						, GameManager.Instance.Language.getString("sound-file-not-ready", CaseHandle.FirstCharUpper)
						, TextAnchor.MiddleLeft));
				}
			} else {
				Debug.LogWarning ("[ViewCard_LangRow] EventButtonPlaySound(): Not exist: " + lang.SoundPath);
			}
		} else {
			try { // v1.3
				controller.EventPlaySound(audioClip);
			} catch (Exception e) { // v1.3
			} // v1.3
		}
		*/
	}
}

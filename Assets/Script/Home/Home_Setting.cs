//-----------------------------------------------------
// mLang v1.3
//-----------------------------------------------------
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Parse;
using SimpleJSON;
using WWUtils.Audio;
using System.Text.RegularExpressions;

public class Home_Setting : Page {

	public GameObject _panelCustomeizedComment = null;

	/*
	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {

	}
	*/

	//-----------------------------------------------------
	// v1.3 commented out
	//-----------------------------------------------------
//	private void RedirectToLogin () {
//		Fader.Instance.FadeIn (0.5f)
//			.LoadLevel ("Login")
//				.FadeOut (0.5f);
//	}


	#region Socket
	public override	void Init (Dictionary<string, object> data) {
		ParseUserIdentity identity = LocalData.Instance.UserIdentity;
		switch (identity) {
		case ParseUserIdentity.Student:
			_panelCustomeizedComment.SetActive(false);
			break;
		case ParseUserIdentity.Teacher:
			_panelCustomeizedComment.SetActive(true);
			break;
		default:
			_panelCustomeizedComment.SetActive(false);
			break;
		}
	}

	public override void OnPageShown (Dictionary<string, object> data) {
		string navigationTitle = GameManager.Instance.Language.getString("profiles", CaseHandle.FirstCharUpperForEachWord);
		NavigationBar.Instance.SetTitle (navigationTitle);
	}
	#endregion
	
	public void EventButtonSetLang (int lang) {
		GameManager.Instance.SetLanguage(lang, true);
	}
	public void EventButtonCustomizeComment () {
		NavigationBar.Instance.PushView(NavigationView.Home_Setting_CustomizeComments);
	}
	public void EventButtonCredit () {
		string navigationTitle = GameManager.Instance.Language.getString("credit", CaseHandle.FirstCharUpperForEachWord);
		NavigationBar.Instance.SetTitle (navigationTitle);
		NavigationBar.Instance.PushView(NavigationView.Home_Setting_Credit);
	}
}

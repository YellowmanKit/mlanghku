//-----------------------------------------------------
// mLang v1.3
//-----------------------------------------------------
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Parse;
using System;

public class Home_SlideMenu : MonoBehaviour
{
	public	Text _txtUsername = null;
	public	Text _txtRole = null;

	private string logObjId;

	// Use this for initialization
	void Start () {
		if (LocalData.Instance.Data["user"] != null) {
			_txtUsername.text = LocalData.Instance.Data["user"]["realName"].Value + " <color=#838383FF>(" + LocalData.Instance.Data["user"]["username"].Value + ")</color>";
		} else {
			_txtUsername.text = "";
		}
		_txtRole.text = "";
	}

	private void CreateLoginRecord(){
		ParseObject loginHistory = new ParseObject ("LogInHistory");
		//loginHistory ["logInUser"] = ParseUser.CurrentUser;
		loginHistory["loginUserId"] = ParseUser.CurrentUser.ObjectId;
		loginHistory.SaveAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				logObjId = loginHistory.ObjectId;
				//Debug.Log(loginHistory.ObjectId);
			}else{
				//Debug.Log("Error: " + t.Exception.Message);
			}
		});
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	#region Event Button
	public void EventButtonHome () {
		NavigationBar.Instance.ContentShowHome ();
	}
	
	public void EventButtonProfiles () {
		NavigationBar.Instance.ContentShowProfile ();
	}
	
	public void EventButtonSetting () {
		NavigationBar.Instance.ContentShowSetting ();
	}

	public void EventButtonSignOut () {
		ParseUser.LogOutAsync ();
		CreateLogOutRecord ();
		
		Fader.Instance.FadeIn (0.5f)
			.LoadLevel ("Login")
				.FadeOut (0.5f);
	}

	private void CreateLogOutRecord(){
		if (LocalData.Instance.UserIdentity == ParseUserIdentity.Student) {
			ParseQuery<ParseObject> query = new ParseQuery<ParseObject> ("LogInHistory");
			query.GetAsync (logObjId).ContinueWith (t => {
				if (!t.IsFaulted) {
					ParseObject log = t.Result;
					DateTime dateNow = DateTime.Now;
					log ["loggedAt"] = dateNow;
					log.SaveAsync ();
				} else {
					Debug.Log ("Error: " + t.Exception.Message);
				}
			});
		}
	}
	#endregion

	#region Socket
	public void Init() 
	{
		//-------------------------------------------
		// Called by NavigationBar::DataLoaded()
		//-------------------------------------------
		Debug.Log ("[Home_SlideMenu] Init()");

		if (LocalData.Instance.Data["user"] != null) {
			_txtUsername.text = LocalData.Instance.Data["user"]["realName"].Value + " <color=#838383FF>(" + LocalData.Instance.Data["user"]["username"].Value + ")</color>";
		} else {
			_txtUsername.text = "";
		}

		if (LocalData.Instance.UserNode != null) {
			ParseUserIdentity identity = LocalData.Instance.UserIdentity;
			switch (identity) {
			case ParseUserIdentity.Student:
				CreateLoginRecord ();
				string courseTitle = LocalData.Instance.Data["my-course"][0]["courseTitle"].Value;
				if (string.IsNullOrEmpty(courseTitle)) {
					_txtRole.text = GameManager.Instance.Language.getString("student", CaseHandle.FirstCharUpper);
				} else {
					_txtRole.text = string.Format(
						GameManager.Instance.Language.getString("student-of-val", CaseHandle.FirstCharUpper),
						courseTitle);
				}
				break;
			case ParseUserIdentity.Teacher:
				_txtRole.text = GameManager.Instance.Language.getString("teacher", CaseHandle.FirstCharUpper);
				break;
			case ParseUserIdentity.SchoolAdmin:
				_txtRole.text = "School Admin";
				break;
			case ParseUserIdentity.SystemAdmin:
				_txtRole.text = "System Admin";
				break;
			default:
				_txtRole.text = "Unknown";
				break;
			}
		}
	}
	#endregion
}

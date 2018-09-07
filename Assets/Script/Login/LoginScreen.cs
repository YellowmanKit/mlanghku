//-----------------------------------------------------
// mLang v1.3
//-----------------------------------------------------
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Parse;
using SimpleJSON;
using System;
using System.IO;
using System.Text.RegularExpressions;

public class LoginScreen : MonoBehaviour 
{
	public enum LoginScreenState {
		CHECK_FOR_VERSION,
		CHECKING_FOR_VERSION,
		INIT,
		WAITING_FOR_LOGIN,
		LOGINED,
		WAITING_FOR_DATA,
		DATA_RETRIEVED,
		END
	}

	public 		LoginScreenState	state = LoginScreenState.CHECK_FOR_VERSION;

	public 		GameObject			_panelLogin = null;
	public 		InputField			_inputUsername = null;
	public		InputField			_inputPassword = null;

	public 		GameObject			_panelForgotPassword = null;
	public 		InputField			_inputForgotPasswordEmail = null;

	public 		GameObject			_panelTest = null;

	// Use this for initialization
	void Start () {
		//_inputUsername.text = "Teacher1A";
		//_inputPassword.text = "123456";
		_panelLogin.SetActive(true);
		_panelTest.SetActive(false);
		_panelForgotPassword.SetActive(false);
	}
	
	// Update is called once per frame
	void Update () {
		switch(state) {
		case LoginScreenState.CHECK_FOR_VERSION:
			/*
			lastConfigFetchedTimestamp = GameManager.Instance.LastConfigFetchedTimestamp;
			int currentTimestamp = GameManager.GetCurrentTimestemp();
			if (currentTimestamp - lastConfigFetchedTimestamp > (1 * 3600)) {
				
				LoadingTask versionTask = new LoadingTask("Checking for update...");
				//versionTask.DisplayMessage = "Checking for update...";
				//versionTask.CanCancel = true;
				versionTask.OnLoadingCompleted += ConfigLoaded;
				versionTask.OnLoadingCancel += ForceRedirectMenu;
				GameManager.Instance.LoadingTasks.Add (versionTask);
				
				FetchConfig(versionTask);
				
				state = LoginScreenState.CHECKING_FOR_VERSION;
			} else {
				state = CheckForUpdate();
			}
			*/
			state = LoginScreenState.INIT;
			break;
		case LoginScreenState.CHECKING_FOR_VERSION:
			break;
		case LoginScreenState.INIT:
			if (ParseUser.CurrentUser == null) {
				state = LoginScreenState.WAITING_FOR_LOGIN;
			} else {
				state = LoginScreenState.LOGINED;
			}
			break;
		case LoginScreenState.WAITING_FOR_LOGIN:
			break;
		case LoginScreenState.LOGINED:
			state = LoginScreenState.DATA_RETRIEVED;
			break;
		case LoginScreenState.WAITING_FOR_DATA:
			break;
		case LoginScreenState.DATA_RETRIEVED:
			Fader.Instance.FadeIn (0.5f)
				.LoadLevel ("Home_Teacher")
					.FadeOut (0.5f);

			state = LoginScreenState.END;
			break;
		case LoginScreenState.END:
			break;
		}
	}

	public void EventButtonLogin () {
		string username = _inputUsername.text;
		string password = _inputPassword.text;

        //Get server domain in user name
        string[] parts = username.Split(new char[]{'@'});
        if (parts.Length > 1) {
            if (parts.Length > 3) {
                GameManager.Instance.AlertTasks.Add (new AlertTask("INCOMPLETE INFO", "More that one '@' in user name"));
                _inputUsername.Select ();
                return;
            }

            string serverDomain = parts[1];
            Match m = Regex.Match(serverDomain, "[^\\.\\:\\w]");
            if (m.Success) {
                GameManager.Instance.AlertTasks.Add (new AlertTask("INCOMPLETE INFO", "Invalid character in server domain"));
                _inputUsername.Select ();
                return;
            }

            username = parts[0];
            ParseInitialize_New.instance.server = serverDomain == "parse" ? "parse" : "http://" + serverDomain + "/parse/";
        } else {
            ParseInitialize_New.instance.server = "";
        }

        ParseInitialize_New.instance.init();

		if (string.IsNullOrEmpty(username)) {
			GameManager.Instance.AlertTasks.Add (new AlertTask("INCOMPLETE INFO", "Please enter username"));
			_inputUsername.Select ();
			return;
		} else if (string.IsNullOrEmpty(password)) {
			GameManager.Instance.AlertTasks.Add (new AlertTask("INCOMPLETE INFO", "Please enter password"));
			_inputPassword.Select ();
			return;
		}
		
		LoginToServer(username, password);
	}
	
	public void EventButton_ShowForgotPassword () {
		_panelForgotPassword.SetActive(true);
	}
	
	public void EventButton_HideForgotPassword () {
		_panelForgotPassword.SetActive(false);
	}
	
	public void EventButton_SubmitForgotPassword ()
	{
		if (string.IsNullOrEmpty(_inputForgotPasswordEmail.text)) {
			GameManager.Instance.AlertTasks.Add (new AlertTask("INCOMPLETE INFO", "Please enter email"));
			_inputForgotPasswordEmail.Select ();
			return;
		}
		
		LoadingTask task = new LoadingTask("Sending Request...");
		//task.DisplayMessage = "Sending Request...";
		//task.CanCancel = true;
		GameManager.Instance.LoadingTasks.Add (task); // v1.3 2017/01/12 added - why didn't have this line in previous version?

		string email = _inputForgotPasswordEmail.text;
		ParseUser.RequestPasswordResetAsync(email).ContinueWith (t => {
			
			if (t.IsFaulted || t.IsCanceled) {
				// The login failed. Check the error to see why.
				foreach(var e in t.Exception.InnerExceptions) {
					ParseException parseException = (ParseException) e;
					Debug.Log("[LoginScreen] SubmitForgotPassword() Error message " + parseException.Message);
					Debug.Log("[LoginScreen] SubmitForgotPassword() Error code: " + parseException.Code);
					GameManager.Instance.AlertTasks.Add (
						new AlertTask(
						GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
						, parseException.Code + ": " + parseException.Message
						, TextAnchor.MiddleLeft));
				}
				//state = LoginScreenState.WAITING_FOR_LOGIN;
			} else {
				Debug.Log ("[LoginScreen] SubmitForgotPassword() success");
				GameManager.Instance.AlertTasks.Add (
					new AlertTask(GameManager.Instance.Language.getString("forgot-password-sent", CaseHandle.FirstCharUpper)
				              ));
			}
			task.Loading = false;
		});
	}
	
	public void EventButton_ShowTest () {
		_panelTest.SetActive(true);
	}

	public void EventButton_BackToMain () {
		PlayerPrefs.SetInt("testIdentity", 0);
		PlayerPrefs.Save();
		_panelTest.SetActive(false);
	}

	public void EventButton_TestAsStudent () {
		PlayerPrefs.SetInt("testIdentity", 1);
		PlayerPrefs.Save();
		LocalData.Instance.CreateTestData(1);
		state = LoginScreenState.DATA_RETRIEVED;
	}
	
	public void EventButton_TestAsTeacher () {
		PlayerPrefs.SetInt("testIdentity", 2);
		PlayerPrefs.Save();
		LocalData.Instance.CreateTestData(1);
		state = LoginScreenState.DATA_RETRIEVED;
	}

	public void EventButton_ClearTestData () {
		//string folderName = "testData";
		//string directioryPath = Path.Combine(Application.persistentDataPath, folderName);
		LocalData.Instance.SafelyDelete(Path.Combine(Application.persistentDataPath, "testData"));
	}
	
	#region Parse
	public void LoginToServer (string username, string password)
	{
		PlayerPrefs.SetInt("testIdentity", 0);
		PlayerPrefs.Save();

		LoadingTask task = new LoadingTask("Connecting...");
		//task.DisplayMessage = "Connecting...";
		//task.CanCancel = true;
		GameManager.Instance.LoadingTasks.Add (task);

		ParseUser.LogInAsync (username, password).ContinueWith (t => {
			
			if (t.IsFaulted || t.IsCanceled) {
				// The login failed. Check the error to see why.
				GameManager.Instance.AlertTasks.Add (new AlertTask ("Login Failed"));
				foreach(var e in t.Exception.InnerExceptions) {
					ParseException parseException = (ParseException) e;
					Debug.Log("[LoginScreen] LoginToServer() Error message " + parseException.Message);
					Debug.Log("[LoginScreen] LoginToServer() Error code: " + parseException.Code);
					GameManager.Instance.AlertTasks.Add (
						new AlertTask(
						GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
						, parseException.Code + ": " + parseException.Message
						, TextAnchor.MiddleLeft));
				}
				state = LoginScreenState.WAITING_FOR_LOGIN;
			} else {
				Debug.Log ("[LoginScreen] LoginToServer() Login success");
				state = LoginScreenState.LOGINED;
			}
			task.Loading = false;
		});
	}

	#endregion
}

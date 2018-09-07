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

public class Home_Profile : Page {
	public Text						_txtUsername = null;

	public InputField				_inputOriginPassword = null;
	public InputField				_inputNewPassword = null;
	public InputField				_inputNewPasswordConfirm = null;
	public InputField				_inputEmail = null;

	private string originEmail = "";

	// Use this for initialization
	void Start () {
		//_inputOriginPassword.text = "12345678";
		//_inputNewPassword.text = "12345678";
		//_inputNewPasswordConfirm.text = "12345678";
		//_inputEmail.text = "apple@apple.com";
	}
	
	// Update is called once per frame
	void Update () {
		switch (state) {
		case PageState.READY:
			break;
		case PageState.INIT:

			break;
		case PageState.WAITING_FOR_DATA:
			break;
		case PageState.DATA_RETRIEVED:
			if (!string.IsNullOrEmpty(_inputNewPassword.text)) {
				ParseUser.LogOutAsync();

				AlertTask logout = new AlertTask(
					GameManager.Instance.Language.getString("updated", CaseHandle.FirstCharUpper)
					, GameManager.Instance.Language.getString("login-again", CaseHandle.FirstCharUpper)
					, TextAnchor.MiddleLeft);
				logout.buttonCompletedEvent_0 += RedirectToLogin;

				GameManager.Instance.AlertTasks.Add (
					logout);

			} else {
				GameManager.Instance.AlertTasks.Add (
					new AlertTask(
					GameManager.Instance.Language.getString("updated", CaseHandle.FirstCharUpper)
					, GameManager.Instance.Language.getString("profile-is-updated", CaseHandle.FirstCharUpper)
					, TextAnchor.MiddleLeft));
			}
			state = PageState.END;

			break;
		case PageState.END:
			break;
		}
	}

	private void RedirectToLogin () {
		Fader.Instance.FadeIn (0.5f)
			.LoadLevel ("Login")
				.FadeOut (0.5f);
	}
	
	#region Event

	public void EventButtonSubmit () {
		// checking
		string originPassword = _inputOriginPassword.text;
		string newPassword = _inputNewPassword.text;
		string newPasswordConfirm = _inputNewPasswordConfirm.text;
		string email = _inputEmail.text;
		
		int errorCount = 0;
		string error = "";//GameManager.Instance.Language.getString("error-semi", CaseHandle.FirstCharUpper);
		if (!string.IsNullOrEmpty(newPassword) || !string.IsNullOrEmpty(newPasswordConfirm)) {
			if (string.IsNullOrEmpty(originPassword)) {
				errorCount++;
				error += "\n- " + GameManager.Instance.Language.getString("origin-password-empty", CaseHandle.FirstCharUpper);
			}
			if (newPassword.Length < 8) {
				errorCount++;
				error += "\n- " + GameManager.Instance.Language.getString("new-password-too-short", CaseHandle.FirstCharUpper);
			}
			if (!newPassword.Equals(newPasswordConfirm)) {
				errorCount++;
				error += "\n- " + GameManager.Instance.Language.getString("new-password-mismatch", CaseHandle.FirstCharUpper);
			}
		}

		if (string.IsNullOrEmpty(email)) {
			errorCount++;
			error += "\n- " + GameManager.Instance.Language.getString("email-empty", CaseHandle.FirstCharUpper);
		} else if (!IsValidEmail(email)) {
			errorCount++;
			error += "\n- " + GameManager.Instance.Language.getString("email-invalid", CaseHandle.FirstCharUpper);
		}

		if (errorCount > 0) {
			error = error.Remove(error.IndexOf("\n"),"\n".Length);
			GameManager.Instance.AlertTasks.Add (
				new AlertTask(
				GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
				, error
				, TextAnchor.MiddleLeft));
			return;
		}

		Debug.Log ("Pass");
		if (originEmail.Equals(email) && string.IsNullOrEmpty(newPassword)) {
			// no change, don't need to upload to Parse
		} else {
			// upload to parse
			SubmitData();
		}
	}

	#endregion

	#region Socket
	public override void OnPageShown (Dictionary<string, object> data) {
		string navigationTitle = GameManager.Instance.Language.getString("profiles", CaseHandle.FirstCharUpperForEachWord);
		NavigationBar.Instance.SetTitle (navigationTitle);
	}

	public override	void Init (Dictionary<string, object> data) {
		// if localData.user
		if (LocalData.Instance.Data.GetKeys().Contains("user")) {
			_txtUsername.text = LocalData.Instance.Data["user"]["realName"];
			originEmail = LocalData.Instance.Data["user"]["email"].Value;
			_inputEmail.text = originEmail;
			//Debug.Log (originEmail);
		} else {
			GameManager.Instance.AlertTasks.Add (
				new AlertTask(
				GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
				, GameManager.Instance.Language.getString("unable-to-load-user", CaseHandle.Normal)
				, TextAnchor.MiddleLeft));
		}
	}
	#endregion

	#region Helper
	
	private bool IsValidEmail(string emailaddress){
		Regex regex = new Regex(@"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@" + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\." + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|" + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$");
		return regex.IsMatch(emailaddress);
	}
	#endregion

	#region Parse
	private void SubmitData () {
		state = PageState.WAITING_FOR_DATA;
		
		string originPassword = _inputOriginPassword.text;
		string newPassword = _inputNewPassword.text;
		string email = _inputEmail.text;
		
		if (LocalData.Instance.IsTestUser > 0) {
			LocalData.Instance.Data["user"]["email"] = email;
			Debug.Log (LocalData.Instance.Data.ToString());
			LocalData.Instance.Save();
			state = PageState.DATA_RETRIEVED;
			return;
		}

		LoadingTask task = new LoadingTask("Saving...");
		//task.DisplayMessage = "Saving...";
		//task.CanCancel = true;
		GameManager.Instance.LoadingTasks.Add (task);
		
		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();
		data ["originPassword"] = originPassword;
		data ["newPassword"] = newPassword;
		data ["email"] = email;
		
		ParseCloud.CallFunctionAsync<string>("UpdateMyProfile", data).ContinueWith(t => {
			if (t.IsFaulted) {
				Debug.LogError("Fail UpdateMyProfile");
				foreach(var e in t.Exception.InnerExceptions) {
					ParseException parseException = (ParseException) e;
					if (parseException.Message.Contains("resolve host")) {
						NavigationBar.Instance.SetConnection(false);
					} else {
					int errorCode = 0;
					int.TryParse(parseException.Message, out errorCode);
					switch(errorCode) {
					case 5:
						GameManager.Instance.AlertTasks.Add (
							new AlertTask(
							GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
							, GameManager.Instance.Language.getString("incorrect-origin-password", CaseHandle.FirstCharUpper)
							, TextAnchor.MiddleLeft));
						break;
					case 99:
						GameManager.Instance.AlertTasks.Add (
							new AlertTask(
							GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
							, GameManager.Instance.Language.getString("incorrect-time", CaseHandle.FirstCharUpper)
							, TextAnchor.MiddleLeft));
						break;
					default:
						GameManager.Instance.AlertTasks.Add (
							new AlertTask(
							GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
							, parseException.Code + ": " + parseException.Message
							, TextAnchor.MiddleLeft));
						break;
					}
					}

				}
				state = PageState.WAITING_FOR_DATA;
			} else {
				NavigationBar.Instance.SetConnection(true);
				string result = t.Result;
				Debug.Log ("UpdateMyProfile: " + result);
				JSONNode node = JSON.Parse(result);

				CacheManager.Instance.HandleUserNode(node["user"]);
				state = PageState.DATA_RETRIEVED;
			}
			task.Loading = false;
		});

	}

	#endregion
}

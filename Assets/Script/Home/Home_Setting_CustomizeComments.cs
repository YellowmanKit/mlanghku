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

public class Home_Setting_CustomizeComments : Page {
	public List<Transform> _listComments = null;
	
	public List<string>	   _comments = null;
	public InputField      _inputComment = null;

	/*
	// Use this for initialization
	void Start () {
		
	}
	*/

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
			ReloadData();
			ReloadTable ();
			state = PageState.INIT;
			break;
		case PageState.END:
			break;
		}
	}
	
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
		ReloadData();
		ReloadTable ();
	}

	public override void OnPageShown (Dictionary<string, object> data) {
		string navigationTitle = GameManager.Instance.Language.getString("profiles", CaseHandle.FirstCharUpperForEachWord);
		NavigationBar.Instance.SetTitle (navigationTitle);
		
		NavigationBar.Instance.SetRightButton(NavigationButtonType.SAVE, SaveCustomizeComments);
	}
	#endregion
	
	public void EventButtonAddNew () {
		if (_inputComment.text != null) {
			_comments.Add (_inputComment.text);
			_inputComment.text = "";
			ReloadTable ();
		}
	}

	public void EventButtonDelete (Text t) {
		//Debug.Log ("Remove " + t.text);
		_comments.Remove(t.text);
		ReloadTable();
	}

	public void ReloadData () {
		_comments = new List<string>();
		JSONArray comments = LocalData.Instance.UserNode["comments"].AsArray;
		for (int i = 0; i < comments.Count; i++) {
			string comment = comments[i].Value;
			_comments.Add (comment);
		}
	}
	
	public void ReloadTable () {
		
		foreach (Transform t in _listComments) { t.gameObject.SetActive(false); }
		GameManager.InstantiateChildren (_comments.Count, _listComments);
		/*
		int missing = _comments.Count - _listComments.Count;
		for (int i = 0; i < missing; i++) {
			Transform copy = Instantiate(_listComments[0]);
			copy.SetParent(_listComments[0].parent);
			copy.localScale = Vector3.one;
			copy.localPosition = Vector3.zero;
			
			_listComments.Add (copy);
		}
		*/

		for (int i = 0; i < _comments.Count; i++) {
			//string comment = _comments[i];
			Transform commentRow = _listComments [i];
			commentRow.FindChild("Text").GetComponent<Text>().text = _comments[i];
			commentRow.gameObject.SetActive(true);
		}
	}
	
	public void SaveCustomizeComments () {
		
		if (LocalData.Instance.IsTestUser > 0) {
			
			JSONArray comments = new JSONArray();
			
			for (int i = 0; i < _comments.Count; i++) {
				string comment = _comments[i];
				comments[-1] = comment;
			}
			LocalData.Instance.UserNode["comments"] = comments;
			LocalData.Instance.Save();
			
			state = PageState.DATA_RETRIEVED;
			return;
		}

		LoadingTask task = new LoadingTask("Saving...");
		//task.DisplayMessage = "Saving...";
		//task.CanCancel = true;
		GameManager.Instance.LoadingTasks.Add (task);
		
		state = PageState.WAITING_FOR_DATA;
		
		Dictionary<string, object> data = new Dictionary<string, object>();
		data["timestamp"] = GameManager.GetCurrentTimestemp();
		data["comments"] = _comments;
		
		ParseCloud.CallFunctionAsync<string>("Teacher_SaveCustomizeComments", data)
			.ContinueWith(t => {
				if (t.IsFaulted) {
					Debug.LogError("Fail Teacher_SaveCustomizeComments");
					foreach(var e in t.Exception.InnerExceptions) {
						
						ParseException parseException = (ParseException) e;
						if (parseException.Message.Contains("resolve host")) {
							NavigationBar.Instance.SetConnection(false);
						} else {
							int errorCode = 0;
							int.TryParse(parseException.Message, out errorCode);
							switch(errorCode) {
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
					
					state = PageState.INIT;
				} else {
					NavigationBar.Instance.SetConnection(true);
					string result = t.Result;
					JSONNode node = JSON.Parse(result);
					
					Debug.Log ("Teacher_SaveCustomizeComments: " + node.ToString());
					CacheManager.Instance.HandleRenewUser(node);
					
					state = PageState.DATA_RETRIEVED;
				}
				
				task.Loading = false;
			});
	}
}

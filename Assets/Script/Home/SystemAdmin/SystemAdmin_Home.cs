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

public class SystemAdmin_Home : Page 
{
	//public GameObject _panelInfo = null;

	public List<GameObject> _btnSchools = null;
	
	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		switch (state) {
		case PageState.READY:
			break;
		case PageState.INIT:
			if (ParseUser.CurrentUser == null) {
				Fader.Instance.FadeIn (0.5f)
					.LoadLevel ("Login")
						.FadeOut (0.5f);
				state = PageState.END;
			} else {

				if (CacheManager.Instance.isUserDateOutDated()) {
					RenewUserData();
					state = PageState.WAITING_FOR_DATA;
				} else {
					state = PageState.DATA_RETRIEVED;
				}

				state = PageState.DATA_RETRIEVED;
			}
			break;
		case PageState.WAITING_FOR_DATA:
			break;
		case PageState.DATA_RETRIEVED:
			IdentityCheck();
			DataLoaded ();
			
			state = PageState.END;
			break;
		case PageState.END:
			break;
		}
	}
	
	#region Socket
	
	public override	void Init (Dictionary<string, object> data) {
		foreach (GameObject btn in _btnSchools) {
			btn.SetActive(false);
		}
		state = PageState.INIT;
	}

	public override void OnPageShown (Dictionary<string, object> data) {
		string title = GameManager.Instance.Language.getString ("home");
		NavigationBar.Instance.SetTitle (title);
		NavigationBar.Instance.SetRightButton ();
		DataLoaded ();
	}
	
	#endregion
	
	#region Helper
	private void DataLoaded () {
		foreach (GameObject btn in _btnSchools) {
			btn.SetActive (false);
		}
		
		// list of all schools
		JSONNode schoolNodes = CacheManager.Instance.CacheNode ["schools"];

		int numOfSchools = schoolNodes.Count;

		GameManager.InstantiateChildren (numOfSchools, _btnSchools);
		/*
		int missing = numOfSchools - _btnSchools.Count;
		for (int i = 0; i < missing; i++) {
			GameObject copy = Instantiate (_btnSchools [0]);
			copy.transform.SetParent (_btnSchools [0].transform.parent);
			copy.transform.localScale = Vector3.one;
			
			_btnSchools.Add (copy);
		}
		*/

		// update table view
		for (int i = 0; i < numOfSchools; i++) {
			JSONNode schoolNode = schoolNodes[i];
			string objectId = schoolNode["objectId"].Value;
//			string abbr = schoolNode["abbreviation"].Value;
			string fullName = schoolNode["fullName"].Value;
			
			// assign the text
			GameObject btn = _btnSchools [i];
			btn.transform.FindChild ("txt_title").GetComponent<Text> ().text = fullName;
			btn.gameObject.SetActive (true);

			btn.name = objectId;
		}
	}

	#endregion
	
	#region Event

	public void EventButtonSelectSchool (GameObject btn) {
		string schoolId = btn.name;
		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["schoolId"] = schoolId;
		
		NavigationBar.Instance.PushView (NavigationView.SystemAdmin_View_School, data);
	}

	#endregion

	/*
	#region Parse
	private void RenewUserData () {

		//Debug.Log ("RenewUserData");
		LoadingTask task = new LoadingTask("Loading Data...");
		//task.DisplayMessage = "Loading Data...";
		//task.CanCancel = true;
		////task.OnLoadingCancel += onCancelled;
		GameManager.Instance.LoadingTasks.Add (task);
		
		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();
		
		ParseCloud.CallFunctionAsync<string>("RenewUser", data).ContinueWith(t => {
			if (t.IsFaulted) {
				Debug.LogError("Fail RenewUserData");
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
			} else {
				NavigationBar.Instance.SetConnection(true);
				string result = t.Result;
				Debug.Log ("RenewUserData: " + result);
				JSONNode node = JSON.Parse(result);
				
				CacheManager.Instance.HandleRenewUser(node);
				
				state = PageState.DATA_RETRIEVED;
			}
			task.Loading = false;
		});

	}
	#endregion
	*/
}

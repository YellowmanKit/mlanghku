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

public class Student_Home : Page
{
	public List<GameObject>	_btnMyProjects = null;

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
			if (ParseUser.CurrentUser == null && LocalData.Instance.IsTestUser == 0) {
				Fader.Instance.FadeIn (0.5f)
					.LoadLevel ("Login")
						.FadeOut (0.5f);
				state = PageState.END;
			} else {
				if (CacheManager.Instance.isUserDateOutDated()
				    || CacheManager.Instance.IsStudentMyProjectsOutdated()
				    || CacheManager.Instance.IsStudentMyCourseOutdated()) {
					Debug.Log ("[Student_Home] UserData outdated");
					RenewUserData();
					state = PageState.WAITING_FOR_DATA;
				} else {
					state = PageState.DATA_RETRIEVED;
				}
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

	void OnDestroy() {
//		for (int idx=0; idx < _btnMyProjects.Count; ++idx) {
//			GameObject btn = _btnMyProjects [idx];
//			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();
//			if (btnImageIcon.sprite != null) {
//				Sprite s = btnImageIcon.sprite;
//				btnImageIcon.sprite = null;
//				Destroy(s.texture);
//			}
//		}
	}
	
	#region Socket

	public override	void Init (Dictionary<string, object> data) {
		Debug.Log ("[Student_Home] Init()");
		foreach (GameObject btn in _btnMyProjects) { btn.SetActive(false); }
		state = PageState.INIT;
	}

	public override void OnPageShown (Dictionary<string, object> data) {
		Debug.Log ("[Student_Home] OnPageShown()");
		string title = GameManager.Instance.Language.getString ("home");
		NavigationBar.Instance.SetTitle (title);
		NavigationBar.Instance.SetRightButton (NavigationButtonType.SEARCH,ShowSearchPage);

		DataLoaded();
	}

	void ShowSearchPage(){
		Debug.Log ("Search page");
		Dictionary<string, object> data = new Dictionary<string, object> ();
		NavigationBar.Instance.PushView(NavigationView.Search,data);
	}
	
	private void DataLoaded () {
		Debug.Log ("[Student_Home] DataLoaded()");
		foreach (GameObject btn in _btnMyProjects) { btn.SetActive(false); }

		// show the projects
		if (LocalData.Instance.Data["my-projects"] != null) 
		{
			GameManager.InstantiateChildren (LocalData.Instance.Data["my-projects"].Count, _btnMyProjects);
			/*
			int numOfProjects = LocalData.Instance.Data["my-projects"].Count;
			int missing = numOfProjects - _btnMyProjects.Count;
			for (int i = 0; i < missing; i++) {
				GameObject copy = Instantiate (_btnMyProjects [0]);
				copy.transform.SetParent (_btnMyProjects [0].transform.parent);
				copy.transform.localScale = Vector3.one;
				
				_btnMyProjects.Add (copy);
			}
			*/

			// Just for debug...
			//foreach (var obj in CacheManager.Instance.CacheNode["my-projects-submitted"].GetKeys()) { Debug.Log (obj.ToString ()); }

			// Parse the "my-projects" JSONNode
			int idx = 0;
			foreach (JSONNode projectNode in LocalData.Instance.Data["my-projects"].Childs) 
			{
				Project project = new Project(projectNode);
				
				// assign the text
				GameObject btn = _btnMyProjects [idx++];
				btn.gameObject.SetActive (true);

				btn.name = project.id;
				btn.transform.FindChild ("Text").GetComponent<Text> ().text = project.getTitle();
				Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();

//				if (btnImageIcon.sprite != null) {
//					Sprite s = btnImageIcon.sprite;
//					btnImageIcon.sprite = null;
//					Destroy(s.texture);
//				}
				
				// update the icon
				//Debug.Log ("ImagePath=[" + project.ImagePath + "] ImageUrl=[" + project.imageUrl + "]");

//				if (project.isImageExist) {
//					btnImageIcon.sprite = DownloadManager.FileToSprite (project.ImagePath);
//				} else if (!string.IsNullOrEmpty(project.imageUrl)) {
				//if (!string.IsNullOrEmpty(project.imageUrl)) {
					btnImageIcon.sprite = DownloadManager.Instance.spriteLoading;
					DownloadManager.Instance.AddLinkage(project.ImagePath, project.imageUrl, btnImageIcon);
				//}

				//-----------------------------------------------------------------------------------------
				// v1.3
				// In Parse.Cloud.beforeSave("Card"...), when the Card status is 2 (failed),
				// the Card's studentAttention will be set to 1, and lastly the 
				// StudentProject's numberOfStudentAttention will be increased.
				//-----------------------------------------------------------------------------------------
				Image imgBadge = btn.transform.FindChild("img_badge").GetComponent<Image>();
				if (CacheManager.Instance.CacheNode["my-projects-submitted"] != null 
				    && CacheManager.Instance.CacheNode["my-projects-submitted"].GetKeys().Contains(project.id))
				{
					JSONNode submittedProject = CacheManager.Instance.CacheNode["my-projects-submitted"][project.id];
					int numOfStudentAttention = submittedProject["numOfStudentAttention"].AsInt;

					//Debug.Log (submittedProject.ToString()); 

					if (numOfStudentAttention > 0) {
						imgBadge.gameObject.SetActive(true);
						imgBadge.transform.FindChild ("Text").GetComponent<Text> ().text = numOfStudentAttention.ToString();
					} else {
						imgBadge.gameObject.SetActive(false);
					}
				} else {
					imgBadge.gameObject.SetActive(false);
				}
			}
		}
	}
	#endregion
	
	#region Event

	public void EventButtonSelectMyProject (GameObject btn) {
		string projectId = btn.name;
		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["projectId"] = projectId;
		NavigationBar.Instance.PushView (NavigationView.Student_MyProject, data);
	}

	public void EventButtonSelectGame(){
		Fader.Instance.FadeIn (0.5f)
			.LoadLevel ("Game1_entry")
			.FadeOut (0.5f);
		//Debug.Log ("Load Game1");
	}

	#endregion

	/*
	#region Parse
	private void RenewUserData () {
		
		Debug.Log ("[Student_Home] RenewUserData()");
		if (LocalData.Instance.IsTestUser > 0) {
			state = PageState.DATA_RETRIEVED;
			return;
		}

		LoadingTask task = new LoadingTask("Loading Data...");
		//task.DisplayMessage = "Loading Data...";
		//task.CanCancel = true;
		////task.OnLoadingCompleted += onCompleted;
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
				Debug.Log ("[Student_Home] RenewUserData() result: " + result);
				JSONNode node = JSON.Parse(result);
				
				CacheManager.Instance.HandleRenewUser(node);

			}
			task.Loading = false;
			
			state = PageState.DATA_RETRIEVED;
		});
	}
	#endregion
	*/
}

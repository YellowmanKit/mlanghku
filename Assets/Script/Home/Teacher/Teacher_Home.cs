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

public class Teacher_Home : Page {
	
	public List<GameObject> _btnCourses = null;
	public List<GameObject> _btnProjects = null;

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
				    || CacheManager.Instance.IsTeacherCoursesListOutdated()) {
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
//		for (int i=0; i<_btnCourses.Count; ++i) {
//			GameObject btn = _btnCourses [i];
//			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();
//			if (btnImageIcon.sprite != null) {
//				Sprite s = btnImageIcon.sprite;
//				btnImageIcon.sprite = null;
//				Destroy(s.texture);
//			}
//		}
//
//		for (int i=0; i<_btnProjects.Count; ++i) {
//			GameObject btn = _btnProjects [i];
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
		Debug.Log ("[Teacher_Home] Init()");
		foreach (GameObject btn in _btnCourses) { btn.SetActive(false); }
		state = PageState.INIT;
	}

	public override void OnPageShown (Dictionary<string, object> data) {
		Debug.Log ("[Teacher_Home] OnPageShown()");
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
		Debug.Log ("[Teacher_Home] DataLoaded()");

		//string j = LocalData.Instance.Data.ToString ();
		//Debug.Log ("json_data: " + j);

		foreach (GameObject btn in _btnCourses ) { btn.SetActive(false); }
		foreach (GameObject btn in _btnProjects) { btn.SetActive(false); }
		
		// show the courses
		GameManager.InstantiateChildren( LocalData.Instance.Data["my-course"].Count, _btnCourses );
		/*
		int numOfCourses = LocalData.Instance.Data["my-course"].Count;
		int missing = numOfCourses - _btnCourses.Count;
		for (int i = 0; i < missing; i++) {
			GameObject copy = Instantiate (_btnCourses [0]);
			copy.transform.SetParent (_btnCourses [0].transform.parent);
			copy.transform.localScale = Vector3.one;

			_btnCourses.Add (copy);
		}
		*/

		// Parse the "Course" JSONNode
		int idx = 0;
		foreach (JSONNode courseNode in LocalData.Instance.Data["my-course"].Childs) {
			string courseId = courseNode ["objectId"].Value;
			string courseTitle = courseNode ["courseTitle"].Value;
			string courseIconUrl = courseNode ["courseIcon"] ["url"].Value;
			int numOfTeacherAttention = courseNode ["numOfTeacherAttention"].AsInt;
			//Debug.Log (courseId);
			
			// assign the text
			GameObject btn = _btnCourses [idx++];
			btn.name = courseId;
			btn.transform.FindChild ("Text").GetComponent<Text> ().text = courseTitle;
			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();

//			if (btnImageIcon.sprite != null) {
//				Sprite s = btnImageIcon.sprite;
//				btnImageIcon.sprite = null;
//				Destroy(s.texture);
//			}
			
			btn.gameObject.SetActive (true);
			
			// update the icon
			string imagePath = Path.Combine(LocalData.Instance.DirectoryPath, "course-" + courseId + ".jpg");
			
			//if (File.Exists (imagePath)) {
			//	btnImageIcon.sprite = DownloadManager.FileToSprite (imagePath);
			//} else 
			//if (!string.IsNullOrEmpty(courseIconUrl)) {
				btnImageIcon.sprite = DownloadManager.Instance.spriteLoading;
				DownloadManager.Instance.AddLinkage(imagePath, courseIconUrl, btnImageIcon);
			//}

			
			// submitted project info?
			Image imgBadge = btn.transform.FindChild("img_badge").GetComponent<Image>();

			if (numOfTeacherAttention > 0) {
				imgBadge.gameObject.SetActive(true);
				imgBadge.transform.FindChild ("Text").GetComponent<Text> ().text = numOfTeacherAttention.ToString();
			} else {
				imgBadge.gameObject.SetActive(false);
			}
		}

		//////////////////////////////////////////////
		// projects (Featured Cards)
		if (LocalData.Instance.Data["my-projects"] != null)
		{
			GameManager.InstantiateChildren (LocalData.Instance.Data ["my-projects"].Count, _btnProjects);
			/*
			int numOfProjects = LocalData.Instance.Data["my-projects"].Count;
			missing = numOfProjects - _btnProjects.Count;
			for (int i = 0; i < missing; i++) {
				GameObject copy = Instantiate (_btnProjects [0]);
				copy.transform.SetParent (_btnProjects [0].transform.parent);
				copy.transform.localScale = Vector3.one;
				
				_btnProjects.Add (copy);
			}
			*/

			// Parse the "Course" JSONNode
			idx = 0;
			foreach (JSONNode projectNode in LocalData.Instance.Data["my-projects"].Childs) {
				Project project = new Project(projectNode);
				
				// assign the text
				GameObject btn = _btnProjects [idx++];
				btn.name = project.id;
				btn.transform.FindChild ("Text").GetComponent<Text> ().text = project.getTitle();
				Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();
				
				btn.gameObject.SetActive (true);
				
				// update the icon
				/*if (project.isImageExist) {
					btnImageIcon.sprite = DownloadManager.FileToSprite (project.ImagePath);
				} else */
				//if (!string.IsNullOrEmpty(project.imageUrl)) {
					btnImageIcon.sprite = DownloadManager.Instance.spriteLoading;
					DownloadManager.Instance.AddLinkage(project.ImagePath, project.imageUrl, btnImageIcon);
				//}
			}
		}
	}
	#endregion
	
	#region Event

	public void EventButtonSelectCourse (GameObject btn) {
		string courseId = btn.name;
		//Debug.Log (courseId);
		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["courseId"] = courseId;
		
		//_container.SocketShowPage (Home_Main.PanelPage.COURSE, data);
		NavigationBar.Instance.PushView (NavigationView.Teacher_View_Course, data);
	}

	public void EventButtonSelectProject (GameObject btn) {
		string projectId = btn.name;
		//Debug.Log (courseId);
		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["projectId"] = projectId;
		data ["tab"] = "featured";

		NavigationBar.Instance.PushView (NavigationView.Teacher_View_Project, data);
	}

	#endregion

	public void EventStartGame1(){
		Fader.Instance.FadeIn (0.5f)
			.LoadLevel ("Game1_entry")
			.FadeOut (0.5f);
	}

	/*
	#region Parse
	private void RenewUserData () {
		Debug.Log ("[Teacher_Home] RenewUserData()");
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

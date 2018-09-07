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

public class Teacher_Course_ViewStudent : Page
{
	private bool					historyLogExist,rotateRefreshBtn;
	public GameObject				refreshButtonObj;


	public enum TabPage {
		Info,
		Featured,
		Projects
	}

	public GameObject 				loginLogObj, logoutLogObj;
	private Student 				student;

	private string					_dynSelectedCourseId = "";
	private string					_dynSelectedStudentId = "";
	
	public Text						_txtTabInfo = null;
	public Text						_txtTabFeatured = null;
	public Text						_txtTabProejcts = null;
	
	public Color[]					_navigationColors = null;
	
	public GameObject				_panelInfo = null;
	public Text						_txtInfoName = null;
	public Text						_txtInfoRealName = null;
	public Text						_txtInfoEmail = null;
	public Text						_txtFeaturedCards = null;
	public Text						_txtNumOfCards = null;
	
	public GameObject				_panelFeatured = null;
	public List<GameObject>			_btnFeaturedCards = null;
	
	public GameObject				_panelProjects = null;
	public List<GameObject>			_btnProjects = null;
	public List<Transform>			_sortImageStudentProject = null;
	private StudentProjectSortBy	_dynStudentProjectSortBy = StudentProjectSortBy.NumOfAttention;
	private SortingOrder			_dynStudentProjectOrder = SortingOrder.Desc;
	
	// Use this for initialization
	void Start () {
		_txtInfoName.text = "";
		_txtInfoRealName.text = "";
		_txtInfoEmail.text = "";
		
		ShowTabPage (TabPage.Projects);
		SetStudentProjectSorting(StudentProjectSortBy.NumOfAttention, SortingOrder.Desc);
	}
	
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
				state = PageState.WAITING_FOR_DATA;
				RenewData();
				//state = PageState.DATA_RETRIEVED;
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
		if (rotateRefreshBtn) {
			refreshButtonObj.GetComponent<LangChooser> ()._key = "refreshing";
			refreshButtonObj.GetComponent<LangChooser> ().Start ();
		} else {
			refreshButtonObj.GetComponent<LangChooser> ()._key = "refresh";
			refreshButtonObj.GetComponent<LangChooser> ().Start ();
		}
	}

	void OnDestroy() {
//		for (int i=0; i<_btnProjects.Count; ++i) {
//			GameObject btn = _btnProjects[i];
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
		Debug.Log ("[Teacher_Course_ViewStudent] Init()");
		if (data != null) {
			if (data.ContainsKey("studentId")) {
				_dynSelectedCourseId = (string)(data ["courseId"]);
				_dynSelectedStudentId = (string)(data ["studentId"]);
				foreach (GameObject btn in _btnProjects     ) { btn.SetActive (false); }
				foreach (GameObject btn in _btnFeaturedCards) { btn.SetActive (false); }
				state = PageState.INIT;
			} else {
				Debug.LogWarning("TeacherCourse: SocketBecomeVisible data no key");
			}
		}
	}

	public override void OnPageShown (Dictionary<string, object> data) {
		Debug.Log ("[Teacher_Course_ViewStudent] OnPageShown()");
		JSONNode studentNode = CacheManager.Instance.CacheNode["course"][_dynSelectedCourseId]["students"][_dynSelectedStudentId];
		NavigationBar.Instance.SetTitle(studentNode["username"].Value);
		NavigationBar.Instance.SetRightButton();
		DataLoaded ();
	}

	#endregion
	
	#region Helper
	private void ShowTabPage (TabPage tabPage) {
		Debug.Log ("[Teacher_Course_ViewStudent] ShowTabPage()");

		_panelInfo.SetActive(false);
		_panelProjects.SetActive (false);
		_panelFeatured.SetActive (false);
		
		_txtTabFeatured.color = _txtTabProejcts.color = _txtTabInfo.color = _navigationColors [1];
		
		switch (tabPage) {
		case TabPage.Info:
			_txtTabInfo.color = _navigationColors[0];
			_panelInfo.SetActive (true);
			break;
		case TabPage.Featured:
			_txtTabFeatured.color = _navigationColors[0];
			_panelFeatured.SetActive (true);
			break;
		case TabPage.Projects:
			_txtTabProejcts.color = _navigationColors[0];
			_panelProjects.SetActive (true);
			break;
		}
	}
	
	private void DataLoaded () {
		Debug.Log ("[Teacher_Course_ViewStudent] DataLoaded()");

		foreach (GameObject btn in _btnProjects     ) { btn.SetActive (false); }
		foreach (GameObject btn in _btnFeaturedCards) { btn.SetActive (false); }
		
		// info
		JSONNode studentNode = CacheManager.Instance.CacheNode["course"][_dynSelectedCourseId]["students"][_dynSelectedStudentId];
		//Debug.Log ("ss " + studentNode.ToString());
		if (studentNode != null) {
			//Student student = new Student(studentNode);
			student = new Student(studentNode);
			
			_txtInfoName.text = student.username;
			_txtInfoRealName.text = student.realName;
			_txtFeaturedCards.text = student.numOfFeaturedCards.ToString();
			_txtNumOfCards.text = student.numOfSubmittedCards.ToString();

			if (!string.IsNullOrEmpty(student.email)) {
				_txtInfoEmail.text = student.email;
			} else {
				_txtInfoEmail.text = "";
			}
		} else {
			_txtInfoName.text = "";
			_txtInfoRealName.text = "";
			_txtInfoEmail.text = "";
			_txtFeaturedCards.text = "0";
		}
		
		// projects
		JSONNode projectsListNode = LocalData.Instance.Data["my-projects"];

		//Debug.Log ("HERE2: [" + projectsListNode.ToString () + "]"); // sorry, no projectId here...

		// build an array of project
		Project[] projects = new Project[projectsListNode.Count];
		int ptr = 0;
		for (int i = 0; i < projectsListNode.Count; i++) {
			Project project = new Project(projectsListNode[i]);

			if (project.courseId.Equals( _dynSelectedCourseId)) {
				projects[ptr++] = project;
			}
		}
		Array.Resize(ref projects, ptr);

		// build studentProject
		StudentProject[] studentProjects = new StudentProject[projects.Length];
		for (int i = 0; i < projects.Length; i++) {
			Project project = projects[i];
			JSONNode studentProjectNode = CacheManager.Instance.CacheNode ["project-" + project.id + "-submitted-list"][_dynSelectedStudentId];

			StudentProject sp = null;
			if (studentProjectNode == null) {
				sp = new StudentProject ();
			} else {
				sp = new StudentProject (studentProjectNode);
			}
			sp.project = project;

			studentProjects[i] = sp;
		}

		// sorting
		StudentProject.Sort(studentProjects, _dynStudentProjectSortBy, _dynStudentProjectOrder);

		GameManager.InstantiateChildren (studentProjects.Length, _btnProjects);
		/*
		int missing = studentProjects.Length - _btnProjects.Count;
		for (int i = 0; i < missing; i++) {
			GameObject copy = Instantiate (_btnProjects [0]);
			copy.transform.SetParent (_btnProjects [0].transform.parent);
			copy.transform.localScale = Vector3.one;
			
			_btnProjects.Add (copy);
		}
		*/

		//int idx = 0;
		for (int i = 0; i < studentProjects.Length; i++) {
			StudentProject studentProject = studentProjects[i];
			Project project = studentProject.project;

			GameObject btn = _btnProjects[i];
			//-----------------------------------------------------------------------
			// v1.3 try to append "projectId" to the button name as well
			//-----------------------------------------------------------------------
			//btn.name = studentProject.id;
			btn.name = studentProject.id + "-" + project.id;
			//-----------------------------------------------------------------------
			btn.transform.FindChild ("txt_title").GetComponent<Text> ().text = project.getTitle();

			TimeSpan timeRemain = (project.dueDate - DateTime.Now);
			int day = timeRemain.Days;
			string dueDate = project.dueDate.ToString("d MMM yyyy, hh:mm tt");
			if (day >= 0) {
				if (day >= 1) {
					dueDate += " <color=#" + GameManager.ColorToHex(GameManager.Instance.validColor[0]) + "> " + day + "d</color>";
				} else {
						dueDate += " <color=#" + GameManager.ColorToHex(GameManager.Instance.validColor[0]) + ">< 1d</color>";
				}
			} else {
							dueDate += " <color=#" + GameManager.ColorToHex(GameManager.Instance.validColor[1]) + ">Passed</color>";
			}
			btn.transform.FindChild ("txt_dueDate").GetComponent<Text> ().text = dueDate;
			btn.transform.FindChild ("txt_cards").GetComponent<Text> ().text = studentProject.numOfCards.ToString();
			btn.transform.FindChild ("txt_featured").GetComponent<Text> ().text = studentProject.numOfFeaturedCards.ToString();
			btn.gameObject.SetActive (true);

			// assign image
			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();
//			if (btnImageIcon.sprite != null) {
//				Sprite s = btnImageIcon.sprite;
//				btnImageIcon.sprite = null;
//				Destroy(s.texture);
//			}

			/*if (project.isImageExist) {
				btnImageIcon.sprite = DownloadManager.FileToSprite (project.ImagePath);
			} else */if (!string.IsNullOrEmpty(project.imageUrl)) {
				btnImageIcon.sprite = DownloadManager.Instance.spriteLoading;
				DownloadManager.Instance.AddLinkage(project.ImagePath, project.imageUrl, btnImageIcon);
			}

			// attention
			Transform badge = btn.transform.FindChild ("img_badge");
			int numOfTeacherAttention = studentProject.numOfTeacherAttention;
			if (numOfTeacherAttention <= 0) {
				badge.gameObject.SetActive(false);
			} else {
				badge.gameObject.SetActive(true);
				badge.FindChild ("Text").GetComponent<Text> ().text = numOfTeacherAttention.ToString();
			}
		}

	}
	#endregion
	
	#region Event
	
	public void EventButtonStudentProjectsSortBy (GameObject btn) {
		StudentProjectSortBy choice = StudentProjectSortBy.Name;
		if (btn.name.Contains ("name")) {
			choice = StudentProjectSortBy.Name;
		} else if (btn.name.Contains ("card")) {
			choice = StudentProjectSortBy.NumOfCard;
		} else if (btn.name.Contains ("attention")) {
			choice = StudentProjectSortBy.NumOfAttention;
		} else {
			Debug.LogError("Sortby unknown " + btn.name);
			return;
		}
		
		if (choice == _dynStudentProjectSortBy) {
			if (_dynStudentProjectOrder == SortingOrder.Asc) {
				SetStudentProjectSorting(choice, SortingOrder.Desc);
			} else {
				SetStudentProjectSorting(choice, SortingOrder.Asc);
			}
		} else {
			SetStudentProjectSorting(choice, SortingOrder.Asc);
		}
		
		DataLoaded ();
	}
	
	private void SetStudentProjectSorting (StudentProjectSortBy key, SortingOrder order) {
		_dynStudentProjectSortBy = key;
		_dynStudentProjectOrder = order;
		
		_sortImageStudentProject[0].gameObject.SetActive(false);
		_sortImageStudentProject[1].gameObject.SetActive(false);
		_sortImageStudentProject[2].gameObject.SetActive(false);
		
		Vector3 scale = Vector3.one;
		if (order == SortingOrder.Asc) {
			scale.y = -1;
		}
		
		//Debug.Log (key.ToString());
		switch(key) {
		case StudentProjectSortBy.Name:
			_sortImageStudentProject[0].gameObject.SetActive(true);
			_sortImageStudentProject[0].transform.localScale = scale;
			break;
		case StudentProjectSortBy.NumOfCard:
			_sortImageStudentProject[1].gameObject.SetActive(true);
			_sortImageStudentProject[1].transform.localScale = scale;
			break;
		case StudentProjectSortBy.NumOfAttention:
			_sortImageStudentProject[2].gameObject.SetActive(true);
			_sortImageStudentProject[2].transform.localScale = scale;
			break;
		}
	}

	public void EventButtonSelectStudentProject (GameObject btn)
	{
		//string studentProjectId = btn.name;
		//Dictionary<string, object> data = new Dictionary<string, object> ();
		//data ["studentProjectId"] = studentProjectId;

		//--------------------------------------------------------------
		// v1.3 try to add "projectId" to the data as well
		//--------------------------------------------------------------
		Dictionary<string, object> data = new Dictionary<string, object> ();
		string[] tokens = btn.name.Split ('-');
		if (tokens.Length > 0) data ["studentProjectId"] = tokens[0];
		if (tokens.Length > 1) data ["projectId"] = tokens[1];
		//--------------------------------------------------------------

		NavigationBar.Instance.PushView (NavigationView.Teacher_View_StudentProject, data);
	}
	
	public void EventButtonSelectTab (GameObject btn) {
		if (btn.name.Contains ("project")) {
			ShowTabPage (TabPage.Projects);
		} else if (btn.name.Contains ("info")) {
			StartCoroutine(LoadStudentLoginHistory ());
			ShowTabPage (TabPage.Info);
		} else if (btn.name.Contains ("featured")) {
			ShowTabPage (TabPage.Featured);
		}
	}

	IEnumerator LoadStudentLoginHistory(){
		if (historyLogExist) {
			yield break;
		}
		yield return new WaitUntil (() => state == PageState.END);
		//Clear old log
		for (int i = 1; i < loginLogObj.transform.childCount; i++) {
			Destroy (loginLogObj.transform.GetChild (i).gameObject);
			Destroy (logoutLogObj.transform.GetChild (i).gameObject);
		}
		rotateRefreshBtn = true;


		//Debug.Log ("Trying to get login history!");

		List<DateTime> lastLogin = new List<DateTime> ();
		List<DateTime> lastLogout = new List<DateTime> ();

		bool done = false;
		bool gotRecord = false;

		//ParseObject log = new ParseObject ("LogInHistory");
		var query = ParseObject.GetQuery ("LogInHistory").WhereEqualTo ("loginUserId", student.objectId).OrderByDescending("createdAt").Limit(20);
		query.FindAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				IEnumerable<ParseObject> logs = t.Result;
				//Debug.Log("Log result found: " + student.objectId);
				foreach(var mylog in logs){
					//Debug.Log("Getting datetime");
					//Debug.Log("Date: " + mylog.CreatedAt);
					lastLogin.Add((DateTime)mylog.CreatedAt);
					lastLogout.Add((DateTime)mylog.UpdatedAt);
					//lastLogout.Add((DateTime)mylog["loggedAt"]);

					gotRecord = true;
				}
			}else{
				//Debug.Log("Failed to get login record! Error: " + t.Exception.Message);
			}
			done = true;
		});
		yield return new WaitUntil (() => done == true);
		//Debug.Log ("Trying to display login history!");
		//float tranStep = Camera.main.pixelHeight * -0.03f;
		loginLogObj.transform.GetChild (0).gameObject.SetActive (true);
		logoutLogObj.transform.GetChild (0).gameObject.SetActive (true);
		if (gotRecord) {
			for (int index = 0; index < lastLogin.Count; index++) {

				lastLogin [index] = lastLogin [index].AddHours ((double)8);
				lastLogout [index] = lastLogout [index].AddHours ((double)8);

				GameObject loginCopy = Instantiate(loginLogObj.transform.GetChild (0).gameObject,loginLogObj.transform.GetChild (0).position,loginLogObj.transform.GetChild (0).rotation);
				loginCopy.transform.SetParent (loginLogObj.transform);
				loginCopy.transform.localScale = Vector3.one;
				//loginCopy.transform.Translate (new Vector3 (0f, tranStep * index, 0f));
				loginCopy.GetComponent<Text> ().text = lastLogin [index].ToString("dddd, d MMM yyyy, hh:mm tt");

				GameObject logoutCopy = Instantiate(logoutLogObj.transform.GetChild (0).gameObject,logoutLogObj.transform.GetChild (0).position,logoutLogObj.transform.GetChild (0).rotation);
				logoutCopy.transform.SetParent (logoutLogObj.transform);
				logoutCopy.transform.localScale = Vector3.one;
				//logoutCopy.transform.Translate (new Vector3 (0f, tranStep * index, 0f));
				if (lastLogout [index] == lastLogin [index]) {
					logoutCopy.GetComponent<Text> ().text = "";
				} else {
					logoutCopy.GetComponent<Text> ().text = lastLogout [index].ToString("dddd, d MMM yyyy, hh:mm tt");
				}
			}
		}
		loginLogObj.transform.GetChild (0).gameObject.SetActive (false);
		logoutLogObj.transform.GetChild (0).gameObject.SetActive (false);

		refreshButtonObj.GetComponent<Button> ().enabled = true;
		historyLogExist = true;
		rotateRefreshBtn = false;

		yield return null;

	}

	public void EventRefreshHistory(){
		historyLogExist = false;
		refreshButtonObj.GetComponent<Button> ().enabled = false;
		StartCoroutine (LoadStudentLoginHistory ());
	}
	
	#endregion

	#region Parse
	
	private void RenewData () 
	{
		Debug.Log ("[Teacher_Course_ViewStudent] RenewData()");

		if (LocalData.Instance.IsTestUser > 0) {
			state = PageState.DATA_RETRIEVED;
			return;
		}

		LoadingTask task = new LoadingTask("Loading Data...");
		//task.DisplayMessage = "Loading Data...";
		//task.CanCancel = true;
		////task.OnLoadingCancel += onCancelled;
		GameManager.Instance.LoadingTasks.Add (task);
		
		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();
		data ["studentId"] = _dynSelectedStudentId;
		
		ParseCloud.CallFunctionAsync<string>("TeacherGetSpecificStudent_StudentProject", data).ContinueWith(t => {
			if (t.IsFaulted) {
				Debug.LogError("Fail TeacherGetSpecificStudent_StudentProject");
				foreach(var e in t.Exception.InnerExceptions) {
					ParseException parseException = (ParseException) e;
					if (parseException.Message.Contains("resolve host")) {
						NavigationBar.Instance.SetConnection(false);
					} else {
					int errorCode = 0;
					int.TryParse(parseException.Message, out errorCode);
					switch(errorCode) {
					case 6:
						GameManager.Instance.AlertTasks.Add (
							new AlertTask(
							GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
							, GameManager.Instance.Language.getString("no-permission", CaseHandle.FirstCharUpper)
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
			} else {
				NavigationBar.Instance.SetConnection(true);
				string result = t.Result;
				Debug.Log ("[Teacher_Course_ViewStudent] RenewData() result: ");
				JSONNode node = JSON.Parse(result);
				CacheManager.Instance.HandleTeacherSpecificStudent_StudentProject(_dynSelectedStudentId, node["studentProject"]);
				//CacheManager.Instance.HandleTeacherSpecificProjectStudentProjectList(_dynSelectedStudentId, node["studentProject"]);
				
				state = PageState.DATA_RETRIEVED;
			}
			task.Loading = false;
		});
	}
	#endregion
}

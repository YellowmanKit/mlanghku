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

public class Teacher_Course : Page {
	public enum TabPage {
		Students,
		Projects
	}
	private TabPage					_dynCurrentPage = TabPage.Students;

	private string					_dynSelectedCourseId = "";
	
	public Text						_txtTabStudents = null;
	public Text						_txtTabProjects = null;
	
	public GameObject				_panelStudents = null;
	public List<GameObject>			_btnStudents = null;
	public List<Transform>			_sortImageStudent = null;
	private StudentSortBy			_dynStudentSortBy = StudentSortBy.NumOfAttention;
	private SortingOrder			_dynStudentOrder = SortingOrder.Desc;
	
	public Color[]					_navigationColors = null;
	
	public GameObject				_panelProjects = null;
	public List<GameObject>			_btnProjects = null;
	public List<Transform>			_sortImageProject = null;
	private ProjectSortBy			_dynProjectSortBy = ProjectSortBy.DueDate;
	private SortingOrder			_dynProjectOrder = SortingOrder.Desc;
	
	// Use this for initialization
	void Start () {
		ShowTabPage (TabPage.Projects);
		SetStudentSorting(StudentSortBy.NumOfAttention, SortingOrder.Desc);
		SetProjectSorting(ProjectSortBy.DueDate, SortingOrder.Desc);
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
				if (CacheManager.Instance.IsTeacherSpecificCourseDataOutdated(_dynSelectedCourseId)) {
					RenewCourseData();
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
//		for (int i = 0; i < _btnProjects.Count; i++) {
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
		Debug.Log ("[Teacher_Course] Init()");
		if (data != null) {
			if (data.ContainsKey("courseId")) {
				_dynSelectedCourseId = (string)(data ["courseId"]);
				foreach (GameObject btn in _btnStudents) { btn.SetActive (false); }
				foreach (GameObject btn in _btnProjects) { btn.SetActive (false); }
				state = PageState.INIT;
			} else {
				Debug.LogWarning("TeacherCourse: SocketBecomeVisible data no key");
			}
		}
	}
	public override void OnPageShown (Dictionary<string, object> data) {
		Debug.Log ("[Teacher_Course] OnPageShown()");
		string title = LocalData.Instance.Data["my-course"] [_dynSelectedCourseId] ["courseTitle"].Value;
		NavigationBar.Instance.SetTitle (title);

		ParseUserIdentity identity = LocalData.Instance.UserIdentity;
		if (identity == ParseUserIdentity.Teacher) {
			if (_dynCurrentPage == TabPage.Students) {
				NavigationBar.Instance.SetRightButton ();
			} else if (_dynCurrentPage == TabPage.Projects) {
				NavigationBar.Instance.SetRightButton (NavigationButtonType.ADD, EventButtonAddProject);
			}
		}

		DataLoaded ();
	}
	#endregion
	
	#region Helper
	private void ShowTabPage (TabPage tabPage) {
		//Debug.Log ("[Teacher_Course] ShowTabPage()");
		_dynCurrentPage = tabPage;
		
		_panelStudents.gameObject.SetActive (false);
		_panelProjects.gameObject.SetActive (false);
		
		_txtTabStudents.color = _txtTabProjects.color = _navigationColors [1];
		
		switch (tabPage) {
		case TabPage.Students:
			_txtTabStudents.color = _navigationColors[0];
			_panelStudents.gameObject.SetActive (true);
			break;
		case TabPage.Projects:
			_txtTabProjects.color = _navigationColors[0];
			_panelProjects.gameObject.SetActive (true);

			if (LocalData.Instance.UserIdentity == ParseUserIdentity.Teacher)
				NavigationBar.Instance.SetRightButton(NavigationButtonType.ADD, EventButtonAddProject);
			break;
		}
	}
	private void DataLoaded () {
		Debug.Log ("[Teacher_Course] DataLoaded()");

		foreach (GameObject btn in _btnStudents) { btn.SetActive (false); }
		foreach (GameObject btn in _btnProjects) { btn.SetActive (false); }
		
		// students
		JSONNode studentsNode = CacheManager.Instance.CacheNode ["course"][_dynSelectedCourseId]["students"];
		//Debug.Log (studentsNode.ToString());
		if (studentsNode != null) {
			// build an array
			Student[] students = new Student[studentsNode.Count];
			for (int i = 0; i < studentsNode.Count; i++) {
				Student student = new Student(studentsNode[i]);
				students[i] = student;
			}

			Student.Sort(students, _dynStudentSortBy, _dynStudentOrder);

			// show the students
			GameManager.InstantiateChildren (students.Length, _btnStudents);
			/*
			int missing = students.Length - _btnStudents.Count;
			for (int i = 0; i < missing; i++) {
				GameObject copy = Instantiate (_btnStudents [0]);
				copy.transform.SetParent (_btnStudents [0].transform.parent);
				copy.transform.localScale = Vector3.one;
				
				_btnStudents.Add (copy);
			}
			*/

			// update table view
			for (int i = 0; i < students.Length; i++) {
				Student student = students[i];
				// assign the text
				GameObject btn = _btnStudents [i];
				btn.name = student.id;
				btn.transform.FindChild ("txt_studentName").GetComponent<Text> ().text = student.realName;
				btn.gameObject.SetActive (true);

				// attention
				Transform badge = btn.transform.FindChild ("img_badge");

				int numOfTeacherAttention = student.numOfTeacherAttention;
				if (numOfTeacherAttention <= 0) {
					badge.gameObject.SetActive(false);
					badge.FindChild ("Text").GetComponent<Text> ().text = "";
				} else {
					badge.gameObject.SetActive(true);
					badge.FindChild ("Text").GetComponent<Text> ().text = numOfTeacherAttention.ToString();
				}
				
				btn.transform.FindChild ("img_cards").FindChild ("Text").GetComponent<Text> ().text = student.numOfSubmittedCards.ToString();
				btn.transform.FindChild ("img_featured").FindChild ("Text").GetComponent<Text> ().text = student.numOfFeaturedCards.ToString();

				Text txt_last_login = btn.transform.FindChild ("img_last_login").FindChild ("Text").GetComponent<Text> ();
				//Debug.Log ("Student userid: " + student.objectId);
				StartCoroutine (GetStudentLastLoginRecordRoutine (txt_last_login, student.objectId));
			}
		}
		
		// projects
		JSONNode allProjectsNode = LocalData.Instance.Data ["my-projects"];
		// filter projects that belong this course
		JSONNode projectsNode = JSON.Parse("{}");
		for (int i = 0; i < allProjectsNode.Count; i++) {
			JSONNode projectNode = allProjectsNode[i];
			string courseId = projectNode["course"]["objectId"].Value;
			if (courseId.Equals(_dynSelectedCourseId)) {
				string projectId = projectNode["objectId"].Value;
				projectsNode[projectId] = projectNode;
			}
		}

		//Debug.Log ("projectsNode: " + projectsNode.ToString());
		if (projectsNode != null) {
			// build ana array
			Project[] projects = new Project[projectsNode.Count];
			for (int i = 0; i < projectsNode.Count; i++) {
				Project proj = new Project(projectsNode[i]);
				projects[i] = proj;
			}
			Project.Sort(projects, _dynProjectSortBy, _dynProjectOrder);

			// show the projects
			GameManager.InstantiateChildren( projects.Length, _btnProjects );
			/*
			int missing = projects.Length - _btnProjects.Count;
			for (int i = 0; i < missing; i++) {
				GameObject copy = Instantiate (_btnProjects [0]);
				copy.transform.SetParent (_btnProjects [0].transform.parent);
				copy.transform.localScale = Vector3.one;
				
				_btnProjects.Add (copy);
			}
			*/

			// update table view
			for (int i = 0; i < projects.Length; i++) {
				Project project = projects[i];
				
				// assign the text
				GameObject btn = _btnProjects [i];
				btn.name = project.id;
				btn.transform.FindChild ("txt_title").GetComponent<Text> ().text = project.getTitle();
				
				TimeSpan timeRemain = (project.dueDate - DateTime.Now);
				int day = timeRemain.Days;
				string dueDate = project.dueDate.ToString("d MMM yyyy, hh:mm tt");
				if (day >= 0) {
					if (day >= 1) {
						dueDate += " <color=#" + GameManager.ColorToHex( GameManager.Instance.validColor[0]) + "> " + day + "d</color>";
					} else {
						dueDate += " <color=#" + GameManager.ColorToHex(GameManager.Instance.validColor[0]) + ">< 1d</color>";
					}
				} else {
					dueDate += " <color=#" + GameManager.ColorToHex(GameManager.Instance.validColor[1]) + ">Passed</color>";
				}

				btn.transform.FindChild ("txt_dueDate").GetComponent<Text> ().text = dueDate;
				btn.transform.FindChild ("txt_done").GetComponent<Text> ().text = project.numOfStudentDone + " / " + studentsNode.Count;
				btn.gameObject.SetActive (true);

				// assign image
				Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();
//				if (btnImageIcon.sprite != null) {
//					Sprite s = btnImageIcon.sprite;
//					btnImageIcon.sprite = null;
//					Destroy(s.texture);
//				}

				/*if (project.isImageExist) {
					btnImageIcon.sprite = DownloadManager.FileToSprite (project.ImagePath);
				} else */
				//if (!string.IsNullOrEmpty(project.imageUrl)) {
					btnImageIcon.sprite = DownloadManager.Instance.spriteLoading;
					DownloadManager.Instance.AddLinkage(project.ImagePath, project.imageUrl, btnImageIcon);
				//}

				// attention
				Transform badge = btn.transform.FindChild ("img_badge");
				if (project.numOfTeacherAttention <= 0) {
					badge.gameObject.SetActive (false);
				} else {
					badge.gameObject.SetActive (true);
					badge.FindChild ("Text").GetComponent<Text> ().text = project.numOfTeacherAttention.ToString();
				}
			}
		}
	}
	#endregion

	IEnumerator GetStudentLastLoginRecordRoutine(Text txt_last_login,string userId){
		DateTime lastLogin = DateTime.Now;
		bool done = false;
		bool gotRecord = false;
		//ParseObject log = new ParseObject ("LogInHistory");
		var query = ParseObject.GetQuery ("LogInHistory").WhereEqualTo ("loginUserId", userId).OrderByDescending("createdAt").Limit(1);
		query.FindAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				IEnumerable<ParseObject> logs = t.Result;
				Debug.Log("Log result found: " + userId);
				foreach(var mylog in logs){
					//Debug.Log("Getting datetime");
					//Debug.Log("Date: " + mylog.CreatedAt);
					lastLogin = (DateTime)mylog.CreatedAt;
					gotRecord = true;
				}

			}else{
				Debug.Log("Failed to get login record! Error: " + t.Exception.Message);
			}
			done = true;
		});
		yield return new WaitUntil (() => done == true);
		if (gotRecord) {
			lastLogin = lastLogin.AddHours ((double)8);
			//Debug.Log ("DateTime: " + lastLogin.ToString ());
			txt_last_login.text = lastLogin.ToString ();
		}

		yield return null;
	}

	#region Event
	public void EventButtonStudentsSortBy (GameObject btn) {
		StudentSortBy choice = StudentSortBy.Name;
		if (btn.name.Contains ("name")) {
			choice = StudentSortBy.Name;
		} else if (btn.name.Contains ("attention")) {
			choice = StudentSortBy.NumOfAttention;
		}  else if (btn.name.Contains ("cards")) {
			choice = StudentSortBy.NumOfCards;
		}  else if (btn.name.Contains ("featured")) {
			choice = StudentSortBy.NumOfFeatured;
		} else {
			Debug.LogError("Sortby unknown " + btn.name);
			return;
		}
		
		if (choice == _dynStudentSortBy) {
			if (_dynStudentOrder == SortingOrder.Asc) {
				SetStudentSorting(choice, SortingOrder.Desc);
			} else {
				SetStudentSorting(choice, SortingOrder.Asc);
			}
		} else {
			SetStudentSorting(choice, SortingOrder.Asc);
		}
		
		DataLoaded ();
	}
	
	private void SetStudentSorting (StudentSortBy key, SortingOrder order) {
		_dynStudentSortBy = key;
		_dynStudentOrder = order;
		
		_sortImageStudent[0].gameObject.SetActive(false);
		_sortImageStudent[1].gameObject.SetActive(false);
		_sortImageStudent[2].gameObject.SetActive(false);
		_sortImageStudent[3].gameObject.SetActive(false);
		
		Vector3 scale = Vector3.one;
		if (order == SortingOrder.Asc) {
			scale.y = -1;
		}
		
		switch(key) {
		case StudentSortBy.Name:
			_sortImageStudent[0].gameObject.SetActive(true);
			_sortImageStudent[0].transform.localScale = scale;
			break;
		case StudentSortBy.NumOfAttention:
			_sortImageStudent[1].gameObject.SetActive(true);
			_sortImageStudent[1].transform.localScale = scale;
			break;
		case StudentSortBy.NumOfCards:
			_sortImageStudent[2].gameObject.SetActive(true);
			_sortImageStudent[2].transform.localScale = scale;
			break;
		case StudentSortBy.NumOfFeatured:
			_sortImageStudent[3].gameObject.SetActive(true);
			_sortImageStudent[3].transform.localScale = scale;
			break;
		}
	}

	public void EventButtonProjectsSortBy (GameObject btn) {
		ProjectSortBy choice = ProjectSortBy.Name;
		if (btn.name.Contains ("name")) {
			choice = ProjectSortBy.Name;
		} else if (btn.name.Contains ("dueDate")) {
			choice = ProjectSortBy.DueDate;
		} else if (btn.name.Contains ("attention")) {
			choice = ProjectSortBy.NumOfAttention;
		} else {
			Debug.LogError("Sortby unknown " + btn.name);
			return;
		}
		
		if (choice == _dynProjectSortBy) {
			if (_dynProjectOrder == SortingOrder.Asc) {
				SetProjectSorting(choice, SortingOrder.Desc);
			} else {
				SetProjectSorting(choice, SortingOrder.Asc);
			}
		} else {
			SetProjectSorting(choice, SortingOrder.Asc);
		}
		
		DataLoaded ();
	}

	private void SetProjectSorting (ProjectSortBy key, SortingOrder order) {
		_dynProjectSortBy = key;
		_dynProjectOrder = order;
		
		_sortImageProject[0].gameObject.SetActive(false);
		_sortImageProject[1].gameObject.SetActive(false);
		_sortImageProject[2].gameObject.SetActive(false);
		
		Vector3 scale = Vector3.one;
		if (order == SortingOrder.Asc) {
			scale.y = -1;
		}
		
		switch(key) {
		case ProjectSortBy.Name:
			_sortImageProject[0].gameObject.SetActive(true);
			_sortImageProject[0].transform.localScale = scale;
			break;
		case ProjectSortBy.DueDate:
			_sortImageProject[1].gameObject.SetActive(true);
			_sortImageProject[1].transform.localScale = scale;
			break;
		case ProjectSortBy.NumOfAttention:
			_sortImageProject[2].gameObject.SetActive(true);
			_sortImageProject[2].transform.localScale = scale;
			break;
		}
	}

	public void EventButtonSelectProject (GameObject btn) {
		string projectId = btn.name;
		
		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["courseId"] = _dynSelectedCourseId;
		data ["projectId"] = projectId;
		
		NavigationBar.Instance.PushView (NavigationView.Teacher_View_Project, data);
	}
	public void EventButtonSelectStudent (GameObject btn) {
		string studentId = btn.name;
		
		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["courseId"] = _dynSelectedCourseId;
		data ["studentId"] = studentId;
		
		NavigationBar.Instance.PushView (NavigationView.Teacher_View_Student, data);
	}
	
	public void EventButtonSelectTab (GameObject btn) {
		if (btn.name.Contains ("student")) {
			ShowTabPage (TabPage.Students);
			NavigationBar.Instance.SetRightButton();
		} else if (btn.name.Contains ("project")) {
			ShowTabPage (TabPage.Projects);
		}
	}
	
	public void EventButtonAddProject () {
		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["courseId"] = _dynSelectedCourseId;
		
		NavigationBar.Instance.PushView (NavigationView.Teacher_View_Course_AddProject, data);
	}
	
	#endregion
	
	#region Parse
	
	private void RenewCourseData ()
	{
		Debug.Log ("[Teacher_Course] RenewCourseData()");

		if (LocalData.Instance.IsTestUser > 0) {
			state = PageState.DATA_RETRIEVED;
			return;
		}

		//Debug.Log ("RenewCourseData");
		LoadingTask task = new LoadingTask("Loading Data...");
		//task.DisplayMessage = "Loading Data...";
		//task.CanCancel = true;
		////task.OnLoadingCancel += onCancelled;
		GameManager.Instance.LoadingTasks.Add (task);
		
		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();
		data ["courseId"] = _dynSelectedCourseId;
		
		ParseCloud.CallFunctionAsync<string>("GetCourseData", data).ContinueWith(t => {
			if (t.IsFaulted) {
				Debug.LogError("Fail GetCourseData");
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
				Debug.Log ("[Teacher_Course] RenewCourseData() result: " + result);
				JSONNode node = JSON.Parse(result);
				CacheManager.Instance.HandleTeacherSpecificCourseData(_dynSelectedCourseId, node);

				state = PageState.DATA_RETRIEVED;
			}
			task.Loading = false;
		});
	}
	#endregion
}

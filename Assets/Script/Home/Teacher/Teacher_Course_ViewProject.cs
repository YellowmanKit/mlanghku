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

public class Teacher_Course_ViewProject : Page {

	public GameObject				rankingObj,allRankingObj;
	public GameObject 				refreshButtonObj;
	private	bool					rotateRefreshBtn;
	private bool 					rankExist;

	private Project					project;

	public enum TabPage {
		Info,
		Featured,
		Students
	}
	private TabPage					_dynCurrentPage = TabPage.Students;
	
	private string					_dynSelectedCourseId = "";
	private string					_dynSelectedProjectId = "";
	
	public Text						_txtTabInfo = null;
	public Text						_txtTabFeatured = null;
	public Text						_txtTabStudents = null;
	
	public Color[]					_navigationColors = null;
	
	public GameObject				_panelInfo = null;
	public Text						_txtInfoProjectName = null;
	public Text						_txtInfoProjectDesc = null;
	public Text						_txtInfoProjectDueDate = null;
	public Text						_txtInfoProjectTimeRemain = null;
	public Text						_txtInfoProjectSubmitted = null;
	
	public GameObject				_panelFeatured = null;
	public List<GameObject>			_btnFeaturedCards = null;
	public GameObject				_panelFeaturedSubMenu = null;
	public GameObject				_btnAddFeaturedCard = null;
	
	public GameObject				_panelStudents = null;
	public List<GameObject>			_btnStudents = null;
	public List<Transform>			_sortImage = null;
	private StudentProjectSortBy	_dynStudentSortBy = StudentProjectSortBy.NumOfAttention;
	private SortingOrder			_dynStudentOrder = SortingOrder.Desc;

	// Use this for initialization
	void Start () {
		_txtInfoProjectName.text = "";
		_txtInfoProjectDesc.text = "";
		_txtInfoProjectDueDate.text = "";
		_txtInfoProjectTimeRemain.text = "";
		_txtInfoProjectSubmitted.text = "";

		SetSorting(StudentProjectSortBy.NumOfAttention, SortingOrder.Desc);
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

				if (CacheManager.Instance.IsTeacherSpecificProjectStudentProjectListOutdated(_dynSelectedProjectId)) {
					RenewData();
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
		if (rotateRefreshBtn) {
			refreshButtonObj.GetComponent<LangChooser> ()._key = "refreshing";
			refreshButtonObj.GetComponent<LangChooser> ().Start ();
		} else {
			refreshButtonObj.GetComponent<LangChooser> ()._key = "refresh";
			refreshButtonObj.GetComponent<LangChooser> ().Start ();
		}
	}

	void OnDestroy() {
//		for (int i=0; i<_btnFeaturedCards.Count; ++i) {
//			GameObject btn = _btnFeaturedCards [i];
//			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();
//
//			if (btnImageIcon.sprite != null) {
//				Sprite s = btnImageIcon.sprite;
//				btnImageIcon.sprite = null;
//				Destroy(s.texture);
//			}
//		}
	}
	
	#region Socket
	
	public override	void Init (Dictionary<string, object> data)
	{
		Debug.Log ("[Teacher_Course_ViewProject] Init()");
		if (data != null) {
			if (data.ContainsKey("projectId")) {
				_dynSelectedProjectId = (string)(data ["projectId"]);
				if (data.ContainsKey("courseId")) {
					_dynSelectedCourseId = (string)(data ["courseId"]);
				} else {
					JSONNode projectNode = LocalData.Instance.Data["my-projects"][_dynSelectedProjectId];
					_dynSelectedCourseId = projectNode["course"]["objectId"].Value;
				}

				foreach (GameObject btn in _btnStudents     ) { btn.SetActive (false); }
				foreach (GameObject btn in _btnFeaturedCards) { btn.SetActive (false); }

				if (data.ContainsKey("tab")) {
					string tab = (string)(data ["tab"]);
					if (tab.Equals("featured")) {
						ShowTabPage(TabPage.Featured);
					} else {
						ShowTabPage(TabPage.Students);
					}
				} else {
					ShowTabPage(TabPage.Students);
				}

				state = PageState.INIT;
			} else {
				Debug.LogWarning("TeacherCourse: SocketBecomeVisible data no key");
			}
		}
	}

	public override void OnPageShown (Dictionary<string, object> data) {
		Debug.Log ("[Teacher_Course_ViewProject] OnPageShown() _dynSelectedProjectId=" + _dynSelectedProjectId);
		if (data != null && data.ContainsKey("action")) {
			string action = (string)data["action"];
			if (action.Equals("reload")) {
				RenewData();
			}
		}

		JSONNode projectNode = LocalData.Instance.Data ["my-projects"][_dynSelectedProjectId];
		project = new Project(projectNode);
		NavigationBar.Instance.SetTitle(project.getTitle());
		
		if (_dynCurrentPage == TabPage.Students) {
			NavigationBar.Instance.SetRightButton ();
		} else if (_dynCurrentPage == TabPage.Info) {
			if (LocalData.Instance.UserIdentity == ParseUserIdentity.Teacher)
				NavigationBar.Instance.SetRightButton(NavigationButtonType.EDIT, EventButtonModifyProjectInfo);
		} else if (_dynCurrentPage == TabPage.Featured) {
			NavigationBar.Instance.SetRightButton(NavigationButtonType.MORE, EventButtonFeaturedSubMenuToggle);
		}
		
		DataLoaded ();
	}
	#endregion
	
	#region Helper
	private void ShowTabPage (TabPage tabPage)
	{
		//Debug.Log ("[Teacher_Course_ViewProject] ShowTabPage()");
		_dynCurrentPage = tabPage;
		
		_panelInfo.SetActive(false);
		_panelStudents.SetActive (false);
		_panelFeatured.SetActive (false);
		_panelFeaturedSubMenu.SetActive(false);
		
		_txtTabFeatured.color = _txtTabStudents.color = _txtTabInfo.color = _navigationColors [1];
		
		switch (tabPage) {
		case TabPage.Info:
			StartCoroutine(SyncGetRanking ());
			_txtTabInfo.color = _navigationColors[0];
			_panelInfo.SetActive (true);
			RefreshTimeRemain ();
			break;
		case TabPage.Featured:
			_txtTabFeatured.color = _navigationColors [0];
			_panelFeatured.SetActive (true);
			break;
		case TabPage.Students:
			_txtTabStudents.color = _navigationColors[0];
			_panelStudents.SetActive (true);
			break;
		}
	}
	
	private void RefreshTimeRemain () {// submitted?
		Debug.Log ("[Teacher_Course_ViewProject] RefreshTimeRemain()");

		JSONNode projectNode = LocalData.Instance.Data["my-projects"][_dynSelectedProjectId];

		if (projectNode != null) {
			Project project = new Project(projectNode);
			
			// time remain
			string timeRemainString = "";
			TimeSpan timeRemain = (project.dueDate - DateTime.Now);
			if (timeRemain.TotalSeconds >= 0) {
				// still have time
				timeRemainString += GameManager.TimeRemainToString(timeRemain.Days, timeRemain.Hours, timeRemain.Minutes);
				
				_txtInfoProjectTimeRemain.text = timeRemainString;
				
			} else {
				// overdue
				timeRemainString = GameManager.Instance.Language.getString("passed", CaseHandle.FirstCharUpper);
				
				_txtInfoProjectTimeRemain.text = timeRemainString;
				
			}
		} else {
			_txtInfoProjectTimeRemain.text = "";
		}
		
		
	}
	
	private void DataLoaded ()
	{
		Debug.Log ("[Teacher_Course_ViewProject] DataLoaded()");

		foreach (GameObject btn in _btnStudents     ) { btn.SetActive (false); }
		foreach (GameObject btn in _btnFeaturedCards) { btn.SetActive (false); }
		
		// list of all students
		JSONNode studentsNodes = CacheManager.Instance.CacheNode ["course"][_dynSelectedCourseId]["students"];
		
		// info
		JSONNode projectNode = LocalData.Instance.Data["my-projects"][_dynSelectedProjectId];
		if (projectNode != null) {
			Project project = new Project(projectNode);
			
			_txtInfoProjectName.text = project.getTitle();
			_txtInfoProjectDesc.text = project.desc;
			_txtInfoProjectDueDate.text = project.dueDate.ToString("dddd, d MMMM yyyy, hh:mm tt");
			_txtInfoProjectSubmitted.text = project.numOfStudentDone + " / " + studentsNodes.Count;
			
			RefreshTimeRemain ();
		} else {
			_txtInfoProjectName.text = "";
			_txtInfoProjectDesc.text = "";
			_txtInfoProjectDueDate.text = "";
			_txtInfoProjectTimeRemain.text = "";
			_txtInfoProjectSubmitted.text = "";
		}

		// students
		StudentProject[] studentProjects = new StudentProject[studentsNodes.Count];
		if (studentsNodes != null) {
			JSONNode studentProjectsNode = CacheManager.Instance.CacheNode ["project-" + _dynSelectedProjectId + "-submitted-list"];
			// build the list
			for (int i = 0; i < studentsNodes.Count; i++) {
				Student student = new Student(studentsNodes[i]);
				StudentProject studentProject = null;
				// have studentProject ? Or just student info
				//Debug.LogWarning(studentProjectsNode.ToString() );
				//Debug.Log (" * " + studentProjectsNode.ToString() + " - " + studentProjectsNode == null);
				if (studentProjectsNode != null && studentProjectsNode.GetKeys().Contains(student.id)) {
					JSONNode studentProjectNode = studentProjectsNode[student.id];
					studentProject = new StudentProject(studentProjectNode);
				} else {
					studentProject = new StudentProject();
				}
				studentProject.student = student;
				studentProjects[i] = studentProject;
			}
		}
		// sorting
		StudentProject.Sort(studentProjects, _dynStudentSortBy, _dynStudentOrder);

		GameManager.InstantiateChildren( studentProjects.Length, _btnStudents );
		/*
		int numOfStudents = studentProjects.Length;
		int missing = numOfStudents - _btnStudents.Count;
		for (int i = 0; i < missing; i++) {
			GameObject copy = Instantiate (_btnStudents [0]);
			copy.transform.SetParent (_btnStudents [0].transform.parent);
			copy.transform.localScale = Vector3.one;
			
			_btnStudents.Add (copy);
		}
		*/

		for (int i = 0; i < studentProjects.Length; i++) {
			StudentProject studentProject = studentProjects[i];
			//string studentId = studentProject.student.id;
			
			// assign the text
			GameObject btn = _btnStudents [i];
			btn.transform.FindChild ("txt_title").GetComponent<Text> ().text = studentProject.student.realName;
			btn.gameObject.SetActive (true);

			//int numOfTeacherAttention = 0;
			//int numOfSubmittedCards = 0;

			if (studentProject.numOfCards == 0) {
				btn.name = "";	// not allowed to click
			} else {
				btn.name = studentProject.id;
			}

			// attention
			Transform badge = btn.transform.FindChild ("img_badge");
			if (studentProject.numOfTeacherAttention <= 0) {
				badge.gameObject.SetActive(false);
			} else {
				//imgBadge.enabled = true;
				badge.gameObject.SetActive(true);
				badge.FindChild ("Text").GetComponent<Text> ().text = studentProject.numOfTeacherAttention.ToString();
			}
			
			// num of cards
			Transform cards = btn.transform.FindChild ("img_cards");
			cards.gameObject.SetActive(true);
			cards.FindChild ("Text").GetComponent<Text> ().text = studentProject.numOfCards.ToString();
			
			// num of featured cards
			Transform img_featured = btn.transform.FindChild ("img_featured");
			img_featured.gameObject.SetActive(true);
			img_featured.FindChild ("Text").GetComponent<Text> ().text = studentProject.numOfFeaturedCards.ToString();
		}

		// featured cards
		JSONNode featruedCardsNode = LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId];
		Card[] featuredCards = LocalData.JSONCardsToListCards(featruedCardsNode, true);

		foreach (GameObject btn in _btnFeaturedCards) { btn.SetActive(false); }
		GameManager.InstantiateChildren( featuredCards.Length, _btnFeaturedCards );
		/*
		int numOfCards = featuredCards.Length;
		missing = numOfCards - _btnFeaturedCards.Count;
		for (int i = 0; i < missing; i++) {
			GameObject copy = Instantiate (_btnFeaturedCards [0]);
			copy.transform.SetParent (_btnFeaturedCards [0].transform.parent);
			copy.transform.localScale = Vector3.one;
			
			_btnFeaturedCards.Add (copy);
		}
		*/

		int idx = 0;
		foreach (FeaturedCard card in featuredCards) {
			// assign the text
			GameObject btn = _btnFeaturedCards [idx++];
			btn.name = card.cardId;
			btn.transform.FindChild ("Text").GetComponent<Text> ().text = card.GetDefaultDisplayName();
			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();

//			if (btnImageIcon.sprite != null) {
//				Sprite s = btnImageIcon.sprite;
//				btnImageIcon.sprite = null;
//				Destroy(s.texture);
//			}
			
			btn.gameObject.SetActive (true);
			// update the icon
			// "cardId" is "card-xxxxxxxxxx"
//			if (card.isImageExist) {
//				btnImageIcon.sprite = DownloadManager.FileToSprite (card.ImagePath);
//			} else {
				// does it provide a url?
				if (!string.IsNullOrEmpty(card.imageUrl)) {
					btnImageIcon.sprite = DownloadManager.Instance.spriteLoading;
					DownloadManager.Instance.AddLinkage(card.ImagePath, card.imageUrl, btnImageIcon);
				} else {
					btnImageIcon.sprite = DownloadManager.Instance.spriteNoImage;
					Debug.LogWarning("No image file, and no url provided for " + card.GetDefaultDisplayName());
				}
//			}

			Transform badge = btn.transform.FindChild ("img_badge");
			if (card.authorId.Equals(LocalData.CurrentUserId)) {
				badge.gameObject.SetActive(true);
			} else {
				badge.gameObject.SetActive(false);
			}
		}

		//------------------------------------------------------------------------------
		// v1.3 check if the text is too long...
		//------------------------------------------------------------------------------
		bool _panelFeatured_isActive = _panelFeatured.activeInHierarchy;
		_panelFeatured.SetActive(true);
		Canvas.ForceUpdateCanvases();
		foreach (GameObject btn in _btnFeaturedCards) {
			GameManager.Instance.CheckTextLength (btn.transform.FindChild ("Text").GetComponent<Text> ());
		}
		_panelFeatured.SetActive (_panelFeatured_isActive);
		//------------------------------------------------------------------------------
	}

	#endregion
	
	#region Event
	public void EventButtonStudentsSortBy (GameObject btn) {
		StudentProjectSortBy choice = StudentProjectSortBy.Name;
		if (btn.name.Contains ("name")) {
			choice = StudentProjectSortBy.Name;
		} else if (btn.name.Contains ("card")) {
			choice = StudentProjectSortBy.NumOfCard;
		} else if (btn.name.Contains ("attention")) {
			choice = StudentProjectSortBy.NumOfAttention;
		} else if (btn.name.Contains ("featured")) {
			choice = StudentProjectSortBy.NumOfFeatured;
		} else {
			Debug.LogError("Sortby unknown " + btn.name);
			return;
		}

		if (choice == _dynStudentSortBy) {
			if (_dynStudentOrder == SortingOrder.Asc) {
				SetSorting(choice, SortingOrder.Desc);
			} else {
				SetSorting(choice, SortingOrder.Asc);
			}
		} else {
			SetSorting(choice, SortingOrder.Asc);
		}
		
		DataLoaded ();
	}

	private void SetSorting (StudentProjectSortBy key, SortingOrder order) {
		_dynStudentSortBy = key;
		_dynStudentOrder = order;

		_sortImage[0].gameObject.SetActive(false);
		_sortImage[1].gameObject.SetActive(false);
		_sortImage[2].gameObject.SetActive(false);
		_sortImage[3].gameObject.SetActive(false);

		Vector3 scale = Vector3.one;
		if (order == SortingOrder.Asc) {
			scale.y = -1;
		}
		
		//Debug.Log (key.ToString());
		switch(key) {
		case StudentProjectSortBy.Name:
			_sortImage[0].gameObject.SetActive(true);
			_sortImage[0].transform.localScale = scale;
			break;
		case StudentProjectSortBy.NumOfCard:
			_sortImage[1].gameObject.SetActive(true);
			_sortImage[1].transform.localScale = scale;
			break;
		case StudentProjectSortBy.NumOfAttention:
			_sortImage[2].gameObject.SetActive(true);
			_sortImage[2].transform.localScale = scale;
			break;
		case StudentProjectSortBy.NumOfFeatured:
			_sortImage[3].gameObject.SetActive(true);
			_sortImage[3].transform.localScale = scale;
			break;
		}
	}

	public void EventButtonSelectStudentProject (GameObject btn) {
		string studentProjectId = btn.name;

		if (string.IsNullOrEmpty(studentProjectId)) {
			
			GameManager.Instance.AlertTasks.Add (
				new AlertTask(
				GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
				, GameManager.Instance.Language.getString("student-did-not-submit-any-cards", CaseHandle.FirstCharUpper)
				, TextAnchor.MiddleLeft));
			return;
		}

		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["studentProjectId"] = studentProjectId;
		data ["projectId"] = _dynSelectedProjectId;

		//Debug.Log ("HERE: studentProjectId=" + studentProjectId + " _dynSelectedProjectId=" + _dynSelectedProjectId);

		NavigationBar.Instance.PushView (NavigationView.Teacher_View_StudentProject, data);
	}
	
	public void EventButtonSelectTab (GameObject btn) {
		if (btn.name.Contains ("student")) {
			ShowTabPage (TabPage.Students);
			NavigationBar.Instance.SetRightButton();
		} else if (btn.name.Contains ("info")) {
			ShowTabPage (TabPage.Info);
			
			if (LocalData.Instance.UserIdentity == ParseUserIdentity.Teacher)
				NavigationBar.Instance.SetRightButton(NavigationButtonType.EDIT, EventButtonModifyProjectInfo);
		} else if (btn.name.Contains ("featured")) {
			ShowTabPage (TabPage.Featured);
			
			NavigationBar.Instance.SetRightButton(NavigationButtonType.MORE, EventButtonFeaturedSubMenuToggle);
		}
	}
	
	public void EventButtonSelectedFeaturedCard (GameObject btn) {
		
		string cardId = btn.name;
		int index = -1;
		JSONNode featuredCardsNode = LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId];
		Card[] featuredCards = LocalData.JSONCardsToListCards(featuredCardsNode, true);
		for (int i = 0; i < featuredCards.Length; i++) {
			if (cardId.Equals(featuredCards[i].cardId)) {
				index = i;
				break;
			}
		}
		if (index >= 0) {
			Dictionary<string, object> data = new Dictionary<string, object> ();
			data ["projectId"] = _dynSelectedProjectId;
			data ["isFeaturedCard"] = true;
			data ["json"] = LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId];
			data ["cardIndex"] = index;
			
			NavigationBar.Instance.PushView (NavigationView.Common_ViewCard, data);
		} else {
			Debug.Log ("Unknown index");
		}
	}
	
	public void EventButtonAddFeaturedCard () {
		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["mode"] = AddCardMode.Teacher_Add;
		data ["projectId"] = _dynSelectedProjectId;
		data ["cardId"] = LocalData.CurrentUserId + "-card-" + GameManager.GetCurrentTimestemp();
		
		NavigationBar.Instance.PushView (NavigationView.Common_AddCard, data);
	}
	
	public void EventButtonModifyProjectInfo () {

		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["courseId"] = _dynSelectedCourseId;
		data ["objectId"] = _dynSelectedProjectId;
		
		NavigationBar.Instance.PushView (NavigationView.Teacher_View_Course_AddProject, data);
	}

	public void EventButtonFeaturedSubMenuToggle () {
		_btnAddFeaturedCard.SetActive(LocalData.Instance.UserIdentity == ParseUserIdentity.Teacher);
		_panelFeaturedSubMenu.SetActive(!_panelFeaturedSubMenu.activeSelf);
	}
	private void EventButtonFeaturedSubMenuShow () {
		_panelFeaturedSubMenu.SetActive(true);
	}
	public void EventButtonFeaturedSubMenuHide() {
		_panelFeaturedSubMenu.SetActive(false);
	}
	public void EventButtonFeaturedSubMenuAddCard () {
		EventButtonAddFeaturedCard();
		EventButtonFeaturedSubMenuHide();
	}
	public void EventButtonFeaturedSubMenuRefresh () {
		
		if (LocalData.Instance.IsTestUser > 0) {
			return;
		}

		RenewData();
		EventButtonFeaturedSubMenuHide();
	}

	#endregion
	
	#region Parse

	//static string dummyPath = Application.dataPath + "/dummytest.json";
	private void RenewData ()
	{
		Debug.Log ("[Teacher_Course_ViewProject] RenewData()");
		if (LocalData.Instance.IsTestUser > 0) {
			Debug.LogWarning("Stopped sync");
			return;
		}

		LoadingTask task = new LoadingTask("Loading Data...");
		//task.DisplayMessage = "Loading Data...";
		//task.CanCancel = true;
		////task.OnLoadingCancel += onCancelled;
		GameManager.Instance.LoadingTasks.Add (task);
		
		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();
		data ["courseId"] = _dynSelectedCourseId;
		data ["projectId"] = _dynSelectedProjectId;
		
		ParseCloud.CallFunctionAsync<string>("TeacherGet_SpecificProject", data).ContinueWith(t => {
			if (t.IsFaulted) {
				NavigationBar.Instance.syncInProgress = false;
				Debug.LogError("Fail TeacherGet_SpecificProject");
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
				Debug.Log ("[Teacher_Course_ViewProject] TeacherGet_SpecificProject: result: " + result);

				//File.WriteAllText(dummyPath, result);

				JSONNode node = JSON.Parse(result);
				CacheManager.Instance.HandleTeacherSpecificProject_StudentProjectList(_dynSelectedProjectId, node["studentProject"]);
				CacheManager.Instance.TeacherHandleStudentList(_dynSelectedCourseId, node["students"]);
				CacheManager.Instance.HandleFeaturedCards(node["featuredCards"], node["langs"]);
				
				state = PageState.DATA_RETRIEVED;
			}
			task.Loading = false;
		});

	}
	#endregion

	IEnumerator SyncGetRanking(){
		if (project.isSlideShow()) {
			allRankingObj.SetActive (false);
			yield break;
		}
		if (rankExist) {
			yield break;
		}
		rankExist = true;
		rotateRefreshBtn = true;
		for (int i = 1; i < rankingObj.transform.childCount; i++) {
			Destroy (rankingObj.transform.GetChild (i).gameObject);
		}
		bool queryDone = false;
		List<int> cards = new List<int> ();
		List<int> featured = new List<int> ();
		List<string> studentObjid = new List<string> ();
		List<string> names = new List<string> ();


		yield return new WaitUntil (() => state == PageState.END);
		Debug.Log ("Searching for project id: " + project.objectId);
		ParseQuery<ParseObject>	projQuery = new ParseQuery<ParseObject> ("Project").WhereEqualTo ("objectId", project.objectId);
		ParseQuery<ParseObject> query = new ParseQuery<ParseObject> ("StudentProject").WhereMatchesQuery ("project", projQuery);
		query.FindAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				IEnumerable<ParseObject> studentProjects = t.Result;
				foreach(var studentProj in studentProjects){
					//Debug.Log(studentProj.ObjectId);
					cards.Add(studentProj.Get<int>("numOfSubmittedCard"));
					featured.Add(studentProj.Get<int>("numOfFeaturedCards"));

					ParseUser tarStudent = (ParseUser)studentProj["student"];
					Debug.Log("Add Student id: " + tarStudent.ObjectId);
					studentObjid.Add(tarStudent.ObjectId);
				}
			}else{
				Debug.Log("Error: " + t.Exception.Message);
				queryDone = true;
			}
			Debug.Log("Proj query done!");
			queryDone = true;

		});
		yield return new WaitUntil (() => queryDone == true);
		if (studentObjid.Count > 0) {
			queryDone = false;
		}
		ParseQuery<ParseUser> studentQuery = ParseUser.Query.Limit(25);
		int loopCount = 0;
		bool nameQueryDone = false;
		foreach (string id in studentObjid) {
			nameQueryDone = false;
			Debug.Log("Seachng for student id: " + id);

			studentQuery.GetAsync (id).ContinueWith (t2 => {
				if(!t2.IsFaulted){
					ParseUser student = t2.Result;
					string name = "" + student.Get<string>("realName") + " ";
					Debug.Log("Add name: " + name);
					names.Add(name);
				}else{
					Debug.Log("Error: " + t2.Exception.Message);
					names.Add("Unknown Student");
				}
				loopCount++;
				if(loopCount == studentObjid.Count){
					Debug.Log("Name query done! count: " + loopCount);
					queryDone = true;
				}
				nameQueryDone = true;
			});
			yield return new WaitUntil (() => nameQueryDone == true);
		}
		yield return new WaitUntil (() => queryDone == true);

		//calculate their score
		List<int> scores = new List<int> ();
		List<int> order = new List<int> ();

		for (int i = 0; i < cards.Count; i++) {
			int _score = cards [i] + featured [i] * 2;
			//Debug.Log ("_score: " + _score);
			if (scores.Count == 0) {
				scores.Add (_score);
				order.Add (i);
			}else{
				for(int j=0;j<scores.Count;j++) {
					//Debug.Log ("scores.Count: " + scores.Count);
					//Debug.Log ("j: " + j);

					if (_score > scores[j]) {
						scores.Insert (j,_score);
						order.Insert (j,i);
						break;
					}
					if (j == scores.Count - 1) {
						scores.Add (_score);
						order.Add (i);
						break;
					}
				}
			}
			foreach (int score in scores) {
				//Debug.Log ("scores: " + score);
			}
			//Debug.Log ("loop: " + i);
		}
//		foreach (string id in studentObjid) {
//			Debug.Log ("id: " + id);
//		}
//		foreach (string name in names) {
//			Debug.Log ("name: " + name);
//		}
//		foreach (int cardNum in cards) {
//			Debug.Log ("card: " + cardNum);
//		}
//		foreach (int score in scores) {
//			Debug.Log ("scores: " + score);
//		}
//		foreach (int ord in order) {
//			Debug.Log ("order: " + ord);
//		}

		//Debug.Log ("Start to set rank log");

		//float tranStep = Camera.main.pixelHeight * -0.04f;

		int count = 0;
		foreach(int i in order){
			GameObject log = Instantiate (rankingObj.transform.Find ("Log").gameObject,rankingObj.transform.Find ("Log").position,rankingObj.transform.Find ("Log").rotation);
			log.transform.SetParent (rankingObj.transform);
			log.transform.localScale = Vector3.one;
			//log.transform.Translate (new Vector3 (0f, tranStep*count, 0f));
			log.SetActive (true);

			log.transform.Find ("name").gameObject.GetComponent<Text> ().text = "" + names [i];
			log.transform.Find ("cards_number").gameObject.GetComponent<Text> ().text = "" + cards [i];
			log.transform.Find ("featured_number").gameObject.GetComponent<Text> ().text = "" + featured [i];
			log.transform.Find ("rank").gameObject.GetComponent<Text> ().text = "" + (count + 1) + ".";
			log.transform.Find ("score").gameObject.GetComponent<Text> ().text = "" + (cards [i] + 2 * featured [i]);
			count++;
		}
		rotateRefreshBtn = false;
		refreshButtonObj.GetComponent<Button> ().enabled = true;
	}

	public void EventRefreshRanking(){
		refreshButtonObj.GetComponent<Button> ().enabled = false;
		rankExist = false;
		StartCoroutine (SyncGetRanking ());
	}
}

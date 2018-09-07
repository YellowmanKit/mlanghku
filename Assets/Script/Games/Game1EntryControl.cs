using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Parse;
using System;

public class Game1EntryControl : MonoBehaviour {

	public Text nameTitle;

	public GameObject confirmName,confirmCourse,confirmProjList,confirmDue,confirmBtn,missionList,expiredList,missionDetailObj,allRankingObjs;

	public GameObject assignMissionBtnObj,courseList,projectList,missionNameObj,dueDateObj,datePickerObj;

	private GameObject selectedMissionDetail;

	public GameObject[] pageObjs,assignMissionObj;

	private int currentPageObjIndex,currectAssignMissionIndex;

	private bool visitedExpired;

	private List<string> coursesObjId = new List<string> ();

	private List<string> projectObjId = new List<string> ();

	private List<string> selectedProjsName = new List<string> ();

	private List<string> selectedProjectObjId = new List<string> ();

	private List<ParseObject> allMissions = new List<ParseObject> ();

	private List<ParseObject> availMissions = new List<ParseObject> ();

	private List<ParseObject> expiredMissions = new List<ParseObject> ();

	public string selectedCourseId,selectedCourseName;

	private int displayProjSP,displayProjEP,displayExpiredSP,displayExpiredEP;

	private Text _dynDatePickerTextFeild;

	public ParseObject selectedMission;

	public bool missionMode;

	public List<string> projectsIdToUseInMission = new List<string> ();

	// Use this for initialization
	void Start () {
		GameObject.DontDestroyOnLoad (gameObject);
		displayProjEP = 4;
		displayExpiredEP = 11;
		CheckIdentity ();
		InitializePage ();

		//Debug.Log ("User id: " + LocalData.Instance.Data ["user"] ["objectId"]);
	}

	private void InitializePage(){
		if (LocalData.Instance != null) {
			nameTitle.text = LocalData.Instance.Data ["user"] ["realName"];
			StartCoroutine(UpdateMissionAndExpiredList ());
		}
		foreach (GameObject obj in pageObjs) {
			obj.SetActive (false);
		}
		pageObjs[0].SetActive(true);
		foreach (GameObject obj in assignMissionObj) {
			obj.SetActive (false);
		}
		assignMissionObj[0].SetActive(true);
	}

	private void CheckIdentity(){
		if (LocalData.Instance == null) {
			return;
		}
		Camera.main.gameObject.GetComponent<AudioListener> ().enabled = false;
		if (LocalData.Instance.UserIdentity == ParseUserIdentity.Teacher) {
			assignMissionBtnObj.SetActive (true);
			UpdateCourseList();
			InitDueDateAndMissionName ();
			datePickerObj.SetActive (true);
			pageObjs [4].transform.FindChild ("Delete").gameObject.SetActive (true);
		} else {
			assignMissionBtnObj.SetActive (false);
		}
	}

	public void EventUpdateRanking(){
		StartCoroutine (UpdateRankingList ());
	}

	IEnumerator UpdateRankingList(){
		allRankingObjs.transform.parent.GetChild (0).GetChild(0).gameObject.GetComponent<Text> ().text = selectedMission.Get<string> ("missionName");
		GameObject loading = allRankingObjs.transform.parent.GetChild (1).gameObject;

		for (int i = 0; i < allRankingObjs.transform.childCount; i++) {
			allRankingObjs.transform.GetChild (i).gameObject.SetActive (false);
		}
		string missionId = selectedMission.ObjectId;
		Transform existRanking = allRankingObjs.transform.FindChild (missionId);
		if (existRanking != null) {
			existRanking.gameObject.SetActive (true);
			yield break;
		}
		loading.GetComponent<MultiLang> ().Start ();

		GameObject oriRankingObj = allRankingObjs.transform.GetChild (0).gameObject as GameObject;
		GameObject rankingObj = Instantiate (oriRankingObj,oriRankingObj.transform.position,oriRankingObj.transform.rotation) as GameObject;
		rankingObj.transform.SetParent (allRankingObjs.transform);
		rankingObj.transform.localScale = Vector3.one;
		rankingObj.name = selectedMission.ObjectId;


		bool done = false;

		ParseQuery<ParseObject> allHighscoreQuery = new ParseQuery<ParseObject> ("Game1BestScore").WhereEqualTo ("inMission", selectedMission).OrderByDescending("HighScore1").Limit(20);

		List<ParseObject> hightScores = new List<ParseObject> ();

		allHighscoreQuery.FindAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				IEnumerable<ParseObject> highscores = t.Result;
				foreach(var highScore in highscores){
					hightScores.Add(highScore);
					Debug.Log("Get highscore!");
				}
			}else{
				Debug.Log("Failed to get all highscore!");
			}
			done = true;
		});
		yield return new WaitUntil (() => done == true);
		int count = 0;
		GameObject oriRow = rankingObj.transform.FindChild("row").gameObject;
		foreach (ParseObject highScore in hightScores) {
			GameObject log = Instantiate (oriRow,oriRow.transform.position,oriRow.transform.rotation);
			log.transform.SetParent (rankingObj.transform);
			log.transform.localScale = Vector3.one;
			//log.transform.Translate (new Vector3 (0f, tranStep*count, 0f));
			log.SetActive (true);

			done = false;
			ParseUser player = (ParseUser)highScore ["Player"];
			player.FetchAsync ().ContinueWith (t => {
				done = true;
			});
			yield return new WaitUntil (() => done == true);
			log.transform.Find ("name").gameObject.GetComponent<Text> ().text = "" + player.Get<string> ("realName");
			log.transform.Find ("score").gameObject.GetComponent<Text> ().text = "" + highScore.Get<int>("HighScore1");
			log.transform.Find ("rank").gameObject.GetComponent<Text> ().text = "" + (count + 1) + ".";
			count++;
			//Debug.Log (player.ObjectId + " & " + LocalData.Instance.Data ["user"] ["objectId"]);
			if (player.ObjectId.Equals(LocalData.Instance.Data ["user"] ["objectId"])) {
				log.transform.Find ("name").gameObject.GetComponent<Text> ().color = Color.green;
				log.transform.Find ("score").gameObject.GetComponent<Text> ().color = Color.green;
				log.transform.Find ("rank").gameObject.GetComponent<Text> ().color = Color.green;
			}
		}
		oriRow.SetActive (false);
		rankingObj.SetActive (true);

		loading.GetComponent<Text> ().text = "";
	}

	public void EventDeleteMission(){
		StartCoroutine (DeleteMission ());
	}

	IEnumerator DeleteMission(){
		pageObjs [6].transform.GetChild (1).gameObject.SetActive (false);
		pageObjs [6].transform.GetChild (2).gameObject.SetActive (false);
		Text deleteMissionText = pageObjs [6].transform.GetChild (0).gameObject.GetComponent<Text> ();
		deleteMissionText.text = "Processing...";
		bool done = false;
		bool success = false;
		selectedMission.DeleteAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				success = true;
			}else{
				success = false;
			}
			done = true;
		});
		yield return new WaitUntil (() => done);
		if (success) {
			deleteMissionText.text = "Mission deleted!";
		} else {
			deleteMissionText.text = "Failed to delete mission!";
		}
		pageObjs [6].transform.GetChild (3).gameObject.SetActive (true);
		StartCoroutine (UpdateMissionAndExpiredList ());
	}


	public void EventFinishDeleteMission(){
		pageObjs [6].transform.GetChild (1).gameObject.SetActive (true);
		pageObjs [6].transform.GetChild (2).gameObject.SetActive (true);
		pageObjs [6].transform.GetChild (3).gameObject.SetActive (false);
		EventSwitchToPage (1);
	}

	public void EventSelectExpiredMission(GameObject btn){
		int index = btn.transform.GetSiblingIndex ();
		selectedMission = expiredMissions [index];
		StartCoroutine(ShowMissionDetail ());
	}

	public void EventSelectAvailMission(GameObject btn){
		int index = btn.transform.GetSiblingIndex ();
		selectedMission = availMissions [index];
		StartCoroutine(ShowMissionDetail ());
	}

	public void GetProjectsIdToUseInMission(){
		GameObject detailProjList = selectedMissionDetail.transform.GetChild (2).GetChild (0).gameObject;
		for (int i = 1; i < detailProjList.transform.childCount; i++) {
			string projId = detailProjList.transform.GetChild (i).gameObject.name;
			projectsIdToUseInMission.Add (projId);
			Debug.Log ("Project id to use in mission: " + projId);
		}
	}

	IEnumerator ShowMissionDetail(){
		for (int i = 0; i < missionDetailObj.transform.childCount; i++) {
			missionDetailObj.transform.GetChild (i).gameObject.SetActive (false);
		}
		Transform existedDetail = missionDetailObj.transform.FindChild (selectedMission.Get<string> ("missionName"));
		if (existedDetail != null) {
			existedDetail.gameObject.SetActive (true);
			selectedMissionDetail = existedDetail.gameObject;
			yield break;
		}
		GameObject detail = Instantiate (missionDetailObj.transform.GetChild (0).gameObject);
		detail.SetActive (true);
		detail.transform.SetParent (missionDetailObj.transform);
		detail.transform.localScale = Vector3.one;
		detail.GetComponent<RectTransform>().localPosition = Vector3.zero;
		detail.name = selectedMission.Get<string> ("missionName");
		selectedMissionDetail = detail;

		detail.transform.GetChild (0).GetChild (0).gameObject.GetComponent<Text> ().text = detail.name;
		detail.transform.GetChild (3).GetChild (0).gameObject.GetComponent<Text> ().text = selectedMission.Get<DateTime>("dueDate").ToString("dddd, d MMM yyyy, hh:mm tt");
		if (selectedMission.Get<DateTime> ("dueDate").CompareTo (DateTime.Now) < 0) {
			detail.transform.GetChild (3).GetChild (0).gameObject.GetComponent<Text> ().color = Color.red;
			missionDetailObj.transform.parent.GetChild (2).gameObject.SetActive (false);
		} else {
			missionDetailObj.transform.parent.GetChild (2).gameObject.SetActive (true);
		}
		bool queryDone = false;
		ParseObject course = new ParseObject ("Course");
		course = (ParseObject)selectedMission ["assignedTo"];
		string courseName = "";
		course.FetchAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				courseName = course.Get<string>("courseTitle");
			}else{
				Debug.Log("Failed to get course data!");
			}
			queryDone = true;
		});

		yield return new WaitUntil (()=>queryDone == true);
		queryDone = false;

		detail.transform.GetChild (1).GetChild (0).gameObject.GetComponent<Text> ().text = courseName;

		var projRelation = selectedMission.GetRelation <ParseObject>("usingProjects");

		var projQuery = projRelation.Query;

		List<ParseObject> projectsList = new List<ParseObject> ();

		projQuery.FindAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				IEnumerable<ParseObject> projects = t.Result;
				foreach(ParseObject proj in projects){
					projectsList.Add(proj);
				}
			}else{
				Debug.Log("Failed to get projs!");
			}
			queryDone = true;
		});

		yield return new WaitUntil (()=>queryDone == true);

		GameObject projListObj = detail.transform.GetChild (2).GetChild(0).gameObject;
		GameObject oriRow = projListObj.transform.GetChild (0).gameObject;
		oriRow.SetActive (true);
		foreach (ParseObject proj in projectsList) {
			GameObject row = Instantiate (oriRow);
			row.transform.SetParent (projListObj.transform);
			row.transform.localScale = Vector3.one;
			row.GetComponent<Text> ().text = "- " + proj.Get<string>("projectTitle");
			row.name = proj.ObjectId;
		}
		oriRow.SetActive (false);
		yield return null;
	}

	IEnumerator UpdateMissionAndExpiredList(){

		allMissions.Clear ();
		availMissions.Clear ();
		expiredMissions.Clear ();
		
		ParseQuery<ParseObject> myMissionsQuery = new ParseQuery<ParseObject> ("Mission");

		ParseUser myUserObjPointer = new ParseUser ();

		ParseObject myCourseObjPointer = new ParseObject ("Course");

		if (LocalData.Instance.UserIdentity == ParseUserIdentity.Teacher) {
			myUserObjPointer.ObjectId = LocalData.Instance.Data ["user"] ["objectId"];
			myMissionsQuery = myMissionsQuery.WhereEqualTo ("assignedBy", myUserObjPointer);
		} else if(LocalData.Instance.UserIdentity == ParseUserIdentity.Student) {
			string courseObjId = LocalData.Instance.Data ["user"] ["studentOfCourse"] ["objectId"];
			myCourseObjPointer = ParseObject.CreateWithoutData ("Course", courseObjId);
			myMissionsQuery = myMissionsQuery.WhereEqualTo ("assignedTo", myCourseObjPointer);
		}

		bool queryDone = false;

		myMissionsQuery.OrderByDescending("createdAt").FindAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				IEnumerable<ParseObject> missions = t.Result;
				foreach(var mission in missions){
					allMissions.Add(mission);
				}
			}else{
				Debug.Log("Failed to find missions! Error: " + t.Exception.Message);
			}
			queryDone = true;
		});

		yield return new WaitUntil (() => queryDone == true);

		foreach (var mission in allMissions) {
			Debug.Log ("Found mission: " + mission ["missionName"]);
			DateTime missionDue = mission.Get<DateTime> ("dueDate");
			if (DateTime.Compare (missionDue, DateTime.Now) > 0) {
				availMissions.Add (mission);
			} else {
				expiredMissions.Add (mission);
			}
		}

		UpdateAvailMissionList ();
		UpdateExpiredMissionList ();
	}

	private void UpdateAvailMissionList(){
		for (int i = 1; i < missionList.transform.childCount; i++) {
			Destroy (missionList.transform.GetChild (i).gameObject);
		}
		GameObject oriRow = missionList.transform.GetChild (0).gameObject;
		oriRow.SetActive (true);

		foreach (var availMission in availMissions) {
			GameObject row = Instantiate (oriRow);
			row.transform.SetParent (missionList.transform);
			row.transform.localScale = Vector3.one;
			row.GetComponent<Text> ().text = "- " + availMission.Get<string>("missionName");
		}
		if (missionList.transform.childCount > 1) {
			oriRow.transform.SetParent (null);
			Destroy (oriRow);
		} else {
			oriRow.SetActive (false);
		}
	}

	private void UpdateExpiredMissionList(){
		for (int i = 1; i < expiredList.transform.childCount; i++) {
			Destroy (expiredList.transform.GetChild (i).gameObject);
		}
		GameObject oriRow = expiredList.transform.GetChild (0).gameObject;
		oriRow.SetActive (true);

		foreach (var expiredMission in expiredMissions) {
			GameObject row = Instantiate (oriRow);
			row.transform.SetParent (expiredList.transform);
			row.transform.localScale = Vector3.one;
			row.GetComponent<Text> ().text = "- " + expiredMission.Get<string>("missionName");
		}
		if (expiredList.transform.childCount > 1) {
			oriRow.transform.SetParent (null);
			Destroy (oriRow);
			UpdateExpiredListDisplay ();
		} else {
			oriRow.SetActive (false);
		}
	}

	private void UpdateExpiredListDisplay(){
		for (int i = 0; i < expiredList.transform.childCount; i++) {
			if (i >= displayExpiredSP && i <= displayExpiredEP) {
				expiredList.transform.GetChild (i).gameObject.SetActive (true);
			} else {
				expiredList.transform.GetChild (i).gameObject.SetActive (false);
			}
		}
	}

	public void EventSetExpiredDisplay(int step){
		if (displayExpiredSP <= 0 && step < 0) {
			return;
		}
		if (displayExpiredEP >= expiredList.transform.childCount - 1 && step > 0) {
			return;
		}
		displayExpiredSP += step;
		displayExpiredEP += step;
		UpdateExpiredListDisplay ();
	}

	public void UploadMission(){
		StartCoroutine (UploadMissionRoutine ());
	}

	IEnumerator UploadMissionRoutine(){
		bool done = false;
		ParseObject mission = new ParseObject ("Mission");
		mission ["missionName"] = confirmName.GetComponent<Text> ().text;
		mission ["dueDate"] = Convert.ToDateTime(confirmDue.GetComponent<Text> ().text);

		ParseUser teacherUserPointer = new ParseUser ();
		teacherUserPointer.ObjectId = LocalData.Instance.Data ["user"] ["objectId"];
		mission ["assignedBy"] = teacherUserPointer;

		mission ["assignedTo"] = ParseObject.CreateWithoutData ("Course", selectedCourseId);

		var projRelation = mission.GetRelation<ParseObject> ("usingProjects");
		foreach (string projId in selectedProjectObjId) {
			projRelation.Add(ParseObject.CreateWithoutData("Project",projId));
		}

		bool successed = false;

		mission.SaveAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				successed = true;
			}else{
				successed = false;
				Debug.Log("Failed to save mission! Error: " + t.Exception.Message);
			}
			done = true;
		});

		yield return new WaitUntil (() => done == true);
		done = false;
		string message = successed ? "Upload Finished!" : "Upload Failed!";
		assignMissionObj [4].transform.FindChild ("message").gameObject.GetComponent<Text> ().text = message;
		assignMissionObj [4].transform.FindChild ("Back").gameObject.SetActive (true);
		if (successed) {
			int missionCount = PlayerPrefs.GetInt ("Game1-Mission-Count");
			missionCount++;
			PlayerPrefs.SetInt ("Game1-Mission-Count", missionCount);
			UpdateCourseList ();
			InitDueDateAndMissionName ();
			StartCoroutine(UpdateMissionAndExpiredList ());
		}
	}

	public void UpdateMissionAssignConfirmPage(){
		confirmName.GetComponent<Text> ().text = missionNameObj.GetComponent<InputField> ().text;
		confirmCourse.GetComponent<Text> ().text = selectedCourseName;
		for (int i = 1; i < confirmProjList.transform.childCount; i++) {
			Destroy (confirmProjList.transform.GetChild (i).gameObject);
		}
		GameObject oriRow = confirmProjList.transform.GetChild (0).gameObject;
		oriRow.SetActive (true);
		foreach (string projName in selectedProjsName) {
			GameObject row = Instantiate(oriRow);
			row.transform.SetParent(confirmProjList.transform);
			row.transform.localScale = Vector3.one;
			row.GetComponent<Text>().text = "- " + projName;
		}
		oriRow.SetActive(false);
		confirmDue.GetComponent<Text> ().text = dueDateObj.GetComponent <Text>().text;

		if (!missionNameObj.GetComponent<InputField> ().text.Equals("") && selectedProjsName.Count > 0 && Convert.ToDateTime (dueDateObj.GetComponent <Text> ().text).CompareTo (DateTime.Now) > 0) {
			confirmBtn.SetActive (true);
		} else {
			confirmBtn.SetActive (false);
		}
//		foreach (string projId in selectedProjectObjId) {
//			Debug.Log ("Selected proj id: " + projId);
//		}
	}

	public void EventButtonDatePicker (Text textfield) {
		_dynDatePickerTextFeild = textfield;

		DatePicker.Instance.ShowDatePicker (Convert.ToDateTime(textfield.text),DateTimeSelected);
	}

	private void DateTimeSelected (DateTime date) {
		if (_dynDatePickerTextFeild != null) {
			_dynDatePickerTextFeild.text = date.ToString("dddd, d MMM yyyy, hh:mm tt");
		} else {
			Debug.LogWarning("No textfield");
		}
	}

	private void InitDueDateAndMissionName(){
		dueDateObj.GetComponent<Text>().text = DateTime.Now.AddDays(7).ToString("dddd, d MMM yyyy, hh:mm tt");
		int missionCount = PlayerPrefs.GetInt ("Game1-Mission-Count",1);
		missionNameObj.GetComponent<InputField>().text = "Mission " + missionCount;
	}

	public void EventSelectProject(GameObject btn){
		string projectId = projectObjId [btn.transform.GetSiblingIndex()];
		if (btn.transform.GetChild (1).gameObject.activeSelf) {
			btn.transform.GetChild (1).gameObject.SetActive (false);
			selectedProjectObjId.Remove(projectId);
			selectedProjsName.Remove (btn.GetComponent<Text> ().text);
		} else {
			btn.transform.GetChild (1).gameObject.SetActive (true);
			selectedProjectObjId.Add(projectId);
			selectedProjsName.Add (btn.GetComponent<Text> ().text);
			Debug.Log (projectId);
		}
		GameObject continueBtn = assignMissionObj [1].transform.FindChild ("Continue").gameObject;
		if (selectedProjectObjId.Count > 0) {
			continueBtn.SetActive (true);
		} else {
			continueBtn.SetActive (false);
		}
	}

	public void UpdateProjectList(){
		displayProjSP = 0;
		displayProjEP = 4;
		selectedProjectObjId.Clear ();
		selectedProjsName.Clear ();
		projectObjId.Clear ();
		GameObject oriRow = projectList.transform.GetChild (0).gameObject;
		oriRow.name = "Project";
		oriRow.transform.GetChild (1).gameObject.SetActive (false);
		oriRow.SetActive (false);
		foreach (var project in LocalData.Instance.Data["my-projects"].Childs) {
			string courseId = (string)project ["course"] ["objectId"];
			if (!courseId.Equals(selectedCourseId,StringComparison.Ordinal)) {
				Debug.Log ("Project isnt in selected course! Proj ID: [" + project ["course"] ["objectId"] + "] Course id: [" + selectedCourseId + "]");
				continue;
			}
			if (project ["projectTitle"].ToString ().Contains ("#SlideShow")) {
				Debug.Log ("Ignore slideshow project: " + project ["objectId"]);
				continue;
			}
			GameObject row = Instantiate(oriRow);
			row.transform.SetParent(projectList.transform);
			row.transform.localScale = Vector3.one;
			row.GetComponent<Text>().text = "" + project ["projectTitle"];
			row.SetActive (true);
			projectObjId.Add (project ["objectId"]);
		}
		if (projectList.transform.childCount > 1) {
			oriRow.transform.SetParent (null);
			Destroy (oriRow);
		} else {
			oriRow.SetActive (false);
		}
		UpdateProjListDisplay ();
	}

	private void UpdateProjListDisplay(){
		if (projectList.transform.childCount == 1) {
			return;
		}
		for (int i = 0; i < projectList.transform.childCount; i++) {
			if (i >= displayProjSP && i <= displayProjEP) {
				projectList.transform.GetChild (i).gameObject.SetActive (true);
			} else {
				projectList.transform.GetChild (i).gameObject.SetActive (false);
			}
		}
	}

	public void EventSetProjDisplay(int step){
		if (displayProjSP <= 0 && step < 0) {
			return;
		}
		if (displayProjEP >= projectList.transform.childCount - 1 && step > 0) {
			return;
		}
		displayProjSP += step;
		displayProjEP += step;
		UpdateProjListDisplay ();
	}

	private void ResetProjList(){
		for (int i = 1; i < projectList.transform.childCount; i++) {
			Destroy (projectList.transform.GetChild (i).gameObject);
		}
	}

	private void UpdateCourseList(){
		selectedCourseId = "";
		selectedCourseName = "";
		for (int i = 1; i < courseList.transform.childCount; i++) {
			Destroy (courseList.transform.GetChild (i).gameObject);
		}
		foreach (var course in LocalData.Instance.Data["my-course"].Childs) {
			GameObject row = Instantiate(courseList.transform.GetChild(0).gameObject);
			row.transform.SetParent(courseList.transform);
			row.transform.localScale = Vector3.one;
			row.GetComponent<Text>().text = "" + course ["courseTitle"];
			coursesObjId.Add (course ["objectId"]);
			row.transform.GetChild (1).gameObject.SetActive (false);
		}
		Destroy(courseList.transform.GetChild(0).gameObject);
		assignMissionObj [0].transform.FindChild ("Continue").gameObject.SetActive (false);
		ResetProjList ();
	}

	public void EventSelectCourse(GameObject btn){
		if (btn.transform.GetChild (1).gameObject.activeSelf) {
			btn.transform.GetChild (1).gameObject.SetActive (false);
			ResetProjList ();
			selectedCourseId = "";
		} else {
			for (int i = 0; i < courseList.transform.childCount; i++) {
				courseList.transform.GetChild (i).GetChild (1).gameObject.SetActive (false);
			}
			btn.transform.GetChild (1).gameObject.SetActive (true);
			string courseId = coursesObjId [btn.transform.GetSiblingIndex()];
			selectedCourseId = courseId;
			Debug.Log (selectedCourseId);
			selectedCourseName = btn.GetComponent<Text> ().text;
		}
		GameObject continueBtn = assignMissionObj [0].transform.FindChild ("Continue").gameObject;
		if (selectedCourseId != "") {
			continueBtn.SetActive (true);
			UpdateProjectList ();
		} else {
			continueBtn.SetActive (false);
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void EventSwitchToAssignMissionObj(int index){
		assignMissionObj[currectAssignMissionIndex].SetActive (false);
		assignMissionObj[index].SetActive (true);
		currectAssignMissionIndex = index;
	}

	public void EventBackToMissionOrExpired(){
		pageObjs[currentPageObjIndex].SetActive (false);
		if (visitedExpired) {
			pageObjs [2].SetActive (true);
			currentPageObjIndex = 2;
		} else {
			pageObjs[1].SetActive (true);
			currentPageObjIndex = 1;
		}
	}

	public void BackToMissions(){
		visitedExpired = false;
	}

	public void EventSwitchToPage(int index){
		if (index == 2) {
			visitedExpired = true;
		}
		pageObjs[currentPageObjIndex].SetActive (false);
		pageObjs[index].SetActive (true);
		currentPageObjIndex = index;
	}

	public void EventLaunchMission(){
		missionMode = true;
		Fader.Instance.FadeIn (0.5f)
			.LoadLevel ("Game1")
			.FadeOut (0.5f);
	}

	public void EventPlayGame(){
		missionMode = false;
		Fader.Instance.FadeIn (0.5f)
			.LoadLevel ("Game1")
			.FadeOut (0.5f);
	}

	public void EventExitGame(){
		Destroy (gameObject);
		Fader.Instance.FadeIn (0.5f)
			.LoadLevel ("Home_Teacher")
			.FadeOut (0.5f);
	}

	public void EventRestart(){
		Fader.Instance.FadeIn (0.5f)
			.LoadLevel ("Game1_entry")
			.FadeOut (0.5f);
	}
}

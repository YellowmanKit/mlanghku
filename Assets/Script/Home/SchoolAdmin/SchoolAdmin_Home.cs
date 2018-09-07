using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Parse;
using SimpleJSON;

public class SchoolAdmin_Home : Page {
	
	private string					_dynSelectedSchoolId = "";
	
	public List<GameObject>			_btnCourses = null;
	public List<Transform>			_sortImageCourses = null;
	private CourseSortBy			_dynCourseSortBy = CourseSortBy.Name;
	private SortingOrder			_dynCourseOrder = SortingOrder.Desc;
	
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
		if (data != null) {
			if (data.ContainsKey("schoolId")) {
				_dynSelectedSchoolId = (string)(data ["schoolId"]);
				
				foreach (GameObject btn in _btnCourses) {
					btn.SetActive (false);
				}
				
				state = PageState.INIT;
			} else {
				Debug.LogWarning("SchoolAdmin_School: SocketBecomeVisible data no key");
			}
		}
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
		Debug.Log (CacheManager.Instance.CacheNode.ToString());
		foreach (GameObject btn in _btnCourses) {
			btn.SetActive (false);
		}
		
		// students
		JSONNode coursesNode = CacheManager.Instance.CacheNode["courses"];
		if (coursesNode != null) {
			// build an array
			Course[] courses = new Course[coursesNode.Count];
			for (int i = 0; i < coursesNode.Count; i++) {
				Course course = new Course(coursesNode[i]);
				
				courses[i] = course;
			}
			
			Course.Sort(courses, _dynCourseSortBy, _dynCourseOrder);
			
			// show the students
			GameManager.InstantiateChildren (courses.Length, _btnCourses);
			/*
			int missing = courses.Length - _btnCourses.Count;
			for (int i = 0; i < missing; i++) {
				GameObject copy = Instantiate (_btnCourses [0]);
				copy.transform.SetParent (_btnCourses [0].transform.parent);
				copy.transform.localScale = Vector3.one;
				
				_btnCourses.Add (copy);
			}
			*/

			// update table view
			for (int i = 0; i < courses.Length; i++) {
				Course course = courses[i];
				// assign the text
				GameObject btn = _btnCourses [i];
				btn.name = course.id;
				btn.transform.FindChild ("txt_courseName").GetComponent<Text> ().text = course.courseTitle;
				btn.transform.FindChild ("txt_teacher").GetComponent<Text> ().text = course.teacherName;
				btn.gameObject.SetActive (true);
			}
		}
	}
	#endregion
	
	#region Event
	public void EventButtonCoursesSortBy (GameObject btn) {
		CourseSortBy choice = CourseSortBy.Name;
		if (btn.name.Contains ("name")) {
			choice = CourseSortBy.Name;
		} else if (btn.name.Contains ("students")) {
			choice = CourseSortBy.NumOfStudent;
		} else {
			Debug.LogError("Sortby unknown " + btn.name);
			return;
		}
		
		if (choice == _dynCourseSortBy) {
			if (_dynCourseOrder == SortingOrder.Asc) {
				SetCourseSorting(choice, SortingOrder.Desc);
			} else {
				SetCourseSorting(choice, SortingOrder.Asc);
			}
		} else {
			SetCourseSorting(choice, SortingOrder.Asc);
		}
		
		DataLoaded ();
	}
	
	private void SetCourseSorting (CourseSortBy key, SortingOrder order) {
		_dynCourseSortBy = key;
		_dynCourseOrder = order;
		
		_sortImageCourses[0].gameObject.SetActive(false);
		
		Vector3 scale = Vector3.one;
		if (order == SortingOrder.Asc) {
			scale.y = -1;
		}
		
		switch(key) {
		case CourseSortBy.Name:
			_sortImageCourses[0].gameObject.SetActive(true);
			_sortImageCourses[0].transform.localScale = scale;
			break;
		}
	}
	
	public void EventButtonSelectCourse (GameObject btn) {
		string courseId = btn.name;
		
		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["courseId"] = courseId;
		data ["schoolId"] = _dynSelectedSchoolId;
		
		NavigationBar.Instance.PushView (NavigationView.Teacher_View_Course, data);
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

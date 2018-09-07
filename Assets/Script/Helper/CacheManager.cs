using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Parse;
using SimpleJSON;

public enum ParseUserIdentity {
	Unknown = -1,
	Student = 1,
	Teacher = 2,
	SchoolAdmin = 3,
	SystemAdmin = 4
}

public enum SortingOrder {
	Asc,
	Desc
}

public class CacheManager : MonoBehaviour {
	
	const int CACHE_SECOND = 120;
	
	private  static CacheManager _instance = null;
	internal static CacheManager Instance { get { return _instance; } }
	
	private  JSONNode			 _dynCacheNode = null;
	internal JSONNode		     CacheNode { get { return _dynCacheNode; } }

	void Awake ()
	{
		Debug.Log ("[CacheManager] Awake()");
		if (_instance == null) {
			DontDestroyOnLoad (gameObject);
			_instance = this;
			//Init();
		} else if (_instance != this) {
			Destroy (gameObject);
		}
	}
	
	void Init() {
		Debug.Log ("[CacheManager] Init()");
		_dynCacheNode = JSON.Parse ("{}");
	}

	public void Init (JSONNode node) {
		Debug.Log ("[CacheManager] Init(node)");
		_dynCacheNode = node;
	}
	
	public void Reset () {
		Debug.Log ("[CacheManager] Reset()");
		Init ();
	}
	
	#region Teacher
	public bool IsTeacherCoursesListOutdated () {
		return CheckDataOutDated("course-list-cachedAt");
	}

	
	public void HandleSchoolList (JSONNode node) {
		#if UNITY_EDITOR
		//Debug.Log ("HandleSchoolList " + node.ToString());
		#endif
		if (node == null) return;

		_dynCacheNode ["schools"] = node;
		_dynCacheNode ["school-list-cachedAt"] = GetCurrentTimestemp ().ToString();   // v1.3 added   - is it a bug?
		//_dynCacheNode ["course-list-cachedAt"] = GetCurrentTimestemp ().ToString(); // v1.3 removed - is it a bug?	
	}

	public void HandleSystemAdminSpecificSchoolData (string schoolId, JSONNode node) {
		#if UNITY_EDITOR
		//Debug.Log ("HandleSystemAdminSpecificSchoolData: " + node.ToString());
		#endif
		if (node == null) return;
		
		//////////////////////////////////////////////////////
		// project handling
		// save to LocalData Permanently!
		// this is one project, so can't use "HandleStudentMyProjectsData"
//		JSONNode temp = JSONNode.Parse("{}");
		JSONNode courses = node["courses"];
		JSONNode teachers = node["teachers"];

		foreach (JSONNode course in courses.Childs) {
			if (course.GetKeys().Contains("courseTeacher")) {
				JSONNode t = course["courseTeacher"];
				string teacherId = t["objectId"].Value;

				foreach (JSONNode teacher in teachers.Childs) {
					if (teacher["objectId"].Value.Equals(teacherId)) {
						course["courseTeacher"] = teacher;	
					}
				}
			}
		}

		_dynCacheNode["schools"][schoolId]["courses"] = courses;
		_dynCacheNode["schools"][schoolId]["teachers"] = teachers;
		_dynCacheNode ["school-" + schoolId + "-cachedAt"] = GetCurrentTimestemp ().ToString();
	}
	
	public bool IsSystemAdminSpecificSchoolDataOutdated (string schoolId) {
		return CheckDataOutDated("school-" + schoolId + "-cachedAt");
	}
	
	public void HandleSchoolAdminRenewUserData (JSONNode node) {
		#if UNITY_EDITOR
		//Debug.Log ("HandleSchoolAdminRenewUserData: " + node.ToString());
		#endif
		if (node == null)
			return;
		
		//////////////////////////////////////////////////////
		// project handling
		// save to LocalData Permanently!
		// this is one project, so can't use "HandleStudentMyProjectsData"
//		JSONNode temp = JSONNode.Parse("{}");
		JSONNode courses = node["courses"];
		JSONNode teachers = node["teachers"];
		
		foreach (JSONNode course in courses.Childs) {
			if (course.GetKeys().Contains("courseTeacher")) {
				JSONNode t = course["courseTeacher"];
				string teacherId = t["objectId"].Value;
				
				foreach (JSONNode teacher in teachers.Childs) {
					if (teacher["objectId"].Value.Equals(teacherId)) {
						course["courseTeacher"] = teacher;	
					}
				}
			}
		}
		
		_dynCacheNode["courses"] = courses;
		_dynCacheNode["teachers"] = teachers;
	}

	public void HandleTeacherCoursesList (JSONNode node) {
		#if UNITY_EDITOR
		//Debug.Log ("HandleCoursesData " + node.ToString());
		#endif
		if (node == null) return;

		// remove the course that not existed any more
		List<string> courseToBeRemove = new List<string>();
		if (LocalData.Instance.Data["my-course"] != null) {
			foreach (JSONNode courseNode in LocalData.Instance.Data["my-course"].Childs) {
				string courseId = courseNode ["objectId"].Value;
				
				if (!node.GetKeys().Contains(courseId)) {
					// not existed anymore
					Debug.Log (courseId + " should be remove");
					string imagePath = Path.Combine(LocalData.Instance.DirectoryPath, "course-" + courseId + ".jpg");
					SafelyDelete(imagePath);
					courseToBeRemove.Add (courseId);
				}
			}
		}
		foreach (string courseId in courseToBeRemove) {
			LocalData.Instance.Data["my-course"].Remove(courseId);
		}
		
		foreach (JSONNode courseNode in node.Childs) {
			// update those existed
			string courseId = courseNode ["objectId"].Value;
			
			// need to download image?
			LocalData.Instance.CheckImageOutdated("course-" + courseId + ".jpg", courseNode["courseIcon"]["url"].Value);
		}
		
		LocalData.Instance.UpdateTeacherCourse(node);
		_dynCacheNode ["course-list-cachedAt"] = GetCurrentTimestemp ().ToString();

	}
	
	public bool IsTeacherSpecificCourseDataOutdated (string courseId) {
		return CheckDataOutDated("course-" + courseId + "-cachedAt");
	}
	
	// handle the course's data, eg: student list, project list of a course
	public void HandleTeacherSpecificCourseData (string courseId, JSONNode node) {
		#if UNITY_EDITOR
		//Debug.Log ("HandleTeacherSpecificCourseData: " + node.ToString());
		#endif
		if (node == null)
			return;

		//////////////////////////////////////////////////////
		// project handling
		// save to LocalData Permanently!
		// this is one project, so can't use "HandleStudentMyProjectsData"
		JSONNode temp = JSONNode.Parse("{}");
		JSONNode projectNodes = node["projects"];
		foreach (JSONNode projectNode in projectNodes.Childs) {
			string projectId = projectNode ["objectId"].Value;
			temp[projectId] = projectNode;
		}
		projectNodes = temp;
		// remove the project that not existed any more
		List<string> projectIdToBeRemove = new List<string>();
		if (LocalData.Instance.Data["my-projects"] != null) {
			foreach (JSONNode projectNode in LocalData.Instance.Data["my-projects"].Childs) {
				//Debug.Log ("Remove project " + projectNode.ToString());
				Project project = new Project(projectNode);
				if (!project.courseId.Equals(courseId))
					continue;

				if (!projectNodes.GetKeys().Contains(project.id)) {
					// not existed anymore
					SafelyDelete(project.ImagePath);
					projectIdToBeRemove.Add (project.id);
				}
			}
		}
		foreach (string projectId in projectIdToBeRemove) {
			LocalData.Instance.Data["my-projects"].Remove(projectId);
		}

		foreach (JSONNode projectNode in projectNodes.Childs) {
			// update those existed
			projectNode.Remove("createdAt");
			projectNode.Remove("updatedAt");
			projectNode.Remove("createdBy");
			HandleStudentSpecificMyProjectData(projectNode);
		}
		LocalData.Instance.Save ();

		//////////////////////////////////////////////////////
		// student handling
		// this is not permanaent, store in cache only
		TeacherHandleStudentList(courseId, node["students"]);

		_dynCacheNode ["course-" + courseId + "-cachedAt"] = GetCurrentTimestemp ().ToString();
	}

	public void TeacherHandleStudentList (string courseId, JSONNode node) {
		#if UNITY_EDITOR
		//Debug.Log ("TeacherHandleStudentList: " + node.ToString());
		#endif
		if (node == null)
			return;

		JSONNode temp = JSON.Parse("{}");
		foreach (JSONNode student in node.Childs) {
			string objectId = student["objectId"];
			temp[objectId] = student;
		}
		node = temp;
		
		// todo
		_dynCacheNode ["course"] [courseId]["students"] = node;
		_dynCacheNode ["course-" + courseId + "-cachedAt"] = GetCurrentTimestemp ().ToString();

		//Debug.Log ("TeacherHandleStudentList completed");
	}
	
	public bool IsTeacherSpecificProjectStudentProjectListOutdated (string projectId) {
		return CheckDataOutDated("project-" + projectId + "-submitted-list-cachedAt");
	}
	
	public void HandleTeacherSpecificProject_StudentProjectList (string projectId, JSONNode node) {
		#if UNITY_EDITOR
		//Debug.Log ("HandleTeacherSpecificProject_StudentProjectList: " + node.ToString());
		#endif
		if (node == null)
			return;
		
		_dynCacheNode["project-" + projectId + "-submitted-list-cachedAt"] = GetCurrentTimestemp ().ToString();
		_dynCacheNode ["project-" + projectId + "-submitted-list"] = node;
	}
	public void HandleTeacherSpecificStudent_StudentProject (string studentId, JSONNode node) {
		#if UNITY_EDITOR
		//Debug.Log ("HandleTeacherSpecificStudent_StudentProject: " + node.ToString());
		#endif
		if (node == null)
			return;

		foreach (JSONNode studentProjectNode in node.Childs) {
			string projectId = studentProjectNode["project"]["objectId"];
			_dynCacheNode ["project-" + projectId + "-submitted-list"][studentId] = studentProjectNode;
		}
	}

	#endregion
	
	#region Student
	public bool IsStudentMyProjectsOutdated () {
		return CheckDataOutDated("my-projects-cachedAt");
	}
	
	public void HandleAllMyProjectsData (JSONNode node) {
		#if UNITY_EDITOR
		//Debug.Log ("HandleAllMyProjectsData: " + node.ToString());
		#endif
		if (node == null)
			return;

		// conversion
		JSONNode temp = JSONNode.Parse("{}");
		foreach (JSONNode projectNode in node.Childs) {
			string projectId = projectNode ["objectId"].Value;
			temp[projectId] = projectNode;
		}
		node = temp;
		
		// remove the course that not existed any more
		List<string> toBeRemoved = new List<string>();
		if (LocalData.Instance.Data["my-projects"] != null) {
			foreach (JSONNode projectNode in LocalData.Instance.Data["my-projects"].Childs) {
				Project project = new Project(projectNode);
				if (!node.GetKeys().Contains(project.id)) {
					// not existed anymore
					SafelyDelete(project.ImagePath);
					toBeRemoved.Add (project.id);
				}
			}
		}
		foreach (string projectId in toBeRemoved) {
			LocalData.Instance.Data["my-projects"].Remove(projectId);
		}
		
		foreach (JSONNode projectNode in node.Childs) {
			// update those existed
			projectNode.Remove("createdAt");
			projectNode.Remove("updatedAt");
			projectNode.Remove("createdBy");
			HandleStudentSpecificMyProjectData(projectNode);
		}
		
		_dynCacheNode ["my-projects-cachedAt"] = GetCurrentTimestemp ().ToString();
		
		// Title, id are permanent data, should be able to view even offline
		// store them in localdata
		LocalData.Instance.Save ();
	}
	
	public void HandleStudentSpecificMyProjectData (JSONNode node) {
		//Debug.Log ("HandleStudentSpecificMyProjectData: " + node.ToString());
		if (node == null)
			return;
		
		Project project = new Project(node);
		// need to download image?
		LocalData.Instance.CheckImageOutdated(project.ImageName, project.imageUrl);
		LocalData.Instance.Data["my-projects"][project.id] = node;

		//_dynCacheNode ["my-projects-" + project.id + "-cachedAt"] = GetCurrentTimestemp ().ToString();
		//_dynCacheNode ["my-projects"] [project.id] = node;
	}
	
	
	public bool IsStudentMyProjectsSubmittedOutdated () {
		return CheckDataOutDated("my-projects-submitted-cachedAt");
	}
	
	public void HandleStudentMyProjectsSubmittedata (JSONNode node) {
		#if UNITY_EDITOR
		//Debug.Log ("HandleStudentMyProjectsSubmittedata: " + node.ToString());
		#endif
		if (node == null)
			return;
		
		// conversion
		JSONNode temp = JSONNode.Parse("{}");
		foreach (JSONNode studentProject in node.Childs) {
			string projectId = studentProject["project"]["objectId"].Value;
			temp[projectId] = studentProject;
		}
		node = temp;
		
		// remove the studentProjectSubmitted that not existed any more
		List<string> toBeRemoved = new List<string>();
		foreach (JSONNode studentProject in _dynCacheNode["my-projects-submitted"].Childs) {	// existing cache
			string projectId = studentProject["project"]["objectId"].Value;
			if (!node.GetKeys().Contains(projectId)) {	// new data contain this?
				// not existed anymore
				toBeRemoved.Add (projectId);
			}
		}
		foreach (string projectId in toBeRemoved) {			
			_dynCacheNode["my-projects-submitted"].Remove(projectId);
		}
		
		foreach (JSONNode studentProjectNode in node.Childs) {
			// update those existed
			HandleStudentSpecificMyProjectSubmittedData(studentProjectNode);
		}
		
		_dynCacheNode ["my-projects-submitted-cachedAt"] = GetCurrentTimestemp ().ToString();

	}
	
	public void HandleStudentSpecificMyProjectSubmittedData (JSONNode node) {
		#if UNITY_EDITOR
		//Debug.Log ("HandleStudentSpecificMyProjectSubmittedData: " + node.ToString());
		#endif
		if (node == null)
			return;
		try {
			string projectId = node ["project"]["objectId"].Value;

			if (string.IsNullOrEmpty(projectId)) {
				// project id not provided, only got the studentProjectId
				string studentProjectId = node ["objectId"].Value;
				foreach (JSONNode studentProejct in _dynCacheNode ["my-projects-submitted"].Childs) {
					string id = studentProejct ["objectId"].Value;
					if (id.Equals(studentProjectId)) {
						// overwrite the node.value to this node
						foreach (string key in node.GetKeys()) {
							studentProejct[key] = node[key].Value;
						}
						break;
					}
				}
			} else {
				_dynCacheNode ["my-projects-submitted-" + projectId + "-cachedAt"] = GetCurrentTimestemp ().ToString();
				_dynCacheNode ["my-projects-submitted"] [projectId] = node;
			}
		} catch (Exception e) {
			Debug.LogError(e.Message);
		}

	}
	
	public bool IsStudentMyCourseOutdated () {
		return CheckDataOutDated("my-course-cachedAt");
	}
	
	public void HandleStudentMyCourseData (JSONNode node) {
		#if UNITY_EDITOR
		//Debug.Log ("HandleStudentMyCourseData: " + node.ToString());
		#endif
		if (node == null)
			return;
		
		_dynCacheNode["my-course-cachedAt"] = GetCurrentTimestemp ().ToString();
		LocalData.Instance.UpdateStudentMyCourse(node);

	}
	
	public bool IsStudentMySpecificProjectCardsOutdated (string projectId) {
		return CheckDataOutDated("my-projectcards-" + projectId + "-cachedAt");
	}
	
	public void HandleStudentMySpecificProjectCards (string projectId, JSONNode cardsNode, JSONNode langsNode) {
		try {
			#if UNITY_EDITOR
			//Debug.Log ("HandleStudentMySpecificProjectCards: " + cardsNode.ToString());
			//Debug.Log ("HandleStudentMySpecificProjectCards: " + langsNode.ToString());
			#endif
			if (cardsNode == null)
				return;
			if (langsNode == null) {
				Debug.LogError("LangsNode is null");
			}

			// combine langs node to cards node
			CombineCardsAndLangs(cardsNode, langsNode);

			//Debug.Log ("After : " + cardsNode.ToString());

			// compare cards with local, any of them are more updated?
			// download them
			//DownloadTask dlTask = new DownloadTask ();
			int change = 0;
			//Debug.Log ("0");
			//Debug.Log ("cardsNode child: " + cardsNode.Count);
			foreach (JSONNode onlineCardNode in cardsNode.Childs) {
				//Debug.Log ("1");
				Card onlineCard = new Card(onlineCardNode);
				//Debug.Log ("2");
				
				// localCard
				bool needToDownLoad = false;
				bool dataChange = false;
				JSONNode cardNode = LocalData.Instance.Data["local-cards"][projectId][onlineCard.cardId];
				Card localCard = new Card(cardNode);
				if (cardNode == null) {
					//Debug.Log ("not exist - " + onlineCard.GetDefaultDisplayName());
					// local don't have this card
					needToDownLoad = true;
				} else {
					//Debug.Log ("exist - " + onlineCard.GetDefaultDisplayName());
					
					// large change
					if (onlineCard.lastUpdatedAt > localCard.lastUpdatedAt) {
						// online one is more updated
						needToDownLoad = true;
						//Debug.Log (localCard.GetDefaultDisplayName() + " : online update");
					} else if (onlineCard.lastUpdatedAt == localCard.lastUpdatedAt) {
						//Debug.Log (localCard.GetDefaultDisplayName() + " : ==");
						// individual check.
						// most of the data should be the same as "lastUpdatedAt" are equal
						// obtained the objectid, url
						if (string.IsNullOrEmpty(localCard.objectId)) {
							cardNode["objectId"] = onlineCardNode["objectId"];
							dataChange = true;
						}
						if (localCard.status != onlineCard.status) {
							cardNode["status"].AsInt = onlineCardNode["status"].AsInt;
							dataChange = true;
						}
					} else if (onlineCard.lastUpdatedAt < localCard.lastUpdatedAt) {
						Debug.Log (localCard.GetDefaultDisplayName() + " : < " + onlineCard.lastUpdatedAt + " " + localCard.lastUpdatedAt);
						// local card is more update
						// if the online card is graded, overwrite to local to prevent extra work to teacher
						if (onlineCard.status == CardStatus.Passed || onlineCard.status == CardStatus.Featured) {
							Debug.LogWarning("Normal: Although local card is more updated, online overwrite local. Reason: online card is passed / featured\n"
							                 + onlineCard.toJSON().ToString());
							needToDownLoad = true;
						}
					}
					if (dataChange)
						change++;
				}

				if (needToDownLoad) {
					if (!onlineCard.isDeleted) {
						Debug.Log ("Need to download " + onlineCard.GetDefaultDisplayName());
						change++;
						onlineCardNode.Remove("createdAt");
						onlineCardNode.Remove("updatedAt");
						LocalData.Instance.Data["local-cards"][projectId][onlineCard.cardId] = onlineCardNode;

						// check url has changed or not
						string imageUrl = onlineCardNode ["image"] ["url"].Value;
						LocalData.Instance.CheckImageOutdated(onlineCard.ImageName, imageUrl);

						// check sound outdated for each language
						//string soundUrl = onlineCardNode ["sound"] ["url"].Value;
						/*
							foreach (CardLang lang in onlineCard.langs.Values) {
								bool soundOutdated = LocalData.Instance.CheckSoundOutdated(lang.SoundName, lang.soundUrl);
								if (soundOutdated && !string.IsNullOrEmpty(lang.soundUrl)) {
									DownloadManager.Instance.AddLinkage(lang.SoundPath, lang.soundUrl);
								}
							}
							*/
						HandleCardLangSoundOutdated(localCard, onlineCard);
					} else {
						Debug.Log("CacheManager remove card from server: "+LocalData.Instance.Data["local-cards"][projectId][onlineCard.cardId].ToString());

						LocalData.Instance.Data["local-cards"][projectId].Remove(onlineCard.cardId);

						string photoPath = Path.Combine (LocalData.Instance.DirectoryPath, onlineCard.cardId + ".jpg");
						string soundPath = Path.Combine (LocalData.Instance.DirectoryPath, onlineCard.cardId + ".wav");
						SafelyDelete(photoPath);
						SafelyDelete(soundPath);
					}

				} else {
					// is the sound file really here?
					/*
						foreach (CardLang lang in onlineCard.langs.Values) {
							LocalData.Instance.CheckSoundOutdated(lang.SoundName, lang.soundUrl);
							if (!lang.isSoundExist && !string.IsNullOrEmpty(lang.soundUrl)) {
								Debug.Log (" sound Need to download " + lang.langKey);
								DownloadManager.Instance.AddLinkage(lang.SoundPath, lang.soundUrl);
							}
						}
						*/
					HandleCardLangSoundOutdated(localCard, onlineCard);
				}

				if (onlineCard.isDeleted) {
					Debug.Log ("onlineCard deleted " + onlineCard.GetDefaultDisplayName());
					// deleted card, remove asset
					onlineCard.SafelyDeleteFiles();
				}
			}

			if (change > 0) {
				//Debug.Log ("LocalData updated");
				LocalData.Instance.Save();
			}
			
			// any local card are more updated?
			// upload them
			_dynCacheNode["my-projectcards-" + projectId + "-cachedAt"] = GetCurrentTimestemp ().ToString();
			_dynCacheNode["my-projectcards-" + projectId] = cardsNode;
		} catch (Exception e) {
			Debug.LogError(e.Message);
		}
	}

	private void HandleCardLangSoundOutdated (Card localCard, Card onlineCard) {
		foreach (CardLang onlineLang in onlineCard.langs.Values) {
			bool foundMatching = false;

			if (localCard != null) {
				foreach (CardLang localLang in localCard.langs.Values) {
					if (onlineLang.langKey == localLang.langKey) {
						// match
						foundMatching = true;

						if (string.IsNullOrEmpty(onlineLang.soundUrl)) {
							// online no url provided
							break;
						}

						if (onlineLang.soundLastUpdatedAt > localLang.soundLastUpdatedAt) {
							// online sound more updated
							SafelyDelete(onlineLang.SoundPath);
							DownloadManager.Instance.AddLinkage(onlineLang.SoundPath, onlineLang.soundUrl);
						}
						/*
						bool soundOutdated = LocalData.Instance.CheckSoundOutdated(onlineLang.SoundName, onlineLang.soundUrl);
						if (soundOutdated && !string.IsNullOrEmpty(onlineLang.soundUrl)) {
							DownloadManager.Instance.AddLinkage(onlineLang.SoundPath, onlineLang.soundUrl);
						}
						*/
					} else {
						continue;
					}
				}
			}

			// if local don't have this lang saved, download it
			if (!foundMatching && !string.IsNullOrEmpty(onlineLang.soundUrl)) {
				DownloadManager.Instance.AddLinkage(onlineLang.SoundPath, onlineLang.soundUrl);
			}
		}
	}

	#endregion
	
	#region Common
	private void RedirectToLogin() {
		
		ParseUser.LogOutAsync();
		Fader.Instance.FadeIn (0.5f)
			.LoadLevel ("Login")
				.FadeOut (0.5f);
	}

	public bool isUserDateOutDated () {
		return CheckDataOutDated("user-cachedAt");
	}
	
	public void HandleRenewUser (JSONNode node)
	{
		HandleUserNode(node["user"]);

		if (node["user"]["banned"].AsInt == 1) {
			Debug.Log ("Banned");
			AlertTask logout = new AlertTask(
				GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
				, GameManager.Instance.Language.getString("account-inactive", CaseHandle.FirstCharUpper)
				, TextAnchor.MiddleLeft);
			logout.buttonCompletedEvent_0 += RedirectToLogin;
			
			GameManager.Instance.AlertTasks.Add (logout);
		}
		
		// teacher
		if (node.GetKeys().Contains("courses"))
			HandleTeacherCoursesList(node["courses"]);

		if (node.GetKeys().Contains("my-course"))
			HandleStudentMyCourseData(node["my-course"]);

		// students
		if (node.GetKeys().Contains("my-projects"))
			HandleAllMyProjectsData(node["my-projects"]);
		if (node.GetKeys().Contains("my-projects-submitted"))
			HandleStudentMyProjectsSubmittedata(node["my-projects-submitted"]);

		// schools
		if (node.GetKeys().Contains("schools"))
			HandleSchoolList(node["schools"]);
		
		if (node.GetKeys().Contains("school") && node.GetKeys().Contains("courses") && LocalData.Instance.UserIdentity == ParseUserIdentity.SchoolAdmin) {
			// special case
			HandleSchoolAdminRenewUserData(node);
		}

		Debug.Log ("[CacheManager] HandleRenewUser() end: " + node["my-projects-submitted"].ToString ());
	}
	
	public void HandleUserNode (JSONNode node) {
		if (node == null)
			return;
		
		#if UNITY_EDITOR
		//Debug.Log ("[CacheManager] HandleUserNode: [" + node.ToString() + "]");
		#endif
		_dynCacheNode ["user-cachedAt"] = GetCurrentTimestemp ().ToString();
		LocalData.Instance.UpdateUserNode (node);
	}


	public void HandleFeaturedCards (JSONNode projectCardsNode, JSONNode langsNode) {
		HandleFeaturedCards (projectCardsNode, langsNode, true);
	}

	//shouldLoadAssets: check if load sould file

	public void HandleFeaturedCards (JSONNode projectCardsNode, JSONNode langsNode, bool shouldLoadAssets) {
		#if UNITY_EDITOR
		//Debug.Log ("[CacheManager] HandleFeaturedCards: [" + projectCardsNode.ToString() + "] langsNode:[" + langsNode.ToString() + "]");
		#endif
		if (projectCardsNode == null)
			return;
		if (langsNode == null) {
			Debug.LogError("LangsNode is null");
		}

		// compare cards with local, any of them are more updated?
		// download them
		int change = 0;
		foreach (string projectId in projectCardsNode.GetKeys()) {
			
			// combine langs node to cards node
			CombineCardsAndLangs(projectCardsNode[projectId], langsNode);
			//Debug.Log ("After combine: " + projectCardsNode[projectId].ToString());

			// remove unused featured card?
			JSONNode localProjectFeaturedCard = LocalData.Instance.Data["featured-cards"][projectId];

			if (localProjectFeaturedCard != null) {
				List<string> toBeRemoved = new List<string>();
				foreach (string cardId in localProjectFeaturedCard.GetKeys()) {
					if (!projectCardsNode[projectId].GetKeys().Contains(cardId)) {
						// not exisited in online
						toBeRemoved.Add (cardId);
					}
				}
				foreach (string cardId in toBeRemoved) {
					Debug.Log ("Delete local featured " + cardId);
					Card deleteCard = new FeaturedCard(LocalData.Instance.Data["featured-cards"][projectId][cardId]);
					deleteCard.SafelyDeleteFiles();
					LocalData.Instance.Data["featured-cards"][projectId].Remove(cardId);
					change++;
				}
			}


			// add new featured card
			foreach (JSONNode onlineCardNode in projectCardsNode[projectId].Childs) {
				FeaturedCard onlineCard = new FeaturedCard(onlineCardNode);

				if (string.IsNullOrEmpty(projectId)) {
					Debug.LogWarning("Featured card with out project id" + onlineCard.ToString());
					continue;
				}

				// localCard
				bool needToDownLoad = false;
				bool dataChange = false;
				JSONNode cardNode = LocalData.Instance.Data["featured-cards"][projectId][onlineCard.cardId];
				Card localCard = new Card(cardNode);
				if (cardNode == null) {
					// local don't have this card
					//Debug.Log ("Featured cards: local does not have " + onlineCard.GetDefaultDisplayName());
					needToDownLoad = true;
				} else {
					
					// large change
					if (onlineCard.lastUpdatedAt > localCard.lastUpdatedAt) {
						//Debug.Log ("Featured cards: local have " + onlineCard.GetDefaultDisplayName() + "; online update");
						// online one is more updated
						needToDownLoad = true;
					} else if (onlineCard.lastUpdatedAt == localCard.lastUpdatedAt) {
						//Debug.Log ("Featured cards: local have " + onlineCard.GetDefaultDisplayName() + "; ==");
						// individual check.
						// most of the data should be the same as "lastUpdatedAt" are equal
						// obtained the objectid, url
						if (!cardNode.GetKeys().Contains("objectId") || string.IsNullOrEmpty( cardNode["objectId"].Value) ) {
							cardNode["objectId"] = onlineCardNode["objectId"];
							dataChange = true;
						}
						if (localCard.status != onlineCard.status) {
							cardNode["status"].AsInt = onlineCardNode["status"].AsInt;
							dataChange = true;
						}
						if (!localCard.Equals(onlineCard)) {
							Debug.Log ("Something not equal about featured card");
							Debug.Log ("localCard: " + localCard.toJSON().ToString());
							Debug.Log ("onlineCard: " + onlineCard.toJSON().ToString());
							needToDownLoad = true;
						}
					} else if (onlineCard.lastUpdatedAt < localCard.lastUpdatedAt) {
						//Debug.Log ("Featured cards: local have " + onlineCard.GetDefaultDisplayName() + "; <");
						// local card is more update?
						// impossible, featured card cannot be modified locally.
						// online one must be the most updated
						// online one overwrite local
						needToDownLoad = true;
					}
					if (dataChange)
						change++;
				}
				
				if (onlineCard.isDeleted) {
					// deleted card, remove asset
					onlineCard.SafelyDeleteFiles();
				} else {
					if (needToDownLoad) {
						change++;
						onlineCardNode.Remove("createdAt");
						onlineCardNode.Remove("updatedAt");
						LocalData.Instance.Data["featured-cards"][projectId][onlineCard.cardId] = onlineCardNode;
						
						// check url has changed or not
						string imageUrl = onlineCardNode ["image"] ["url"].Value;
						LocalData.Instance.CheckImageOutdated(onlineCard.ImageName, imageUrl);

						// check sound outdated for each language
						//string soundUrl = onlineCardNode ["sound"] ["url"].Value;
						/*
						foreach (CardLang lang in onlineCard.langs.Values) {
							bool soundOutdated = LocalData.Instance.CheckSoundOutdated(lang.SoundName, lang.soundUrl);
							if (soundOutdated && !string.IsNullOrEmpty(lang.soundUrl)) {
								DownloadManager.Instance.AddLinkage(lang.SoundPath, lang.soundUrl);
							}
						}
						*/
						if (shouldLoadAssets) HandleCardLangSoundOutdated(localCard, onlineCard);
					} else {
						// is the sound file really here?
						/*
						foreach (CardLang lang in onlineCard.langs.Values) {
							LocalData.Instance.CheckSoundOutdated(lang.SoundName, lang.soundUrl);
							if (!lang.isSoundExist && !string.IsNullOrEmpty(lang.soundUrl)) {
								DownloadManager.Instance.AddLinkage(lang.SoundPath, lang.soundUrl);
							}
						}
						*/
						if (shouldLoadAssets) HandleCardLangSoundOutdated(localCard, onlineCard);
					}
				}
			}
		}
		
		if (change > 0) {
			//Debug.Log ("LocalData updated");
			LocalData.Instance.Save();
		}

		//_dynCacheNode["featured-cards-" + projectId + "-cachedAt"] = GetCurrentTimestemp ().ToString();
		//_dynCacheNode["featured-cards-" + projectId] = node;
	}
	#endregion
	
	#region Helper

	public static void CombineCardsAndLangs (JSONNode cardsNode, JSONNode langsNode) {
		// combine langs node to cards node
		foreach (JSONNode langNode in langsNode.Childs) {
			string cardId = langNode["cardId"];
			string langKey = langNode["langKey"];
			if (cardsNode.GetKeys().Contains(cardId)) {
				cardsNode[cardId]["langs"][langKey] = langNode;
			}
		}
	}
	
	private bool CheckDataOutDated (string key) {
		if (CacheNode == null)
			return true;
		
		if (Mathf.Abs (GetCurrentTimestemp () - CacheNode[key].AsInt) <= CACHE_SECOND) {
			return false;
		} else {
			return true;
		}
	}
	
	public static DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	public static int GetCurrentTimestemp () {
		int timestamp = (int)((DateTime.UtcNow - epochStart).TotalSeconds);
		return timestamp;
	}
	
	private void SafelyDelete (string path) {
//		Debug.LogWarning ("CacheManager.SafelyDelete " + path);
		LocalData.Instance.SafelyDelete(path);
	}

	
	private void WriteFakeCache () {
		if (LocalData.Instance.IsTestUser == 0) {
			return;
		} else {
			string path = Path.Combine (LocalData.Instance.DirectoryPath, "cache.json");
			string data = _dynCacheNode.ToString();
			File.WriteAllText (path, data);
		}
	}
	#endregion
}
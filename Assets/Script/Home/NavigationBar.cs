using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Parse;
using SimpleJSON;

public enum NavigationView {
	Teacher_Home = 0,
	Teacher_View_Course = 1,
	Teacher_View_Course_AddProject = 2,
	Teacher_View_Student = 3,
	Teacher_View_Student_ProjectList = 4,
	Teacher_View_Project = 5,
	Teacher_View_StudentProject = 6,
	Teacher_View_Project_ModifyProject = 7,
	Teacher_View_Card = 8,
	Student_Home = 9,
	Student_MyProject = 10,
	Common_AddCard = 11,
	Common_ViewCard = 12,
	Home_Profile = 13,
	Home_Setting = 14,
	Home_Setting_CustomizeComments = 15,
	Home_Setting_Credit = 16,
	SystemAdmin_Home = 17,
	SystemAdmin_View_School = 18,
	SchoolAdmin_Home = 19,
	SlideShow = 20,
	Search = 21
}

public enum NavigationButtonType {
	ADD = 0,
	SEARCH = 1,
	INFO = 2,
	EDIT = 3,
	MORE = 4,
	CANCEL = 5,
	SYNC = 6,
	SAVE = 7
}

public class NavigationBar : Page
{
	public bool syncInProgress;

	private bool			DEBUG = false;
	
//	public enum NavigationBarState {
//		INIT,
//		WAITING_FOR_DATA,
//		DATA_RETRIEVED,
//		END
//	}

	public delegate void EventHandler();
	
	//public NavigationBarState       state = NavigationBarState.INIT;
	
	private  static NavigationBar   _instance = null;
	internal static NavigationBar   Instance { get { return _instance; } }

	private Animator				_ani = null;
	public Home_SlideMenu			_slideMenu = null;
	
	// navigation bar
	public Sprite[]					_imgNavButtonImages = null;
	public Transform				_navLeftButton = null;
	public GameObject				_btnHideSlidingMenu = null;
	
	public Transform				_navRightButton = null;
	public event EventHandler		_navRightButtonEventHandler = null;
	
	public Text						_lblNavigationTitle = null;
	public Transform				_contentViewParent = null;
	public Sprite[]					_spriteLeftIcons = null;
	
	public GameObject				_barNoConnection = null;
	private bool					_dynConnected = true;
	
	public Stack<Page>				_stackPages = null;
	
	public GameObject[]				_prefabViews = null;
	
	void Awake () {
		Debug.Log ("[NavigationBar] Awake()");
		//---------------------------------------------------------
		// v1.3 why NavigationBar need DontDestroyOnLoad()?
		//---------------------------------------------------------
		//if (_instance == null) {
			//DontDestroyOnLoad (gameObject);
			_instance = this;
			Init();
		//} else if (_instance != this) {
		//	Destroy (gameObject);
		//}
	}
	
	private void Init() {
		Debug.Log ("[NavigationBar] Init()");
		_barNoConnection.SetActive(false);
		
		_stackPages = new Stack<Page> ();
		
		// clear the existing
		if (!DEBUG) {
			foreach (Transform child in _contentViewParent) {
				Destroy (child.gameObject);
			}
		}
		
		SetRightButton();
		_btnHideSlidingMenu.gameObject.SetActive(false);
		
		CacheManager.Instance.Reset(); // just clear the Cache
		LocalData.Instance.Reset(); // this will call LocalData::LoadDataFromFile()
	
		state = PageState.INIT; // v1.3 added
	}
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (syncInProgress) {
			HideLeftButton ();
		} else {
			RestoreLeftButton ();
		}
		switch (state) {
		case PageState.INIT:
			if (ParseUser.CurrentUser == null && LocalData.Instance.IsTestUser == 0) {
				Fader.Instance.FadeIn (0.5f)
					.LoadLevel ("Login")
						.FadeOut (0.5f);
				state = PageState.END;
			} else {
				
				state = PageState.WAITING_FOR_DATA;
				RenewUserData();
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
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (AlertPanel.Instance.gameObject.activeSelf) {
				AlertPanel.Instance.EventButton(0);
			} else if (SoundRecorder.Instance.gameObject.activeSelf) {
				SoundRecorder.Instance.EventButtonCancel();
			} else if (ImagePicker.Instance.gameObject.activeSelf) {
				ImagePicker.Instance.EventButtonCancel();
			} else if (DatePicker.Instance.gameObject.activeSelf) {
				DatePicker.Instance.EventButtonDone();
			} else if (ImageViewer.Instance.gameObject.activeSelf) {
				ImageViewer.Instance.EventButtonClose();
			} else if (LoadingPanel.Instance.gameObject.activeSelf) {
				
			} else {
				Page p = _stackPages.Peek();
				if (p.HandledEscape()) {
					// page override?
				} else {
					// default
					EventButtonToggleLeftButton();
				}
			}
		}
		if (_dynConnected == _barNoConnection.activeSelf) {
			_barNoConnection.SetActive(!_dynConnected);
		}
	}

	#region Event
	private void DataLoaded () {
		_slideMenu.Init ();
		if (!DEBUG) {
			PushHomeView();
		}
	}
	
	public void EventButtonToggleLeftButton () {
		if (_stackPages.Count <= 1) {
			EventButtonHideSlidingMenu();
		} else {
			PopView();
		}
	}
	
	public void EventButtonHideSlidingMenu () {
		if (_ani == null)
			_ani = GetComponent<Animator> ();
		
		if (_stackPages.Count > 1) {
			_btnHideSlidingMenu.gameObject.SetActive(false);
			return;
		}
		
		bool activated = _ani.GetBool ("activated");
		_ani.SetBool ("activated", !activated);
		
		_btnHideSlidingMenu.gameObject.SetActive(activated);
	}
	
	public void EventButtonToggleRightButton () {
		if (_navRightButtonEventHandler != null)
			_navRightButtonEventHandler ();
	}
	
	public void ContentShowHome () {
		if (_ani == null)
			_ani = GetComponent<Animator> ();
		_ani.SetBool ("activated", true);
		
		// clear all content, push home screen
		PopAllView();
		PushHomeView();
	}
	
	public void ContentShowProfile () {
		if (_ani == null)
			_ani = GetComponent<Animator> ();
		_ani.SetBool ("activated", true);
		
		PopAllView();
		PushView(NavigationView.Home_Profile, null);
	}
	
	public void ContentShowSetting () {
		if (_ani == null)
			_ani = GetComponent<Animator> ();
		_ani.SetBool ("activated", true);
		
		PopAllView();
		PushView(NavigationView.Home_Setting, null);
	}
	
	#endregion
	
	#region Socket
	public void SetTitle (string title) {
		if (string.IsNullOrEmpty(title)) {
			title = "";
		}
		_lblNavigationTitle.text = title.ToUpper ();
	}
	
	public void SetRightButton() {
		_navRightButton.gameObject.SetActive (false);
		_navRightButtonEventHandler = null;
	}
	
	public void SetRightButton (NavigationButtonType type, EventHandler handler) {
		_navRightButton.FindChild("Image").GetComponent<Image>().sprite = _imgNavButtonImages[(int)type];
		_navRightButtonEventHandler = handler;
		
		_navRightButton.gameObject.SetActive (true);
	}
	#endregion

	#region Helper

	public void PushView (NavigationView view, Dictionary<string, object> data = null)
	{
		GameObject prefabView = _prefabViews [(int)view];
		GameObject copy = Instantiate (prefabView);
		copy.name = view.ToString ();

		copy.transform.SetParent (_contentViewParent.parent);

		RectTransform rt = copy.GetComponent<RectTransform> ();
		rt.anchorMax = new Vector2 (1, 1);
		rt.anchorMin = new Vector2 (0, 0);
		rt.anchoredPosition = _contentViewParent.GetComponent<RectTransform>().anchoredPosition;
		rt.sizeDelta = _contentViewParent.GetComponent<RectTransform>().sizeDelta;

		copy.transform.SetParent (_contentViewParent);

		copy.transform.localScale = Vector3.one;

		copy.gameObject.SetActive (true);

		Page page = copy.GetComponent<Page> ();
		if (page != null) {
			page.Init (data);
			_stackPages.Push (page);
		} else {
			Debug.LogError("No page " + prefabView.name);
		}
		ViewChange ();
	}

	public void PopView (Dictionary<string, object> data = null) {
		if (_stackPages.Count <= 1) {
			return;
		}
		Page p = _stackPages.Peek();
		if (!p.AllowPopEvent())
			return;
		
		// allowed
		p = _stackPages.Pop ();
		
		Destroy (p.gameObject);
		
		ViewChange (data);
	}
	
	private void PopAllView () {
		_btnHideSlidingMenu.SetActive(false);
		
		while (_stackPages.Count > 0) {
			Page p = _stackPages.Pop ();
			Destroy (p.gameObject);
		}
		ViewChange ();
	}
	
	private void PushHomeView () {
		if (LocalData.Instance.IsTestUser == 0) {
			if (LocalData.Instance.UserIdentity == ParseUserIdentity.Student) {
				PushView (NavigationView.Student_Home);
			} else if (LocalData.Instance.UserIdentity == ParseUserIdentity.Teacher) {
				PushView (NavigationView.Teacher_Home);
			} else if (LocalData.Instance.UserIdentity == ParseUserIdentity.SchoolAdmin) {
				PushView (NavigationView.SchoolAdmin_Home);
			} else if (LocalData.Instance.UserIdentity == ParseUserIdentity.SystemAdmin) {
				PushView (NavigationView.SystemAdmin_Home);
			} else {
				Debug.LogError("No implemented");
			}
		} else {
			//Debug.Log (LocalData.Instance.IsTestUser);
			if (LocalData.Instance.IsTestUser == 1) {
				PushView (NavigationView.Student_Home);
			} else if (LocalData.Instance.IsTestUser == 2) {
				PushView (NavigationView.Teacher_Home);
			}
		}
	}
	
	private void ViewChange (Dictionary<string, object> data = null) {
		if (_stackPages.Count <= 1) {
			_navLeftButton.FindChild("Image").GetComponent<Image>().sprite = _spriteLeftIcons [0];
		} else {
			_navLeftButton.FindChild("Image").GetComponent<Image>().sprite = _spriteLeftIcons [1];
		}
		
		_navRightButton.gameObject.SetActive (false);
		_navRightButtonEventHandler = null;
		
		if (_stackPages.Count > 0) {
			Page top = _stackPages.Peek ();
			if (top != null) {
				top.OnPageShown (data);
			}
		}
	}
	
	public void HideLeftButton () {
		_navLeftButton.gameObject.SetActive(false);
	}
	
	public void RestoreLeftButton () {
		_navLeftButton.gameObject.SetActive(true);
		//ViewChange();
	}
	
	public void SetConnection (bool success) {
		_dynConnected = success;
	}

	#endregion

	#region Page
	public override void Init (Dictionary<string, object> data) {}
	public override void OnPageShown (Dictionary<string, object> data) {}
	#endregion

	//private void IdentityCheck () {
	//	Debug.Log ("[NavigationBar] [TODO] IdentityCheck() " + LocalData.Instance.UserIdentity.ToString ());
	/*
		ParseUserIdentity identity = LocalData.Instance.UserIdentity;
		
		if (identity == ParseUserIdentity.Unknown) {
			Debug.LogError ("Unknown role");
			ParseUser.LogOutAsync ();
			Fader.Instance.FadeIn (0.5f)
				.LoadLevel ("Login")
					.FadeOut (0.5f);
		} else if (identity == ParseUserIdentity.Student && !Application.loadedLevelName.Equals ("Home_Student")) {
			// student go a wrong page
			Application.LoadLevel ("Home_Student");
		} else if (identity == ParseUserIdentity.Teacher && !Application.loadedLevelName.Equals ("Home_Teacher")) {
			// teacher go a wrong page
			Application.LoadLevel ("Home_Teacher");
		}
		*/
	//}

	/*
	#region Parse
	private void RenewUserData ()
	{
		Debug.Log ("[NavigationBar] RenewUserData()");
		if (LocalData.Instance.IsTestUser > 0) {
			state = PageState.DATA_RETRIEVED;
			return;
		}
		
		LoadingTask task = new LoadingTask("Loading Data...");
		//task.DisplayMessage = "Loading Data...";
		//task.CanCancel = true;
		GameManager.Instance.LoadingTasks.Add (task);
		
		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();
		
		ParseCloud.CallFunctionAsync<string>("RenewUser", data).ContinueWith(t => {
			if (t.IsFaulted) {
				Debug.LogError("[NavigationBar] RenewUserData() Fail");
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
				Debug.Log ("[NavigationBar] RenewUser() result:[" + result + "]");
				JSONNode node = JSON.Parse(result);
				
				CacheManager.Instance.HandleRenewUser(node); // setup a cached version
			}
			task.Loading = false;
			
			state = PageState.DATA_RETRIEVED;
		});
	}
	#endregion
	*/
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Parse;
using SimpleJSON;

public class Common_ViewCard : Page {

	private string					_dynSelectedProjectId = "";
	private int						_dynCurrentIndex = -1;
	
	public GameObject				_panelSubMenu = null;
	public GameObject				_btnSubMenuRefresh = null;
	public GameObject				_btnSubMenuEdit = null;
	public GameObject				_btnSubMenuRemove = null;

	private Card[]					cards = null;
	private JSONNode				cardsNode = null;
	private bool					isFeaturedCardMode = false;

	private CardController			_dynPreviousCardController = null;
	public CardController			_dynCurrentCardController = null;

	public GameObject slideShowPanel;
	
	private bool					_destroyed = false;
	private ParseCardUploadTask		_syncTask = null;

	private CardController			_prefabBookPage = null;

	public Text						_txtDebug = null;

	internal CardController			CurrentCardController {
		get { return _dynCurrentCardController; }
		set {
			_dynCurrentCardController = value;
			if (_dynCurrentCardController != null) {
				if (isFeaturedCardMode) {
//					NavigationBar.Instance.SetRightButton(NavigationButtonType.MORE, EventButtonSubMenuToggle);
					_panelSubMenu.SetActive(false);
					_btnSubMenuRefresh.SetActive(false);
					_btnSubMenuEdit.SetActive(false);
					_btnSubMenuRemove.SetActive(false);
					// featured cards section.
					// user maybe student / teacher
					ParseUserIdentity identity = LocalData.Instance.UserIdentity;
					if (identity == ParseUserIdentity.Student) {
						// featured card mode must be featured card, they must not be edit by student
						//NavigationBar.Instance.SetRightButton();
						_btnSubMenuRefresh.SetActive(true);
					} else if (identity == ParseUserIdentity.Teacher) {
						// if that card is belong to this teacher, allow to edit
						// else, no way
						if (_dynCurrentCardController.IsCreatedByMe) {
							//NavigationBar.Instance.SetRightButton(NavigationButtonType.EDIT, EventEditCard);
							_btnSubMenuRefresh.SetActive(true);
							_btnSubMenuEdit.SetActive(true);
							_btnSubMenuRemove.SetActive(true);
						} else {
							//NavigationBar.Instance.SetRightButton();
							_btnSubMenuRefresh.SetActive(true);
							_btnSubMenuRemove.SetActive(true);
						}
					} else if (identity == ParseUserIdentity.SystemAdmin || identity == ParseUserIdentity.SchoolAdmin) {
						_btnSubMenuRefresh.SetActive(true);
						_btnSubMenuEdit.SetActive(false);
						_btnSubMenuRemove.SetActive(false);
					}
				} else {
					_panelSubMenu.SetActive(false);
					// student viewing their cards
					if (_dynCurrentCardController.card.status == CardStatus.Passed || _dynCurrentCardController.card.status == CardStatus.Featured) {
						NavigationBar.Instance.SetRightButton();
					} else {
						NavigationBar.Instance.SetRightButton(NavigationButtonType.EDIT, EventEditCard);
					}
				}
			} else {
				NavigationBar.Instance.SetRightButton();
			}
		}
	}
	private CardController			_dynNextCardController = null;

	private AudioSource 			_audioSource;
	private bool					_dynSwiping = false;

	bool isSlideShow = false;
	Card slideShowIndexCard = null;

	// Use this for initialization
	void Start () {
		//Time.timeScale = 0.1f;
		_panelSubMenu.SetActive(false);
	}
	
	void OnEnable(){
		EasyTouch.On_SwipeStart += On_SwipeStart;	
		EasyTouch.On_SwipeEnd += On_SwipeEnd;		
	}
	
	void OnDisable(){
		UnsubscribeEvent();
		
	}

	bool shouldSync = false;
	void OnDestroy(){
		if (shouldSync) {
			SyncLocalUpdatedCardsToServer ();
		}

		UnsubscribeEvent();
		_destroyed = true;
		if (_syncTask != null)
			_syncTask.OnLoadingCompleted -= SyncTaskCompleted;
	}
	
	void UnsubscribeEvent(){
		EasyTouch.On_SwipeStart -= On_SwipeStart;	
		EasyTouch.On_SwipeEnd -= On_SwipeEnd;	
	}

	// Update is called once per frame
	void Update () {
		ParseUserIdentity identity = LocalData.Instance.UserIdentity;
		if (isFeaturedCardMode && (identity == ParseUserIdentity.Teacher || identity == ParseUserIdentity.SchoolAdmin)) {
			switch (state) {
			case PageState.READY:
				break;
			case PageState.INIT:
				break;
			case PageState.WAITING_FOR_DATA:
				break;
			case PageState.DATA_RETRIEVED:
				EventTeacherRemoveFromFeaturedSuccess();
				state = PageState.END;
				break;
			case PageState.END:
				break;
			}
		}
	}
	
	#region Socket

	public override	void Init (Dictionary<string, object> data) {
		Debug.Log ("[Common_ViewCard] Init()");
		//Get the attached AudioSource component
		_audioSource = this.GetComponent<AudioSource>();

		if (data != null) {

			if (data.ContainsKey("projectId")) {
				_dynSelectedProjectId = (string)(data ["projectId"]);
				_dynCurrentIndex = (int)(data ["cardIndex"]);
				cardsNode = (JSONNode) (data ["json"]);
				isFeaturedCardMode = (bool) (data["isFeaturedCard"]);

				string title = LocalData.Instance.Data ["my-projects"] [_dynSelectedProjectId] ["projectTitle"];
				if (title.Contains ("#SlideShow")) {
					isSlideShow = true;
					slideShowPanel.SetActive (true);
				}
			} else {
				Debug.LogWarning("AddCard: Init data no key");
			}
		}
	}

	public override void OnPageShown (Dictionary<string, object> data) {
		
		Debug.Log ("[Common_ViewCard] OnPageShown()");

		if (data != null && data.ContainsKey("action")) {
			string action = (string)data["action"];
			if (action.Equals("sync")) {
				RenewSpecificProjectCards();
			} else if (action.Equals("reload")) {
				Dictionary<string, object> popViewData = new Dictionary<string, object>();
				popViewData["action"] = "reload";
				NavigationBar.Instance.PopView(popViewData);
				//RenewSpecificProjectCards();
			}
		}
		
		// todo
		JSONNode projectNode = LocalData.Instance.Data["my-projects"][_dynSelectedProjectId];
		string projectTitle = projectNode["projectTitle"].Value;
		projectTitle = DeprecatedHelper.removeTitleTag (projectTitle);
		NavigationBar.Instance.SetTitle (projectTitle);
		NavigationBar.Instance.SetRightButton();
		
		DataLoaded();
	}
	
	private void DataLoaded () {
		Debug.Log ("[Common_ViewCard] DataLoaded()");

		CardSortBy order = isSlideShow ? CardSortBy.SlideNumber : CardSortBy.Name;
		cards = LocalData.JSONCardsToListCards(cardsNode, isFeaturedCardMode, order);

		if (isSlideShow) {
			slideShowIndexCard = DeprecatedHelper.getSlideShowIndexCard (cards);
		}

		RefreshCurrentCard();
	}
	
	#endregion
	
	#region Event
	// At the swipe end 
	private void On_SwipeStart(Gesture gesture){
		_dynSwiping = true;
	}

	private void On_SwipeEnd(Gesture gesture){
		_dynSwiping = false;
		if (ImageViewer.Instance.isActiveAndEnabled)
			return;
		if (_dynCurrentCardController._panelComment.activeSelf) {
			return;
		}

		// Get the swipe angle
		//float angles = gesture.GetSwipeOrDragAngle();
		//Debug.Log ("Last swipe : " + gesture.swipe.ToString() + " /  vector : " + gesture.swipeVector.normalized + " / angle : " + angles.ToString("f2"));

		if (gesture.swipe == EasyTouch.SwipeDirection.Left) {
			EventButtonNextCard();
			//_txtDebug.text += "L";
		} else if (gesture.swipe == EasyTouch.SwipeDirection.Right) {
			EventButtonPreviousCard();
			//_txtDebug.text += "R";
		} else {
			//_txtDebug.text += "X";
		}
	}

	public void EventButtonPlaySound () {
		_audioSource.Play ();
	}

	private void EventEditCard () {
		Card currentCard = cards[_dynCurrentIndex];

		if (currentCard == null)
			return;

		if (!isFeaturedCardMode && (currentCard.status == CardStatus.Passed || currentCard.status == CardStatus.Featured)) {
			GameManager.Instance.AlertTasks.Add (
				new AlertTask(
				GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
				, GameManager.Instance.Language.getString("modify-passed-card", CaseHandle.FirstCharUpper)
				, TextAnchor.MiddleCenter));
			return;
		}

		if (isFeaturedCardMode && !currentCard.IsCreatedByMe) {
			// should not happened, because the "edit" button is hidden
			GameManager.Instance.AlertTasks.Add (
				new AlertTask(
				GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
				, GameManager.Instance.Language.getString("modify-others-card", CaseHandle.FirstCharUpper)
				, TextAnchor.MiddleCenter));
			return;
		}

		Dictionary<string, object> data = new Dictionary<string, object> ();
		if (LocalData.Instance.UserIdentity == ParseUserIdentity.Student) {
			data ["mode"] = AddCardMode.Student_Modify;
		} else if (LocalData.Instance.UserIdentity == ParseUserIdentity.Teacher) {
			data ["mode"] = AddCardMode.Teacher_Modify;
		}
		data ["projectId"] = _dynSelectedProjectId;
		data ["cardId"] = currentCard.cardId;
		
		NavigationBar.Instance.PushView (NavigationView.Common_AddCard, data);
	}

	public void EventButtonNextCard () {
		_dynPreviousCardController.SelfDestroy();

		CurrentCardController.SetAniTrigger("flipOut");	// it move to left

		_dynPreviousCardController = CurrentCardController;
		CurrentCardController = _dynNextCardController;
		_dynNextCardController = null;			// create a new one later (RefreshNextCard)

		_dynCurrentIndex = LoopIndex(_dynCurrentIndex + 1);
		//Debug.Log (_dynCurrentIndex);
		RefreshNextCard();
	}

	public void EventButtonPreviousCard () {
		_dynNextCardController.SelfDestroy();

		_dynPreviousCardController.SetAniTrigger("flipIn");	// it move to right

		_dynNextCardController = CurrentCardController;
		CurrentCardController = _dynPreviousCardController;
		_dynPreviousCardController = null;

		_dynCurrentIndex = LoopIndex(_dynCurrentIndex - 1);
		//Debug.Log (_dynCurrentIndex);

		RefreshPreviousCard();
	}

	public void EventButtonShowImage (Image img) {
		if (_dynSwiping)
			return;

		if (img.sprite != null)
			ImageViewer.Instance.EventShowSprite(img.sprite);
	}

	
	public void EventButtonSubMenuToggle () {
		_panelSubMenu.SetActive(!_panelSubMenu.activeSelf);
	}
	private void EventButtonSubMenuShow () {
		_panelSubMenu.SetActive(true);
	}
	public void EventButtonSubMenuHide() {
		_panelSubMenu.SetActive(false);
	}
	public void EventButtonSubMenuEditCard () {
		EventEditCard ();
		EventButtonSubMenuHide();
	}
	public void EventButtonSubMenuRefresh () {
		
		if (LocalData.Instance.IsTestUser > 0) {
			return;
		}

		Card currentCard = _dynCurrentCardController.card;
		if (currentCard == null)
			return;

		_dynCurrentCardController.RefreshCardRes();
		RefreshCurrentCard();

		EventButtonSubMenuHide();
	}
	public void EventButtonSubMenuRemoveFromFeatured () {
		RemoveFromFeatured(_dynCurrentCardController.card);
		EventButtonSubMenuHide();
	}

	public void EventButtonSlideShowUp() {
		if (_dynCurrentIndex == 0)
			return;
		swipeCard (_dynCurrentIndex - 1, _dynCurrentIndex);
		--_dynCurrentIndex;
		_dynCurrentCardController._txtPage.text = (_dynCurrentIndex+1) + " / " + cards.Length;
		RefreshNextCard ();
		RefreshPreviousCard ();
	}

	public void EventButtonSlideShowDown() {
		if (_dynCurrentIndex == cards.Length-1)
			return;
		swipeCard (_dynCurrentIndex, _dynCurrentIndex+1);
		++_dynCurrentIndex;
		_dynCurrentCardController._txtPage.text = (_dynCurrentIndex+1) + " / " + cards.Length;
		RefreshNextCard ();
		RefreshPreviousCard ();
	}

	void swipeCard(int i0, int i1) {
		Card card0 = cards [i0];
		Card card1 = cards [i1];
		cards [i0] = card1;
		cards [i1] = card0;
		card0.slideNumber = i1;
		card1.slideNumber = i0;

		shouldSync = true;
		foreach (string key in LocalData.Instance.Data["local-cards"][_dynSelectedProjectId].Keys) {
			JSONNode json = LocalData.Instance.Data ["local-cards"] [_dynSelectedProjectId] [key];
			if (json ["cardId"].Value == card0.cardId) {
				json ["langs"] ["#SlideShow"] ["name"] = "" + i1;
				json["lastUpdatedAt"] = GameManager.GetCurrentTimestemp().ToString();
			} else if (json ["cardId"].Value == card1.cardId) {
				json ["langs"] ["#SlideShow"] ["name"] = "" + i0;
				json["lastUpdatedAt"] = GameManager.GetCurrentTimestemp().ToString();
			}
		}
		LocalData.Instance.Save();
	}

	#endregion

	#region Helper
	private void RefreshCurrentCard () {
		if (cards.Length == 0) {
			//This could happen if the last cards is just deleted
			Debug.Log("No cards available");
			NavigationBar.Instance.PopView();
			return;
		}
		
		_dynCurrentIndex = LoopIndex(_dynCurrentIndex);
		
		//JSONNode currentCardNode = cardsNode[_dynCurrentIndex];
		Card currentCard = cards[_dynCurrentIndex];
		CurrentCardController.Init(currentCard, _audioSource, (_dynCurrentIndex+1) , cards.Length, isFeaturedCardMode, slideShowIndexCard);

		RefreshPreviousCard();
		RefreshNextCard();

		CurrentCardController.SetAniTrigger("defaultIn");
		CurrentCardController = CurrentCardController;	// for display / hide edit button
	}

	private CardController CreateEmptyCardController () {
		if (_prefabBookPage == null) {
			_prefabBookPage = Instantiate(_dynCurrentCardController);
			_prefabBookPage.name = "Sample";
			_prefabBookPage.transform.SetParent(CurrentCardController.transform.parent);
			_prefabBookPage.transform.localScale = Vector3.one;
			_prefabBookPage.GetComponent<RectTransform>().anchorMax = CurrentCardController.GetComponent<RectTransform>().anchorMax;
			_prefabBookPage.GetComponent<RectTransform>().anchorMin = CurrentCardController.GetComponent<RectTransform>().anchorMin;
			_prefabBookPage.GetComponent<RectTransform>().anchoredPosition = CurrentCardController.GetComponent<RectTransform>().anchoredPosition;
			_prefabBookPage.GetComponent<RectTransform>().sizeDelta = CurrentCardController.GetComponent<RectTransform>().sizeDelta;
			_prefabBookPage.gameObject.SetActive(false);
		}

		CardController card = Instantiate(_prefabBookPage);
		card.name = "Page";
		card.transform.SetParent(CurrentCardController.transform.parent);
		card.transform.localScale = Vector3.one;
		card.GetComponent<RectTransform>().anchorMax = CurrentCardController.GetComponent<RectTransform>().anchorMax;
		card.GetComponent<RectTransform>().anchorMin = CurrentCardController.GetComponent<RectTransform>().anchorMin;
		card.GetComponent<RectTransform>().anchoredPosition = CurrentCardController.GetComponent<RectTransform>().anchoredPosition;
		card.GetComponent<RectTransform>().sizeDelta = CurrentCardController.GetComponent<RectTransform>().sizeDelta;
		card.gameObject.SetActive(true);
		return card;
	}

	private int LoopIndex (int idx) {
		if (idx >= cards.Length)
			idx = 0;
		else if (idx < 0)
			idx = cards.Length - 1;
		return idx;
	}
	
	private void RefreshPreviousCard () {
//		bool newCreated = false;
		if (_dynPreviousCardController == null) {
			_dynPreviousCardController = CreateEmptyCardController();
			_dynPreviousCardController.transform.SetAsLastSibling();
//			newCreated = true;
		}

		if (cards.Length == 0) {
			Debug.LogError("No cards available");
			NavigationBar.Instance.PopView();
			return;
		}

		int idx = LoopIndex(_dynCurrentIndex - 1);
		
		Card currentCard = cards[idx];
		_dynPreviousCardController.Init(currentCard, _audioSource, (idx+1) , cards.Length, isFeaturedCardMode, slideShowIndexCard);

//		if (newCreated) {
			_dynPreviousCardController.SetAniTrigger("defaultOut");
//		} else {
//			_dynPreviousCardController.SetAniTrigger("flipOut");
//		}
	}

	private void RefreshNextCard () {
		
//		bool newCreated = false;
		if (_dynNextCardController == null) {
			_dynNextCardController = CreateEmptyCardController();
			_dynNextCardController.transform.SetAsFirstSibling();
//			newCreated = true;
		}

		if (cards.Length == 0) {
			Debug.LogError("No cards available");
			NavigationBar.Instance.PopView();
			return;
		}
		
		int idx = LoopIndex(_dynCurrentIndex + 1);
		
		Card currentCard = cards[idx];
		_dynNextCardController.Init(currentCard, _audioSource, (idx+1) , cards.Length, isFeaturedCardMode, slideShowIndexCard);
		
//		if (newCreated) {
			_dynNextCardController.SetAniTrigger("defaultIn");
//		} else {
//			_dynNextCardController.SetAniTrigger("flipIn");
//		}
	}
	#endregion

	#region Parse
	
	private void RenewSpecificProjectCards () {
		
		Debug.Log ("Common_ViewCard::RenewSpecificProjectCards");

		if (LocalData.Instance.IsTestUser > 0) {
			//state = PageState.DATA_RETRIEVED;
			return;
		}

		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();
		data ["projectId"] = _dynSelectedProjectId;
		
		ParseCloud.CallFunctionAsync<string>("Student_Get_SpecificStudentProject", data)
			.ContinueWith(t => {
				if (t.IsFaulted) {
					Debug.LogError("Fail Student_Get_SpecificStudentProject");
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

					string result = t.Result;
					Debug.Log ("Common_ViewCard::RenewSpecificProjectCards CloudCode[Student_Get_SpecificStudentProject] return:" + result);

					JSONNode node = JSON.Parse(result);
					
					CacheManager.Instance.HandleStudentSpecificMyProjectSubmittedData(node["studentProject"]);
					CacheManager.Instance.HandleStudentMySpecificProjectCards(_dynSelectedProjectId, node["cards"], node["langs"]);
					CacheManager.Instance.HandleFeaturedCards(node["featuredCards"], node["langs"]);
					
					SyncLocalUpdatedCardsToServer();
				}
				//task.Loading = false;
			});
	}
	
	private void SyncLocalUpdatedCardsToServer () {
		
		string studentProjectId = CacheManager.Instance.CacheNode["my-projects-submitted"][_dynSelectedProjectId]["objectId"];
		if (string.IsNullOrEmpty(studentProjectId)) {
			Debug.LogError("studentProjectId not found");
			return;
		}

		
		if (_syncTask != null) {
			Debug.LogWarning("Sync task is already in progress");
			return;
		}

		_syncTask = SyncManager.Instance.SyncProject(studentProjectId, _dynSelectedProjectId, SyncTaskCompleted);
	}

	private void SyncTaskCompleted () {
		_syncTask = null;
		
		if (_destroyed)
			return;
		DataLoaded();
	}
	private void RemoveFromFeatured (Card card) {

		if (LocalData.Instance.IsTestUser > 0) {
			// teacher remove card from featured
			// Write to local and cache
			JSONNode studentCardsNode = null;
			string studentProjectId = "";
			foreach (string key in CacheManager.Instance.CacheNode["studentProjectsCards"].GetKeys()) {
				JSONNode studentProject = CacheManager.Instance.CacheNode["studentProjectsCards"][key];
				if (studentProject["projectId"].Value.Equals(_dynSelectedProjectId)) {
					studentCardsNode = studentProject["cards"];
					studentProjectId = key;
					break;
				}
			}
			if (studentCardsNode == null)
				studentCardsNode = JSON.Parse("{}");
			JSONNode featuredCardsNode = LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId];
			if (featuredCardsNode == null)
				featuredCardsNode = JSON.Parse("{}");
			
			Debug.Log (studentCardsNode.ToString());
			Debug.Log ("Finding " +card.objectId);
			if (studentCardsNode.GetKeys().Contains(card.objectId)) {
				studentCardsNode[card.objectId]["status"].AsInt = 1;
				Debug.Log ("Found card");
			}
			CacheManager.Instance.CacheNode["studentProjectsCards"][studentProjectId]["cards"] = studentCardsNode;
			
			if (featuredCardsNode.GetKeys().Contains(card.objectId)) {
				featuredCardsNode.Remove(card.objectId);
			}
			
			// write
			string path_1 = Path.Combine (LocalData.Instance.DirectoryPath, "data_1.json");
			JSONNode studentData = null;
			if (File.Exists (path_1)) {
				string json = File.ReadAllText (path_1);
				studentData = JSON.Parse (json);
			} else {
				studentData = JSON.Parse ("{}");
				Debug.LogWarning ("Data file for test user not existed");
			}
			studentData["local-cards"][_dynSelectedProjectId] = studentCardsNode;
			File.WriteAllText (path_1, studentData.ToString());
			
			LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId] = featuredCardsNode;
			LocalData.Instance.Save();

			EventTeacherRemoveFromFeaturedSuccess();
			
			return;
		}

		//Debug.Log ("Delete");
		LoadingTask task = new LoadingTask("Saving...");
		//task.DisplayMessage = "Saving...";
		//task.CanCancel = true;
		////task.OnLoadingCancel += onCancelled;
		GameManager.Instance.LoadingTasks.Add (task);

		if (card == null || string.IsNullOrEmpty(card.objectId)) {
			Debug.LogWarning("Unable to remove " + card.ToString());
			return;
		}
		
		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();
		data ["objectId"] = card.objectId;
		
		ParseCloud.CallFunctionAsync<string>("Teacher_RemoveCardFromFeatured", data).ContinueWith(t => {
			if (t.IsFaulted) {
				Debug.LogError("Fail Teacher_RemoveCardFromFeatured");
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
				Debug.Log ("Teacher_RemoveCardFromFeatured: " + result);
				JSONNode node = JSON.Parse(result);
				Debug.Log (node.ToString());
				
				
				state = PageState.DATA_RETRIEVED;
			}
			task.Loading = false;
		});
	}
	
	private void EventTeacherRemoveFromFeaturedSuccess () {
		Dictionary<string, object> popViewData = new Dictionary<string, object>();
		popViewData["action"] = "reload";
		
		NavigationBar.Instance.PopView(popViewData);
	}

	#endregion
}

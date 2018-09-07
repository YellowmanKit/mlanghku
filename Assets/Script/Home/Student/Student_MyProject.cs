using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Parse;
using SimpleJSON;

public class Student_MyProject : Page {

	public GameObject				rankingObj,allRankingObj;
	public GameObject 				refreshRankingBtn;
	private	bool					rotateRefreshBtn;
	private bool 					rankExist;

	public enum TabPage {
		INFO,
		FEATURED,
		CARDS
	}
	private TabPage					_dynCurrentPage = TabPage.CARDS;
	private string					_dynSelectedProjectId = "";
	private Project 				project;
	private Card[] 					localCards;
	private Card[] 					featuredCards;

	public Text						_txtTabInfo = null;
	public Text						_txtTabFeatured = null;
	public Text						_txtTabCards = null;
	
	public Color[]					_navigationColors = null;
	
	public GameObject				_panelInfo = null;
	public Color[]					_txtColor = null;
	public Text						_txtProjectName = null;
	public Text						_txtProjectDesc = null;
	public Text						_txtSubmissionStatus = null;
	public Text						_txtGradingStatus = null;
	public Text						_txtDueDate = null;
	public Text						_txtTimeRemain = null;
	
	public GameObject				_panelFeatured = null;
	public List<GameObject>			_btnFeaturedCards = null;
	public GameObject				_panelFeaturedSubMenu = null;

	public GameObject				_panelCards = null;
	public List<GameObject>			_btnMyCards = null;
	
	public Sprite[]					_spriteOnlineStatus = null;
	public Sprite[]					_spriteBadge = null;

	public GameObject 				_bottomBar;
	public GameObject 				_bottomBarSlideShow;
	
	private bool					_dynConnectedToInternet = false;
	private ParseCardUploadTask		_syncTask = null;
	public Text						_txtLastSyncAt = null;
	public Text						_txtLastSyncAtSlideShow = null;
	private bool					_destroyed = false;

	private bool isSlideShow = false;

	// Use this for initialization
	void Start () {
		_txtProjectName.text = "";
		_txtProjectDesc.text = "";
		_txtSubmissionStatus.text = "";
		_txtGradingStatus.text = "";
		_txtDueDate.text = "";
		_txtTimeRemain.text = "";

		ShowTabPage (TabPage.CARDS);
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
				if (CacheManager.Instance.IsStudentMySpecificProjectCardsOutdated(_dynSelectedProjectId)) {
					Debug.Log ("[Student_MyProject] IsStudentMySpecificProjectCards Outdated");
					state = PageState.WAITING_FOR_DATA;
					RenewSpecificProjectCards();
				} else {
					_dynConnectedToInternet = true;
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
		if (rotateRefreshBtn) {
			refreshRankingBtn.GetComponent<LangChooser> ()._key = "refreshing";
			refreshRankingBtn.GetComponent<LangChooser> ().Start ();
		} else {
			refreshRankingBtn.GetComponent<LangChooser> ()._key = "refresh";
			refreshRankingBtn.GetComponent<LangChooser> ().Start ();
		}
	}
	
	#region Socket
	public override	void Init (Dictionary<string, object> data) {
		if (data != null) {
			if (data.ContainsKey("projectId")) {
				_dynSelectedProjectId = (string)(data ["projectId"]);
				foreach (GameObject btn in _btnMyCards) { btn.SetActive(false); }
				state = PageState.INIT;
			} else {
				Debug.LogWarning("[Student_MyProject] Init data no key");
			}
		}
	}

	public override void OnPageShown (Dictionary<string, object> data) 
	{
		Debug.Log ("[Student_MyProject] OnPageShown()");

		bool shouldSync = false;
		if (data != null && data.ContainsKey("action")) {
			string action = (string)data["action"];
			if (action.Equals("sync")) {
				RenewSpecificProjectCards();
				shouldSync = true;
			}
		}

		DataLoaded();

		if (!shouldSync) {
			int cachedAt = CacheManager.Instance.CacheNode ["my-projectcards-" + _dynSelectedProjectId + "-cachedAt"].AsInt;
			DateTime epochStart = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			DateTime currentTime = epochStart.AddSeconds (cachedAt).ToLocalTime ();
			_txtLastSyncAt.text = string.Format (GameManager.Instance.Language.getString ("last-sync-semi-val", CaseHandle.FirstCharUpper)
				, currentTime.ToString ("dd/MM/yyyy hh:mm tt"));
		}

		NavigationBar.Instance.SetRightButton ();

		if (_dynCurrentPage == TabPage.INFO) {
			//stay empty
		} else if (_dynCurrentPage == TabPage.FEATURED) {
			NavigationBar.Instance.SetRightButton(NavigationButtonType.MORE, EventButtonFeaturedSubMenuToggle);
		} else if (_dynCurrentPage == TabPage.CARDS) {
			if (!project.dueDatePassed) {
				//------------------------------------------------------------------------------
				// v1.3 TODO: double check why card cannot be added if it is slideshow project
				// and one card is graded?
				//------------------------------------------------------------------------------
				if (project.isSlideShow () && localCards != null && localCards.Length > 0 && localCards [0].status != CardStatus.Not_Graded) {
					//prohibit edit if it's graded
					//stay empty
				} else {
					NavigationBar.Instance.SetRightButton (NavigationButtonType.ADD, EventButtonAddCard);
				}
			}
		}

		NavigationBar.Instance.SetTitle (project.getTitle());
	}
	
	private void DataLoaded () 
	{
		Debug.Log("[Student_MyProject] DataLoaded() " + LocalData.Instance.Data.ToString());

		if (!LocalData.Instance.Data["my-projects"].GetKeys().Contains(_dynSelectedProjectId)) {
			Debug.LogError("[Student_MyProject] No such project");
			return;
		}
		
		///////////////////////////////////////////////////
		// info page
		JSONNode projectNode = LocalData.Instance.Data["my-projects"][_dynSelectedProjectId];
		if (projectNode != null) {
			project = new Project(projectNode);
			
			_txtProjectName.text = project.getTitle();
			_txtProjectDesc.text = project.desc;
			_txtDueDate.text = project.dueDate.ToString("dddd, d MMMM yyyy, hh:mm tt");
			
			RefreshTimeRemain ();
		}

		if (project.isSlideShow ()) {
			isSlideShow = true;
			//update UI for slide show
			_bottomBar.SetActive(false);
			_bottomBarSlideShow.SetActive (true);

			_txtLastSyncAt = _txtLastSyncAtSlideShow;
		}
		
		///////////////////////////////////////////////////
		// cards page
		JSONNode localCardsNode = LocalData.Instance.Data["local-cards"][_dynSelectedProjectId];
		//Card[] localCards = LocalData.JSONCardsToListCards(localCardsNode, false, CardSortBy.Status, SortingOrder.Desc);
		CardSortBy order = isSlideShow ? CardSortBy.SlideNumber : CardSortBy.Name;
		localCards = LocalData.JSONCardsToListCards(localCardsNode, false, order);

		if (localCards.Length > 0) { // v1.3 2017/01/04 handle empty card list
			//------------------------------------------------------------------------------
			// v1.3 TODO: double check why card cannot be added if it is slideshow project
			// and one card is graded?
			//------------------------------------------------------------------------------
			if (project.isSlideShow () && localCards [0].status != CardStatus.Not_Graded) {
				//prohibit edit if it's graded

				NavigationBar.Instance.SetRightButton ();
			}
		} else {
			Debug.Log ("[Student_MyProject] Empty card list");
		}

		// show the cards
		foreach (GameObject btn in _btnMyCards) { btn.SetActive(false); }

		GameManager.InstantiateChildren( localCards.Length, _btnMyCards );
		/*
		int numOfCards = localCards.Length;
		int missing = numOfCards - _btnMyCards.Count;
		for (int i = 0; i < missing; i++) {
			GameObject copy = Instantiate (_btnMyCards [0]);
			copy.transform.SetParent (_btnMyCards [0].transform.parent);
			copy.transform.localScale = Vector3.one;
			
			_btnMyCards.Add (copy);
		}
		*/

		///////////////////////////////////////////////////
		/// Online parse record
		Dictionary<string, int> parseCardsLastUpdatedAt = new Dictionary<string, int>();
		JSONNode onlineCards = CacheManager.Instance.CacheNode["my-projectcards-" + _dynSelectedProjectId];
		if (onlineCards != null && onlineCards.Count > 0) {
			foreach (JSONNode onlineCard in onlineCards.Childs) {
				parseCardsLastUpdatedAt[onlineCard ["cardId"].Value] = onlineCard ["lastUpdatedAt"].AsInt;
			}
		}
		
		//////////////////////////////////////////////////
		/// Update the Student Cards
		int idx = 0;
		foreach (Card card in localCards)
		{
			// assign the text
			GameObject btn = _btnMyCards [idx++];
			btn.name = card.cardId;

			btn.transform.FindChild ("Text").GetComponent<Text> ().text = card.GetDefaultDisplayName();

			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();
			
			btn.gameObject.SetActive (true);

//			if (btnImageIcon.sprite != null) {
//				Sprite s = btnImageIcon.sprite;
//				btnImageIcon.sprite = null;
//				Destroy(s.texture);
//			}
			
			// update the icon
			// "cardId" is "card-xxxxxxxxxx"
			//Debug.Log (card.ImagePath);
//			if (card.isImageExist) {
//				btnImageIcon.sprite = DownloadManager.FileToSprite (card.ImagePath);
//			} else {
//				// does it provide a url?
//				if (!string.IsNullOrEmpty(card.imageUrl)) {
//					btnImageIcon.sprite = DownloadManager.Instance.spriteLoading;
//					DownloadManager.Instance.AddLinkage(card.ImagePath, card.imageUrl, btnImageIcon);
//				} else {
//					btnImageIcon.sprite = DownloadManager.Instance.spriteNoImage;
//					Debug.LogWarning("No image file, and no url provided for " + card.GetDefaultDisplayName());
//				}
//			}

			btnImageIcon.sprite = DownloadManager.Instance.spriteLoading;
			DownloadManager.Instance.AddLinkage(card.ImagePath, card.imageUrl, btnImageIcon);
			
			
			Image imgOnline = btn.transform.FindChild("img_online_status").GetComponent<Image>();
			if (!_dynConnectedToInternet) {
				imgOnline.gameObject.SetActive(false);
			} else {
				if (!string.IsNullOrEmpty(card.objectId)) {
					// have object id, it is uploaded
					imgOnline.gameObject.SetActive(true);
					imgOnline.sprite = _spriteOnlineStatus[0];
				} else if (!card.isReadyToUpload) {
					// non complete info, so it is not uploaded
					imgOnline.gameObject.SetActive(true);
					imgOnline.sprite = _spriteOnlineStatus[1];
				} else {
					imgOnline.gameObject.SetActive(false);
				}
			}

			Image imgBadge = btn.transform.FindChild("img_badge").GetComponent<Image>();
			if (card.HasGraded) {
				imgBadge.gameObject.SetActive(true);
				if (card.status == CardStatus.Passed) {
					imgBadge.sprite = _spriteBadge[0];
				} else if (card.status == CardStatus.Featured) {
					imgBadge.sprite = _spriteBadge[2];
				} else if (card.status == CardStatus.Failed) {
					imgBadge.sprite = _spriteBadge[1];
					//imgOnline.gameObject.SetActive(false);
				}
				imgOnline.gameObject.SetActive(false);
			} else {
				imgBadge.gameObject.SetActive(false);
			}
		}

		// last sync at
		/*
		if (_dynConnectedToInternet) {
			if (CacheManager.Instance.CacheNode.GetKeys().Contains("my-projectcards-" + _dynSelectedProjectId + "-cachedAt")) {
				int cachedAt = CacheManager.Instance.CacheNode["my-projectcards-" + _dynSelectedProjectId + "-cachedAt"].AsInt;
				DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				DateTime currentTime = epochStart.AddSeconds(cachedAt).ToLocalTime();
				_txtLastSyncAt.text = string.Format(GameManager.Instance.Language.getString("last-sync-semi-val", CaseHandle.FirstCharUpper)
				                                    , currentTime.ToString("dd/MM/yyyy hh:mm tt"));
			}
		} else {
			// unable to connect
			_txtLastSyncAt.text = GameManager.Instance.Language.getString("synchronization-failed", CaseHandle.FirstCharUpper);
		}
		*/

		///////////////////////////////////////////////////
		// featured cards page
		JSONNode featruedCardsNode = LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId];
		featuredCards = LocalData.JSONCardsToListCards(featruedCardsNode, true);

		if (isSlideShow) {
			//leave only the first page of slide show
			List<Card> coverCards = new List<Card>();
			foreach (Card card in featuredCards) {
				if (card.slideNumber == 0) {
					coverCards.Add (card);
				}
			}
			featuredCards = coverCards.ToArray ();
		}

		// show the cards
		foreach (GameObject btn in _btnFeaturedCards) { btn.SetActive(false); }
		
		GameManager.InstantiateChildren( featuredCards.Length, _btnFeaturedCards );
		/*
		numOfCards = featuredCards.Length;
		missing = numOfCards - _btnFeaturedCards.Count;
		for (int i = 0; i < missing; i++) {
			GameObject copy = Instantiate (_btnFeaturedCards [0]);
			copy.transform.SetParent (_btnFeaturedCards [0].transform.parent);
			copy.transform.localScale = Vector3.one;
			
			_btnFeaturedCards.Add (copy);
		}
		*/

		//////////////////////////////////////////////////
		/// Update the featured cards

		idx = 0;
		foreach (FeaturedCard card in featuredCards) 
		{
			// assign the text
			GameObject btn = _btnFeaturedCards [idx++];
			btn.name = card.cardId;

			//--------------------------------------------------------------------------------------
			// v1.3 TODO: modify Cloudcode so that authorRealName is also returned.
			//--------------------------------------------------------------------------------------
			String displayText = isSlideShow ? card.authorRealName : card.GetDefaultDisplayName();
			btn.transform.FindChild ("Text").GetComponent<Text> ().text = displayText;
			//--------------------------------------------------------------------------------------
			if (isSlideShow) {
				StartCoroutine (GetAuthorRealName (card, btn.transform.FindChild ("Text").GetComponent<Text> ()));
			}

			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();

//			if (btnImageIcon.sprite != null) {
//				Sprite s = btnImageIcon.sprite;
//				btnImageIcon.sprite = null;
//				Destroy(s.texture);
//			}
			
			btn.gameObject.SetActive (true);
			//Debug.Log (card.ImageName);
			//Debug.Log (card.SoundName);
			// update the icon
			// "cardId" is "card-xxxxxxxxxx"
//			if (card.isImageExist) {
//				//Debug.Log (card.ImagePath);
//				//Debug.Log (card.SoundPath);
//				btnImageIcon.sprite = DownloadManager.FileToSprite (card.ImagePath);
//			} else {
				// does it provide a url?
				//if (!string.IsNullOrEmpty(card.imageUrl)) {
					btnImageIcon.sprite = DownloadManager.Instance.spriteLoading;
					DownloadManager.Instance.AddLinkage(card.ImagePath, card.imageUrl, btnImageIcon);
				//} else {
				//	btnImageIcon.sprite = DownloadManager.Instance.spriteNoImage;
				//	Debug.LogWarning("[Student_MyProject] No image file, and no url provided for " + card.GetDefaultDisplayName());
				//}
//			}

			Transform badge = btn.transform.FindChild ("img_badge");
			if (ParseUser.CurrentUser != null && card.authorId.Equals(ParseUser.CurrentUser.ObjectId)) {
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
		foreach (GameObject btn_mycard in _btnMyCards) {
			GameManager.Instance.CheckTextLength (btn_mycard.transform.FindChild ("Text").GetComponent<Text> ());
		}
		foreach (GameObject btn_featured in _btnFeaturedCards) {
			GameManager.Instance.CheckTextLength (btn_featured.transform.FindChild ("Text").GetComponent<Text> ());
		}
		_panelFeatured.SetActive (_panelFeatured_isActive);
		//------------------------------------------------------------------------------
	}

	IEnumerator GetAuthorRealName(Card card,Text _txtAuthor){
		bool done = false;
		string realName = "";
		_txtAuthor.text = "";


		realName = PlayerPrefs.GetString ("" + card.objectId + "-author-name-by-card");
		if (realName == "") {
			var user = new ParseObject ("User");
			string username = "";
			ParseQuery<ParseObject> query = ParseObject.GetQuery ("Card");
			Debug.Log ("Card obj id: " + card.objectId);
			query.GetAsync (card.objectId).ContinueWith (t => {
				if (!t.IsFaulted) {
					ParseObject Card = t.Result;
					//Debug.Log("Trying to get user id");
					user = (ParseObject)Card ["author"];
				} else {
					Debug.Log ("Failed to create query for Card!");
				}
				done = true;
			});
			yield return new WaitUntil (() => done == true);

			realName = PlayerPrefs.GetString ("" + user.ObjectId + "-author-name-by-user");
			if (realName == "") {
				done = false;
				ParseQuery<ParseUser> query2 = ParseUser.Query;
				query2.GetAsync (user.ObjectId).ContinueWith (t2 => {
					if (!t2.IsFaulted) {
						ParseUser userObj = t2.Result;
						realName = userObj.Get<string> ("realName");
					} else {
						Debug.Log ("Failed to create query for User!");
						Debug.Log (t2.Exception.Message);
					}
					done = true;
				});

				yield return new WaitUntil (() => done == true);
				PlayerPrefs.SetString ("" + card.objectId + "-author-name-by-card", realName);
				PlayerPrefs.SetString ("" + user.ObjectId + "-author-name-by-user", realName);

			} else {
				Debug.Log ("Author name by user is known!");
			}
		} else {
			Debug.Log ("Author name by card is known!");
		}
		_txtAuthor.text = realName;
		yield return null;
	}

	#endregion
	
	#region Event
	public void EventButtonSelectTab (GameObject btn) {
		if (btn.name.Contains ("info")) {
			ShowTabPage (TabPage.INFO);
		} else if (btn.name.Contains ("cards")) {
			ShowTabPage (TabPage.CARDS);
		} else if (btn.name.Contains ("feature")) {
			ShowTabPage (TabPage.FEATURED);
		}
	}
	
	public void EventButtonAddCard () {
		
		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["mode"] = AddCardMode.Student_Add;
		data ["projectId"] = _dynSelectedProjectId;
		data ["cardId"] = LocalData.CurrentUserId + "-card-" + GameManager.GetCurrentTimestemp();

		//check if slide show and add slide number
		if (isSlideShow) {
			int cardCount = 0;
			foreach (Card card in localCards) {
				if (!card.isDeleted) ++cardCount;
			}
			data ["slideNumber"] = ""+ cardCount;
		}

		NavigationBar.Instance.PushView (NavigationView.Common_AddCard, data);
		
	}
	
	public void EventButtonSelectedCard (GameObject btn)
	{
		string cardId = btn.name;
		int index = -1;
		for (int i = 0; i < localCards.Length; i++) {
			if (cardId.Equals(localCards[i].cardId)) {
				index = i;
				break;
			}
		}
		if (index >= 0) {
			Dictionary<string, object> data = new Dictionary<string, object> ();
			data ["projectId"] = _dynSelectedProjectId;
			data ["isFeaturedCard"] = false;
			data ["json"] = LocalData.Instance.Data["local-cards"][_dynSelectedProjectId];
			data ["cardIndex"] = index;
			
			NavigationBar.Instance.PushView (NavigationView.Common_ViewCard, data);
		} else {
			Debug.Log ("Unknown index");
		}
	}

	public void EventButtonSelectedFeaturedCard (GameObject btn)
	{
		//Debug.Log ("[Student_MyProject] EventButtonSelectedFeaturedCard()");

		string cardId = btn.name;
		int index = -1;
		for (int i = 0; i < featuredCards.Length; i++) {
			if (cardId.Equals(featuredCards[i].cardId)) {
				index = i;
				break;
			}
		}
		if (index >= 0) {
			if (isSlideShow) {

				Debug.Log ("[Student_MyProject] EventButtonSelectedFeaturedCard() isSlideShow");

				//play the slide show
				Card targetCard = featuredCards[index];

				JSONNode featruedCardsNode = LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId];
				Card[] fcards = LocalData.JSONCardsToListCards(featruedCardsNode, true, CardSortBy.SlideNumber);
				List<Card> cards = new List<Card> ();
				foreach (Card card in fcards) {
					if (card.authorId == targetCard.authorId) {
						cards.Add (card);
					}
				}

				Dictionary<string, object> data = new Dictionary<string, object> ();
				data ["name"] = project.getTitle();
				data ["cards"] = cards.ToArray ();

				NavigationBar.Instance.PushView (NavigationView.SlideShow, data);
			} else {
				
				Debug.Log ("[Student_MyProject] EventButtonSelectedFeaturedCard() isNotSlideShow");

				//show the individual card
				Dictionary<string, object> data = new Dictionary<string, object> ();
				data ["projectId"] = _dynSelectedProjectId;
				data ["isFeaturedCard"] = true;
				data ["json"] = LocalData.Instance.Data ["featured-cards"] [_dynSelectedProjectId];
				data ["cardIndex"] = index;
			
				NavigationBar.Instance.PushView (NavigationView.Common_ViewCard, data);
			}
		} else {
			Debug.Log ("Unknown index");
		}
	}
	
	public void EventButtonSyncNow () {
		RenewSpecificProjectCards();
	}

	public void EventButtonSlideShowPlay() {
		//playback
		Dictionary<string, object> data = new Dictionary<string, object> ();
		data ["name"] = project.getTitle();
		data ["cards"] = localCards;

		NavigationBar.Instance.PushView (NavigationView.SlideShow, data);
	}
	
	public void EventButtonFeaturedSubMenuToggle () {
		_panelFeaturedSubMenu.SetActive(!_panelFeaturedSubMenu.activeSelf);
	}
	private void EventButtonFeaturedSubMenuShow () {
		_panelFeaturedSubMenu.SetActive(true);
	}
	public void EventButtonFeaturedSubMenuHide() {
		_panelFeaturedSubMenu.SetActive(false);
	}
	public void EventButtonFeaturedSubMenuRefresh () {
		if (LocalData.Instance.IsTestUser > 0) {
			return;
		}

		LoadingTask task = new LoadingTask("Loading Data...");
		//task.DisplayMessage = "Loading Data...";
		//task.CanCancel = true;
		GameManager.Instance.LoadingTasks.Add (task);

		RenewSpecificProjectCards(task);
		EventButtonFeaturedSubMenuHide();
	}
	
	#endregion
	

	
	#region Helper
	
	private void ShowTabPage (TabPage tabPage) {
		
		_dynCurrentPage = tabPage;
		
		_panelInfo.gameObject.SetActive (false);
		_panelCards.gameObject.SetActive (false);
		_panelFeatured.gameObject.SetActive (false);
		_panelFeaturedSubMenu.SetActive(false);
		
		NavigationBar.Instance.RestoreLeftButton();
		
		_txtTabFeatured.color = _txtTabInfo.color = _txtTabCards.color = _navigationColors [1];
		switch (tabPage) {
		case TabPage.INFO:
			StartCoroutine(SyncGetRanking ());
			_txtTabInfo.color = _navigationColors[0];
			_panelInfo.gameObject.SetActive (true);
			RefreshTimeRemain();
			NavigationBar.Instance.SetRightButton();
			break;
		case TabPage.FEATURED:
			_txtTabFeatured.color = _navigationColors[0];
			_panelFeatured.gameObject.SetActive (true);
			NavigationBar.Instance.SetRightButton(NavigationButtonType.MORE, EventButtonFeaturedSubMenuToggle);
			break;
		case TabPage.CARDS:
			_txtTabCards.color = _navigationColors[0];
			_panelCards.gameObject.SetActive (true);
			
			if (!project.dueDatePassed) {
				if (project.isSlideShow () && localCards != null && localCards.Length > 0 && localCards [0].status != CardStatus.Not_Graded) {
					NavigationBar.Instance.SetRightButton ();
				} else {
					NavigationBar.Instance.SetRightButton (NavigationButtonType.ADD, EventButtonAddCard);
				}
			}
			break;
		}
	}
	
	private void RefreshTimeRemain () {// submitted?
		// network connected?
		if (CacheManager.Instance.CacheNode.GetKeys().Contains("my-projects-submitted-cachedAt")) {
			bool submitted = false;
			//DateTime submitTime = DateTime.Now;
			int numOfCardSubmitted = 0;
			// this project submitted?
			foreach (JSONNode submittedNode in CacheManager.Instance.CacheNode["my-projects-submitted"].Childs) {
				string projectId = submittedNode["project"]["objectId"].Value;
				if (projectId.Equals(_dynSelectedProjectId)) {
					submitted = true;
					//submitTime = Convert.ToDateTime(submittedNode["createdAt"]);
					numOfCardSubmitted = submittedNode["numOfSubmittedCard"].AsInt;
					break;
				}
			}
			if (submitted && numOfCardSubmitted > 0) {
				// submitted
				if (numOfCardSubmitted > 1) {
					_txtSubmissionStatus.text = string.Format(
						GameManager.Instance.Language.getString("val-cards-submitted", CaseHandle.FirstCharUpper),
						numOfCardSubmitted);
				} else if (numOfCardSubmitted == 1) {
					_txtSubmissionStatus.text = GameManager.Instance.Language.getString("1-card-submitted", CaseHandle.FirstCharUpper);
				}
				_txtSubmissionStatus.color = _txtColor[1];
			} else {
				_txtSubmissionStatus.text = GameManager.Instance.Language.getString("no-attempt", CaseHandle.FirstCharUpper);
				_txtSubmissionStatus.color = _txtColor[2];
			}
			
			// time remain
			string timeRemainString = "";
			TimeSpan timeRemain = (project.dueDate - DateTime.Now);
			if (timeRemain.TotalSeconds >= 0) {
				// still have time
				timeRemainString += GameManager.TimeRemainToString(timeRemain.Days, timeRemain.Hours, timeRemain.Minutes);
				
				_txtTimeRemain.text = timeRemainString;
				_txtTimeRemain.color = _txtColor[0];
				
			} else {
				// overdue
				timeRemainString = GameManager.Instance.Language.getString("passed", CaseHandle.FirstCharUpper);
				
				_txtTimeRemain.text = timeRemainString;
				_txtTimeRemain.color = _txtColor[0];
				
			}
		} else {
			Debug.LogWarning ("No connection: can't check submit status");
		}
		
		
	}

	void OnDestroy () {
		_destroyed = true;
		if (_syncTask != null)
			_syncTask.OnLoadingCompleted -= SyncTaskCompleted;

//		for (int i=0; i<_btnFeaturedCards.Count; ++i) {
//			GameObject btn = _btnFeaturedCards [i];
//			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();
//			if (btnImageIcon.sprite != null) {
//				Sprite s = btnImageIcon.sprite;
//				btnImageIcon.sprite = null;
//				Destroy(s.texture);
//			}
//		}
//
//		for (int i=0; i<_btnMyCards.Count; ++i) {
//			GameObject btn = _btnMyCards [i];
//			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();
//			if (btnImageIcon.sprite != null) {
//				Sprite s = btnImageIcon.sprite;
//				btnImageIcon.sprite = null;
//				Destroy(s.texture);
//			}
//		}
	}
	#endregion
	
	#region Parse
	
	private void RenewSpecificProjectCards (LoadingTask task = null)
	{
		Debug.Log ("[Student_MyProject] RenewSpecificProjectCards()");

		if (LocalData.Instance.IsTestUser > 0) {
			state = PageState.DATA_RETRIEVED;
			return;
		}

		/*
		//-------------------------------------------------------
		// v1.3 TODO: double check why this part is commented out?
		//-------------------------------------------------------
		LoadingTask task = new LoadingTask("Loading Data...");
		//task.DisplayMessage = "Loading Data...";
		//task.CanCancel = true;
		GameManager.Instance.LoadingTasks.Add (task);
		//-------------------------------------------------------
		*/

		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();
		data ["projectId"] = _dynSelectedProjectId;
		
		_txtLastSyncAt.text = GameManager.Instance.Language.getString("synchronization-in-progress", CaseHandle.FirstCharUpper);
		
		ParseCloud.CallFunctionAsync<string>("Student_Get_SpecificStudentProject", data)
			.ContinueWith(t => {
				if (task != null)
					task.Loading = false;
				if (_destroyed)
					return;
				if (t.IsFaulted) {
					NavigationBar.Instance.syncInProgress = false;
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
					NavigationBar.Instance.SetConnection(true);
					string result = t.Result;
					JSONNode node = JSON.Parse(result);
					Debug.Log ("[Student_MyProject] Student_Get_SpecificStudentProject() result: " + node["featuredCards"].ToString());
					CacheManager.Instance.HandleStudentSpecificMyProjectSubmittedData(node["studentProject"]);
					CacheManager.Instance.HandleStudentMySpecificProjectCards(_dynSelectedProjectId, node["cards"], node["langs"]);
					CacheManager.Instance.HandleFeaturedCards(node["featuredCards"], node["langs"]);

					//-------------------------------------------------------
					// v1.3 TODO: double check why this part is needed?
					//-------------------------------------------------------
					SyncLocalUpdatedCardsToServer();
					
					_dynConnectedToInternet = true;
				}
				//task.Loading = false;
				state = PageState.DATA_RETRIEVED;
			});
	}
	
	private void SyncLocalUpdatedCardsToServer ()
	{
		if (LocalData.Instance.IsTestUser > 0) {
			Debug.LogWarning("Stopped sync");
			return;
		}

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
	
	private void SyncTaskCompleted ()
	{
		if (LocalData.Instance.IsTestUser > 0) {
			Debug.LogWarning("[Student_MyProject] Stopped sync");
			return;
		}

		_syncTask = null;
		
		if (_destroyed) return;
		
		Debug.Log ("[Student_MyProject] SyncTaskCompleted");
		
		if (_dynConnectedToInternet) {
			if (CacheManager.Instance.CacheNode.GetKeys().Contains("my-projectcards-" + _dynSelectedProjectId + "-cachedAt")) {
				int cachedAt = CacheManager.Instance.CacheNode["my-projectcards-" + _dynSelectedProjectId + "-cachedAt"].AsInt;
				DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				DateTime currentTime = epochStart.AddSeconds(cachedAt).ToLocalTime();
				_txtLastSyncAt.text = string.Format(GameManager.Instance.Language.getString("last-sync-semi-val", CaseHandle.FirstCharUpper)
				                                    , currentTime.ToString("dd/MM/yyyy hh:mm tt"));
			}
		} else {
			// unable to connect
			_txtLastSyncAt.text = GameManager.Instance.Language.getString("synchronization-failed", CaseHandle.FirstCharUpper);
		}
		
		
		DataLoaded();
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
		refreshRankingBtn.GetComponent<Button> ().enabled = true;
	}

	public void EventRefreshRanking(){
		refreshRankingBtn.GetComponent<Button> ().enabled = false;
		rankExist = false;
		StartCoroutine (SyncGetRanking ());
	}
		
}

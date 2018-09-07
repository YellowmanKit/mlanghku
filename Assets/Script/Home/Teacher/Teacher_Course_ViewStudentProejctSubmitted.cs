using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Parse;
using SimpleJSON;
using WWUtils.Audio;
using System.Net;

public class Teacher_Course_ViewStudentProejctSubmitted : Page {

	public Color[] 					sharedIconColor;

	public Image					recordBtnBadge;
	public Sprite					done_badge,none_badge;
	public Text						RecordedText;
	public GameObject				removeRecordBtn,recordCmtBtn;
	private List<string>			cardObjIdOnCell = new List<string>();
	private List<int>				modifiedSoundCommentIndex = new List<int>();
	public List<int>				modifiedToShareIndex = new List<int>();
	public List<bool>				cardInCellIsShared = new List<bool>();
	private WebClient				client;
	private bool					soundCmtDownloadDone,soundCmtSyncNotDone;

	public enum TabPage {
		TABLE,
		CARDS
	}
	private JSONNode				cardsNodeFromServer = null;
	private JSONNode				studentNode = null;

//	private TabPage					_dynCurrentPage = TabPage.CARDS;
	private string 					_dynSelectedProjectId;
	private string					_dynSelectedStudentProjectId = "";

	private AudioSource				_audio = null;
	public	AudioSource				_audioForComment;
	
	private Card[]	 				_dynCards = null;
	private Dictionary<string, AudioClip>		_dynAudioRes = null;

	private int						_dynCurrentCardIndex = -1;
	internal Card					CurrentCard { get { return _dynCurrentCardIndex >= 0 && _dynCurrentCardIndex < _dynCards.Length ? _dynCards[_dynCurrentCardIndex] : null; } }
	internal int					CurrentCardIndex {
		get { return _dynCurrentCardIndex; }
		set {
			if (value < 0 || value >= _btnTableCell.Count) {
				return;
			}
			_panelComments.SetActive(false);
			_panelAudio.SetActive(false);
			_panelText.SetActive(false);

			_txtControlComment.text = "";

			// origin selected cell
			if (_dynCurrentCardIndex >= 0 && _dynCurrentCardIndex < _btnTableCell.Count) {
				
				GameObject obj = _btnTableCell[_dynCurrentCardIndex];
				Image imgBtn = obj.GetComponent<Image>();
				imgBtn.color = _tableCellColors[0];

				RenewCurrentCardGraded();
			}

			_dynCurrentCardIndex = value;
			
			// current selected cell
			if (_dynCurrentCardIndex >= 0 && _dynCurrentCardIndex < _btnTableCell.Count) {
				
				GameObject obj = _btnTableCell[_dynCurrentCardIndex];
				Image imgBtn = obj.GetComponent<Image>();
				imgBtn.color = _tableCellColors[1];

				RenewCurrentCardGraded();
				RenewCommentDisplay();

				// position of scroll content
				int padding = 20;
				float height = 0;
				// get height individually
				for (int i = 0; i < _dynCurrentCardIndex - 1; i++) {
					GameObject cell = _btnTableCell[i];
					LayoutElement elem = cell.GetComponent<LayoutElement>();
					height += elem.preferredHeight + padding;
				}
				//_rtScrollContent.anchoredPosition = new Vector2(0, (padding+150) * (_dynCurrentCardIndex - 1)); 
				_rtScrollContent.anchoredPosition = new Vector2(0, height); 
			}
			IndicateRecordCommentIsDoneOrNot ();
			EventCurrentRowPlaySound ();
		}
	}
	
	public Color[]					_navigationColors = null;
	
	public GameObject				_panelCardView = null;
	
	public GameObject				_panelTableView = null;

	public GameObject				_panelComments = null;
	public List<Transform>			_commentObjs = null;
	public List<Transform>			_newCommentObjs = null;

	public GameObject				_viewChoices = null;
	public GameObject				_viewChoicesSlideShow = null;
	
	public GameObject				_panelAudio = null;
	public List<Transform>			_audioLangs = null;

	public InputField				_inputNewComment = null;
	public List<GameObject>			_btnTableCell = null;
	public Color[]					_tableCellColors = null;

	public Button					_btnControlPlaySound = null;
	public Text						_txtControlComment = null;
	public Sprite[]					_spriteGrading = null;
	public RectTransform			_rtScrollContent = null;

	private List<int>				_newlyCommented = null;
	private bool					_dynSaved = true;

	private GameObject				_btnChoices = null;

	private bool isSlideShow;
	private Card slideShowIndexCard;

	public Button slideShowCross;
	public Button slideShowTick;
	public Button slideShowFeatured;

	//----------------------------------------------------
	// v1.3 added -- Big-T button can choose lang
	// reference to _panelAudio and _audioLangs
	//----------------------------------------------------
	public GameObject				_panelText = null;
	public List<Transform>			_textLangs = null;
	//----------------------------------------------------

	// Use this for initialization
	void Start () {
		_newlyCommented = new List<int>();
		_txtControlComment.text = "";

		_audio = GetComponent<AudioSource>();

		ShowTabPage (TabPage.TABLE);

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
				RenewSpecificStudentProject(PageState.DATA_RETRIEVED);
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
		foreach (AudioClip ac in _dynAudioRes.Values) {
			Destroy(ac);
		}
	}

	public void EventButtonRecording () {
		if (!modifiedSoundCommentIndex.Contains (_dynCurrentCardIndex)) {
			modifiedSoundCommentIndex.Add (_dynCurrentCardIndex);
		}
		soundCmtSyncNotDone = true;
		_audioForComment.Stop();
		SoundRecorder.Instance.ShowSoundRecording(SoundRecordCompletedHandler);
	}

	public void EventButtonPlayRecordComment(){
		if (_audioForComment.clip != null) {
			_audioForComment.Play ();
		}
	}

	private void SoundRecordCompletedHandler (AudioClip clip) {
		if (clip != null) {
			// new clip received
			_audioForComment.Stop();
			if (_audioForComment.clip != null) {
				Destroy(_audioForComment.clip);
			}
			_audioForComment.clip = clip;
			//_isSoundChanged = true;
		}
		IndicateRecordCommentIsDoneOrNot ();
		//RenewSoundButton();
	}

	private void IndicateRecordCommentIsDoneOrNot(){
		_audioForComment = _btnTableCell [CurrentCardIndex].GetComponent<AudioSource> ();
		if (_audioForComment.clip != null) {
			recordBtnBadge.sprite = done_badge;
			float length = _audioForComment.clip.length;
			RecordedText.text = "Recorded - " + Mathf.Floor(length / 59f).ToString("00") + ":" + (length % 60f).ToString("00");
			removeRecordBtn.SetActive (true);
		} else {
			recordBtnBadge.sprite = none_badge;
			RecordedText.text = "No record";
			removeRecordBtn.SetActive (false);
		}
	}

	public void EventRemoveRecordedComment(){
		if (!modifiedSoundCommentIndex.Contains (_dynCurrentCardIndex)) {
			modifiedSoundCommentIndex.Add (_dynCurrentCardIndex);
		}
		_audioForComment.clip = null;
		IndicateRecordCommentIsDoneOrNot ();
	}
	
	#region Socket

	public override	void Init (Dictionary<string, object> data) {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] Init()");
		_panelText.SetActive(false);
		_panelAudio.SetActive(false);
		_panelComments.SetActive(false);
		if (data != null) {
			if (data.ContainsKey("studentProjectId")) {
				_dynSelectedStudentProjectId = (string)(data ["studentProjectId"]);
				//--------------------------------------------------------------------------------------
				// TODO: v1.3 if called from Teacher_Course_view_student there is no "projectId" given...
				//--------------------------------------------------------------------------------------
				if (data.ContainsKey("projectId")) _dynSelectedProjectId = (string)data ["projectId"];
				//--------------------------------------------------------------------------------------

				Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] Init() _dynSelectedProjectId=" + _dynSelectedProjectId + " _dynSelectedStudentProjectId=" + _dynSelectedStudentProjectId);
	
				foreach (GameObject btn in _btnTableCell) {
					btn.SetActive(false);
				}

				state = PageState.INIT;
			} else {
				Debug.LogWarning("Student_MyProject: Init data no key");
			}
		}

	}

	public override void OnPageShown (Dictionary<string, object> data) {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] OnPageShown()");
		/*
		JSONNode projectNode = LocalData.Instance.Data["my-projects"][_dynSelectedStudentProjectId];
		string projectTitle = projectNode["projectTitle"].Value;
		NavigationBar.Instance.SetTitle (projectTitle);
		*/
		
		if (LocalData.Instance.UserIdentity == ParseUserIdentity.Teacher)
			NavigationBar.Instance.SetRightButton (NavigationButtonType.SAVE, EventButtonSync);
	}

	private void DataLoaded () {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] DataLoaded()");
		// title
		string studentName = studentNode["username"];
		NavigationBar.Instance.SetTitle (studentName);

		///////////////////////////////////////////////////
		// Table View
		//_dynCards = new List<Card>();
		_dynCards = LocalData.JSONCardsToListCards(cardsNodeFromServer, false, CardSortBy.Status, SortingOrder.Asc);
		//Debug.Log (cardsNodeFromServer.ToString());
		if (_dynAudioRes == null)
			_dynAudioRes = new Dictionary<string, AudioClip>();

		foreach (GameObject btn in _btnTableCell) {
			btn.SetActive(false);
		}
		//_btnControlPlaySound.interactable = false;

		if (cardsNodeFromServer == null || cardsNodeFromServer.Count == 0) {
			Debug.LogWarning("No cards");
			_dynCurrentCardIndex = -1;
			return;
		}

		///////////////////////////////////////////////////
		//Check if slide show
		//Debug.Log("Check if slide show");
		//--------------------------------------------------------------------------------------
		// TODO: v1.3 if called from Teacher_Course_view_student there is no "projectId" given...
		//--------------------------------------------------------------------------------------
		if ((!string.IsNullOrEmpty(_dynSelectedProjectId)) && (LocalData.Instance.Data ["my-projects"] [_dynSelectedProjectId] != null))
		{
			JSONNode projectNode = LocalData.Instance.Data ["my-projects"] [_dynSelectedProjectId];
			Project project = new Project (projectNode);

			Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] DataLoaded() projectTitle: " + project.title);
			
			isSlideShow = project.isSlideShow ();
			if (isSlideShow) {
				//do init for slide show
				_viewChoices.SetActive (false);
				_viewChoicesSlideShow.SetActive (true);
				_btnChoices = _viewChoicesSlideShow;
				_btnTableCell [0].transform.FindChild ("img_graded").gameObject.SetActive (false);
				_btnTableCell [0].transform.FindChild ("img_featured").gameObject.SetActive (false);

				//search index card
				slideShowIndexCard = DeprecatedHelper.getSlideShowIndexCard (_dynCards);
			} else {
				_btnChoices = _viewChoices;
			}
		} else {
			//--------------------------------------------------------------------------------------
			// assume no slideshow...
			//--------------------------------------------------------------------------------------
			isSlideShow = false;
			_btnChoices = _viewChoices;
		}	

		///////////////////////////////////////////////
		/// preload audio
		foreach (Card card in _dynCards) {
			//Debug.Log (card.projectId);
			foreach (CardLang lang in card.getLangs().Values) {
				if (LocalData.Instance.IsTestUser > 0) {
					try {
						Debug.Log("preload audio name(" + lang.SoundName + ") path[" + lang.SoundPath + "]");
						WAV wav = new WAV(lang.SoundPath);
						AudioClip audioClip = AudioClip.Create("temp", wav.SampleCount, 1, wav.Frequency,false);
						audioClip.SetData(wav.LeftChannel, 0);
						if (_dynAudioRes.ContainsKey(lang.SoundName)) { // v1.3 2017/01/09 added
							if (_dynAudioRes[lang.SoundName] != null) {
								Destroy(_dynAudioRes[lang.SoundName]);
							}
						}
						_dynAudioRes[lang.SoundName] = audioClip;
					} catch (Exception e) {
						Debug.LogError(e.Message);
					}
				} else {
					//----------------------------------------------------------
					// v1.3 does this help for teacher->slideshow?
					// Added SoundPath to PreloadAudio() so that after downloaded,
					// we can save the audio file to local path as well.
					//----------------------------------------------------------
					StartCoroutine(PreloadAudio(lang.SoundPath, lang.SoundName, lang.soundUrl));
					//----------------------------------------------------------
				}
			}
		}

		GameManager.InstantiateChildren (_dynCards.Length, _btnTableCell);
		for (int i = 0; i < _dynCards.Length; i++) {
			cardInCellIsShared.Add (false);
		}
		/*
		int numOfCards = _dynCards.Length;
		int missing = numOfCards - _btnTableCell.Count;
		for (int i = 0; i < missing; i++) {
			GameObject copy = Instantiate (_btnTableCell [0]);
			copy.transform.SetParent (_btnTableCell [0].transform.parent);
			copy.transform.localScale = Vector3.one;
			
			_btnTableCell.Add (copy);
		}
		*/

		//////////////////////////////////////////////////
		/// Update the button
		int idx = 0;
		foreach (Card card in _dynCards) {
			// assign the text

			int localIndex = idx++;

			GameObject btn = _btnTableCell [localIndex];
			btn.name = card.cardId;

			Teacher_Course_ViewStudentProejctSubmitted_Cell cell = btn.GetComponent<Teacher_Course_ViewStudentProejctSubmitted_Cell>();
			//----------------------------------------------------------------
			// v1.3 swap these two lines so that I can check whether the text is too long
			// inside Teacher_Course_ViewStudentProejctSubmitted_Cell.Init()
			//----------------------------------------------------------------
			btn.gameObject.SetActive (true);
			cell.Init (card);

			// comment?
			RenewButtonWithIndex(localIndex);

			//add by Kit
			//Debug.Log ("ID: " + card.cardId);
			cardObjIdOnCell.Add(card.objectId);
		}
//		foreach (string id in cardObjIdOnCell) {
//			Debug.Log (id + " " + cardObjIdOnCell.IndexOf(id));
//		}

		CurrentCardIndex = 0;

		if (isSlideShow)
			RenewSlideShowGraded ();

		////////////////////////////////////////////////
		// comments panel (new buton)
		JSONArray comments = LocalData.Instance.UserNode["comments"].AsArray;

		foreach (Transform comment in _newCommentObjs) {
			comment.gameObject.SetActive(false);
		}
		
		// existing comments
		GameManager.InstantiateChildren( comments.Count, _newCommentObjs );
		/*
		int missing = comments.Count - _newCommentObjs.Count;
		for (int i = 0; i < missing; i++) {
			Transform copy = Instantiate (_newCommentObjs [0]);
			copy.transform.SetParent (_newCommentObjs [0].transform.parent);
			copy.transform.localScale = Vector3.one;
			
			_newCommentObjs.Add (copy);
		}
		*/

		//////////////////////////////////////////////////
		/// Update the content
		for (int i = 0; i < comments.Count; i++) {
			string comment = comments[i].Value;
			Transform commentObj = _newCommentObjs[i];
			
			commentObj.transform.FindChild ("Text").GetComponent<Text> ().text = comment;
			
			commentObj.gameObject.SetActive(true);
		}
		
		_btnChoices.SetActive(LocalData.Instance.UserIdentity == ParseUserIdentity.Teacher);

		StartCoroutine(SyncGetCardsRoutine ());

		//IndicateRecordCommentIsDoneOrNot ();
	}
	
	public override bool AllowPopEvent () {
		
		if (LocalData.Instance.UserIdentity != ParseUserIdentity.Teacher)
			return true;

		if ((_newlyCommented.Count > 0 && _dynSaved == false) || soundCmtSyncNotDone) {
			string[] option = new string[]{
				GameManager.Instance.Language.getString("no", CaseHandle.FirstCharUpper),
				GameManager.Instance.Language.getString("yes", CaseHandle.FirstCharUpper)
			};
			AlertTask fail = new AlertTask(
				GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
				, GameManager.Instance.Language.getString("marked-card-not-save", CaseHandle.FirstCharUpper) + "\n" + GameManager.Instance.Language.getString("sure-to-quit", CaseHandle.FirstCharUpper)
				, option
				, TextAnchor.MiddleCenter);
			fail.buttonCompletedEvent_1 += ForcePop;
			GameManager.Instance.AlertTasks.Add (fail);

			return false;
		} else {
			return true;
		}
	}

	public override bool HandledEscape () {
		if (_panelAudio.activeSelf) {
			EventShowAudioPanel();
			return true;
		} else if (_panelComments.activeSelf) {
			EventShowCommentPanel();
			return true;
		}
		return false;
	}
	#endregion
	
	#region Event
	private void ForcePop () {
		_newlyCommented.Clear();
		_dynSaved = true;
		NavigationBar.Instance.EventButtonToggleLeftButton();
	}

	public void EventButtonSelectTab (GameObject btn) {
		if (btn.name.Contains ("table")) {
			ShowTabPage (TabPage.TABLE);
		} else if (btn.name.Contains ("cards")) {
			ShowTabPage (TabPage.CARDS);
		}
	}
	
	public void EventButtonSelectedCard (GameObject btn) {

		int index = -1;
		for (int i = 0; i < _btnTableCell.Count; i++) {
			if (btn == _btnTableCell[i]) {
				index = i;
				break;
			}
		}

		CurrentCardIndex = index;
	}
	
	public void EventControlButtonPrevious () {
		_panelComments.SetActive(false);
		_panelAudio.SetActive(false);
		_panelText.SetActive(false);

		if (CurrentCardIndex > 0) {
			CurrentCardIndex--;
		}
	}
	public void EventControlButtonNext () {
		_panelComments.SetActive(false);
		_panelAudio.SetActive(false);
		_panelText.SetActive(false);

		if (CurrentCardIndex < _dynCards.Length - 1)
		CurrentCardIndex++;
	}
	
	public void EventControlButtonShowImage () {
		_panelComments.SetActive(false);
		_panelAudio.SetActive(false);
		_panelText.SetActive(false);

		if (CurrentCard == null)
			return;

		GameObject btn = _btnTableCell [CurrentCardIndex];
		Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();

		ImageViewer.Instance.EventShowSprite(btnImageIcon.sprite);
	}

	//----------------------------------------------------------
	// v1.3 added
	//----------------------------------------------------------
	public void EventShowTextPanel () {
		_panelComments.SetActive(false);
		_panelAudio.SetActive(false);

		if (!_panelText.activeSelf)
			RenewTextList();

		_panelText.SetActive(!_panelText.activeSelf);
	}
	public void EventControlButtonShowText (GameObject obj) {
		string langKey = obj.name;
		//Debug.Log (langKey);

		Card card = CurrentCard;
		if (card == null)
			return;

		int show_text_idx = -1;
		for (int i = 0; i < CurrentCard.LangsInOrder.Length; i++) {
			if (CurrentCard.LangsInOrder [i].langKey == langKey) {
				show_text_idx = i;
				break;
			}
		}
		if (show_text_idx >= 0) {
			TextViewer.Instance.showText (CurrentCard.LangsInOrder[show_text_idx]);
		}
	}
	//----------------------------------------------------------
	// v1.3 comment out for backup purpose
	//----------------------------------------------------------
	/*
	public void EventControlButtonShowText (GameObject obj) {
		_panelComments.SetActive(false);
		_panelAudio.SetActive(false);

		if (CurrentCard == null)
			return;
		
		TextViewer.Instance.showText (CurrentCard.LangsInOrder);

//		string message = "";
//		CardLang[] langs = card.LangsInOrder;
//		for (int i = 0; i < langs.Length; i++) {
//			CardLang lang = langs[i];
//
//			if (i != 0)
//				message += "\n";
//			message += GameManager.Instance.Language.getString("lang-" + lang.langKey, CaseHandle.FirstCharUpper);
//			message += ": ";
//			message += lang.getName();
//		}
//
//		string[] option = new string[]{
//			GameManager.Instance.Language.getString("confirm", CaseHandle.FirstCharUpper)
//		};
//		AlertTask textMessage = new AlertTask(
//			""
//			, message
//			, option
//			, TextAnchor.MiddleCenter);
//		textMessage.buttonCompletedEvent_1 += ForcePop;
//		GameManager.Instance.AlertTasks.Add (textMessage);
	}
	*/
	//----------------------------------------------------------

	public void EventShowCommentPanel () {
		
		if (LocalData.Instance.UserIdentity != ParseUserIdentity.Teacher)
			return;

		_panelAudio.SetActive(false);
		_panelText.SetActive(false);
		//Debug.Log ("Triggered");
		
		// update the panel
		RenewCommentList();
		
		_panelComments.SetActive(!_panelComments.activeSelf);
		
	}
	
	public void EventShowAudioPanel () {
		_panelComments.SetActive(false);
		_panelText.SetActive(false);

		if (!_panelAudio.activeSelf)
			RenewAudioList();
		
		_panelAudio.SetActive(!_panelAudio.activeSelf);
	}

	public void EventButtonPlaySound (GameObject obj) {
		string langKey = obj.name;
		//Debug.Log (langKey);
		
		Card card = CurrentCard;
		if (card == null)
			return;

		string soundName = card.GetLangByKey(langKey).SoundName;
		if (_dynAudioRes.ContainsKey(soundName)) {
			_audio.clip = _dynAudioRes[soundName];
			_audio.Play();
		}
	}

	public void EventCurrentRowPlaySound () {
		string langKey = "zh_w";
		//Debug.Log (langKey);

		Card card = CurrentCard;
		if (card == null)
			return;

		var CardLang = card.GetLangByKey (langKey);
		if (CardLang == null) {
			langKey = "zh";
			CardLang = card.GetLangByKey (langKey);
		}
		if (CardLang == null) {
			return;
		}
		string soundName = card.GetLangByKey(langKey).SoundName;
		if (_dynAudioRes.ContainsKey(soundName)) {
			_audio.clip = _dynAudioRes[soundName];
			_audio.Play();
		}
	}
	
	private void EventControlButtonPlaySound () {
		_panelComments.SetActive(false);
		_panelAudio.SetActive(false);
		_panelText.SetActive(false);

		
		/*
		Card card = CurrentCard;
		if (card == null)
			return;

		string objectId = card.objectId;
		if (_dynAudioRes.ContainsKey(objectId)) {
			_audio.clip = _dynAudioRes[objectId];
			_audio.Play();
		}
		*/
	}

	public void EventControlButtonUnlike () {
		_panelComments.SetActive(false);
		_panelAudio.SetActive(false);
		_panelText.SetActive(false);

		//different behaviour for slide show
		if (isSlideShow) {
			foreach (Card card in _dynCards) {
				card.status = CardStatus.Failed;
			}

			RenewSlideShowGraded ();
		} else {
			Card card = CurrentCard;
			if (card == null)
				return;
			if (card.status == CardStatus.Failed) {
				card.status = CardStatus.Not_Graded;
			} else {
				card.status = CardStatus.Failed;
			}
			RenewCurrentCardGraded();
			EventControlButtonNext ();
		}

		NewlyUpdated ();
	}
	
	public void EventControlButtonLike () {
		_panelComments.SetActive(false);
		_panelAudio.SetActive(false);
		_panelText.SetActive(false);

		//different behaviour for slide show
		if (isSlideShow) {
			foreach (Card card in _dynCards) {
				card.status = CardStatus.Passed;
			}

			RenewSlideShowGraded ();
		} else {
			Card card = CurrentCard;
			if (card == null)
				return;

			if (card.status == CardStatus.Passed) {
				card.status = CardStatus.Not_Graded;
			} else {
				card.status = CardStatus.Passed;
			}
			RenewCurrentCardGraded();
			EventControlButtonNext ();
		}

		NewlyUpdated ();
	}
	
	public void EventControlButtonFeature () {
		_panelComments.SetActive(false);
		_panelAudio.SetActive(false);
		_panelText.SetActive(false);

		//different behaviour for slide show
		if (isSlideShow) {
			foreach (Card card in _dynCards) {
				card.status = CardStatus.Featured;
			}
			RenewSlideShowGraded ();
		} else {
			Card card = CurrentCard;
			if (card == null)
				return;
			if (card.status == CardStatus.Featured) {
				card.status = CardStatus.Not_Graded;
			} else {
				card.status = CardStatus.Featured;
			}
			RenewCurrentCardGraded();
			EventControlButtonNext ();
		}

		NewlyUpdated ();
	}

	public void EventControlButtonSlideShowPlay () {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] EventControlButtonSlideShowPlay()");
		_panelComments.SetActive(false);
		_panelAudio.SetActive(false);
		_panelText.SetActive(false);

		//playback
		Dictionary<string, object> data = new Dictionary<string, object> ();
		string studentProjectTitle = CacheManager.Instance.CacheNode ["studentProjectsCards"] [_dynSelectedStudentProjectId] ["projectTitle"];
		data ["name"] = studentProjectTitle;
		data ["cards"] = _dynCards;

		NavigationBar.Instance.PushView (NavigationView.SlideShow, data);
	}

	public void EventCommentRemove (Text txt) {
		
		Card card = CurrentCard;
		if (card == null)
			return;
		string comment = txt.text;

		card.comments.Remove(comment);
		RenewCommentList();
		RenewCommentDisplay();
	}
	
	public void EventCommentAdd () {
		
		Card card = CurrentCard;
		if (card == null)
			return;
		
		string comment = _inputNewComment.text.Trim();
		if (!string.IsNullOrEmpty(comment)) {
			if (!card.comments.Contains(comment)) {
				card.comments.Add (comment);
				_inputNewComment.text = "";
			}
			RenewCommentList();
			RenewCommentDisplay();
		}
	}
	
	public void EventCommentAdd (Text txt) {
		
		Card card = CurrentCard;
		if (card == null)
			return;
		
		string comment = txt.text.Trim();
		if (!string.IsNullOrEmpty(comment)) {
			if (!card.comments.Contains(comment))
				card.comments.Add (comment);
			RenewCommentList();
			RenewCommentDisplay();
		}
	}
	
	#endregion
	

	
	#region Helper
	
	private void NewlyUpdated () {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] NewlyUpdated()");
		if (!_newlyCommented.Contains(CurrentCardIndex)) {
			_newlyCommented.Add (CurrentCardIndex);
			_dynSaved = false;
		}
	}
	
	private void EventButtonSync () {

		if (LocalData.Instance.UserIdentity != ParseUserIdentity.Teacher)
			return;

		
		if (LocalData.Instance.IsTestUser > 0) {
			// Write to local and cache
			JSONNode cardNodes = JSON.Parse("{}");
			string projectId = _dynCards[0].projectId;
			if (string.IsNullOrEmpty(projectId)) {
				Debug.LogError("Empty project id");
			}
			JSONNode featuredCardsNode = LocalData.Instance.Data["featured-cards"][projectId];
			if (featuredCardsNode == null)
				featuredCardsNode = JSON.Parse("{}");

			foreach (Card card in _dynCards) {

				JSONNode cardNode = card.toJSON();

				// comments
				JSONArray commentNodes = new JSONArray();
				foreach (string comment in card.comments) {
					commentNodes[-1] = comment;
				}
				cardNode["comments"] = commentNodes;

				cardNodes[card.objectId] = cardNode;

				if (card.status == CardStatus.Featured) {
					// additional data
					featuredCardsNode[card.objectId] = cardNode;
					// copy image and sound
					FeaturedCard fc = new FeaturedCard(cardNode);
					// copy image
					if (!File.Exists(fc.ImagePath))
						File.Copy(card.ImagePath, fc.ImagePath);
					foreach (CardLang lang in card.langs.Values) {
						string originPath = lang.SoundPath;
						if (originPath.Length == 0)
							continue;
						foreach (CardLang featuredLang in fc.langs.Values) {
							if (lang.langKey == featuredLang.langKey) {
								string featuredPath = featuredLang.SoundPath;
								if (!File.Exists (featuredPath)) {
									File.Copy (originPath, featuredPath);
								}
							}
						}
					}
				} else if (featuredCardsNode.GetKeys().Contains(card.objectId)) {
					featuredCardsNode.Remove(card.objectId);
				}
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
			studentData["local-cards"][projectId] = cardNodes;
			CacheManager.Instance.CacheNode ["studentProjectsCards"] [_dynSelectedStudentProjectId] ["cards"] = cardNodes;
			File.WriteAllText (path_1, studentData.ToString());

			LocalData.Instance.Data["featured-cards"][projectId] = featuredCardsNode;
			LocalData.Instance.Save();

			_newlyCommented.Clear();
			_dynSaved = true;
			
			return;
		}
		
		List<Card> cardToUpdated = new List<Card>();
		foreach (Card card in _dynCards) {
			string cardId = card.cardId;
			// find the matching card
			Card originCard = null;
			if (cardsNodeFromServer.GetKeys().Contains(cardId)) {
				originCard = new Card(cardsNodeFromServer[cardId]);
			} else {
				Debug.LogWarning(cardId + " not existed!?");
				continue;
			}

			if (!card.IsCommentEqual(originCard)) {
				cardToUpdated.Add (card);
			}
		}
		SyncLocalUpdatedCardsToServer(cardToUpdated);
	}

	private void RenewSlideShowGraded() {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] RenewSlideShowGraded()");
		slideShowCross.interactable = true;
		slideShowTick.interactable = true;
		slideShowFeatured.interactable = true;
		switch (slideShowIndexCard.status) {
		case CardStatus.Failed:
			slideShowCross.interactable = false;
			break;
		case CardStatus.Passed:
			slideShowTick.interactable = false;
			break;
		case CardStatus.Featured:
			slideShowFeatured.interactable = false;
			break;
		}
	}
	
	private void RenewCommentList () {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] RenewCommentList()");
		//-----------------------------------------------------------------
		// v1.3 TODO: double check why the design is so strange -- only show
		// the comments of the slideShowIndexCard?
		//-----------------------------------------------------------------
		Card card = isSlideShow ? slideShowIndexCard : CurrentCard;
		if (card == null) return;

		foreach (Transform comment in _commentObjs) { comment.gameObject.SetActive(false); }

		GameManager.InstantiateChildren( card.comments.Count, _commentObjs );
		/*
		int missing = card.comments.Count - _commentObjs.Count;
		for (int i = 0; i < missing; i++) {
			Transform copy = Instantiate (_commentObjs [0]);
			copy.transform.SetParent (_commentObjs [0].transform.parent);
			copy.transform.localScale = Vector3.one;
			
			_commentObjs.Add (copy);
		}
		*/

		for (int i = 0; i < card.comments.Count; i++) {
			//string comment = card.comments[i];
			Transform commentObj = _commentObjs[i];
			commentObj.transform.FindChild ("Text").GetComponent<Text> ().text = card.comments[i];
			commentObj.gameObject.SetActive(true);
		}
	}

	private void RenewAudioList () {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] RenewAudioList()");
		Card card = CurrentCard;
		if (card == null) return;

		foreach (Transform audio in _audioLangs) { audio.gameObject.SetActive(false); }

		CardLang[] sortedLangs = card.LangsInOrder;
		GameManager.InstantiateChildren (sortedLangs.Length, _audioLangs);
		/*
		int missing = sortedLangs.Length - _audioLangs.Count;
		for (int i = 0; i < missing; i++) {
			Transform copy = Instantiate (_audioLangs [0]);
			copy.transform.SetParent (_audioLangs [0].transform.parent);
			copy.transform.localScale = Vector3.one;
			
			_audioLangs.Add (copy);
		}
		*/

		//////////////////////////////////////////////////
		/// Update the content
		for (int i = 0; i < sortedLangs.Length; i++) {
			CardLang lang = sortedLangs[i];
			Transform audioObj = _audioLangs[i];

			audioObj.name = lang.langKey;

			//audioObj.FindChild ("img_flag").GetComponent<Image> ().sprite = DownloadManager.GetNationFlagByLangKey(lang.langKey);

			string langName = GameManager.Instance.Language.getString("lang-" + lang.langKey);
			audioObj.transform.FindChild ("Text").GetComponent<Text> ().text = langName;
			
			audioObj.gameObject.SetActive(true);

			// button active
			Button btn = audioObj.GetComponent<Button>();
			if (_dynAudioRes.ContainsKey(lang.SoundName)) {
				btn.interactable = true;
			} else {
				btn.interactable = false;
			}
		}

	}

	//---------------------------------------------------------------------
	// v1.3 Big-T button can choose lang
	//---------------------------------------------------------------------
	private void RenewTextList () {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] RenewTextList()");
		Card card = CurrentCard;
		if (card == null) return;

		foreach (Transform t in _textLangs) { t.gameObject.SetActive(false); }

		CardLang[] sortedLangs = card.LangsInOrder;
		GameManager.InstantiateChildren (sortedLangs.Length, _textLangs);
		/*
		int missing = sortedLangs.Length - _textLangs.Count;
		for (int i = 0; i < missing; i++) {
			Transform copy = Instantiate (_textLangs [0]);
			copy.transform.SetParent (_textLangs [0].transform.parent);
			copy.transform.localScale = Vector3.one;

			_textLangs.Add (copy);
		}
		*/

		//////////////////////////////////////////////////
		/// Update the content
		for (int i = 0; i < sortedLangs.Length; i++) {
			CardLang lang = sortedLangs[i];
			Transform textObj = _textLangs[i];

			textObj.name = lang.langKey;

			//audioObj.FindChild ("img_flag").GetComponent<Image> ().sprite = DownloadManager.GetNationFlagByLangKey(lang.langKey);

			string langName = GameManager.Instance.Language.getString("lang-" + lang.langKey);
			textObj.transform.FindChild ("Text").GetComponent<Text> ().text = langName;

			textObj.gameObject.SetActive(true);

			// button active
			Button btn = textObj.GetComponent<Button>();
			if (!string.IsNullOrEmpty(lang.getName())) {
				btn.interactable = true;
			} else {
				btn.interactable = false;
			}
		}
	}
	//----------------------------------------------------------------------------

	private void RenewCurrentCardGraded () {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] RenewCurrentCardGraded()");
		RenewButtonWithIndex(CurrentCardIndex);
	}

	private void RenewButtonWithIndex (int idx) {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] RenewButtonWithIndex()");
		//No need update for slide show
		if (isSlideShow)
			return;

		GameObject obj = _btnTableCell[idx];
		
		Card card = _dynCards[idx];
		if (card != null && _dynAudioRes.ContainsKey(card.objectId)) {
			//_btnControlPlaySound.interactable = true;
		} else {
			//_btnControlPlaySound.interactable = false;
		}
		
		Transform imgGraded = obj.transform.FindChild("img_graded");
		Image imgTick = imgGraded.FindChild("tick").GetComponent<Image>();
		Image imgCross = imgGraded.FindChild("cross").GetComponent<Image>();
		//Debug.Log (imgTick.enabled);
		//Debug.Log (imgCross.enabled);
		Image imgFeatured = obj.transform.FindChild("img_featured").GetComponent<Image>();
		//Debug.Log (card.status);
		if (card.status == CardStatus.Not_Graded) {
			imgTick.gameObject.SetActive(false);
			imgCross.gameObject.SetActive(false);
			imgFeatured.gameObject.SetActive(false);
		} else if (card.status == CardStatus.Passed) {
			imgTick.gameObject.SetActive(true);
			imgCross.gameObject.SetActive(false);
			imgFeatured.gameObject.SetActive(false);
		} else if (card.status == CardStatus.Featured) {
			imgTick.gameObject.SetActive(true);
			imgCross.gameObject.SetActive(false);
			imgFeatured.gameObject.SetActive(true);
		} else if (card.status == CardStatus.Failed) {
			imgTick.gameObject.SetActive(false);
			imgCross.gameObject.SetActive(true);
			imgFeatured.gameObject.SetActive(false);
		}
	}

	private void RenewCommentDisplay () {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] RenewCommentPlay()");
		if (isSlideShow) {
			_txtControlComment.text = slideShowIndexCard == null ? "" : slideShowIndexCard.CommemtToString ();
		} else {
			_txtControlComment.text = CurrentCard == null ? "" : CurrentCard.CommemtToString ();
		}
	}
		
	IEnumerator PreloadAudio(string soundPath, string soundName, string soundUrl)
	{
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] PreloadAudio() path[" + soundPath + "] name[" + soundName + "] url[" + soundUrl + "]");

		WWW www = new WWW(soundUrl);
		yield return www;

		AudioClip clip = www.GetAudioClip(false, false);

		if (clip != null) {
			//------------------------------------------------------------------
			// v1.3 also save the audio file to local path for SlideShow
			//------------------------------------------------------------------
			File.WriteAllBytes(soundPath, www.bytes);
			DownloadManager.Instance.AddLinkage(soundPath, soundUrl); // v1.3 need??
			//------------------------------------------------------------------

			_dynAudioRes[soundName] = clip;

			if (_panelAudio.activeSelf) {
				// audio panel is on
				// check one by one to see if the name match
				Card card = CurrentCard;
				if (card != null) {

					foreach (CardLang lang in card.langs.Values) {
						string langSoundName = lang.SoundName;
						if (langSoundName.Equals(soundName)) {
							// this file download completed, and the audio panel is showing,
							// update it
							foreach (Transform btn in _audioLangs) {
								if (btn.name.Equals(lang.langKey)) {
									btn.GetComponent<Button>().interactable = true;
									break;
								}
							}
							break;
						}
					}
				}
			}
		}
	}

	private void ShowTabPage (TabPage tabPage) {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] ShowTabPage()");

//		_dynCurrentPage = tabPage;
		
		_panelCardView.gameObject.SetActive (false);
		_panelTableView.gameObject.SetActive (false);
		_panelComments.SetActive(false);
		
		NavigationBar.Instance.RestoreLeftButton();
		
		switch (tabPage) {
		case TabPage.TABLE:
			_panelTableView.gameObject.SetActive (true);
			break;
		case TabPage.CARDS:
			_panelCardView.gameObject.SetActive (true);
			
			//NavigationBar.Instance.SetRightButton(NavigationButtonType.ADD, EventButtonAddCard);
			break;
		}
	}

	void SetSharedIcon(int cellIndex,bool shared){
		Image icon = _btnTableCell [cellIndex].transform.FindChild ("img_shared").GetComponent<Image> ();
		icon.enabled = true;
		if (!shared) {
			icon.color = sharedIconColor [0];
		} else {
			icon.color = sharedIconColor [1];
		}
	}

	public void EventSetToShare(){
		if (_dynCurrentCardIndex < 0 || _dynCurrentCardIndex > _btnTableCell.Count) {
			return;
		}
		modifiedToShareIndex.Add (_dynCurrentCardIndex);
		cardInCellIsShared [_dynCurrentCardIndex] = !cardInCellIsShared [_dynCurrentCardIndex];
		SetSharedIcon (_dynCurrentCardIndex, cardInCellIsShared [_dynCurrentCardIndex]);
	}

	void InitializeShareIcon(){
		for (int i = 0; i < cardInCellIsShared.Count; i++) {
			SetSharedIcon(i,cardInCellIsShared [i]);
		}
	}
	#endregion

	
	#region Parse
	private void RenewSpecificStudentProject (PageState onCompletedState) {
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] RenewSpecificStudentProject()");
		if (LocalData.Instance.IsTestUser > 0) {
			cardsNodeFromServer = CacheManager.Instance.CacheNode["studentProjectsCards"][_dynSelectedStudentProjectId]["cards"];
			studentNode = CacheManager.Instance.CacheNode["studentProjectsCards"][_dynSelectedStudentProjectId]["student"];
			state = onCompletedState;
			return;
		}

		LoadingTask task = new LoadingTask("Loading Data...");
		//task.DisplayMessage = "Loading Data...";
		//task.CanCancel = true;
		////task.OnLoadingCompleted += DataLoaded;
		GameManager.Instance.LoadingTasks.Add (task);

		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();
		data ["studentProjectId"] = _dynSelectedStudentProjectId;
		
		ParseCloud.CallFunctionAsync<string>("Teacher_Get_SpecificStudentProject", data)
			.ContinueWith(t => {
				if (t.IsFaulted) {
					Debug.LogError("Fail Teacher_Get_SpecificStudentProject");
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
					string result = t.Result;
					JSONNode node = JSON.Parse(result);
					Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] RenewSpecificStudentProject() result: " + result);
					//CacheManager.Instance.HandleTeacherSpecificProjectSpecificStudentProject(_dynSelectedStudentProjectId, node);
					
					JSONNode cardsNode = node["cards"];
					JSONNode langsNode = node["langs"];

					// combine langs node to cards node
					CacheManager.CombineCardsAndLangs(cardsNode, langsNode);
					cardsNodeFromServer = cardsNode;			// <-- main cards info

					////////////////////////////////////////////////////////
					// addition info
					JSONNode studentProject = node["studentProject"];
					//string studentProjectId = studentProject["objectId"].Value;
					
					JSONNode project = studentProject["project"];
					string projectId = project["objectId"].Value;
					
					JSONNode course = node["course"];
					string courseId = course["objectId"].Value;
					
					studentNode = studentProject["student"];
					string studentId = studentNode["objectId"].Value;
					
					if (!string.IsNullOrEmpty(projectId) && !string.IsNullOrEmpty(studentId))
						CacheManager.Instance.CacheNode ["project-" + projectId + "-submitted-list"][studentId] = studentProject;
					if (project != null && !string.IsNullOrEmpty(projectId)) {
						LocalData.Instance.Data ["my-projects"][projectId] = project;
						LocalData.Instance.Save();
					}
					if (!string.IsNullOrEmpty(courseId) && !string.IsNullOrEmpty(studentId))
						CacheManager.Instance.CacheNode  ["course"] [courseId]["students"][studentId] = studentNode;
					// course
					if (!string.IsNullOrEmpty(courseId)) {
						LocalData.Instance.Data ["my-course"][courseId] = course;
						LocalData.Instance.Save();
					}
				}
				task.Loading = false;
				state = onCompletedState;
			});
	}

	private void SyncLocalUpdatedCardsToServer (List<Card> toBeUpload) {
		StartCoroutine(SyncUploadCardsRecordedComment ());
		Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] SyncLocalUpdatedCardsToServer()");
		if (LocalData.Instance.IsTestUser > 0) {
			return;
		}
		
		LoadingTask task = new LoadingTask("Loading Data...");
		//task.DisplayMessage = "Loading Data...";
		//task.CanCancel = true;
		GameManager.Instance.LoadingTasks.Add (task);
		
		Dictionary<string, object> data = new Dictionary<string, object>();
		data["timestamp"] = GameManager.GetCurrentTimestemp();
		
		// JSONNode card to Dictionary
		Dictionary<string, object> cards = new Dictionary<string, object>();
		foreach (Card card in toBeUpload) {
			Debug.Log ("Upload " + card.GetDefaultDisplayName());
			Dictionary<string, string> cardDict = new Dictionary<string, string>();
			JSONNode json = card.toJSON();
			foreach (string key in json.GetKeys()) {
				cardDict[key] = json[key].Value;
			}
			
			cards[card.objectId] = cardDict;
		}
		
		data["cards"] = cards;
		data["studentProjectId"] = _dynSelectedStudentProjectId;
		
		ParseCloud.CallFunctionAsync<string>("Teacher_BatchSaveCardsComment", data)
			.ContinueWith(t => {
				if (t.IsFaulted) {
					Debug.LogError("Fail Teacher_BatchSaveCardsComment");
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
					JSONNode node = JSON.Parse(result);
					Debug.Log ("[Teacher_Course_ViewStudentProjectSubmitted] SyncLocalUpdatedCardsToServer() result: " + result);
					RenewSpecificStudentProject (state);
					CacheManager.Instance.HandleFeaturedCards(node["featuredCards"], node["langs"]);
					
					// clear the list tha are updated, avoid duplicated update
					_newlyCommented.Clear();
					_dynSaved = true;
				}
				task.Loading = false;
				//state = PageState.DATA_RETRIEVED;
				// no need to reload table, because only want to update the cache
			});
	}

	///*Add some functions to save and load the sound record comments
	//Kit Wong 21/3/2017
	int taskCount;

	IEnumerator SyncGetCardsRoutine(){
		Debug.Log ("Start get card routine");
		recordCmtBtn.GetComponent<Button> ().enabled = false;
		Color oriColor = recordCmtBtn.GetComponent<Image> ().color;
		recordCmtBtn.GetComponent<Image> ().color = Color.gray;
		taskCount = 0;
		for (int index = 0; index < _btnTableCell.Count; index++) {
			StartCoroutine (GetCardRoutine (index));
		}
		yield return new WaitUntil (() => taskCount >= _btnTableCell.Count);
		Debug.Log ("Card sync complete");
		IndicateRecordCommentIsDoneOrNot ();
		recordCmtBtn.GetComponent<Image> ().color = oriColor;
		recordCmtBtn.GetComponent<Button> ().enabled = true;

		InitializeShareIcon ();
		EventCurrentRowPlaySound ();
	}

	IEnumerator GetCardRoutine(int index){
		Debug.Log ("_btnTableCell.Count: " + _btnTableCell.Count);
		string soundName = cardObjIdOnCell [index] + "-teacher-comment.wav";
		string soundPath = Path.Combine (LocalData.Instance.DirectoryPath, soundName);
		string fileUrl = "";
		bool queryDone = false;
		Debug.Log ("Start card query! card id: " + cardObjIdOnCell [index]);
		ParseObject stdCard = ParseObject.CreateWithoutData("Card",cardObjIdOnCell [index]);
		stdCard.FetchAsync ().ContinueWith (t => {
			Debug.Log ("task done");
			if (!t.IsFaulted) {
					Debug.Log ("task is not fault");
					Debug.Log("Card id: " + stdCard.Get<string>("cardId"));
					if(stdCard.ContainsKey("commentSound")){
					var soundParseFile = (ParseFile)stdCard.Get<ParseFile> ("commentSound");
					Debug.Log ("task is still not fault");
						if (soundParseFile != null) {
							fileUrl = soundParseFile.Url.AbsoluteUri;
							Debug.Log ("Sound File url: " + fileUrl);
						}else{
							Debug.Log ("No sound comment");
						}
					}
					if(stdCard.ContainsKey("toShare")){
						bool toShare = stdCard.Get<bool>("toShare");
						cardInCellIsShared[index] = toShare;
						Debug.Log("Cell " + index + " toShare: " + toShare.ToString());
					}
				Debug.Log ("task is still not fault");
			} else {
				Debug.Log ("Failed to get sound comment! Error: " + t.Exception.Message);
			}
			queryDone = true;
		});

		yield return new WaitUntil (() => queryDone == true);

		Debug.Log ("Query done");
		if (!fileUrl.Equals("")) {
			Debug.Log ("Directory: " + soundPath);
			if (!File.Exists (soundPath)) {
				Debug.Log ("Directory doesnt exist! Get to download it!");
				var commentSoundRequest = new WWW(fileUrl);
				yield return commentSoundRequest;
				File.WriteAllBytes (soundPath, commentSoundRequest.bytes);
				Debug.Log ("Sound file downloaded to " + soundPath);
			}
			WAV wav = new WAV (soundPath);
			Debug.Log ("WAV created!");
			AudioClip audioClip = AudioClip.Create ("temp", wav.SampleCount, 1, wav.Frequency, false);
			audioClip.SetData (wav.LeftChannel, 0);
			_btnTableCell [index].GetComponent<AudioSource> ().clip = audioClip;
			taskCount++;
		} else {
			Debug.Log ("No sound comment!");
			taskCount++;
		}
	}

	private void DownloadCompletedHandler (object sender, System.ComponentModel.AsyncCompletedEventArgs  e)
	{
		client.Dispose();
		client = null;
		soundCmtDownloadDone = true;
	}

	IEnumerator SyncUploadCardsRecordedComment(){

		if (modifiedSoundCommentIndex.Count <= 0 && modifiedToShareIndex.Count <= 0) {
			yield return null;
		}
		int taskCount = 0;
		int taskTotal = 0;

		NavigationBar.Instance.syncInProgress = true;
		for(int i=0;i< _btnTableCell.Count;i++) {
			bool hvChangedSound = modifiedSoundCommentIndex.Contains (i);
			bool hvChangedToShare = modifiedToShareIndex.Contains (i);
			if (hvChangedSound || hvChangedToShare) {
				taskTotal++;
			}else{
				continue;
			}
			Debug.Log ("Card modified! Uploading...");
			AudioSource audioToSave = _btnTableCell [i].GetComponent<AudioSource> ();
			bool toSave = false;
			string soundName = cardObjIdOnCell [i] + "-teacher-comment.wav";
			string soundPath = Path.Combine (LocalData.Instance.DirectoryPath, soundName);
			if (audioToSave.clip != null) {
				SavWav.Save (soundPath, audioToSave.clip);
				Debug.Log ("Sound file saved to " + soundPath);
				toSave = true;
			}
				
			FileStream fileStream = null;
			ParseFile fileSound = null;
			if (hvChangedSound) {
				fileStream = new FileStream (soundPath, FileMode.Open);
				fileSound = new ParseFile (soundName, fileStream);
			}

			ParseObject cardToSave = ParseObject.CreateWithoutData("Card",cardObjIdOnCell [i]);
			if (hvChangedToShare) {
				cardToSave ["toShare"] = cardInCellIsShared [i];
			}
			cardToSave.FetchAsync ().ContinueWith (t => {
				if (!t.IsFaulted) {
					Debug.Log("Card retrieved");
					//Debug.Log("toShare: " + cardToSave["toShare"].ToString());
					if (hvChangedSound) {
						Debug.Log ("Try to save comment sound");
						if (toSave) {
							cardToSave ["commentSound"] = fileSound;
							Debug.Log ("Sound cmt set! obj id:" + cardToSave.ObjectId);
						} else {
							cardToSave ["commentSound"] = null;
							Debug.Log ("No Sound cmt to created on parse!");
						}
						cardToSave ["commentSoundLastUpdatedAt"] = GameManager.GetCurrentTimestemp ();
						Debug.Log ("Timestemp set!");
					}

					cardToSave.SaveAsync ().ContinueWith (t2 => {
						if (!t2.IsFaulted) {
							Debug.Log ("Card uploaded!");
						} else {
							Debug.Log ("Card upload failed! Error: " + t2.Exception.Message);
						}
						fileStream.Close ();
					});
				} else {
					Debug.Log ("Failed to upload sound comment! Error: " + t.Exception.Message);
				}
				taskCount++;
			});
			
		}
		yield return new WaitUntil (() => taskCount == taskTotal);
		soundCmtSyncNotDone = false;
		NavigationBar.Instance.syncInProgress = false;

		modifiedSoundCommentIndex.Clear ();
		modifiedToShareIndex.Clear ();

		Debug.Log ("Upload card routine is done");
	}
	///*Add some functions to save and load the sound record comments
	#endregion
//
//	IEnumerator GetCardRoutine(){
//		Debug.Log ("Start get card routine");
//		recordCmtBtn.GetComponent<Button> ().enabled = false;
//		Color oriColor = recordCmtBtn.GetComponent<Image> ().color;
//		recordCmtBtn.GetComponent<Image> ().color = Color.gray;
//		for (int index = 0; index < _btnTableCell.Count; index++) {
//			Debug.Log ("_btnTableCell.Count: " + _btnTableCell.Count);
//			string soundName = cardObjIdOnCell [index] + "-teacher-comment.wav";
//			string soundPath = Path.Combine (LocalData.Instance.DirectoryPath, soundName);
//			string fileUrl = "";
//			bool queryDone = false;
//			Debug.Log ("Start card query! card id: " + cardObjIdOnCell [index]);
//			//ParseQuery<ParseObject> query = ParseObject.GetQuery ("Card");
//			ParseObject stdCard = ParseObject.CreateWithoutData("Card",cardObjIdOnCell [index]);
//			stdCard.FetchAsync ().ContinueWith (t => {
//				Debug.Log ("task done");
//				if (!t.IsFaulted) {
//					Debug.Log ("task is not fault");
//					Debug.Log("Card id: " + stdCard.Get<string>("cardId"));
//					//if(stdCard["commentSound"] != null){
//
//					if(stdCard.ContainsKey("commentSound")){
//						var soundParseFile = (ParseFile)stdCard.Get<ParseFile> ("commentSound");
//						//ParseFile soundParseFile = (ParseFile)stdCard["commentSound"];
//						Debug.Log ("task is still not fault");
//						if (soundParseFile != null) {
//							fileUrl = soundParseFile.Url.AbsoluteUri;
//							Debug.Log ("Sound File url: " + fileUrl);
//						}else{
//							Debug.Log ("No sound comment");
//						}
//					}
//					if(stdCard.ContainsKey("toShare")){
//						bool toShare = stdCard.Get<bool>("toShare");
//						cardInCellIsShared[index] = toShare;
//						Debug.Log("Cell " + index + " toShare: " + toShare.ToString());
//					}
//					//}
//					Debug.Log ("task is still not fault");
//				} else {
//					Debug.Log ("Failed to get sound comment! Error: " + t.Exception.Message);
//				}
//				queryDone = true;
//			});
//
//			yield return new WaitUntil (() => queryDone == true);
//
//			Debug.Log ("Query done");
//			if (!fileUrl.Equals("")) {
//				//				try {
//				Debug.Log ("Directory: " + soundPath);
//				if (!File.Exists (soundPath)) {
//					soundCmtDownloadDone = false;
//					Debug.Log ("Directory doesnt exist! Get to download it!");
//					client = new WebClient ();
//					client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler (DownloadCompletedHandler);
//					client.DownloadFileAsync (new Uri (fileUrl), soundPath);
//					Debug.Log ("Sound file downloaded to " + soundPath);
//				}else{
//					soundCmtDownloadDone = true;
//					//queryDone = true;
//				}
//				yield return new WaitUntil (() => soundCmtDownloadDone == true);
//				WAV wav = new WAV (soundPath);
//				Debug.Log ("WAV created!");
//				AudioClip audioClip = AudioClip.Create ("temp", wav.SampleCount, 1, wav.Frequency, false);
//				audioClip.SetData (wav.LeftChannel, 0);
//				_btnTableCell [index].GetComponent<AudioSource> ().clip = audioClip;
//				//				} catch (Exception e) {
//				//					Debug.Log ("Error: " +  e.Source + " : " + e.Message);
//				//				}
//			} else {
//				Debug.Log ("No sound comment!");
//			}
//		}
//		Debug.Log ("Sound sync complete");
//		IndicateRecordCommentIsDoneOrNot ();
//		recordCmtBtn.GetComponent<Image> ().color = oriColor;
//		recordCmtBtn.GetComponent<Button> ().enabled = true;
//
//		InitializeShareIcon ();
//	}

//	IEnumerator SyncUploadCardsRecordedComment(){
//		if (modifiedSoundCommentIndex.Count <= 0) {
//			yield return null;
//		}
//		int taskCount = 0;
//		//bool done = false;
//		NavigationBar.Instance.syncInProgress = true;
//		for(int i=0;i< modifiedSoundCommentIndex.Count;i++) {
//			int index = modifiedSoundCommentIndex [i];
//			Debug.Log ("Sound cmt modified! Uploading...");
//			AudioSource audioToSave = _btnTableCell [index].GetComponent<AudioSource> ();
//			bool toSave = false;
//			string soundName = cardObjIdOnCell [index] + "-teacher-comment.wav";
//			string soundPath = Path.Combine (LocalData.Instance.DirectoryPath, soundName);
//			if (audioToSave.clip != null) {
//				SavWav.Save (soundPath, audioToSave.clip);
//				Debug.Log ("Sound file saved to " + soundPath);
//				toSave = true;
//			}
//			FileStream fileStream = new FileStream (soundPath, FileMode.Open);
//			ParseFile fileSound = new ParseFile (soundName, fileStream);
//
//			ParseQuery<ParseObject> query = ParseObject.GetQuery ("Card");
//			query.GetAsync (cardObjIdOnCell [index]).ContinueWith (t => {
//				if (!t.IsFaulted) {
//					ParseObject Card = t.Result;
//					if(toSave) {
//						Card ["commentSound"] = fileSound;
//						Debug.Log("Sound cmt set! obj id:" + Card.ObjectId);
//					}else{
//						Card ["commentSound"] = null;
//						Debug.Log("No Sound cmt to created on parse!");
//					}
//					Card ["commentSoundLastUpdatedAt"] = GameManager.GetCurrentTimestemp ();
//					Debug.Log("Timestemp set!");
//					Card.SaveAsync ().ContinueWith(t2=>{
//						if(!t2.IsFaulted){
//							Debug.Log("Sound cmt uploaded!");
//						}else{
//							Debug.Log("Sound cmt upload failed! Error: " + t2.Exception.Message);
//						}
//						fileStream.Close ();
//					});
//				} else {
//					Debug.Log ("Failed to upload sound comment! Error: " + t.Exception.Message);
//				}
//				//done = true;
//				taskCount++;
//			});
//		}
//		//yield return new WaitUntil (() => done == true);
//		yield return new WaitUntil (() => taskCount == modifiedSoundCommentIndex.Count);
//		soundCmtSyncNotDone = false;
//		NavigationBar.Instance.syncInProgress = false;
//	}
}

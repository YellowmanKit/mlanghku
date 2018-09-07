using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Parse;
using SimpleJSON;
using WWUtils.Audio;

public enum AddCardMode {
	Student_Add,
	Student_Modify,
	Teacher_Add,
	Teacher_Modify
}

public enum TeacherAddCardState {
	IDLE,
	UPLOADING_FILE,
	ALL_FILE_UPLOADED,
	PARSE_SAVING,
	COMPLETED
}

public class Common_AddCard : Page {

	private FileStream fileStream;

	protected CardUploadState		uploadState = CardUploadState.IDLE;

	private AddCardMode				_mode = AddCardMode.Student_Add;
	public NewLangPicker			_newLangPicker = null;
	public LayoutElement			_layoutLangs = null;

	private string					_dynSelectedProjectId = "";
	private string					_dynSelectedCardId = "";

	private Card					_currentCard = null;

	public Text						_txtSubTitle = null;
	public Button					_btnDeleteCard = null;

	public Image					_imgIcon = null;

	private Texture2D				_textureIcon = null;
	private bool					_isImageChanged = false;

	public List<AddCard_LangRow>	_listLangRow = null;
	public Transform				_btnAddLang = null;

	private int 							fileUploadRemain = 99;			// for teacher uplaod cards
	private Dictionary<string, ParseFile> 	files = null;
	private Card							teacherCardToBeUpload = null;
	private LoadingTask 					uploadingTask;

	private bool 							isSlideShow = false;

	bool shouldRemoveTexture = false;

	// Use this for initialization
	void Start () {
		_newLangPicker.EventButtonClose();

		//Kay: Hot-fix for disappeared image
		if (_imgIcon.sprite == null) {
			_imgIcon.sprite = Resources.Load<Sprite> ("no_image");
		}
	}

	void OnDestroy () {
		if (shouldRemoveTexture) {
			Destroy (_textureIcon);
			_textureIcon = null;
		}
	}
	
	// Update is called once per frame
	void Update () {
		// for teacher only
		if (_mode == AddCardMode.Teacher_Add || _mode == AddCardMode.Teacher_Modify) {
			/*
			switch (state) {
			case PageState.READY:
				break;
			case PageState.INIT:
				break;
			case PageState.WAITING_FOR_DATA:
				break;
			case PageState.DATA_RETRIEVED:
				EventTeacherAddCardSuccess();
				state = PageState.END;
				break;
			case PageState.END:
				break;
			}
			*/
			switch(uploadState) {
			case CardUploadState.IDLE:
				break;
			case CardUploadState.UPLOADING_FILE:
				if (fileUploadRemain == files.Values.Count) {
					if (uploadingTask != null)
						uploadingTask.Loading = false;
					uploadState = CardUploadState.ALL_FILE_UPLOADED;
				}
				break;
			case CardUploadState.ALL_FILE_UPLOADED:
				SaveParseRecord ();
				uploadState = CardUploadState.PARSE_SAVING;
				break;
			case CardUploadState.PARSE_SAVING:
				break;
			case CardUploadState.COMPLETED:
				EventTeacherAddCardSuccess();
				uploadState = CardUploadState.IDLE;
				break;
			}
		}
	}

	
	#region Socket
	public override void OnPageShown (Dictionary<string, object> data) {
		
	}
	
	public override	void Init (Dictionary<string, object> data) {
		
		if (data == null || !data.ContainsKey ("projectId")) {
			Debug.LogWarning ("AddCard: Init data no project id");
			return;
		}

		_mode = (AddCardMode) (data["mode"]);
		_dynSelectedProjectId = (string)(data ["projectId"]);
		_dynSelectedCardId = (string)(data ["cardId"]);
		
		if (string.IsNullOrEmpty(_dynSelectedCardId)) {
			Debug.LogWarning("Empty card id");
			
			_dynSelectedCardId = LocalData.CurrentUserId + "-card-" + GameManager.GetCurrentTimestemp();
		}

		string projectTitle = LocalData.Instance.Data["my-projects"] [_dynSelectedProjectId] ["projectTitle"].Value;
		projectTitle = DeprecatedHelper.removeTitleTag (projectTitle);

		_currentCard = null;
		switch(_mode) {
			case AddCardMode.Student_Add:
				break;
			case AddCardMode.Student_Modify:
				if (LocalData.Instance.Data["local-cards"][_dynSelectedProjectId] != null && 
				    LocalData.Instance.Data["local-cards"][_dynSelectedProjectId].GetKeys().Contains(_dynSelectedCardId)) {
					// modify an existing card
					JSONNode cardNode = LocalData.Instance.Data["local-cards"][_dynSelectedProjectId][_dynSelectedCardId];
					_currentCard = new Card(cardNode);
				}
				break;
			case AddCardMode.Teacher_Add:
				break;
			case AddCardMode.Teacher_Modify:
				if (LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId] != null && 
				    LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId].GetKeys().Contains(_dynSelectedCardId)) {
					// modify an existing card
					JSONNode cardNode = LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId][_dynSelectedCardId];
					_currentCard = new FeaturedCard(cardNode);
				}
				break;
		}

		if (_currentCard == null) {
			string navigationTitle = GameManager.Instance.Language.getString("add-card", CaseHandle.FirstCharUpperForEachWord);
			NavigationBar.Instance.SetTitle (navigationTitle);
			
			string subTitle = String.Format(GameManager.Instance.Language.getString("add-card-to-val"), projectTitle);
			_txtSubTitle.text = subTitle;

			// create a new card
			JSONNode node = JSON.Parse("{}");
			node["cardId"] = _dynSelectedCardId;
			_currentCard = new Card(node);

			CardLang lang_zh_w = new CardLang(_currentCard.cardId, "zh_w", null);
			_currentCard.langs["zh_w"] = lang_zh_w;
			_listLangRow[0].Init(_currentCard, lang_zh_w);

			//Slide show data
			if (data.ContainsKey("slideNumber") && data ["slideNumber"] != null) {
				isSlideShow = true;
				int slideNumber = int.Parse ((string)data ["slideNumber"]);
				_currentCard.slideNumber = slideNumber;
			}
			
			_btnDeleteCard.gameObject.SetActive(false);

		} else {
			if (_mode == AddCardMode.Student_Modify && (_currentCard.status == CardStatus.Passed || _currentCard.status == CardStatus.Featured)) {
				Debug.LogWarning("Modify an passed card!");
				GameManager.Instance.AlertTasks.Add (
					new AlertTask(
					GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
					, GameManager.Instance.Language.getString("modify-passed-card", CaseHandle.FirstCharUpper)
					, TextAnchor.MiddleLeft));
				NavigationBar.Instance.PopView();
				return;
			}

			Dictionary<string, CardLang> langs = _currentCard.getLangs ();

			// lang row
			int missing = langs.Count - _listLangRow.Count;
			for (int i = 0; i < missing; i++) {
				AddCard_LangRow copy = Instantiate(_listLangRow[0]);
				copy.transform.SetParent(_listLangRow[0].transform.parent);
				copy.transform.localScale = Vector3.one;
				_listLangRow.Add (copy);
			}

			int idx = 0;
			foreach (CardLang lang in langs.Values) {
				AddCard_LangRow row = _listLangRow[idx++];
				row.Init(_currentCard, lang);
			}
			
			if (_currentCard.isImageExist) {
				Debug.LogWarning("Common_AddCard A");
//				_imgIcon.sprite = DownloadManager.FileToSprite (_currentCard.ImagePath);
				DownloadManager.Instance.AddLinkage(_currentCard.ImagePath, null, _imgIcon);
				_textureIcon = _imgIcon.sprite.texture;
				shouldRemoveTexture = false;
			}

			if (_mode == AddCardMode.Student_Modify) {
				Debug.LogWarning("Common_AddCard A");
				//				_imgIcon.sprite = DownloadManager.FileToSprite (_currentCard.ImagePath);
				DownloadManager.Instance.AddLinkage(_currentCard.ImagePath, null, _imgIcon);
				_textureIcon = _imgIcon.sprite.texture;
				shouldRemoveTexture = false;
			}

			if (_currentCard.HasGraded) {
				_btnDeleteCard.gameObject.SetActive(false);
			} else {
				_btnDeleteCard.gameObject.SetActive(true);
			}


			// title
			string navigationTitle = GameManager.Instance.Language.getString("edit-card", CaseHandle.FirstCharUpperForEachWord);
			NavigationBar.Instance.SetTitle (navigationTitle);
			
			string subTitle = GameManager.Instance.Language.getString("edit-card", CaseHandle.FirstCharUpperForEachWord);
			_txtSubTitle.text = subTitle;

			if (_currentCard.isSlide()) {
				isSlideShow = true;
			}

		}
		FixLangLayout();
		
		//RenewSoundButton();

		if (isSlideShow) {
			_btnAddLang.gameObject.SetActive (false);

			Debug.Log ("Common_AddCard init with slide number: " + _currentCard.slideNumber);
		}
	}
	#endregion
	
	#region Event
	public void EventButtonSelectIcon () {
		ImagePicker.Instance.ShowImagePicker (ProjectIconSelected, isSlideShow);
	}

	private void ProjectIconSelected (Texture2D img) {
		if (img == null) {
			Debug.LogWarning("User cancel");
			return;
		}

		if (shouldRemoveTexture) {
//			Texture2D.DestroyImmediate(_textureIcon, false);
			Destroy(_textureIcon);
		}

		shouldRemoveTexture = true;
		_textureIcon = img;
		/*
		Vector3 offset = Vector3.zero;
		int rot = 90;
		Vector3 tiling = Vector3.zero;
		Matrix4x4 m =  Matrix4x4.TRS (offset, Quaternion.Euler (0, 0, rot), tiling);
		_textureIcon.set
		*/
		//_textureIcon.
		//_textureIcon = TextureRotate.rotateTexture(_textureIcon, 180);

		Sprite sprite = Sprite.Create(_textureIcon, new Rect(0, 0, _textureIcon.width, _textureIcon.height), Vector2.zero);
		_imgIcon.sprite = sprite;
		Debug.LogWarning("Common_AddCard B");

		_isImageChanged = true;

		_currentCard.imageLastUpdatedAt = GameManager.GetCurrentTimestemp();
		
		//img = null;
		//Destroy (img);
		sprite = null;
		img = null;
	}
	
	public void EventButtonRotate (int angle) {
		Texture t = _textureIcon;
		if (angle == -1) {
			_textureIcon = TextureRotate.rotateTextureLeft(_textureIcon);
		} else if (angle == 1) {
			_textureIcon = TextureRotate.rotateTextureRight(_textureIcon);
		}
		if (shouldRemoveTexture) Destroy(t);
		shouldRemoveTexture = true;

		Sprite sprite = Sprite.Create(_textureIcon, new Rect(0, 0, _textureIcon.width, _textureIcon.height), Vector2.zero);
		_imgIcon.sprite = sprite;
		Debug.LogWarning("Common_AddCard C");
		
		_isImageChanged = true;
		_currentCard.imageLastUpdatedAt = GameManager.GetCurrentTimestemp();
		sprite = null;
	}

	public void EventButtonAddLang () {
		_newLangPicker.EventShow(_currentCard);
	}

	public void EventAddLangCompleted (string langKey) {

		AddCard_LangRow copy = Instantiate(_listLangRow[0]);

		copy.transform.SetParent(_listLangRow[0].transform.parent);
		copy.transform.localScale = Vector3.one;
		_listLangRow.Add (copy);

		CardLang lang = new CardLang(_currentCard.cardId, langKey, null);
		copy.Init(_currentCard, lang);
		_currentCard.langs[langKey] = lang;

		FixLangLayout();
	}

	public void EventButtonDeleteLang (GameObject obj){
		string langKey = obj.name;

		if (langKey.Equals("zh_w")) {		// default lang
			return;
		}

		_currentCard.DeleteLang(langKey);

		// remove row
		AddCard_LangRow deleteRow = null;
		foreach (AddCard_LangRow row in _listLangRow) {
			if (row.langKey == langKey) {
				deleteRow = row;
				continue;
			}
		}
		if (deleteRow != null) {
			//_listLangRow.Remove(deleteRow);
			// destroy the game object
			_listLangRow.Remove(deleteRow);
			Destroy(deleteRow.gameObject);
		}
		FixLangLayout();
	}

	private void FixLangLayout () {
		_layoutLangs.preferredHeight = _listLangRow.Count * 250 + 155;

		// sort the langs
		CardLang[] sortedLangs = _currentCard.LangsInOrder;
		for (int i = 0; i < sortedLangs.Length; i++) {
			CardLang lang = sortedLangs[i];

			foreach (AddCard_LangRow row in _listLangRow) {
				if (row.name.Equals(lang.langKey)) {
					row.transform.SetSiblingIndex(i);
					break;
				}
			}
		}

		_btnAddLang.SetAsLastSibling();
	}

	public void EventButtonDelete () {
		JSONNode record = LocalData.Instance.Data["local-cards"][_dynSelectedProjectId][_dynSelectedCardId];

		Card card = new Card(record);
		if (card.HasGraded) {
			GameManager.Instance.AlertTasks.Add (
				new AlertTask(
				GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
				, GameManager.Instance.Language.getString("delete-graded-card", CaseHandle.FirstCharUpper)
				, TextAnchor.MiddleLeft));
			return;
		}

		record["lastUpdatedAt"] = GameManager.GetCurrentTimestemp().ToString();
		if (!string.IsNullOrEmpty(_currentCard.objectId)) {
			record["objectId"] = _currentCard.objectId;
		}

		LocalData.Instance.DeleteLocalCard(record);

		Dictionary<string, object> data = new Dictionary<string, object>();
		data["action"] = "sync";

		NavigationBar.Instance.PopView(data);
	}

	public void EventButtonSubmit () {
		if (string.IsNullOrEmpty(_dynSelectedProjectId)) {
			Debug.LogError("No _dynSelectedProjectId");
			return;
		}

		// no matter teacher / student, image is necessary
		if (_textureIcon == null) {
			GameManager.Instance.AlertTasks.Add (
				new AlertTask(
				GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
				, "- " + GameManager.Instance.Language.getString("photo-is-necessary", CaseHandle.FirstCharUpper)
				, TextAnchor.MiddleLeft));
			return;
		}

		foreach (AddCard_LangRow langRow in _listLangRow) {
			langRow.ApplyToCardLang();
		}
		
		JSONNode record = _currentCard.toJSON();
		record["projectId"] = _dynSelectedProjectId;
		record["lastUpdatedAt"] = GameManager.GetCurrentTimestemp().ToString();

		//Added by Kit
		record["authorRealName"] = LocalData.Instance.Data["user"]["realName"].Value;
		Debug.Log ("Author name : " + LocalData.Instance.Data ["user"] ["realName"].Value);
		//Added by Kit

		if (_mode == AddCardMode.Student_Add || _mode == AddCardMode.Student_Modify) {
			// is origin exist?
			if (_currentCard.status == CardStatus.Passed || _currentCard.status == CardStatus.Featured) {
				GameManager.Instance.AlertTasks.Add (
					new AlertTask(
					GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
					, GameManager.Instance.Language.getString("modify-passed-card", CaseHandle.FirstCharUpper)
					, TextAnchor.MiddleLeft));
				return;
			}
			NavigationBar.Instance.syncInProgress = true;
			// save image file, only student will store the image locally
			Debug.LogWarning("_isImageChanged: "+_isImageChanged+" "+_currentCard.ImagePath);
			if (_isImageChanged) {
				string photoPath = _currentCard.ImagePath;
				File.WriteAllBytes(photoPath, _textureIcon.EncodeToJPG());
				Debug.LogWarning("File.Exists "+File.Exists(_currentCard.ImagePath));
				record["imageLastUpdatedAt"] = GameManager.GetCurrentTimestemp().ToString();
			}


			Card originCard = new Card(LocalData.Instance.Data["local-cards"][_dynSelectedProjectId][_dynSelectedCardId]);

			Debug.Log ("cards: " + _dynSelectedProjectId + " - " + _dynSelectedCardId);
			//Debug.Log (LocalData.Instance.Data["local-cards"][_dynSelectedProjectId][_dynSelectedCardId].ToString());

			Dictionary<string, object> popViewData = null;
			if (_currentCard.Equals(originCard) && !_isImageChanged) {
				// nothing changed?
				// then do nothing, no need to save
			} else {
				// some changes, write to file
				record["objectId"] = "";
				foreach (AddCard_LangRow langRow in _listLangRow) {
					langRow.WriteToFile();
				}
				record["status"].AsInt = 0;
				LocalData.Instance.AddLocalCard(record);
				popViewData = new Dictionary<string, object>();
				popViewData["action"] = "sync";
			}
			
			NavigationBar.Instance.PopView(popViewData);
		} else if (_mode == AddCardMode.Teacher_Add || _mode == AddCardMode.Teacher_Modify) {
			// check enough data
			// all field must be entered
			int errorCount = 0;
			string error = "";
			// texture
			if (_textureIcon == null) {
				errorCount++;
				error += "\n- " + GameManager.Instance.Language.getString("photo-is-necessary", CaseHandle.FirstCharUpper);
			}

			foreach (AddCard_LangRow row in _listLangRow) {
				if (!row.IsCompleted()) {
					errorCount++;
					error += "\n- " + String.Format(GameManager.Instance.Language.getString("lang-incompleted"), row.cardLang.langKey);
				}
			}

			if (errorCount > 0) {
				error = error.Remove(error.IndexOf("\n"),"\n".Length);
				GameManager.Instance.AlertTasks.Add (
					new AlertTask(
					GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
					, error
					, TextAnchor.MiddleLeft));
				return;
			}
			NavigationBar.Instance.syncInProgress = true;

			Card originCard = new FeaturedCard(LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId][_dynSelectedCardId]);
			
			Card newCard = new FeaturedCard(record);
			if (_isImageChanged) {
				string photoPath = newCard.ImagePath;
				File.WriteAllBytes(photoPath, _textureIcon.EncodeToJPG());
				record["imageLastUpdatedAt"] = GameManager.GetCurrentTimestemp().ToString();
			}

			if (_currentCard.Equals(originCard) && !_isImageChanged) {
				// nothing changed?
				// then do nothing, no need to save
				EventTeacherAddCardSuccess();
			} else {
				newCard.objectId = "";
				// some changes, write to file
				foreach (AddCard_LangRow langRow in _listLangRow) {
					langRow.cardLang.SetFeaturedCard();
					langRow.WriteToFile();
				}
				
				UploadCard(newCard);
			}


		}
		//Debug.LogWarning ("=========");
		//Debug.LogWarning (record.ToString());
		//Debug.Log ("5. " + _currentCard.toJSON().ToString());

	}

	private void EventTeacherAddCardSuccess () {
		Dictionary<string, object> popViewData = new Dictionary<string, object>();
		popViewData["action"] = "reload";
		
		NavigationBar.Instance.PopView(popViewData);
	}

	#endregion
	
	#region Parse
	//---------------------------------------------------------------------------
	// v1.3 TODO : double check why only teacher will upload the card immediatly,
	// but student will store the card locally (and upload in Student_Project.cs)?
	//---------------------------------------------------------------------------
	private void UploadCard (Card card)
	{
		if (LocalData.Instance.IsTestUser == 2) {
			// teacher
			card.objectId = "card" + GameManager.GetCurrentTimestemp();
			
			JSONNode featuredCardsNode = LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId];
			if (featuredCardsNode == null)
				featuredCardsNode = JSON.Parse("{}");

			featuredCardsNode[card.objectId] = card.toJSON();
			
			LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId] = featuredCardsNode;
			LocalData.Instance.Save();

			EventTeacherAddCardSuccess();
			return;
		}

		teacherCardToBeUpload = card;

		uploadState = CardUploadState.UPLOADING_FILE;

		files = new Dictionary<string, ParseFile>();

		if (uploadingTask != null) {
			uploadingTask.Loading = false;
			uploadingTask = null;
		}

		uploadingTask = new LoadingTask("Uploading File...");
		//uploadingTask.DisplayMessage = "Uploading File...";
		//uploadingTask.CanCancel = true;
		GameManager.Instance.LoadingTasks.Add (uploadingTask);
		
		//byte[] dataImage = _textureIcon.EncodeToJPG();
		//ParseFile fileImage = new ParseFile(card.ImageName, dataImage);
		
		Debug.Log (card.ImagePath);
		string cardId = card.cardId;
		if (card.isDeleted) {
			fileUploadRemain = 0;	// no need to upload files
		} else {
			// validation
			if (!card.isImageExist) {
				// no image file
				Debug.LogError("Card don't have enough info: Should be checked before");
				fileUploadRemain = -99;
				return;
			}
			// check langs sound file
			foreach (CardLang lang in card.getLangs().Values) {
				if (!lang.isSoundExist) {
					// no sound file
					Debug.LogError("Card don't have enough info: Should be checked before");
					fileUploadRemain = -99;
					return;
				}
			}
			
			fileUploadRemain = 0;
			
			// check for image upload first
			if (_isImageChanged) {
				byte[] byteImage = File.ReadAllBytes (card.ImagePath);
				ParseFile fileImage = new ParseFile(card.ImageName, byteImage);
				
				fileUploadRemain++;
				UploadFile(cardId, card.ImageName, fileImage);
			}
			// check for lang sound files
			
			foreach (AddCard_LangRow langRow in _listLangRow) {
				if (!langRow.isSoundChanged) {
					continue;
				}

				CardLang lang = langRow.cardLang;
				fileStream = new FileStream(lang.SoundPath, FileMode.Open);
				ParseFile fileSound = new ParseFile(lang.SoundName, fileStream);

				fileUploadRemain++;
				UploadFile(cardId, lang.SoundName, fileSound);
				//fileStream.Close();
			}
			
		}
	}

	private void UploadFile (string cardId, string filename, ParseFile file)
	{
		file.SaveAsync ().ContinueWith(t1 => {
			if (t1.IsFaulted || t1.IsCanceled) {
				NavigationBar.Instance.syncInProgress = false;
				foreach(var e in t1.Exception.InnerExceptions) {
					ParseException parseException = (ParseException) e;
					Debug.Log (parseException.Code + ": " + parseException.Message);
				}
				fileUploadRemain = -99;
				GameManager.Instance.AlertTasks.Add (
					new AlertTask(
					GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
					, GameManager.Instance.Language.getString("file-upload-failed", CaseHandle.FirstCharUpper)
					, TextAnchor.MiddleLeft));
				uploadState = CardUploadState.IDLE;
			} else {
				files[filename] = file;
				//Debug.Log ("Finish upload : " + file.Url.AbsoluteUri);
				if (filename.EndsWith(".wav")) {
					LocalData.Instance.Data["sound"][filename]["url"] = file.Url.AbsoluteUri;
					LocalData.Instance.Save();
				} else if (filename.EndsWith(".jpg")) {
					LocalData.Instance.Data["image"][filename]["url"] = file.Url.AbsoluteUri;
					LocalData.Instance.Save();
				}
			}
			fileStream.Close();
		});
	}

	private void SaveParseRecord () 
	{
		//Debug.Log ("SubmitData");
		if (fileUploadRemain == -99) {
			GameManager.Instance.AlertTasks.Add (
				new AlertTask(
				GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
				, GameManager.Instance.Language.getString("file-upload-failed", CaseHandle.FirstCharUpper)
				, TextAnchor.MiddleLeft));
			return;
		}

		LoadingTask task = new LoadingTask("Loading Data...");
		//task.DisplayMessage = "Loading Data...";
		//task.CanCancel = true;
		////task.OnLoadingCancel += onCancelled;
		GameManager.Instance.LoadingTasks.Add (task);

		// Card to Dictioanry
		Dictionary<string, string> cardDict = new Dictionary<string, string>();
		Dictionary<string, object> langs = new Dictionary<string, object>();

		JSONNode cardJson = teacherCardToBeUpload.toJSON();
		foreach (string key in cardJson.GetKeys()) {
			if (key.Equals("langs")) {
				string[] langKeys = new string[cardJson[key].Count];
				int i = 0;
				foreach (JSONNode node in cardJson["langs"].Childs) {
					langKeys[i++] = node["langKey"].Value;
				}
				cardDict["langs"] = string.Join(",", langKeys);
			} else {
				cardDict[key] = cardJson[key].Value;
			}
		}

		// langs
		if (cardJson.GetKeys().Contains("langs")) {
			foreach (JSONNode langNode in cardJson["langs"].Childs) {
				Debug.Log ("Upload " + langNode["langKey"]);
				Dictionary<string, string> langDict = new Dictionary<string, string>();
				foreach (string key in langNode.GetKeys()) {
					langDict[key] = langNode[key].Value;
				}
				langs[teacherCardToBeUpload.cardId + "-" + langNode["langKey"]] = langDict;
			}
		}
		
		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();
		data ["projectId"] = _dynSelectedProjectId;

		data ["card"] = cardDict;
		data ["langs"] = langs;
		data ["files"] = files;


		ParseCloud.CallFunctionAsync<string>("Teacher_AddCard", data).ContinueWith(t => {
			NavigationBar.Instance.syncInProgress = false;
			if (t.IsFaulted) {
				Debug.LogError("Fail Teacher_AddCard");
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
				uploadState = CardUploadState.IDLE;
			} else {
				NavigationBar.Instance.SetConnection(true);
				string result = t.Result;
				Debug.Log ("Teacher_AddCard: " + result);
				JSONNode node = JSON.Parse(result);
				Debug.Log (node.ToString());

				
				//state = PageState.DATA_RETRIEVED;
				uploadState = CardUploadState.COMPLETED;
			}
			task.Loading = false;
		});

	}
	#endregion
}

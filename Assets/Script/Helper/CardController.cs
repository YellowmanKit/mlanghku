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

public class CardController : MonoBehaviour {

	public GameObject				playCommentBtn;

	private bool 					soundCmtDownloadDone;
	private WebClient				client;

	private Animator				_ani = null;

	public Card						card = null;
	//private bool					_isFeatured = false;

	public Image					_imgPhoto = null;
	public Text						_txtAuthor = null;
	public Text						_txtRef = null;
	public Text						_txtPage = null;

	//Added by Kit 15/3/2017
	public Text						_txtLastUpdated = null;

	public List<ViewCard_LangRow>	_langRows = null;
	public GameObject				_langRowScrollBar = null;

	public Image					_imgBadge = null;
	public Sprite[]					_spriteBadge = null;

	public GameObject				_panelComment = null;
	public GameObject				_btnShowComment = null;
	public Text						_txtComment = null;

	private float					_selfDestroyTimer = 0.5f;
	private bool					_selfDestory = false;

	private AudioSource				_audio = null;

	internal bool					IsCreatedByMe {
		get {
			return card.IsCreatedByMe;
		}
	}

	// Use this for initialization
	void Start () {
		//_ani = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
		if (_selfDestory) {
			_selfDestroyTimer -= Time.deltaTime;
			if (_selfDestroyTimer <= 0)
				Destroy(gameObject);
		}
	}

	void OnDestroy() {
//		Destroy(_imgPhoto.sprite.texture);
	}

	#region Event
	
	public void Init (Card _card, AudioSource audio, int currentPage, int totalPage, bool isFeaturedMode, Card slideShowIndexCard = null) {
		card = _card;
		//_isFeatured = isFeatured;
		_audio = audio;
		_ani = GetComponent<Animator>();
		_panelComment.SetActive(false);
		
		_txtPage.text = currentPage + " / " + totalPage;

		string objectId = string.IsNullOrEmpty(card.objectId) ? "-" : card.objectId;

//		if (card.isImageExist) {
//			_imgPhoto.sprite = DownloadManager.FileToSprite (card.ImagePath);
//		} else {
//			_imgPhoto.sprite = DownloadManager.Instance.spriteLoading;
//			if (!string.IsNullOrEmpty(card.imageUrl)) {
//				DownloadManager.Instance.AddLinkage(card.ImagePath, card.imageUrl, _imgPhoto);
//			}
//		}

		_imgPhoto.sprite = DownloadManager.Instance.spriteLoading;
		DownloadManager.Instance.AddLinkage(card.ImagePath, card.imageUrl, _imgPhoto);

		foreach (ViewCard_LangRow row in _langRows) {
			row.gameObject.SetActive(false);
		}

		Dictionary<string, CardLang> langs = card.getLangs ();

		// enough rows for lang?
		int missing = langs.Count - _langRows.Count;
		for (int i = 0; i < missing; i++) {
			ViewCard_LangRow copy = Instantiate(_langRows[0]);
			copy.transform.SetParent(_langRows[0].transform.parent);
			copy.transform.localScale = Vector3.one;
			_langRows.Add (copy);
		}

		int idx = 0;
		foreach (CardLang lang in langs.Values) {
			ViewCard_LangRow row = _langRows[idx++];
			//----------------------------------------------------------------
			// v1.3 swap these two lines so that I can StartCoroutine() inside 
			// ViewCard_LangRow to check whether the text is too long
			//----------------------------------------------------------------
			row.gameObject.SetActive(true);
			row.Init(card.cardId, lang, this);
			//----------------------------------------------------------------
		}

		// sort the langs
		CardLang[] sortedLangs = card.LangsInOrder;
		for (int i = 0; i < sortedLangs.Length; i++) {
			CardLang lang = sortedLangs[i];
			
			foreach (ViewCard_LangRow row in _langRows) {
				if (row.name.Equals(lang.langKey)) {
					row.transform.SetSiblingIndex(i);
					break;
				}
			}
		}

		if (_langRows.Count <= 3) {
			_langRowScrollBar.SetActive(false);
		} else {
			_langRowScrollBar.SetActive(true);
		}

		_txtRef.text = "ref: " + card.cardId + " / " + objectId;


//		if (isFeaturedMode) {
//
//			//----------------------------------------------------------
//			// v1.3 TODO: add authorRealName to Cloudcode
//			//----------------------------------------------------------
//			//Debug.Log("isFeatureMode: " + isFeaturedMode.ToString() + " card:[" + card.cardId + "] authorId=[" + card.authorId + "] authorName=[" + card.authorName + "] authorRealName=[" + card.authorRealName);
//			//----------------------------------------------------------
//
//			if (string.IsNullOrEmpty(card.authorRealName)) {
//				_txtAuthor.gameObject.SetActive(false);
//			} else {
//				_txtAuthor.gameObject.SetActive(true);
//				_txtAuthor.text = string.Format(GameManager.Instance.Language.getString("author-string", CaseHandle.FirstCharUpper)
//				                                , card.authorRealName);
//			}
//		} else {
//			_txtAuthor.gameObject.SetActive(false);
//		}

		Card tarCard = card.slideNumber>=0 ? slideShowIndexCard : card;

		if (tarCard != null && tarCard.HasGraded) {
			_imgBadge.gameObject.SetActive (true);
			if (isFeaturedMode) {
				if (IsCreatedByMe) {
					_imgBadge.sprite = _spriteBadge [3];
				} else {
					_imgBadge.gameObject.SetActive (false);
				}
				_btnShowComment.SetActive (false);
			} else {
				if (card.status == CardStatus.Passed) {
					_imgBadge.sprite = _spriteBadge [0];
				} else if (card.status == CardStatus.Featured) {
					_imgBadge.sprite = _spriteBadge [2];
				} else if (card.status == CardStatus.Failed) {
					_imgBadge.sprite = _spriteBadge [1];
				}

//				if (tarCard.HasComment) {
//					Debug.Log ("Card has comment");
//					_txtComment.text = tarCard.CommemtToString ();
//					_btnShowComment.SetActive (true);
//				} else {
//					Debug.Log ("Card has no comment");
//					_btnShowComment.SetActive (false);
//				}
			}
			if (tarCard.HasComment) {
				Debug.Log ("Card has comment");
				_txtComment.text = tarCard.CommemtToString ();
				_btnShowComment.SetActive (true);
			} else {
				Debug.Log ("Card has no comment");
				_btnShowComment.SetActive (false);
			}

		} else {
			if (_imgBadge != null)
				_imgBadge.gameObject.SetActive (false);
			_btnShowComment.SetActive (false);
		}

		//Added By Kit to display last-updated date-time on cards
		//Debug.Log(isFeaturedMode);
		//Debug.Log("realname: " + card.authorRealName);
		var timeStemp = TimeSpan.FromSeconds((double)card.lastUpdatedAt);
		var startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		//HK timezone is utc+8
		var lastUpdateTime = startTime + timeStemp + TimeSpan.FromHours(8);

		if (_txtLastUpdated != null) {
			_txtLastUpdated.text = lastUpdateTime.ToString ();
		}

		StartCoroutine (GetCommentSoundRoutine ());
		StartCoroutine (GetAuthorRealName ());
		//Added By Kit

	}

	IEnumerator GetCommentSoundRoutine(){
		Debug.Log ("Start get sound routine");
		string soundName = card.objectId + "-teacher-comment.wav";
		string soundPath = Path.Combine (LocalData.Instance.DirectoryPath, soundName);
		string fileUrl = "";
		bool queryDone = false;
		int soundLastUpdateLocal = PlayerPrefs.GetInt("" + soundName + "-last-updated-at");
		int soundLastUpdateOnParse = 0;
		ParseQuery<ParseObject> query = ParseObject.GetQuery ("Card");
		query.GetAsync (card.objectId).ContinueWith (t => {
			if (!t.IsFaulted) {
				ParseObject Card = t.Result;
				soundLastUpdateOnParse = Card.Get<int>("commentSoundLastUpdatedAt");
				Debug.Log("soundLastUpdateOnParse: " + soundLastUpdateOnParse);
				var soundParseFile = Card.Get<ParseFile> ("commentSound");
				if (soundParseFile != null) {
					fileUrl = soundParseFile.Url.AbsoluteUri;
				}
			} else {
				Debug.Log ("Failed to get sound comment! Error: " + t.Exception.Message);
			}
			queryDone = true;
		});
		yield return new WaitUntil (() => queryDone == true);
		if (fileUrl != "") {
			//				try {
			//Debug.Log ("Directory: " + soundPath);
			if (!File.Exists (soundPath) || soundLastUpdateOnParse != soundLastUpdateLocal) {
				soundCmtDownloadDone = false;
				Debug.Log ("Comment sound need to be download!");
				client = new WebClient ();
				client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler (DownloadCompletedHandler);
				client.DownloadFileAsync (new Uri (fileUrl), soundPath);
				Debug.Log ("Sound file downloaded to " + soundPath);
				PlayerPrefs.SetInt ("" + soundName + "-last-updated-at", soundLastUpdateOnParse);
			}else{
				soundCmtDownloadDone = true;
			}
			yield return new WaitUntil (() => soundCmtDownloadDone == true);
			WAV wav = new WAV (soundPath);
			//Debug.Log ("WAV created!");
			AudioClip audioClip = AudioClip.Create ("temp", wav.SampleCount, 1, wav.Frequency, false);
			audioClip.SetData (wav.LeftChannel, 0);
			playCommentBtn.GetComponent<AudioSource> ().clip = audioClip;
			playCommentBtn.SetActive (true);

			//				} catch (Exception e) {
			//					Debug.Log ("Error: " +  e.Source + " : " + e.Message);
			//				}
		} else {
			playCommentBtn.SetActive (false);
			//Debug.Log ("No parse file!");
		}
	}

	IEnumerator GetAuthorRealName(){
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

	private void DownloadCompletedHandler (object sender, System.ComponentModel.AsyncCompletedEventArgs  e)
	{
		client.Dispose();
		client = null;
		soundCmtDownloadDone = true;
	}
	
	public void EventButtonShowComments () {
		_panelComment.SetActive(true);
	}
	public void EventButtonHideComments () {
		_panelComment.SetActive(false);
	}

	public void EventPlaySound (AudioClip clip) {
		_audio.Stop();
		_audio.clip = clip;
		_audio.Play();
	}
	/*
	public void AniInitOut (bool visible) {
		if (_ani == null)
			Debug.Log ("_ani is null");
		else {
			_ani.SetTrigger("InitOut");
			_ani.SetBool("visible", visible);
		}
	}

	public void OnRightHandSide (bool visible) {
		if (_ani == null)
			Debug.Log ("_ani is null");
		else
			_ani.SetBool("visible", visible);
	}
*/
	public void SetAniTrigger (string trigger) {
		if (_ani == null)
			Debug.Log ("_ani is null");
		else {
			_ani.SetTrigger(trigger);
		}
	}

	public void SelfDestroy () {
		_selfDestory = true;
	}

	public void RefreshCardRes () {
		_audio.clip = null;
		
		card.SafelyDeleteFiles();
	}
	#endregion
}

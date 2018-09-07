using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System;
using System.IO;
using Parse;

public class SyncManager : MonoBehaviour 
{
	private  static SyncManager _instance = null;
	internal static SyncManager Instance { get { return _instance; } }

	private Dictionary<string, ParseCardUploadTask>  syncTasks = null;
	
	void Awake ()
	{
		Debug.Log ("[SyncManager] Awake()");
		if (_instance == null) {
			DontDestroyOnLoad (gameObject);
			_instance = this;
			Init();
		} else if (_instance != this) {
			Destroy (gameObject);
		}
	}

	void Init() {
		Debug.Log ("[SyncManager] Init()");
		syncTasks = new Dictionary<string, ParseCardUploadTask>();
	}

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		if (syncTasks != null) {
			foreach (ParseCardUploadTask t in syncTasks.Values) {
				t.Update ();
			}
		}
	}

	public ParseCardUploadTask SyncProject (string studentProjectId, string projectId, ParseCardUploadTask.EventHandler OnLoadingCompleted) {
		if (syncTasks.ContainsKey(projectId)) {
			ParseCardUploadTask task = syncTasks[projectId];
			task.requestForSync++;
			task.OnLoadingCompleted += OnLoadingCompleted;
			return task;
		} else {
			ParseCardUploadTask task = new ParseCardUploadTask (studentProjectId, projectId);
			task.OnLoadingCompleted += OnLoadingCompleted;
			task.requestForSync++;
			syncTasks[projectId] = task;
			return task;
		}
	}
}

public enum CardUploadState {
	IDLE,
	PREPARING,
	DATA_RECEIVED,
	UPLOADING_FILE,
	ALL_FILE_UPLOADED,
	PARSE_SAVING,
	COMPLETED
}

public class ParseCardUploadTask : MonoBehaviour {

	
	public delegate void EventHandler();
	public event EventHandler OnLoadingCompleted;

	public int						requestForSync = 0;

	protected CardUploadState		state = CardUploadState.IDLE;
	
	protected string				projectId = null;
	protected string				studentProjectId = null;
	protected List<Card> 			cardToBeUpload= null;
	protected List<Card> 			imageToBeUpload= null;
	protected List<CardLang> 		langToBeUpload= null;
	protected List<Card> 			requireFileUpload= null;
	protected Dictionary<string, ParseFile>	files = null;
	protected Dictionary<string, int>		cardUploadRemain = null;
	
	//private DateTime				beginAt;
	
	public ParseCardUploadTask (string _studentProjectId, string _projectId) {
		//beginAt = DateTime.Now;

		studentProjectId = _studentProjectId;
		projectId = _projectId;

	}
	
	public void Update () {
		//Debug.Log (state.ToString());
		switch(state) {
		case CardUploadState.IDLE:
			if (requestForSync > 0) {
				state = CardUploadState.IDLE;
				InitSync();
			}
			break;
		case CardUploadState.PREPARING:
			break;
		case CardUploadState.DATA_RECEIVED:
			UploadCards();
			break;
		case CardUploadState.UPLOADING_FILE:
			bool completed = true;
			foreach (int remain in cardUploadRemain.Values) {
				if (remain > 0) {
					completed = false;
					break;
				}
			}
			if (completed) {
				state = CardUploadState.ALL_FILE_UPLOADED;
			}
			break;
		case CardUploadState.ALL_FILE_UPLOADED:
			SaveParseRecord ();
			state = CardUploadState.PARSE_SAVING;
			break;
		case CardUploadState.PARSE_SAVING:
			break;
		case CardUploadState.COMPLETED:
			triggerCompleted();
			state = CardUploadState.IDLE;
			break;
		}
		
		
	}
	
	public void InitSync () {
		requestForSync = 0;
		
		JSONNode projectNode = LocalData.Instance.Data["my-projects"][projectId];
		Project project = new Project(projectNode);

		//////////////////////////////////////////////////////////////////
		// check which one need to upload
		
		// compare timestamp, which cards are updated or new (require them having all information
		JSONNode localCards = LocalData.Instance.Data["local-cards"][projectId];
		
		
		Dictionary<string, Card> parseCards = new Dictionary<string, Card>();
		JSONNode onlineCardsNode = CacheManager.Instance.CacheNode["my-projectcards-" + projectId];
		
		if (onlineCardsNode != null && onlineCardsNode.Count > 0) {
			foreach (JSONNode onlineCardNode in onlineCardsNode.Childs) {
				Card onlineCard = new Card(onlineCardNode);
				parseCards[onlineCard.cardId] = onlineCard;
			}
		}

		// any cards need to upload?
		cardToBeUpload = new List<Card>();
		imageToBeUpload = new List<Card>();
		langToBeUpload = new List<CardLang>();
		requireFileUpload = new List<Card>();
		foreach (JSONNode localCard in localCards.Childs) {
			Card card = new Card(localCard);
			if (parseCards.ContainsKey(card.cardId)) {
				Card onlineCard = parseCards[card.cardId];
				
				if (onlineCard.status == CardStatus.Passed || onlineCard.status == CardStatus.Featured) {
					continue;
				}
				
				// update a card
				if (card.lastUpdatedAt > onlineCard.lastUpdatedAt) {
					// local one is more updated
					if (card.isDeleted) {
						cardToBeUpload.Add (card);
					} else if (card.isReadyToUpload) {
						//Debug.Log (card.nameEng + ": local one is more update " + card.lastUpdatedAt + " vs " + onlineCard.lastUpdatedAt);
						//Debug.Log (card.nameEng + ": image " + card.imageLastUpdatedAt + " vs " + onlineCard.imageLastUpdatedAt);
						//Debug.Log (card.nameEng + ": sound " + card.soundLastUpdatedAt + " vs " + onlineCard.soundLastUpdatedAt);
						cardToBeUpload.Add (card);
						
						// image?
						if (card.imageLastUpdatedAt > onlineCard.imageLastUpdatedAt) {
							//Debug.Log ("Upload image");
							imageToBeUpload.Add (card);
							if (!requireFileUpload.Contains(card))
								requireFileUpload.Add(card);
						}

						// local new sound?
						// local remove some sound?
						foreach (CardLang lang in card.langs.Values) {
							CardLang onlineMathchingLang = onlineCard.GetLangByKey(lang.langKey);
							if (onlineMathchingLang == null) {
								// online does not have this lang
								// upload it
								if (!langToBeUpload.Contains(lang))
									langToBeUpload.Add (lang);
								if (!requireFileUpload.Contains(card))
									requireFileUpload.Add(card);
							} else if (lang.soundLastUpdatedAt > onlineMathchingLang.soundLastUpdatedAt) {
								// online one is too old, uplaod it
								if (!langToBeUpload.Contains(lang))
									langToBeUpload.Add (lang);
								if (!requireFileUpload.Contains(card))
									requireFileUpload.Add(card);
							}
						}
					}
				}
			} else {
				// online don't have this card
				if (card.isReadyToUpload && !project.dueDatePassed) {
					// only able to upload new card when dueDate not passed
					cardToBeUpload.Add (card);
					imageToBeUpload.Add (card);
					foreach (CardLang lang in card.langs.Values) {
						if (!langToBeUpload.Contains(lang))
							langToBeUpload.Add (lang);
					}
					if (!requireFileUpload.Contains(card))
						requireFileUpload.Add(card);
					//Debug.Log (card.nameEng + ": online data no this record " + card.cardId);
				}
			}
		}

		#if UNITY_EDITOR
		Debug.Log ("[SyncManager] InitSync() cardToBeUpload.Count: " + cardToBeUpload.Count + " imageToBeUpload.Count: " + imageToBeUpload.Count + " soundToBeUpload.Count: " + langToBeUpload.Count);
		#endif
		
		
		// check which one need to upload
		//////////////////////////////////////////////////////////////////
		files = new Dictionary<string, ParseFile>();
		cardUploadRemain = new Dictionary<string, int>();
		
		if (cardToBeUpload.Count == 0) {
			clearDeletedCard ();
			state = CardUploadState.COMPLETED;
			return;
		}
		
		state = CardUploadState.DATA_RECEIVED;
	}

	private void clearDeletedCard() {
		JSONNode localCards = LocalData.Instance.Data["local-cards"][projectId];
		foreach (JSONNode card in localCards.Childs) {
			if (int.Parse(card["isDeleted"]) > 0) {
				LocalData.Instance.Data ["local-cards"] [projectId].Remove (card["cardId"]);
				LocalData.Instance.Save ();
			}
		}
	}
	
	private void UploadCards() {
//		Debug.LogWarning("UploadCards");
		if (cardToBeUpload.Count > 0) {
			if (requireFileUpload.Count > 0) {
				Debug.Log (cardToBeUpload.Count + " cards' files need to upload");
				foreach (Card card in requireFileUpload) {
					Debug.Log ("loop: upload " + card.GetDefaultDisplayName());
					UploadCardFiles(card);
				}
				state = CardUploadState.UPLOADING_FILE;
			} else {
				Debug.Log ("no file upload, only need to update");
				state = CardUploadState.ALL_FILE_UPLOADED;
			}
		} else {
			Debug.Log ("All selected card is up to date");
			clearDeletedCard ();
			state = CardUploadState.COMPLETED;
		}
	}
	
	private void UploadCardFiles (Card card) {
//		Debug.LogWarning("UploadCardFiles");
		// !!!
		string cardId = card.cardId;
		if (card.isDeleted) {
			cardUploadRemain[cardId] = 0;	// no need to upload files
		} else {
			// validation
			if (imageToBeUpload.Contains (card) && !card.isImageExist) {
				// no image file
				Debug.LogError("Card don't have enough info: Should be checked before");
				cardUploadRemain[cardId] = -99;
				return;
			}
			// check langs sound file
//			foreach (CardLang lang in card.langs.Values) {
//				if (langToBeUpload.Contains (lang) && !lang.isSoundExist) {
//					// no sound file
//					Debug.LogError("Card don't have enough info: Should be checked before");
//					cardUploadRemain[cardId] = -99;
//					return;
//				}
//			}

			cardUploadRemain[cardId] = 0;

			//List<ParseFile> toBeUplaod = new List<ParseFile>();

			// check for image upload first
			if (imageToBeUpload.Contains (card)) {
				byte[] byteImage = File.ReadAllBytes (card.ImagePath);
				ParseFile fileImage = new ParseFile(card.ImageName, byteImage);
				//toBeUplaod.Add (fileImage);

				cardUploadRemain[cardId]++;
				UploadFile(cardId, card.ImageName, fileImage);
			}
			// check for lang sound files
			foreach (CardLang lang in card.langs.Values) {
				if (langToBeUpload.Contains(lang) && lang.SoundName != "") {
					try {
						// !!!
						Debug.Log ("Waiting " + lang.langKey + " " + lang.name);
						/*
						using (FileStream fileStream = new FileStream(lang.SoundPath, FileMode.Open)) {
							Debug.Log ("use " + lang.langKey + " " + lang.name);
							// do work here.
							ParseFile fileSound = new ParseFile(lang.SoundName, fileStream);
							//toBeUplaod.Add (fileSound);
							
							cardUploadRemain[cardId]++;
							UploadFile(cardId, lang.SoundName, fileSound);
						}
						*/

						//--------------------------------------------------------------------
						//v1.3 Will using File.ReadAllBytes better than FileStream?
						// Because FileStream seems lock the file and cannot be played!
						//--------------------------------------------------------------------
						byte[] fileData = File.ReadAllBytes(lang.SoundPath);
						ParseFile fileSound = new ParseFile(lang.SoundName, fileData);
						//FileStream fileStream = new FileStream(lang.SoundPath, FileMode.Open);
						//ParseFile fileSound = new ParseFile(lang.SoundName, fileStream);
						//--------------------------------------------------------------------
						////toBeUplaod.Add (fileSound);
						
						cardUploadRemain[cardId]++;
						UploadFile(cardId, lang.SoundName, fileSound);

						//fileStream.Close();

					} catch (Exception e) {
						cardUploadRemain[cardId] = -99;
						Debug.LogError ("SyncManger (UploadCardFiles): " + lang.langKey + ": " + lang.name + ": " + e.Message);
						break;
					}
				}
			}
			/*
			if (toBeUplaod.Count == 0) {
				Debug.LogWarning("File without doing anything?" + card.toJSON());
				return;
			}
			*/
			//cardUploadRemain[cardId] = toBeUplaod.Count;

		}
	}

	private void UploadFile (string cardId, string filename, ParseFile file) {
		Debug.Log ("Upload " + filename);
		file.SaveAsync ().ContinueWith(t1 => {
			if (t1.IsFaulted || t1.IsCanceled) {
				
				foreach(var e in t1.Exception.InnerExceptions) {
					ParseException parseException = (ParseException) e;
					Debug.Log (parseException.Code + ": " + parseException.Message);
				}
				cardUploadRemain[cardId] = -99;
			} else {
				files[filename] = file;
				cardUploadRemain[cardId] = cardUploadRemain[cardId] - 1;
				Debug.Log ("Finish upload : " + file.Url.AbsoluteUri);
				if (filename.EndsWith(".wav")) {
					LocalData.Instance.Data["sound"][filename]["url"] = file.Url.AbsoluteUri;
					LocalData.Instance.Save();
				} else if (filename.EndsWith(".jpg")) {
					LocalData.Instance.Data["image"][filename]["url"] = file.Url.AbsoluteUri;
					LocalData.Instance.Save();
				}
			}
		});
	}
	
	
	private void SaveParseRecord () {
		Dictionary<string, object> data = new Dictionary<string, object>();
		data["timestamp"] = GameManager.GetCurrentTimestemp();
		data["projectId"] = projectId;
		data["studentProjectId"] = studentProjectId;
		
		// JSONNode card to Dictionary
		Dictionary<string, object> cards = new Dictionary<string, object>();
		Dictionary<string, object> langs = new Dictionary<string, object>();
		foreach (Card card in cardToBeUpload) {
			string cardId = card.cardId;
			
			bool fileOK = true;
			// does this card require file upload?
			if (requireFileUpload.Contains(card)) {
				// success?
				if (cardUploadRemain.ContainsKey(cardId)) {
					fileOK = cardUploadRemain[cardId] == 0;
				} else {
					Debug.LogWarning ("cardSuccess no key : " + cardId);
					fileOK = false;
				}
			}
			
			if (fileOK) {
				Dictionary<string, string> cardDict = new Dictionary<string, string>();
				JSONNode cardJson = card.toJSON();
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
				cards[cardId] = cardDict;

				if (cardJson.GetKeys().Contains("langs")) {
					foreach (JSONNode langNode in cardJson["langs"].Childs) {
						Dictionary<string, string> langDict = new Dictionary<string, string>();
						foreach (string key in langNode.GetKeys()) {
							langDict[key] = langNode[key].Value;
						}
						langs[cardId + "-" + langNode["langKey"]] = langDict;
					}
				}


			} else {
				Debug.LogWarning ("File not ok for " + card.GetDefaultDisplayName());
			}
		}
		
		data["cards"] = cards;
		data["langs"] = langs;
		data["files"] = files;
		/*
		foreach (string key in files.Keys) {
			Debug.Log ("+================== " + key);
		}
		*/
		//NavigationBar.Instance.syncInProgress = true;
		ParseCloud.CallFunctionAsync<string>("Student_BatchSaveCards", data)
			.ContinueWith(t => {
				NavigationBar.Instance.syncInProgress = false;
				//syncInProgress = false;
				if (t.IsFaulted) {
					Debug.LogError("Fail BatchSaveCards");
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

					Debug.Log ("Student_BatchSaveCards: " + node.ToString());

					// save the objectId to local (update localData)
					CacheManager.Instance.HandleStudentSpecificMyProjectSubmittedData(node["studentProject"]);
					CacheManager.Instance.HandleStudentMySpecificProjectCards(projectId, node["cards"], node["langs"]);
					
					Debug.Log ("BatchSaveCards: " + result);
					
				}

				clearDeletedCard ();
				state = CardUploadState.COMPLETED;

			});
		
	}
	
	public void triggerCompleted () {
		if (OnLoadingCompleted != null) {
			OnLoadingCompleted();
		}
	}
}
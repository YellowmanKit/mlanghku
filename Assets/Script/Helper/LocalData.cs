using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Parse;
using SimpleJSON;

public class LocalData : MonoBehaviour {
	
	private  static LocalData _instance = null;
	internal static LocalData Instance { get { return _instance; } }
	
	private  JSONNode _data = null;
	internal JSONNode Data { get { return _data; } }

	private float _dynWriteTimer = 0;

	private System.Threading.Thread mainThread = null;

	private  int _dynIsTestUser = 0;
	internal int IsTestUser {
		get {
			if (mainThread.Equals(System.Threading.Thread.CurrentThread)) {
				_dynIsTestUser = PlayerPrefs.GetInt("testIdentity", 0);
				return _dynIsTestUser;
			} else {
				return _dynIsTestUser;
			}
		}
	}

	static internal string CurrentUserId {
		get {
			if (LocalData.Instance.IsTestUser == 0) {
				if (ParseUser.CurrentUser == null) {
					Debug.LogWarning("[LocalData] Unknown User id");
					return "-";
				} else {
					return ParseUser.CurrentUser.ObjectId;
				}
			} else {
				return "testuser" + LocalData.Instance.IsTestUser;
			}
		}
	}

	private  string persistentDataPath = "";
	internal string DirectoryPath {
		get {
			string directioryPath = "";
			if (IsTestUser == 0) {
				string userId = ParseUser.CurrentUser.ObjectId;
				directioryPath = Path.Combine(persistentDataPath, userId);
				
				if (!Directory.Exists (directioryPath)) {
					Directory.CreateDirectory(directioryPath);	
				}
			} else {
				string folderName = "testData";
				directioryPath = Path.Combine(persistentDataPath, folderName);
				
				if (!Directory.Exists (directioryPath)) {
					Directory.CreateDirectory(directioryPath);	
				}
			}
			
			return directioryPath;
		}
	}
	
	internal JSONNode 		   UserNode { get { return _data["user"]; } }
	internal ParseUserIdentity UserIdentity {
		get {
			int identity = UserNode == null ? -1 : UserNode ["identity"].AsInt;
			return (ParseUserIdentity)identity;
		}
	}
	

	void Awake ()
	{
		Debug.Log ("[LocalData] Awake()");
		if (_instance == null) {
			DontDestroyOnLoad (gameObject);
			_instance = this;
			persistentDataPath = Application.persistentDataPath;
			mainThread = System.Threading.Thread.CurrentThread;
			//Init();
		} else if (_instance != this) {
			Destroy (gameObject);
		}
	}
	
	public void Reset () {
		Debug.Log ("[LocalData] Reset()");
		//_data = JSON.Parse ("{}");
		Init ();
	}
	
	private void Init() {
		Debug.Log ("[LocalData] Init()");
		persistentDataPath = Application.persistentDataPath;
		LoadDataFromFile ();
	}
	
	private void LoadDataFromFile() 
	{
		Debug.Log ("[LocalData] LoadDataFromFile()");
		if (IsTestUser == 0) 
		{
			// v1.3 2017/01/10
			// Assume the user data are already downloaded to userId/data.json
			// Load the data into _data
			if (ParseUser.CurrentUser == null) {
				_data = JSON.Parse ("{}");
			} else {
				string path = Path.Combine (DirectoryPath, "data.json");
				Debug.Log ("[LocalData] LoadDataFromFile(): Using path: " + path);
				if (File.Exists (path)) {
					string json = File.ReadAllText (path);
					_data = JSON.Parse (json);
				} else {
					_data = JSON.Parse ("{}");
					Debug.LogWarning ("[LocalData] LoadDataFromFile(): Data file [" + path + "] not existed");
				}
				
				// if I found which folder I am using, remove the other
				string[] directories = Directory.GetDirectories(persistentDataPath);
				foreach (string directory in directories){
					if (directory.Equals(DirectoryPath)) {
						//Debug.Log ("[LocalData] LoadDataFromFile(): keep in use directory : " + directory);
					} else {
						Debug.Log("[LocalData] LoadDataFromFile(): Remove previous directory : " + directory);
						SafelyDelete(directory);
					}
				}
			}
		} else {
			// connect two test user data
			// exchange the cards
			ConnectFakeUserData ();

			// load file
			string path = Path.Combine (DirectoryPath, "data_" + IsTestUser + ".json");
			Debug.Log ("[LocalData] LoadDataFromFile(): Using data: " + path);
			if (File.Exists (path)) {
				string json = File.ReadAllText (path);
				_data = JSON.Parse (json);
			} else {
				_data = JSON.Parse ("{}");
				Debug.LogWarning ("[LocalData] LoadDataFromFile(): Data file [" + path + "] not existed");
			}

			// load fake cache
			path = Path.Combine (DirectoryPath, "cache.json");
			if (File.Exists (path)) {
				string fakeCacheJson = File.ReadAllText (path);
				CacheManager.Instance.Init (JSON.Parse(fakeCacheJson));
			} else {
				Debug.LogWarning("[LocalData] LoadDataFromFile(): No fake cache found");
			}
		}
	}

	private void ConnectFakeUserData ()
	{
		//-------------------------------------------------------------------------------------------------
		// v1.3 2017/01/10
		// The files Resource/testData/data_1.json and data_2.json are updated independently, depends
		// on which test account the player used.  After a new login,
		// this function setup linkage between them and update the cache.json (for students new cards)
		// and data_1.json (in case teacher updated project info and features).
		// Because teacher's info if not affected by the "linkage", there is no need to update data_2.json.
		//-------------------------------------------------------------------------------------------------
		Debug.Log ("[LocalData] ConnectFakeUserData()");
		string path_1 = Path.Combine (DirectoryPath, "data_1.json");
		string path_2 = Path.Combine (DirectoryPath, "data_2.json");
		JSONNode studentData = null;
		JSONNode teacherData = null;

		if (File.Exists (path_1)) {
			string json = File.ReadAllText (path_1);
			studentData = JSON.Parse (json);
		} else {
			studentData = JSON.Parse ("{}");
			Debug.LogWarning ("[LocalData] ConnectFakeUserData() Data file for test user not existed");
		}
		if (File.Exists (path_2)) {
			string json = File.ReadAllText (path_2);
			teacherData = JSON.Parse (json);
		} else {
			teacherData = JSON.Parse ("{}");
			Debug.LogWarning ("[LocalData] ConnectFakeUserData() Data file for test user not existed");
		}
		
		JSONNode cacheData = null;
		string cachePath = Path.Combine (DirectoryPath, "cache.json");
		if (File.Exists (cachePath)) {
			string fakeCacheJson = File.ReadAllText (cachePath);
			cacheData = JSON.Parse(fakeCacheJson);
		} else {
			cacheData = JSON.Parse("{}");
			Debug.LogWarning("[LocalData] ConnectFakeUserData() No fake cache found");
		}

		JSONNode studentUserData = studentData["user"];

		string studentId = studentUserData["objectId"];

		//-----------------------------------------------------------------------
		// Student may add cards if player try TestAsStudent account.
		// So we need to copy the information to cache
		// student local-cards -> Cache
		//-----------------------------------------------------------------------
		cacheData["studentProjectsCards"] = JSON.Parse("{}");
		foreach (string projectId in studentData["local-cards"].GetKeys()) {
			JSONNode projectCards = studentData["local-cards"][projectId];
			
			int numOfFeaturedCards = 0;
			int numOfStudentAttention = 0;
			int numOfSubmittedCard = 0;
			int numOfTeacherAttention = 0;
			List<string> cardIdToBeRemove = new List<string>();
			foreach (JSONNode card in projectCards.Childs) {
				Card c = new Card(card);
				if (!c.AllLangReady){
					//---------------------------------------------------------------------------------
					// v1.3 2017/01/10 TODO: why need AllLangReady?  What is in real case?
					//---------------------------------------------------------------------------------
					Debug.Log ("[LocalData] ConnectFakeUserData(): card " + c.cardId + " does not have all langs ready -- remove it"); 
					cardIdToBeRemove.Add (c.cardId);
					continue;
				}
				if (card["isDeleted"].AsInt == 1)
					continue;

				numOfSubmittedCard++;
				if (card["status"].AsInt == 0) {
					numOfTeacherAttention++;
				} else if (card["status"].AsInt == 1) {
				} else if (card["status"].AsInt == 2) {
					numOfStudentAttention++;
				}
			}
			foreach (string cardId in cardIdToBeRemove) {
				projectCards.Remove(cardId);
			}
			studentUserData["numOfFeaturedCards"].AsInt = numOfFeaturedCards;
			studentUserData["numOfSubmittedCards"].AsInt = numOfSubmittedCard;
			studentUserData["numOfTeacherAttention"].AsInt = numOfTeacherAttention;
			//Debug.Log(studentUserData.ToString());

			JSONNode studentProjectNode = JSON.Parse("{}");
			studentProjectNode["numOfFeaturedCards"].AsInt = numOfFeaturedCards;
			studentProjectNode["numOfStudentAttention"].AsInt = numOfStudentAttention;
			studentProjectNode["numOfSubmittedCard"].AsInt = numOfSubmittedCard;
			studentProjectNode["numOfTeacherAttention"].AsInt = numOfTeacherAttention;
			studentProjectNode["student"] = studentUserData;

			// v1.3 2017/01/10 TODO: so why generate a random "studentProjectId" here?  Is it the same in real case?
			string studentProjectId = UnityEngine.Random.Range(10000,99999) + "sp" + UnityEngine.Random.Range(10000,99999);
			studentProjectNode["objectId"] = studentProjectId;

			cacheData["studentProjectsCards"][studentProjectId] = JSON.Parse("{}");
			cacheData["studentProjectsCards"][studentProjectId]["projectId"] = projectId;
			cacheData["studentProjectsCards"][studentProjectId]["cards"] = projectCards;
			cacheData["studentProjectsCards"][studentProjectId]["student"] = studentUserData;

			cacheData["project-" + projectId + "-submitted-list"][studentId] = studentProjectNode;
		}

		cacheData ["course"]["coursea"]["students"]["testuser1"] = studentUserData;

		//-------------------------------------------------------------------------------------
		// teacher -> student
		// Projects and feature cards info may change if player try TestAsTeacher account.
		// So we need to copy project info from teacher (data_2.json) to student (data_1.json).
		//-------------------------------------------------------------------------------------
		studentData["my-projects"] = teacherData["my-projects"];
		studentData["featured-cards"] = teacherData["featured-cards"];

		// save
		File.WriteAllText (cachePath, cacheData.ToString());
		File.WriteAllText (path_1, studentData.ToString());
	}
	
	private void WriteToFile ()
	{
		Debug.Log ("[LocalData] WriteToFile()");
		if (IsTestUser == 0) {
			if (ParseUser.CurrentUser == null) {
				Debug.Log("[LocalData] WriteToFile() User is not login");
			} else {
				string path = Path.Combine (DirectoryPath, "data.json");
				string data = _data.ToString();
				File.WriteAllText (path, data);
			}
		} else {
			string path = Path.Combine (DirectoryPath, "data_"+ IsTestUser + ".json");
			string data = _data.ToString();
			File.WriteAllText (path, data);
		}
	}

	[ContextMenu("Clear File")]
	public void ClearFile() {
		string path = Path.Combine (DirectoryPath, "data.json");
		File.Delete (path);
	}
	
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (_dynWriteTimer > 0) {
			_dynWriteTimer -= Time.deltaTime;
			if (_dynWriteTimer <= 0) {
				_dynWriteTimer = 0;
				WriteToFile();
			}
		}
	}
	
	public bool CheckImageOutdated (string filename, string url) {
		try {
			if (string.IsNullOrEmpty(url)) {
				Debug.LogWarning("Check outdated: url is empty " + filename);
				return false;
			}
			
			//Debug.Log ("image url is null ? " + _data["image"][filename]["url"] == null);
			if (_data["image"][filename]["url"] != null 
			    && _data["image"][filename]["url"].Value != null 
			    && _data["image"][filename]["url"].Value.Equals(url)) {
				return false;
			}

			string imagePath = Path.Combine(DirectoryPath, filename);
			SafelyDelete(imagePath);
		} catch (Exception e) {
			Debug.LogError(System.Reflection.MethodBase.GetCurrentMethod().Name + " " + e.Message);
		}

		return true;
	}
	/*
	public bool CheckSoundOutdated (string filename, string url) {
		try {
			if (string.IsNullOrEmpty(url)) {
				Debug.LogWarning("Check outdated: url is empty " + filename);
				return false;
			}
			//Debug.Log ("Sound url is null ? " + _data["sound"][filename]["url"] == null);
			if (_data["sound"][filename]["url"] != null 
			    && _data["sound"][filename]["url"].Value != null 
			    && _data["sound"][filename]["url"].Value.Equals(url)) {
				return false;
			}
			
			string soundPath = Path.Combine(DirectoryPath, filename);
			SafelyDelete(soundPath);
		} catch (Exception e) {
			Debug.LogError(System.Reflection.MethodBase.GetCurrentMethod().Name + " " + e.Message);
		}

		return true;
	}
	*/
	
	public void AddLocalCard (JSONNode node) {
		if (!node.GetKeys().Contains("projectId")) {
			Debug.LogError("No project id");
			return;
		} else if (!node.GetKeys().Contains("cardId")) {
			Debug.LogError("No card id");
			return;
		}
		if (LocalData.Instance.IsTestUser > 0) {
			node["objectId"] = "card" + GameManager.GetCurrentTimestemp();
		}

		Debug.Log ("AddLocalCard: " + node.ToString());
		
		string projectId = node["projectId"].Value;
		string cardId = node["cardId"].Value;
		
		_data["local-cards"][projectId][cardId] = node;
		
		Save ();
	}
	
	public void DeleteLocalCard (JSONNode node) {
		
		string projectId = node["projectId"].Value;
		string cardId = node["cardId"].Value;

		node["isDeleted"].AsInt = 1;

		_data["local-cards"][projectId][cardId] = node;
		//_data["local-cards"][projectId].Remove(cardId);

		// file?
		string photoPath = Path.Combine (DirectoryPath, cardId + ".jpg");
		string soundPath = Path.Combine (DirectoryPath, cardId + ".wav");
		SafelyDelete(photoPath);
		SafelyDelete(soundPath);

		if (node.GetKeys ().Contains ("langs") && node ["langs"].GetKeys ().Contains ("#SlideShow")) {
			//a slide of slide show is deleted, check if slide number of other slides should be updated
			//TODO: better OO design
			JSONNode localCardsNode = _data["local-cards"][projectId];
			Card[] cards = JSONCardsToListCards(localCardsNode, false, CardSortBy.Status, SortingOrder.Desc);
			int slideNumber = 0;
			foreach (Card card in cards) {
				if (slideNumber != card.slideNumber) {
					card.slideNumber = slideNumber;
					card.lastUpdatedAt = GameManager.GetCurrentTimestemp ();
					_data["local-cards"][projectId][card.cardId] = card.toJSON();
				}

				++slideNumber;
			}
		}

		Save ();
	}

	public static Card[] JSONCardsToListCards (JSONNode node, bool isFeaturedMode = false, CardSortBy sorting = CardSortBy.Name, SortingOrder order = SortingOrder.Asc) {
		Card[] cards = new Card[node.Count];
		int idx = 0;
		foreach (JSONNode cardNode in node.Childs) {
			Card card = null;
			if (isFeaturedMode) {
				card = new FeaturedCard(cardNode);
			} else {
				card = new Card(cardNode);
			}
			if (!card.isDeleted) {
				cards[idx++] = card;
			}
		}
		if (idx > 0)
			Array.Resize(ref cards, idx);
		else
			cards = new Card[0];

		Card.Sort(cards, sorting, order);

		return cards;
	}
	
	public List<Card> GetLocalCardsFromProject (string projectId) {
		JSONNode localCardsNode = _data["local-cards"][projectId];
		
		List<Card> cards = new List<Card>();
		foreach (JSONNode cardNode in localCardsNode.Childs) {
			Card card = new Card(cardNode);
			if (!card.isDeleted) {
				cards.Add (card);
			}
		}
		return cards;
	}
	
	public List<FeaturedCard> GetFeaturedCardsFromProject (string projectId) {
		JSONNode featuredCardsNode = _data["featured-cards"][projectId];
		
		List<FeaturedCard> cards = new List<FeaturedCard>();
		foreach (JSONNode cardNode in featuredCardsNode.Childs) {
			FeaturedCard card = new FeaturedCard(cardNode);
			if (!card.isDeleted) {
				cards.Add (card);
			}
		}
		return cards;
	}

	public void HandleSaveCards (string projectId, JSONNode savedCards) {
		foreach (JSONNode saveCard in savedCards.Childs) {
			string objectId = saveCard["objectId"];
			string cardId = saveCard["cardId"];
			_data["local-cards"][projectId][cardId]["objectId"] = objectId;

			Debug.Log (_data["local-cards"][projectId][cardId]["nameEng"].Value + " now have an objectId: " + objectId);
		}
		Save ();
	}

	public void UpdateUserNode (JSONNode node) {
		_data["user"] = node;
		Save ();
	}
	
	public void UpdateStudentMyProject (JSONNode node) {
		_data["my-projects"] = node;
		Save ();
	}

	public void UpdateStudentMyCourse (JSONNode node) {
		string courseId = node["objectId"];
		_data ["my-course"][courseId] = node;
		Save ();
	}

	public void UpdateTeacherCourse (JSONNode node) {
		_data ["my-course"] = node;
		Save ();
	}

	/*
	public void UpdateStudentMySpecificProject (JSONNode node) {
		string objectId = node["objectId"].Value;
		_data["my-projects"][objectId] = node;
		Save ();
	}
	*/

	public void Save () {
		_dynWriteTimer = 1f; // delay one second before WriteFile -- but why?
		//Debug.Log (_dynWriteTimer);
	}
	
	public void SafelyDelete (string path) {
		if (path == "") return;
		try {
			Debug.Log("[LocalData] SafelyDelete:[" + path + "]");
			// remove cache url
			string filename = Path.GetFileName(path);
			if (LocalData.Instance.Data != null) {
				if (LocalData.Instance.Data["image"] != null && LocalData.Instance.Data["image"].GetKeys().Contains(filename)) {
					LocalData.Instance.Data["image"].Remove (filename);
					Save ();
				} else if (LocalData.Instance.Data["sound"] != null && LocalData.Instance.Data["sound"].GetKeys().Contains(filename)) {
					LocalData.Instance.Data["sound"].Remove (filename);
					Save ();
				}
			}

			if (File.Exists(path)) {	// delete it, the main function will download the new image
				// remove file
				Debug.Log("[LocalData] SafelyDelete: Delete file " + path);
				File.Delete(path);
			} else if (Directory.Exists(path)) {
				Debug.Log("[LocalData] SafelyDelete: Delete directory " + path);
				Directory.Delete(path, true);
			}
		} catch (Exception e) {
			Debug.LogError(System.Reflection.MethodBase.GetCurrentMethod().Name + " " + e.Message);
		}
	}

	public void CreateTestData (int identity)
	{
		//------------------------------------------------------------------------------------------
		// v1.3 2017/01/10
		// Copy json/image/wav from Resource/testData folder to DirectoryPath to simulate data from server.
		//------------------------------------------------------------------------------------------
		string path1 = Path.Combine(DirectoryPath, "data_1.json");
		string path2 = Path.Combine(DirectoryPath, "data_2.json");
		Debug.Log ("[LocalData] CreateTestData(" + identity.ToString() + "): check path [" + path1 + "] and [" + path2 + "]");

		if (!File.Exists (path1) || !File.Exists (path2)) {
			Debug.Log ("[LocalData] CreateTestData() no path exists - create new files");
			string[] imageName = new string[] {"course-coursea", "police", "project-projecta", "testuser1-card-fireman", "testuser1-card-nurse"};
			string[] soundName = new string[] {"testuser1-card-fireman-en", "testuser1-card-fireman-pth", "testuser1-card-nurse-en"};
			string[] textName = new string[] {"cache", "data_1", "data_2"};
			
			foreach (string fileName in textName) {
				TextAsset data  = Resources.Load("testData/" + fileName) as TextAsset;
				string content = data.text;
				string path = Path.Combine(DirectoryPath, fileName + ".json");
				File.WriteAllText(path, content);
			}
			
			foreach (string fileName in imageName) {
				Texture2D texture = Resources.Load("testData/" + fileName) as Texture2D;//omit extension
				string path = Path.Combine(DirectoryPath, fileName + ".jpg");
				File.WriteAllBytes(path, texture.EncodeToJPG());
			}

			foreach (string fileName in soundName) {
				AudioClip audio = Resources.Load("testData/" + fileName) as AudioClip;//omit extension
				string path = Path.Combine(DirectoryPath, fileName + ".wav");
				SavWav.Save (path, audio);
			}

			//------------------------------------------------------------------------------------------
			// renew the due date
			// v1.3 2017/01/10 :
			// as ConnectFakeUserData() will copy teacher's "my-projects" to student's data_1.json,
			// there is no need to update the data_1.json at this moment.
			//------------------------------------------------------------------------------------------
			JSONNode teacherData = null;

			if (File.Exists (path2)) {
				string json = File.ReadAllText (path2);
				teacherData = JSON.Parse (json);
			} else {
				teacherData = JSON.Parse ("{}");
				Debug.LogWarning ("[LocalData] CreateTestData() Data file for test user not existed");
			}

			System.DateTime dueDate = System.DateTime.Now.AddDays(UnityEngine.Random.Range(7, 14));
			foreach (JSONNode projectNode in teacherData["my-projects"].Childs) {
				projectNode["dueDate"]["iso"] = dueDate.ToString();
			}

			// save
			File.WriteAllText (path2, teacherData.ToString());
			//------------------------------------------------------------------------------------------
		} else {
			// connect them
			Debug.Log ("[LocalData] CreateTestData() path exists - use existing files");
		}
	}
}

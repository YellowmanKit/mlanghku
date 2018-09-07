using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using SimpleJSON;
using Parse;
using System.Threading.Tasks;

public class ImagePicker : MonoBehaviour {
	
	public static ImagePicker _instance = null;
	internal static ImagePicker Instance { get { return _instance; } }
	
	public delegate void EventHandler(Texture2D texture);
	public event EventHandler		_responseHandler = null;

	public GameObject container;
	public GameObject containerFeatured;
	public GameObject				_panelFeatured = null;
	public List<GameObject>			_btnFeaturedCards = null;

	public Texture2D			_debugTexture = null;


	List<Card> featuredCards = new List<Card>();

	public readonly static Queue<System.Action> ExecuteOnMainThread = new Queue<System.Action>();
	
	void Awake () {
		_instance = this;
	}

	void Update() {

		while (ExecuteOnMainThread.Count > 0)
		{
			ExecuteOnMainThread.Dequeue().Invoke();
		}
	}
	
	void OnDestroy () {
		NativeToolkit.OnImagePicked -= ImageSelected;
		NativeToolkit.OnCameraShotComplete -= ImageSelected;

//		int idx = 0;
//		foreach (FeaturedCard card in featuredCards) {
//			// assign the text
//			GameObject btn = _btnFeaturedCards [idx++];
//			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();
//			Destroy(btnImageIcon.sprite.texture);
//		}
	}
	
	// Use this for initialization
	void Start () {
		NativeToolkit.OnImagePicked += ImageSelected;
		NativeToolkit.OnCameraShotComplete += ImageSelected;
		
		RectTransform t = GetComponent<RectTransform>();
		t.anchoredPosition = new Vector2(0, 0);
		
		gameObject.SetActive(false);
	}
	
	public void ShowImagePicker (EventHandler handler) {
		ShowImagePicker (handler, false);
	}

	public void ShowImagePicker (EventHandler handler, bool canChooseFeatured) {
		_responseHandler = handler;
		
		gameObject.SetActive (true);

		if (canChooseFeatured) {
			container.SetActive (false);
			containerFeatured.SetActive (true);
		} else {
			container.SetActive (true);
			containerFeatured.SetActive (false);
		}
	}
	
	public void EventButtonTakePhoto () {
		#if UNITY_EDITOR
		string filePath = EditorUtility.OpenFilePanel("Open Session","E:\\Download","");
		if (string.IsNullOrEmpty(filePath)) {
			return;
		}

		byte[] b = File.ReadAllBytes (filePath);
		Texture2D texture = new Texture2D (1, 1, TextureFormat.RGB24, false);
		texture.LoadImage (b);

		ImageSelected(texture, "");

		#else
		NativeToolkit.TakeCameraShot();
		#endif
	}

	public void EventButtonChooseExisting () {
		#if UNITY_EDITOR
		ImageSelected(_debugTexture, "");
		#else
		NativeToolkit.PickImage();
		#endif
	}

	bool isFeaturedCardLoaded = false;
	int projectToLoadCount;
	LoadingTask loadingTask;

	public void EventButtonChooseFeatured () {
		if (LocalData.Instance.IsTestUser > 0) {
			refreshFeaturedCardPanel ();
			showFeaturedCards ();
			return;
		}

		if (isFeaturedCardLoaded) {
			showFeaturedCards ();
			return;
		} else {
			isFeaturedCardLoaded = true;
		}

		//TODO: create a function in parse cloud code to get all featured cards at once

		IEnumerable<string> projectIDs = LocalData.Instance.Data ["my-projects"].Keys;

		loadingTask = new LoadingTask("Loading Data...");
		//loadingTask.DisplayMessage = "Loading Data...";
		//loadingTask.CanCancel = true;
		GameManager.Instance.LoadingTasks.Add (loadingTask);

		projectToLoadCount = 0;

		foreach (string projectID in projectIDs) {
			++projectToLoadCount;
			loadFeaturedCardForProjectID (projectID);
		}
	}

	void loadFeaturedCardForProjectID(string projectID) {
		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();
		data ["projectId"] = projectID;

		ParseCloud.CallFunctionAsync<string>("Student_Get_SpecificStudentProject", data)
			.ContinueWith(t => {
				ExecuteOnMainThread.Enqueue (() => {
					onLoadProject(t);
				});
			});
	}

	void onLoadProject(Task<string> t) {
		--projectToLoadCount;
		
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
			NavigationBar.Instance.SetConnection(true);
			string result = t.Result;
			JSONNode node = JSON.Parse(result);
			Debug.Log ("onLoadProject: " + result);
//			CacheManager.Instance.HandleStudentSpecificMyProjectSubmittedData(node["studentProject"]);
//			CacheManager.Instance.HandleStudentMySpecificProjectCards(projectID, node["cards"], node["langs"]);
			CacheManager.Instance.HandleFeaturedCards(node["featuredCards"], node["langs"], false);
		}

		if (projectToLoadCount == 0) {
			if (loadingTask != null) loadingTask.Loading = false;

			refreshFeaturedCardPanel ();
			showFeaturedCards ();
		}
	}

	void refreshFeaturedCardPanel() {
		featuredCards.Clear();

		foreach (string projectID in LocalData.Instance.Data["featured-cards"].Keys) {
			JSONNode featruedCardsNode = LocalData.Instance.Data["featured-cards"][projectID];
			Card[] fcs = LocalData.JSONCardsToListCards(featruedCardsNode, true);
			featuredCards.AddRange (fcs);
		}

		// show the cards
		foreach (GameObject btn in _btnFeaturedCards) {
			btn.SetActive(false);
		}

		GameManager.InstantiateChildren (featuredCards.Count, _btnFeaturedCards);
		/*
		int missing = featuredCards.Count - _btnFeaturedCards.Count;
		for (int i = 0; i < missing; i++) {
			GameObject copy = Instantiate (_btnFeaturedCards [0]);
			copy.transform.SetParent (_btnFeaturedCards [0].transform.parent);
			copy.transform.localScale = Vector3.one;

			_btnFeaturedCards.Add (copy);
		}
		*/

		//////////////////////////////////////////////////
		/// Update the button
		int idx = 0;
		foreach (FeaturedCard card in featuredCards) {

			// assign the text
			GameObject btn = _btnFeaturedCards [idx++];
			btn.name = card.cardId;
			btn.transform.FindChild ("Text").GetComponent<Text> ().text = card.GetDefaultDisplayName();
			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();

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
				if (!string.IsNullOrEmpty(card.imageUrl)) {
					btnImageIcon.sprite = DownloadManager.Instance.spriteLoading;
					DownloadManager.Instance.AddLinkage(card.ImagePath, card.imageUrl, btnImageIcon);
				} else {
					btnImageIcon.sprite = DownloadManager.Instance.spriteNoImage;
					Debug.LogWarning("No image file, and no url provided for " + card.GetDefaultDisplayName());
				}
//			}

			Transform badge = btn.transform.FindChild ("img_badge");
			if (ParseUser.CurrentUser != null && card.authorId.Equals(ParseUser.CurrentUser.ObjectId)) {
				badge.gameObject.SetActive(true);
			} else {
				badge.gameObject.SetActive(false);
			}
		}
	}

	public void EventButtonSelectedFeaturedCard (GameObject btn) {
		

		string cardId = btn.name;
		int index = -1;
		for (int i = 0; i < featuredCards.Count; i++) {
			if (cardId.Equals(featuredCards[i].cardId)) {
				index = i;
				break;
			}
		}
		if (index >= 0) {
//			Dictionary<string, object> data = new Dictionary<string, object> ();
//			data ["projectId"] = _dynSelectedProjectId;
//			data ["isFeaturedCard"] = true;
//			data ["json"] = LocalData.Instance.Data["featured-cards"][_dynSelectedProjectId];
//			data ["cardIndex"] = index;
//
//			NavigationBar.Instance.PushView (NavigationView.Common_ViewCard, data);

			Image btnImageIcon = btn.transform.FindChild ("img_icon").GetComponent<Image> ();
			ImageSelected (btnImageIcon.sprite.texture, "");
			_panelFeatured.SetActive (false);
		} else {
			Debug.Log ("Unknown index");
		}
	}

	void showFeaturedCards() {
		_panelFeatured.SetActive (true);
	}

	public void hideFeaturedCards() {
		_panelFeatured.SetActive(false);
	}

	public void EventButtonCancel () {
		if (_responseHandler != null)
			_responseHandler (null);
		gameObject.SetActive (false);
		
	}
	
	private void ImageSelected (Texture2D img, string path) {
		/*
		Debug.Log (img.GetPixels().Length);
		Debug.Log (img.width + " " + img.height);
		img.Resize (16, 16);
		Debug.Log (img.GetPixels().Length);
		Debug.Log (img.width + " " + img.height);
		*/
		//Debug.Log (img.GetPixels().Length);
		//Debug.Log (img.width + " " + img.height);

		// resize?
		int maxPixel = 640;
		if (img.width > maxPixel || img.height > maxPixel) {
			// which one larger?
			float scale = 1;
			if (img.width > img.height) {
				scale = (float)maxPixel / (float)img.width;
			} else {
				scale = (float)maxPixel / (float)img.height;
			}
			//Debug.Log ("Resize " + scale);
			
			int newWidth = Mathf.FloorToInt(img.width * scale);
			int newHeight = Mathf.FloorToInt(img.height * scale);

			// resize
			// !!!
			//img = resizeTexture (img, newWidth, newHeight, 1);
			TextureScale.Bilinear (img, newWidth, newHeight);
		}/* else {
			Debug.Log ("no resize");
		}
		*/

		#if UNITY_ANDROID
		//------------------------------------------------
		// v1.3
		//------------------------------------------------
		Texture t = img;
		img = TextureRotate.rotateTextureRight(img);
		//Destroy(t);
		DestroyImmediate(t,true);
		//------------------------------------------------
		#endif

		//Debug.Log (img.GetPixels().Length);
		//Debug.Log (img.width + " " + img.height);
		/*
		// debug
		// write to file
		Debug.Log ("a: " + img.width + " " + img.height);
		string photoPath = Path.Combine (LocalData.Instance.DirectoryPath, "test.jpg");
		byte[] data =  img.EncodeToJPG();
		File.WriteAllBytes(photoPath, data);
		//
		*/
		
		if (_responseHandler != null)
			_responseHandler (img);
		gameObject.SetActive (false);

		//img = null;
	}

	/*
	Texture2D resizeTexture(Texture2D tex, int width, int height, int algo)	{
		Texture2D resizedImage = new Texture2D(width, height);
		Texture2D bTemp = (Texture2D)Instantiate(tex, Vector3.zero, Quaternion.identity);
		switch (algo)
		{
		case (1): //bilinear
			float fraction_x, fraction_y, one_minus_x, one_minus_y;
			int ceil_x, ceil_y, floor_x, floor_y;
			Color c1 = new Color();
			Color c2 = new Color();
			Color c3 = new Color();
			Color c4 = new Color();
			float red, green, blue;
			
			float b1, b2;
			
			float nXFactor = (float)bTemp.width/(float)width;
			float nYFactor = (float)bTemp.height/(float)height;
			
			for (int x = 0; x < width; ++x)
				for (int y = 0; y < height; ++y)
			{
				// Setup
				floor_x = (int)Mathf.Floor((x * nXFactor));
				floor_y = (int)Mathf.Floor((y * nYFactor));
				ceil_x = floor_x + 1;
				if (ceil_x >= bTemp.width) ceil_x = floor_x;
				
				ceil_y = floor_y + 1;
				if (ceil_y >= bTemp.height) ceil_y = floor_y;
				
				fraction_x = x * nXFactor - floor_x;
				fraction_y = y * nYFactor - floor_y;
				one_minus_x = 1.0f - fraction_x;
				one_minus_y = 1.0f - fraction_y;
				
				c1 = bTemp.GetPixel(floor_x, floor_y);
				c2 = bTemp.GetPixel(ceil_x, floor_y);
				c3 = bTemp.GetPixel(floor_x, ceil_y);
				c4 = bTemp.GetPixel(ceil_x, ceil_y);
				
				// Blue
				
				b1 = (one_minus_x * c1.b + fraction_x * c2.b);
				
				b2 = (one_minus_x * c3.b + fraction_x * c4.b);
				
				blue = (one_minus_y * (float)(b1) + fraction_y * (float)(b2));
				
				// Green
				
				b1 = (one_minus_x * c1.g + fraction_x * c2.g);
				
				b2 = (one_minus_x * c3.g + fraction_x * c4.g);
				
				green = (one_minus_y * (float)(b1) + fraction_y * (float)(b2));
				
				// Red
				
				b1 = (one_minus_x * c1.r + fraction_x * c2.r);
				
				b2 = (one_minus_x * c3.r + fraction_x * c4.r);
				
				red = (one_minus_y * (float)(b1) + fraction_y * (float)(b2));
				resizedImage.SetPixel(x,y, new Color(red, green, blue));
				
			}
			resizedImage.Apply();
			break;
		default:
			Debug.Log("not implemented");
			break;
		}
		
		if (tex != null) {
			Texture2D.DestroyImmediate(tex, false);
		}
		//Debug.Log (resizedImage.width + " " + resizedImage.height);
		return resizedImage;
	}
*/


}

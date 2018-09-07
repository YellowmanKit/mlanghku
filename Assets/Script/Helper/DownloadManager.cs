using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System;
	
public class DownloadManager : MonoBehaviour {
	
	public Sprite					spriteLoading = null;
	public Sprite					spriteNoImage = null;
	
	private static DownloadManager _instance = null;
	internal static DownloadManager Instance { get { return _instance; } }

	public Sprite[]					nationFlags = null;
	
	//public List<DownloadTask> 		pendingTasks = null;
	//public List<DownloadTask> 		downloadTasks = null;
	
	private Dictionary<string, OnlineAsset>		files = null;
	private List<OnlineAsset>					toBeStopDownload = null;
	
	void Awake ()
	{
		Debug.Log ("[DownloadManager] Awake()");
		if (_instance == null) {
			DontDestroyOnLoad (gameObject);
			_instance = this;
			Init();
		} else if (_instance != this) {
			Destroy (gameObject);
		}
	}
	
	void Init() {
		Debug.Log ("[DownloadManager] Init()");
		files = new Dictionary<string, OnlineAsset>();
		toBeStopDownload = new List<OnlineAsset>();
		//pendingTasks = new List<DownloadTask>();
		//downloadTasks = new List<DownloadTask>();
	}

	/*
	public void AddTask (DownloadTask task) {
		//pendingTasks.Add (task);
	}
	*/

	// download. Once completed, show the image to target
	public void AddLinkage (string filePath, string url, UnityEngine.Object elem, bool saveToLocal = true) 
	{
		if (files.ContainsKey(filePath) && (url == null || url.Equals(files[filePath].url))) {
			// already existed an download, no need to do anything
			// add a new linkage between file and element
			// once download completed, file will set the image to element
			Debug.Log("DownloadManager:AddLinkage exist an download, link the elem only");
			files[filePath].AddLinkage(url, elem);
		} else if (File.Exists(filePath)) {
			Debug.Log("DownloadManager:AddLinkage filePath already exist ");
			if (filePath.EndsWith(".wav")) {
				files[filePath] = new OnlineAssetAudio(filePath);
				files[filePath].url = url;
				files[filePath].AddLinkage(url, elem);
			} else if (filePath.EndsWith(".jpg") || filePath.EndsWith(".png")) {
				files[filePath] = new OnlineAssetImage(filePath);
				files[filePath].url = url;
				files[filePath].AddLinkage(url, elem);
			} else {
				Debug.LogWarning("Unknown file type for local only storage " + filePath);
			}
		} else if (url != null) {
			Debug.Log("DownloadManager:AddLinkage other case");
			// create new because of url change?
			// if change, remove the existing file and download again (seldom happend, but possible)
			if (files.ContainsKey(filePath)) {
				OnlineAsset oa = files[filePath];
				toBeStopDownload.Add(oa);
				/*
				StopCoroutine( oa.Download() );
				oa.status = OnlineAssetStatus.Completed;
				oa = null;
				*/
			}

			// create new
			if (filePath.EndsWith(".wav")) {
				files[filePath] = new OnlineAssetAudio(filePath, url);
				files[filePath].AddLinkage(url, elem);
			} else if (filePath.EndsWith(".jpg") || filePath.EndsWith(".png")) {
				files[filePath] = new OnlineAssetImage(filePath, url, saveToLocal);
				files[filePath].AddLinkage(url, elem);
			} else {
				Debug.LogWarning("Unknown file type " + filePath);
			}
		}
	}

	// only download (can be sound or image)
	public void AddLinkage (string filePath, string url) {
		if (files.ContainsKey(filePath) && files[filePath].url.Equals(url)) {
			// already existed an download, no need to do anything
		} else {
			// create new because of url change?
			// if change, remove the existing file and download again (seldom happend, but possible)
			if (files.ContainsKey(filePath)) {
				OnlineAsset oa = files[filePath];
				toBeStopDownload.Add (oa);
				/*
				StopCoroutine( oa.Download() );
				oa.status = OnlineAssetStatus.Completed;
				oa = null;
				*/
			}

			// first time seeing this file,
			// create a new download
			if (filePath.EndsWith(".wav")) {
				files[filePath] = new OnlineAssetAudio(filePath, url);
			} else if (filePath.EndsWith(".jpg") || filePath.EndsWith(".png")) {
				files[filePath] = new OnlineAssetImage(filePath, url);
			} else {
				Debug.LogWarning("Unknown file type " + filePath);
			}
		}
	}

	
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (files != null) {
			List<string> keys = new List<string> (files.Keys);
			foreach (string key in keys) {
				OnlineAsset oa = files[key];
				switch (oa.status) {
				case OnlineAssetStatus.Init:
					StartCoroutine( oa.Download() );
					break;
				case OnlineAssetStatus.Downloading:
					break;
				case OnlineAssetStatus.Retrived:
					oa.AfterCompleted();
					break;
				case OnlineAssetStatus.Completed:
					break;
				}
			}

			foreach (OnlineAsset oa in toBeStopDownload) {
				StopCoroutine( oa.Download() );
				oa.status = OnlineAssetStatus.Completed;
			}
			toBeStopDownload.Clear();
		}
	}
	
	public static Texture2D FileToTexture (string filePath)
	{
		if (File.Exists (filePath)) {
			byte[] b = File.ReadAllBytes (filePath);

			Texture2D texture = new Texture2D (1, 1, TextureFormat.RGB24, false);
			texture.name = Path.GetFileName(filePath);

			texture.LoadImage (b);
			
			//Debug.Log (texture.width + " " + texture.height);
			if (texture.width == 8 && texture.height == 8) {
				// fail image?
				b = null;
				texture = null;
				LocalData.Instance.SafelyDelete(filePath);
				return null;
			}

			return texture;
		} else {
			return null;
		}
		
	}

	public static Sprite GetNationFlagByLangKey (string key) {
		if (Instance == null)
			return null;

		int idx = CardLang.LangKeyToIndex(key);
		if (idx >= 0 && idx < Instance.nationFlags.Length) {
			return Instance.nationFlags[idx];
		} else {
			return Instance.spriteNoImage;
		}
	}

	public void Delete (string filePath) {
		if (files.ContainsKey(filePath)) {
			files.Remove(filePath);
		}
	}
}
/*
public class DownloadTask {
	public List<OnlineAsset> 	toBeDownloaded = null;
	internal int Count { get { return toBeDownloaded.Count; } }
	
	public DownloadTask () {
		toBeDownloaded = new List<OnlineAsset> ();
	}
	
	public void AddOnlineAsset (OnlineAsset oa) {
		toBeDownloaded.Add (oa);
	}
	
	internal bool Completed {
		get {
			bool allCompleted = true;
			foreach (OnlineAsset oa in toBeDownloaded) {
				if (oa.completed) {
					if (!oa.afterCompletedTriggered)
						oa.AfterCompleted();
				} else {
					allCompleted = false;
				}
			}
			
			return allCompleted;
		}
	}
}
*/

public enum OnlineAssetStatus {
	Init,
	Downloading,
	Retrived,
	Completed
}

public abstract class OnlineAsset
{
	protected string filePath = "";
	public string url = "";

	public OnlineAssetStatus status = OnlineAssetStatus.Init;

	protected bool saveToLocal = true;

	protected bool afterCompletedTriggered = false;
	protected List<UnityEngine.Object> targets = new List<UnityEngine.Object>();
	
	abstract public IEnumerator Download();
	abstract public void AddLinkage (string _url, UnityEngine.Object target);
	abstract public void AfterCompleted ();
}

public class OnlineAssetImage : OnlineAsset
{
	public Texture2D texture = null;

	public OnlineAssetImage (string _filePath) {
		filePath = _filePath;
		texture = DownloadManager.FileToTexture(_filePath);
		status = OnlineAssetStatus.Completed;
	}
	
	public OnlineAssetImage (string _filePath, string _url, bool _saveToLocal = true) {
		filePath = _filePath;
		url = _url;
		saveToLocal = _saveToLocal;
	}

	public override void AddLinkage (string _url, UnityEngine.Object target) 
	{
		targets.Add (target);

		if (status == OnlineAssetStatus.Completed) {
			if (target is Image) {
				Image img = (Image)target;
				try { // v1.3 added
					Sprite sprite = Sprite.Create (texture, new Rect (0, 0, texture.width, texture.height), Vector2.zero);
					//---------------------------------------------------------------------
					// v1.3 TODO double check whether I need to Destroy the old sprite
					//---------------------------------------------------------------------
					img.sprite = sprite;
				} catch (Exception ex) {
				}
			}
		}
	}
	
	public override IEnumerator Download () 
	{
		status = OnlineAssetStatus.Downloading;
		WWW www = new WWW(url);
		yield return www;

		Debug.Log ("[DownloadManager] DownloadImage Done: " + url);

		if (!string.IsNullOrEmpty (www.error)) {
			Debug.Log (www.error);
		} else { // v1.3 added
			texture = www.texture;
			texture.name = Path.GetFileName(url);

			if (saveToLocal) {
				File.WriteAllBytes(filePath, texture.EncodeToJPG());
				string filename = Path.GetFileName(filePath);
				LocalData.Instance.Data["image"][filename]["url"] = url;
				LocalData.Instance.Save();
			}
		}

		www.Dispose ();
		www = null;
		
		status = OnlineAssetStatus.Retrived;
	}
	
	public override void AfterCompleted () 
	{
		Sprite sprite = null; // v1.3 added

		try { // v1.3 added
			sprite = Sprite.Create (texture, new Rect (0, 0, texture.width, texture.height), Vector2.zero);
		} catch (Exception ex) {
		}

		List<UnityEngine.Object> toBeRemoved = new List<UnityEngine.Object>();
		foreach (UnityEngine.Object elem in targets) {

			if (elem == null) {
				toBeRemoved.Add (elem);
				continue;
			}

			if (elem is Image) {
				Image img = (Image)elem;
				if (img == null) continue;

				//---------------------------------------------------------------------
				// v1.3 TODO double check whether I need to Destroy the old sprite
				//---------------------------------------------------------------------
				if (sprite != null) { img.sprite = sprite; } // v1.3 added
			}
		}
		foreach (UnityEngine.Object obj in toBeRemoved) {
			targets.Remove(obj);
		}
		
		status = OnlineAssetStatus.Completed;
	}
}

public class OnlineAssetAudio : OnlineAsset 
{
	WebClient client = null;

	public OnlineAssetAudio (string _filePath) {
		filePath = _filePath;
		status = OnlineAssetStatus.Completed;
	}

	public OnlineAssetAudio (string _filePath, string _url) {
		filePath = _filePath;
		url = _url;
	}
	
	public override void AddLinkage (string _url, UnityEngine.Object elem)
	{
		targets.Add (elem);
		/*
		if (!url.Equals(_url)) {
			url = _url;
			completed = false;
		}
		*/
		
		if (status == OnlineAssetStatus.Completed) {
			if (elem is Button) {
				Button btn = (Button)elem;
				btn.interactable = true;
			}
		}
	}
	
	public override IEnumerator Download()
	{
		status = OnlineAssetStatus.Downloading;
		//Debug.Log ("DownloadAudio: " + Path.GetFileName(filePath) + " " + url);

		if (client != null) {
			client.Dispose();
			client = null;
		}

		client = new WebClient();
		client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler (DownloadCompletedHandler);
		client.DownloadFileAsync (new Uri (url), filePath);

		yield return 1;
	}
	
	private void DownloadCompletedHandler (object sender, System.ComponentModel.AsyncCompletedEventArgs  e)
	{
		Debug.Log ("[DownloadManager] DownloadCompletedHandler (Audio) Done: " + url);
		string filename = Path.GetFileName(filePath);
		LocalData.Instance.Data["sound"][filename]["url"] = url;
		LocalData.Instance.Save();

		
		client.Dispose();
		client = null;

		status = OnlineAssetStatus.Retrived;
	}

	public override void AfterCompleted ()
	{
		List<UnityEngine.Object> toBeRemoved = new List<UnityEngine.Object>();
		foreach (UnityEngine.Object elem in targets) {

			if (elem == null) {
				toBeRemoved.Add (elem);
				continue;
			}

			if (elem is Button) {
				Button btn = (Button)elem;
				if (btn == null) continue;
				btn.interactable = true;
			}
		}
		foreach (UnityEngine.Object obj in toBeRemoved) {
			targets.Remove(obj);
		}

		status = OnlineAssetStatus.Completed;
	}
}
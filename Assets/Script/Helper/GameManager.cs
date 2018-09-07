//-----------------------------------------------------
// mLang v1.3
//-----------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System;
using System.IO;
using Parse;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {
	
	private  static GameManager _instance = null;
	internal static GameManager Instance { get { return _instance; } }

	public Color[]				validColor = null;
	
	private Lang 				_language = null;
	internal Lang				Language { get { return _language; } }

	private List<LoadingTask>   _loadingTasks = null;
	internal List<LoadingTask>  LoadingTasks { get { return _loadingTasks; } set { _loadingTasks = value; } }
	
	private List<AlertTask>     _alertTasks = null;
	internal List<AlertTask>    AlertTasks { get { return _alertTasks; } set { _alertTasks = value; } }

	//private string persistentPath = "";

	// life cycle
	void Awake () {
		Debug.Log ("[GameManager] Awake()");
		if (_instance == null) {
			DontDestroyOnLoad (gameObject);
			_instance = this;
			Init();
		} else if (_instance != this) {
			Destroy (gameObject);
		}
	}
	
	void Init() {
		Debug.Log ("[GameManager] Init()");

		_loadingTasks = new List<LoadingTask>();
		_alertTasks = new List<AlertTask>();

		int lang = PlayerPrefs.GetInt ("language");
		//Debug.Log ("Lang : " + lang);
		SetLanguage(lang);
		// language
		//TextAsset xmlLang  = Resources.Load("lang") as TextAsset;
		//_language = new Lang (xmlLang.ToString(), "English", true);
		//_language = new Lang (xmlLang.ToString(), "Chinese", true);

		//persistentPath = Application.persistentDataPath;

		#if UNITY_EDITOR
		Application.runInBackground = true;
		#endif
	}

	public void SetLanguage (int lang, bool save = false) {
		TextAsset xmlLang  = Resources.Load("lang") as TextAsset;
		switch (lang) {
		case 0:
			_language = new Lang (xmlLang.ToString(), "English", true);
			break;
		case 1:
			_language = new Lang (xmlLang.ToString(), "Chinese", true);
			break;
		default:
			_language = new Lang (xmlLang.ToString(), "English", true);
			break;
		}

		if (save) {
			PlayerPrefs.SetInt("language", lang);
			PlayerPrefs.Save();

			Fader.Instance.FadeIn (0.5f)
				.LoadLevel (UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name)
					.FadeOut (0.5f);
		}
	}

	// Use this for initialization
	void Start () {
		Debug.Log ("[GameManager] Start()");
		if (LoadingPanel.Instance == null) {
			Debug.LogWarning("[GameManager] No loading panel");
		}
	}
	
	// Update is called once per frame
	void Update () {
		// alert
		if (AlertPanel.Instance != null) {
			if (_alertTasks.Count > 0 && !AlertPanel.Instance.IsBusy) {
				AlertPanel.Instance.ShowAlert (_alertTasks [0]);
				_alertTasks.RemoveAt (0);
			}
		}
		
		// loading
		if (LoadingPanel.Instance != null) {
			if (_loadingTasks.Count > 0 && !LoadingPanel.Instance.IsBusy) {
				LoadingPanel.Instance.ShowLoading(_loadingTasks[0]);
				_loadingTasks.RemoveAt(0);
			}
		}

		if (Input.GetKeyDown(KeyCode.F13)) {
		//if (Input.GetKeyDown(KeyCode.Z)) { // v1.3 easier for MacBook development
			Application.CaptureScreenshot("screenshot/" + GetCurrentTimestemp() + ".jpg");
		}
	}

	#region Parse Data
	public static DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	public static int GetCurrentTimestemp () {
		int timestamp = (int)((DateTime.UtcNow - epochStart).TotalSeconds);
		return timestamp;
	}
	
	
	public static string TimeRemainToString (int day, int hour = -1, int min = -1, int sec = -1) {
		string output = "";
		// still have time
		if (day > 1)
			output += String.Format(GameManager.Instance.Language.getString("val-days"), day) + " ";
		else if (day > 0)
			output += GameManager.Instance.Language.getString("1-day") + " ";
		
		if (hour > 1)
			output += String.Format(GameManager.Instance.Language.getString("val-hours"), hour) + " ";
		else if (hour > 0)
			output += GameManager.Instance.Language.getString("1-hour") + " ";
		
		if (min > 1)
			output += String.Format(GameManager.Instance.Language.getString("val-minutes"), min) + " ";
		else if (min > 0)
			output += GameManager.Instance.Language.getString("1-minute") + " ";
		
		if (sec > 1)
			output += String.Format(GameManager.Instance.Language.getString("val-seconds"), sec) + " ";
		else if (sec > 0)
			output += GameManager.Instance.Language.getString("1-second") + " ";
		
		output = output.TrimEnd();
		
		return output;
	}

	public static string ColorToHex(Color32 color)
	{
		string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
		return hex;
	}
	
	public static Color HexToColor(string hex)
	{
		byte r = byte.Parse(hex.Substring(0,2), System.Globalization.NumberStyles.HexNumber);
		byte g = byte.Parse(hex.Substring(2,2), System.Globalization.NumberStyles.HexNumber);
		byte b = byte.Parse(hex.Substring(4,2), System.Globalization.NumberStyles.HexNumber);
		return new Color32(r,g,b, 255);
	}

	//----------------------------------------------------------------
	// v1.3 added
	//----------------------------------------------------------------
	public void CheckTextLength( Text text_component )
	{
		//yield return new WaitForEndOfFrame ();

		int ncv = text_component.cachedTextGenerator.characterCountVisible;

		//int nc  = text_component.cachedTextGenerator.characterCount;
		//Debug.Log ("[" + text_component.text + "] length=" + text_component.text.Length.ToString() + " ncv=" + ncv.ToString());

		if ((ncv > 4)&&(ncv < text_component.text.Length)) {
			text_component.text = text_component.text.Substring (0, ncv - 4) + "...";
		}
	}

	public static void InstantiateChildren(int need, List<GameObject> child)
	{
		int missing = need - child.Count;
		for (int i = 0; i < missing; i++) {
			GameObject copy = Instantiate (child [0]);
			copy.transform.SetParent (child [0].transform.parent);
			copy.transform.localScale = Vector3.one;

			child.Add (copy);
		}
	}
	public static void InstantiateChildren(int need, List<Transform> child)
	{
		int missing = need - child.Count;
		for (int i = 0; i < missing; i++) {
			Transform copy = Instantiate (child [0]);
			copy.transform.SetParent (child [0].transform.parent);
			copy.transform.localScale = Vector3.one;

			child.Add (copy);
		}
	}
	//----------------------------------------------------------------

	#endregion
}

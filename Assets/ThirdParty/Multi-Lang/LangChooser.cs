//-----------------------------------------------------
// mLang v1.3
// Seems that this class is not used by the project...
//-----------------------------------------------------
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum CaseHandle
{
	Normal,
	Upper,
	FirstCharUpper,
	FirstCharUpperForEachWord,
	Lower
}

public class LangChooser : MonoBehaviour {


	public string				_key = "";
	public CaseHandle			_case = CaseHandle.Normal;

	// Use this for initialization
	public void Start () {
		string lang = GameManager.Instance.Language.getString (_key);

		if (string.IsNullOrEmpty (lang)) {
			Debug.LogWarning("LangChosser: " + _key + " is missing");
			return;
		}

		switch (_case) {
		case CaseHandle.Upper:
			lang = lang.ToUpper();
			break;
		case CaseHandle.Lower:
			lang = lang.ToLower();
			break;
		case CaseHandle.FirstCharUpper:
			lang = lang[0].ToString().ToUpper() + lang.Substring(1).ToLower();
			break;
		case CaseHandle.FirstCharUpperForEachWord:
			string[] words = lang.Split(' ');
			for(int i =0; i < words.Length; i++) {
				words[i] = words[i][0].ToString().ToUpper() + words[i].Substring(1).ToLower();
			}
			lang = string.Join(" ", words);
			break;
		}

		// change the text
		bool useful = false;

		TextMesh txtMesh = GetComponent<TextMesh> ();
		if (txtMesh != null) {
			useful = true;
			txtMesh.text = lang;
		}
		
		Text txt = GetComponent<Text> ();
		if (txt != null) {
			useful = true;
			txt.text = lang;
		}

		if (!useful) {
			Debug.LogWarning("LangChosser: " + gameObject.name + " did not find text component");
		}
	}
	/*
	public static string GetString (string key, CaseHandle c = CaseHandle.Normal) {

		string lang = GameManager.Instance.Language.getString (key);
		
		if (string.IsNullOrEmpty (lang)) {
			Debug.LogWarning("LangChosser: " + key + " is missing");
			return "";
		}

		switch (c) {
		case CaseHandle.Upper:
			lang = lang.ToUpper();
			break;
		case CaseHandle.Lower:
			lang = lang.ToLower();
			break;
		case CaseHandle.FirstCharUpper:
			lang = lang[0].ToString().ToUpper() + lang.Substring(1).ToLower();
			break;
		}

		return lang;

	}
	*/
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultiLang : MonoBehaviour {

	public enum Mode{
		normal,
		firstCharUpper,
		AllUpper
	}

	public Mode mode = Mode.normal;

	public string chinese,english;

	// Use this for initialization
	public void Start () {
		int lang = PlayerPrefs.GetInt ("language");
		if (lang == 0) {
			GetComponent<Text> ().text = GetString(english,mode);
		} else {
			GetComponent<Text> ().text = chinese;
		}
	}

	string GetString(string str,Mode m){
		switch (m) {
		case Mode.normal:
			return str;
		case Mode.firstCharUpper:
			char[] ca = str.ToCharArray ();
			ca [0] = char.ToUpper (ca [0]);
			return new string (ca);
		case Mode.AllUpper:
			char[] ca2 = str.ToCharArray ();
			for (int i = 0; i < ca2.Length; i++) {
				ca2 [i] = char.ToUpper (ca2 [i]);
			}
			return new string (ca2);
		}
		return "";
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

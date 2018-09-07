using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DescriptionControl : MonoBehaviour {

	// Use this for initialization
	void Start () {
		if (PlayerPrefs.GetInt (gameObject.name) == 1) {
			Destroy (gameObject);
		}
	}
	
	public void EventGotIt(){
		Destroy (gameObject);
	}

	public void EventNeverShowAgain(){
		PlayerPrefs.SetInt (gameObject.name, 1);
		Destroy (gameObject);
	}

	public void DirectToStore(){
		if (Application.platform == RuntimePlatform.Android) {
			Application.OpenURL ("https://play.google.com/store/apps/details?id=com.Inreader.mLangHKUnew&hl=zh_HK");
		} else {
			Application.OpenURL ("https://itunes.apple.com/hk/app/mlang/id1289140490?l=zh&mt=8");
		}
	}
}

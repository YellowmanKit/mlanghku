using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class NewLangPicker : MonoBehaviour {

	private bool					_dynSetup = false;

	public Common_AddCard			controller = null;
	private string 					selectedLangKey = "";

	public List<Transform>			_listBtns = null;

	
	void Setup () {
		_dynSetup = true;
		int i = 0;
		foreach (string langKey in Enum.GetNames(typeof(LangOrder))) {
			// missing?
			if (i >= _listBtns.Count) {
				Transform copy = Instantiate (_listBtns [0]);
				copy.transform.SetParent (_listBtns[0].parent);
				copy.transform.localScale = Vector3.one;
				
				_listBtns.Add (copy);
			}
			
			Transform btn = _listBtns[i++];
			if (btn != null) {
				btn.name = langKey;
				//btn.FindChild("img_flag").GetComponent<Image>().sprite = DownloadManager.GetNationFlagByLangKey(langKey);
				btn.FindChild("txt_lang").GetComponent<Text>().text = GameManager.Instance.Language.getString("lang-" + langKey, CaseHandle.FirstCharUpper);
			} else {
				i--;
			}
		}
	}

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void EventSelectedLang (GameObject btnLang) {
		selectedLangKey = btnLang.name;

		controller.EventAddLangCompleted(selectedLangKey);
		gameObject.SetActive(false);
	}

	public void EventShow (Card card) {
		if (!_dynSetup)
			Setup ();

		selectedLangKey = "";

		foreach (Transform btn in _listBtns) {
			btn.gameObject.SetActive(true);
		}

		foreach (string key in card.langs.Keys) {
			foreach (Transform btn in _listBtns) {
				if (btn.name.Equals(key)) {
					btn.gameObject.SetActive(false);	// repeat, don't show
				}
			}
		}
		gameObject.SetActive(true);
	}
	
	public void EventButtonClose () {
		gameObject.SetActive(false);
	}
	/*
	public void EventButtonConfirm () {
		controller.EventAddLangCompleted(selectedLangKey);

		gameObject.SetActive(false);
	}
	*/
}

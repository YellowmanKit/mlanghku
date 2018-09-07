using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Page_Search : Page {

	enum SearchMode{
		mycard,
		standard,
		prefix
	}

	public GameObject[] contents;

	public Text[] navBtnTxt;

	public Color[] txtColor;

	public override	void Init (Dictionary<string, object> data) {

	}

	public override void OnPageShown (Dictionary<string, object> data) {
		string title = GameManager.Instance.Language.getString ("search");
		NavigationBar.Instance.SetTitle (title);
	}

	public void EventSelectContent(int index){
		for (int i = 0; i < contents.Length; i++) {
			if (i == index) {
				contents [i].SetActive (true);
				navBtnTxt [i].color = txtColor [0];
			} else {
				contents [i].SetActive (false);
				navBtnTxt [i].color = txtColor [1];
			}
		}
	}

}

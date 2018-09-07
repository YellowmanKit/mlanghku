//-----------------------------------------------------
// mLang v1.3
//-----------------------------------------------------
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TextViewer : MonoBehaviour 
{	
	public   static TextViewer _instance = null;
	internal static TextViewer Instance { get { return _instance; } }

	public Text langNameText;
	public Text cardLangText;

	public LayoutElement scrollViewLayout; // v1.3 2017/01/09

	void Awake () {
		_instance = this;
	}

	void Start () {
		RectTransform t = GetComponent<RectTransform>();
		t.anchoredPosition = new Vector2(0, -245f);
		gameObject.SetActive(false);
	}

//	void Update () {
//		Debug.Log (GetComponent<RectTransform>().anchoredPosition);
//	}

	//----------------------------------------------------------
	// v1.3 added
	//----------------------------------------------------------
	public void showText(CardLang lang) {
		updateText(lang);
	}
	//----------------------------------------------------------
	// v1.3 comment out for backup purpose
	//----------------------------------------------------------
	//public void showText(CardLang[] langs) {
	//	//TODO show more than one lang
	//	updateText(langs[0]);
	//}
	//----------------------------------------------------------

	private void updateText(CardLang lang) {
		updateText (
			GameManager.Instance.Language.getString ("lang-" + lang.langKey, CaseHandle.FirstCharUpper),
			lang.getName ()
		);
	}

	private void updateText(string langName, string cardLang) {
		langNameText.text = langName;
		cardLangText.text = cardLang;
		gameObject.SetActive(true);

		StartCoroutine (UpdateScrollViewHeight ()); // v1.3 2017/01/09
	}

	public void EventButtonClose () {
		gameObject.SetActive(false);
	}

	//-----------------------------------------------------------
	// v1.3 2017/01/09
	// After assigned text to cardLangText, try to get its height
	// and scale its parent ScrollView accordingly.
	//-----------------------------------------------------------
	private IEnumerator UpdateScrollViewHeight()
	{
		yield return new WaitForEndOfFrame ();
		float text_height = cardLangText.rectTransform.rect.height;
		if (text_height > 750) text_height = 750;
		scrollViewLayout.preferredHeight = text_height;
	}
}

//-----------------------------------------------------
// mLang v1.3
//-----------------------------------------------------
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AlertPanel : MonoBehaviour 
{
	public   static AlertPanel _instance = null;
	internal static AlertPanel Instance { get { return _instance; } }

	public RectTransform _imgTitleObject = null;
	public RectTransform _imgButtonObject = null;
	
	public Text			 _txtHeading = null;
	public Text			 _txtContent = null;
	
	public GameObject[]	 _buttons = null;

	private AlertTask	 _dynCurrentTask = null;

	public RectTransform _container = null;
	private bool		 _dynIsSizeFitted = true;

	internal bool IsBusy { get { return gameObject.activeSelf; } }

	void Awake () {
		_instance = this;
	}

	void Start () {
		RectTransform t = GetComponent<RectTransform>();
		t.anchoredPosition = new Vector2(0, 0);
		gameObject.SetActive(false);
	}


	void Update () {
		if (!_dynIsSizeFitted) {
			FitSize();
			_dynIsSizeFitted = true;
		}
	}

	public void ShowAlert (AlertTask task) {

		_dynCurrentTask = task;

		_txtHeading.text = task.title.ToUpper();
		_txtContent.text = task.content;
		_txtContent.alignment = task.textAnchor;

		// hide all button
		foreach (GameObject btn in _buttons) { btn.SetActive(false); }

		for (int i = 0; i < task.buttonTexts.Length && i < _buttons.Length; i++) {
			Transform btn = _buttons[i].transform;
			Transform text = btn.FindChild("Text");
			Text txt = text.GetComponent<Text>();
			txt.text = task.buttonTexts[i];
			btn.gameObject.SetActive(true);
		}

		gameObject.SetActive(true);

		_dynIsSizeFitted = false;
	}

	public void FitSize () {
		// fit size
		float height = 15;
		height += _imgTitleObject.sizeDelta.y;
		height += _imgButtonObject.sizeDelta.y;
		float contentHeight = Mathf.Min (300, Mathf.Max (200, _txtContent.GetComponent<RectTransform>().sizeDelta.y));
		height += contentHeight;
		_container.sizeDelta = new Vector2(_container.sizeDelta.x, height);
		//Debug.Log ("FitSize() contentHeight=" + contentHeight.ToString() + " totalHeight=" + height.ToString());
	}

	public void EventButton (int i) {
		if (_dynCurrentTask != null) {
			_dynCurrentTask.EventTrigger(i);
			_dynCurrentTask = null;
		}
		gameObject.SetActive(false);
	}

}

public class AlertTask 
{
	public string title = "Alert";
	public string content = "";
	public TextAnchor textAnchor = TextAnchor.MiddleCenter;
	
	public string[] 		buttonTexts;
	
	public delegate void EventHandler();
	public event EventHandler buttonCompletedEvent_0;
	public event EventHandler buttonCompletedEvent_1;
	public event EventHandler buttonCompletedEvent_2;
	
	public AlertTask (string msg, TextAnchor ta = TextAnchor.MiddleCenter) {
		content = msg;
		buttonTexts = new string[] {"CLOSE"};
		textAnchor = ta;
	}
	public AlertTask (string head, string msg, TextAnchor ta = TextAnchor.MiddleCenter) {
		title = head;
		content = msg;
		buttonTexts = new string[] {"CLOSE"};
		textAnchor = ta;
	}

	public AlertTask (string head, string msg, string[] texts, TextAnchor ta = TextAnchor.MiddleCenter) {
		title = head;
		content = msg;
		buttonTexts = texts;
		if (buttonTexts.Length > 3) {
			Debug.LogError("[AlertPanel] AlertTask() : Too many button");
		}
		textAnchor = ta;
	}

	public void EventTrigger (int i) {
		switch (i) {
		case 0:
			if (buttonCompletedEvent_0 != null) buttonCompletedEvent_0();
			break;
		case 1:
			if (buttonCompletedEvent_1 != null) buttonCompletedEvent_1();
			break;
		case 2:
			if (buttonCompletedEvent_2 != null) buttonCompletedEvent_2();
			break;
		}
	}
		
}

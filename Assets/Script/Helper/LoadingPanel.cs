//-----------------------------------------------------
// mLang v1.3
//-----------------------------------------------------
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using SimpleJSON;
using Parse;

public class LoadingPanel : MonoBehaviour
{
	public   static LoadingPanel _instance = null;
	internal static LoadingPanel Instance { get { return _instance; } }

	public GameObject _btnCancel = null;
	public Text       _txtDisplay = null;
	
	LoadingTask currentTask = null;
	
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
		if (currentTask != null){
			if (currentTask.Loading) {
				currentTask.Update();
				_txtDisplay.text = currentTask.DisplayMessage;
			} else {
				currentTask.triggerCompleted();
				currentTask = null;
				HideLoading();
			}
		}
	}

	public void EventCancelTask () {
		if (currentTask != null){
			currentTask.triggerCancel();
			currentTask = null;
		}
		HideLoading();
	}
	
	public void ShowLoading (LoadingTask task) {
		currentTask = task;

		if (task.CanCancel) {
			_btnCancel.SetActive(true);
		} else {
			_btnCancel.SetActive(false);
		}

		_txtDisplay.text = task.DisplayMessage;

		gameObject.SetActive(true);
	}
	
	public void HideLoading () {
		gameObject.SetActive(false);
	}
}

public class LoadingTask
{
	protected bool _dynCanCancel = false;
	internal  bool CanCancel { set { _dynCanCancel = value; } get { return _dynCanCancel; } }
	
	protected string _dynDisplayMessage = "";
	internal  string DisplayMessage { set { _dynDisplayMessage = value; } get { return _dynDisplayMessage; } }
	
	protected bool loading = false;
	internal  bool Loading { get { return loading; } set { loading = value; } }
	
	public delegate void EventHandler();
	public event EventHandler OnLoadingCompleted;
	public event EventHandler OnLoadingCancel;

	// v1.3 make the constructor more handy
	public LoadingTask ( string _msg = "", bool _cancancel = true ) { _dynDisplayMessage = _msg; _dynCanCancel = _cancancel; loading = true; }

	public virtual void Update () {}
	
	public void triggerCompleted () {
		if (OnLoadingCompleted != null) {
			OnLoadingCompleted();
		}
	}
	
	public void triggerCancel () {
		if (OnLoadingCancel != null)
			OnLoadingCancel();
	}
}
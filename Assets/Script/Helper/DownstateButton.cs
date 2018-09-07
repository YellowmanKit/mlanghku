using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DownstateButton : Button {
	
	public delegate void EventHandler();
	public event EventHandler OnClickEvent;
	public event EventHandler OnReleaseEvent;

	private bool isDown			= false;

	public void Update() {
		if (!isDown) {
			if(IsPressed())	{
				OnClick();
			}
		} else {
			if(!IsPressed()) {
				OnRelease();
			}
		}
	}

	public void OnClick () {
		//Debug.Log ("OnClick");
		isDown = true;

		if (OnClickEvent != null) {
			OnClickEvent();
		}
	}

	public void OnRelease () {
		//Debug.Log ("Released");
		isDown = false;

		if (OnReleaseEvent != null) {
			OnReleaseEvent();
		}
	}
}
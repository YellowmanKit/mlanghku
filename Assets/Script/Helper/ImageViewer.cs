using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ImageViewer : MonoBehaviour {
	
	public static ImageViewer _instance = null;
	internal static ImageViewer Instance { get { return _instance; } }

	public Image		image = null;

	
	void Awake () {
		_instance = this;
	}

	void Start () {
		
		RectTransform t = GetComponent<RectTransform>();
		t.anchoredPosition = new Vector2(0, 0);
		gameObject.SetActive(false);
	}

	void Update () {
	
	}

	public void EventShowSprite (Sprite s) {
		image.sprite = s;
		gameObject.SetActive(true);
	}

	public void EventButtonClose () {
		gameObject.SetActive(false);
	}
}

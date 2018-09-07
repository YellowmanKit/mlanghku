using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class BounsCounterControl : MonoBehaviour {

	public Image barImg, counterImg;
	public float bounsTime,maxTime;
	public Text countDown;
	public RectTransform barRT;
	public Color C;
	public ShakeTheStar STS;
	public AudioSource[] UIAS;

	private string lastTime;
	private RectTransform countDownRT;
	private float actTime;
	// Use this for initialization
	void Start () {
		maxTime = 5f;
		countDownRT = countDown.gameObject.GetComponent<RectTransform> (); 
		lastTime = "" + 0;
	}
	
	// Update is called once per frame
	void Update () {
		bounsTime = Mathf.Clamp(bounsTime - Time.deltaTime,0f,maxTime);
		UpdateActTime ();
		countDown.text = "" + Mathf.Ceil (bounsTime);
		if (STS != null) {
			STS.shakeTime = actTime;
		}
		UpdateBar ();
		CountDownPulse ();
	}

	public void AnswerSelected(bool correct){
		if (bounsTime > 0f) {
			if (STS != null) {
				STS.ShotTheStar (correct, bounsTime);
			}
		}
		bounsTime = 0;
	}

	private void UpdateActTime(){
		actTime += (bounsTime - actTime) * 0.1f;
	}

	private void CountDownPulse(){
		if (lastTime != countDown.text) {
			lastTime = countDown.text;
			countDownRT.localScale = new Vector3 (2f, 2f, 1f);
			UIAS [1].Play ();
		}
		if (countDownRT.localScale.x > 1f) {
			float newScale = countDownRT.localScale.x - 2f * Time.deltaTime;
			countDownRT.localScale = new Vector3 (newScale, newScale, 1f);
		}
	}

	private void UpdateBar(){
		barRT.localScale = new Vector3 (1f, 1f * actTime / maxTime, 1f);
		C.r = 1f - 1f * actTime / maxTime;
		C.g = 1f * actTime / maxTime;
		barImg.color = C;
		counterImg.color = C;

		Color C2 = countDown.color;
		C2.r = C.r;
		C2.g = C.g;
		countDown.color = C2;
	}
}

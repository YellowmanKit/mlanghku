using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ShakeTheStar : MonoBehaviour {

	public float speed,range,shakeTime;
	public bool toRight;
	public GameObject starExplosion,exploSound;
	public Game1Control G1C;

	private RectTransform RT;
	public float rotated,baseSpd,baseRng,maxTime;
	public float oriYPos,diff;
	private bool shot;
	private Vector3 shotTarget;
	private int scoreCarried;
	// Use this for initialization
	void Start () {
		RT = GetComponent<RectTransform> ();
		baseSpd = speed;
		baseRng = range;
		oriYPos = RT.localPosition.y;
		diff = -145f;
		maxTime = 5f;
		shotTarget = GameObject.Find ("StarTarget").GetComponent<RectTransform> ().position;
		GetComponentInChildren<ParticleSystem> ().Pause ();
		G1C = GameObject.Find ("GameControl").GetComponent<Game1Control> ();
			
	}
	
	// Update is called once per frame
	void Update () {
		if (shot) {
			MoveStarToTarget ();
			return;
		}
		UpdateVar ();
		StarShaking ();
		StarMove ();
		if (shakeTime <= 0.1f) {
			StarDie ();
		}
	}

	private void MoveStarToTarget(){
		float newX = RT.position.x;
		float newY = RT.position.y;
		if (RT.position.x > shotTarget.x) {
			newX -= (RT.position.x - shotTarget.x) * Time.deltaTime * 3f;
		}
		if (RT.position.y < shotTarget.y) {
			newY -= (RT.position.y - shotTarget.y) * Time.deltaTime * 9f;
		}
		RT.position = new Vector3 (newX, newY, 0f);
		if (Mathf.Abs(RT.position.x - shotTarget.x) < 0.1f && Mathf.Abs(RT.position.y - shotTarget.y) < 0.1f) {
			G1C.AddScore (scoreCarried);
			Instantiate (starExplosion, transform.position, Quaternion.identity);
			Instantiate (exploSound);
			//GetComponent<Image> ().enabled = false;
			Destroy (gameObject);
		}
	}

	public void ShotTheStar(bool correct,float shotTime){
		if (correct) {
			scoreCarried = (int)Mathf.Ceil (shotTime);
			shot = true;
			if (gameObject != null) {
				GetComponentInChildren<ParticleSystem> ().Play ();
			}
		} else {
			//Instantiate(starExplosion,Camera.main.ScreenToWorldPoint (RT.position),Quaternion.identity);
		}

	}

	private void StarDie (){
		Instantiate (starExplosion, transform.position, Quaternion.identity);
		Destroy (gameObject);
	}

	private void UpdateVar(){
		//shakeTime = Mathf.Clamp(shakeTime - Time.deltaTime,0f,5f);
		speed = baseSpd * 6f / (shakeTime + 1f);
		range = baseRng * 2f / (shakeTime + 1f);
	}

	private void StarMove(){
		float newY = oriYPos + diff * (1f - shakeTime / maxTime);
		RT.localPosition = new Vector3 (RT.localPosition.x, newY, RT.localPosition.z);
	}

	private void StarShaking(){
		float zRotate = 0;
		if (toRight && rotated > -range) {
			zRotate = -speed * Time.deltaTime;
		} else if (toRight && rotated < -range) {
			toRight = false;
		} else if (!toRight && rotated < range) {
			zRotate = speed * Time.deltaTime;
		} else if (!toRight && rotated > range) {
			toRight = true;
		}
		RT.Rotate (0f, 0f, zRotate);
		rotated += zRotate;
	}
}

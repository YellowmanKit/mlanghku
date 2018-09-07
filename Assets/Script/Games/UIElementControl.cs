using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIElementControl : MonoBehaviour {

	public bool toRotate;

	public float rotateSpd;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (toRotate) {
			transform.Rotate (new Vector3 (0f, 0f, 360f * Time.deltaTime * rotateSpd));
		}
	}
}

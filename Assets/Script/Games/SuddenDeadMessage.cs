using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuddenDeadMessage : MonoBehaviour {

	public Transform movementObj;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		transform.position = movementObj.position;
	}
}

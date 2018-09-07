using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataHelper : MonoBehaviour {

	public bool isMissionMode;

	public string missionParseId;

	// Use this for initialization
	void Start () {
		GameObject.DontDestroyOnLoad (gameObject);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

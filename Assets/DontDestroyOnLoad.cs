using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour {

	// Use this for initialization
	void Start () {
		DontDestroyOnLoad(gameObject);
		Destroy(this);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

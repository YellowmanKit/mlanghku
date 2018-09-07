using UnityEngine;
using System.Collections;

#pragma warning disable 0618

public class LoadExamples : MonoBehaviour {

	public void LoadExample(string level){
		Application.LoadLevel( level );
	}
}

#pragma warning restore 0618

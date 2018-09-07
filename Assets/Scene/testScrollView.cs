using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class testScrollView : MonoBehaviour {

	public LayoutElement parent_layout;
	public Text text_element;

	// Use this for initialization
	void Start () {
		//Debug.Log ("Height=" + t.rectTransform.rect.height.ToString ());
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyUp (KeyCode.A)) {
			text_element.text = "New Text aksjhd aslkdjalk jdka  dksjd djak dkj kdjak dkjsk dksjdkjkdj kjs kd ksjkdja ksdjksjd ksk dk jkdjskjieui jhajhwh djadwjwajdkwdjw hdajd djahd wakd j New Text aksjhd aslkdjalk jdka  dksjd djak dkj kdjak dkjsk dksjdkjkdj kjs kd ksjkdja ksdjksjd ksk dk jkdjskjieui jhajhwh djadwjwajdkwdjw hdajd djahd wakd j New Text aksjhd aslkdjalk jdka  dksjd djak dkj kdjak dkjsk dksjdkjkdj kjs kd ksjkdja ksdjksjd ksk dk jkdjskjieui jhajhwh djadwjwajdkwdjw hdajd djahd wakd j New Text aksjhd aslkdjalk jdka  dksjd djak dkj kdjak dkjsk dksjdkjkdj kjs kd ksjkdja ksdjksjd ksk dk jkdjskjieui jhajhwh djadwjwajdkwdjw hdajd djahd wakd j New Text aksjhd aslkdjalk jdka  dksjd djak dkj kdjak dkjsk dksjdkjkdj kjs kd ksjkdja ksdjksjd ksk dk jkdjskjieui jhajhwh djadwjwajdkwdjw hdajd djahd wakd j New Text aksjhd aslkdjalk jdka  dksjd djak dkj kdjak dkjsk dksjdkjkdj kjs kd ksjkdja ksdjksjd ksk dk jkdjskjieui jhajhwh djadwjwajdkwdjw hdajd djahd wakd j New Text aksjhd aslkdjalk jdka  dksjd djak dkj kdjak dkjsk dksjdkjkdj kjs kd ksjkdja ksdjksjd ksk dk jkdjskjieui jhajhwh djadwjwajdkwdjw hdajd djahd wakd j";
			//Debug.Log ("Height=" + text_element.rectTransform.rect.height.ToString ());
			StartCoroutine (UpdateParentHeight ());
		}
		if (Input.GetKeyUp (KeyCode.S)) {
			//Debug.Log ("Check Height=" + text_element.rectTransform.rect.height.ToString ());
		}
	}

	private IEnumerator UpdateParentHeight()
	{
		yield return new WaitForEndOfFrame();
		//Debug.Log ("Coro Height=" + text_element.rectTransform.rect.height.ToString ());
		parent_layout.preferredHeight = text_element.rectTransform.rect.height;
	}
}

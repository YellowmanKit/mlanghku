//------------------------------------------------------------------------------
// mLang v1.3
//------------------------------------------------------------------------------
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System;

public class Teacher_Course_ViewStudentProejctSubmitted_Cell : MonoBehaviour {

	private Card			_dynCard = null;
	private LayoutElement	_dynLayoutElement = null;

	public Transform 		_parentCells = null;
	public List<Transform> 	_langRows = null;

	/*
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	*/

	void OnDestroy() {
		// Image btnImageIcon = transform.FindChild ("img_icon").GetComponent<Image> ();
		// Destroy(btnImageIcon.sprite.texture);
	}

	public void Init (Card _card) 
	{
		_dynCard = _card;
		_dynLayoutElement = GetComponent<LayoutElement>();

		//////////////////////////////////////////////////////
		/// Icon
		Image btnImageIcon = transform.FindChild ("img_icon").GetComponent<Image> ();
//		if (LocalData.Instance.IsTestUser > 0) {
//			btnImageIcon.sprite = DownloadManager.FileToSprite(_card.ImagePath);
//		} else {
			//if (!string.IsNullOrEmpty(_dynCard.imageUrl)) {
				btnImageIcon.sprite = DownloadManager.Instance.spriteLoading;
				DownloadManager.Instance.AddLinkage(_dynCard.ImagePath, _dynCard.imageUrl, btnImageIcon, false);
			//} else {
			//	btnImageIcon.sprite = DownloadManager.Instance.spriteNoImage;
			//	Debug.LogWarning("No image file, and no url provided for " + _dynCard.GetDefaultDisplayName());
			//}
//		}

		////////////////////////////////////////////////////
		/// langs
		foreach (Transform row in _langRows) { row.gameObject.SetActive(false); }

		GameManager.InstantiateChildren (_dynCard.getLangs ().Count, _langRows);
		/*
		// missing?
		int missing = _dynCard.getLangs().Count - _langRows.Count;
		for (int i = 0; i < missing; i++) {
			GameObject copy = Instantiate (_langRows [0].gameObject);
			copy.transform.SetParent (_parentCells);
			copy.transform.localScale = Vector3.one;
			
			_langRows.Add (copy.transform);
		}
		*/

		CardLang[] langs = _dynCard.LangsInOrder;
		for (int i = 0; i < langs.Length; i++) {
			CardLang lang = langs[i];
			Transform row = _langRows[i];

			//row.FindChild ("img_flag").GetComponent<Image> ().sprite = DownloadManager.GetNationFlagByLangKey(lang.langKey);
			row.FindChild ("txt_name").GetComponent<Text> ().text = lang.getName();
			row.FindChild ("txt_lang").GetComponent<Text> ().text = GameManager.Instance.Language.getString("lang-" + lang.langKey, CaseHandle.FirstCharUpper);

			row.gameObject.SetActive(true);
		}

		float langRowHeight = _langRows[0].GetComponent<LayoutElement>().preferredHeight;
		_dynLayoutElement.preferredHeight = Mathf.Max (100, (langs.Length - 1) * langRowHeight + 50);

		//------------------------------------------------------------------------------
		// v1.3 If the text has three lines, show "..."
		//------------------------------------------------------------------------------
		Canvas.ForceUpdateCanvases();
		for (int i = 0; i < langs.Length; i++) {
			GameManager.Instance.CheckTextLength(_langRows[i].FindChild ("txt_name").GetComponent<Text> ());
		}
		//------------------------------------------------------------------------------
	}
}

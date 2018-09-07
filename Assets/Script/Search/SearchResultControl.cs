using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Parse;

public class SearchResultControl : MonoBehaviour {

	public GameObject cardObj,detailObj;

	public Transform content,detail;

	int showingDetail = -1;

	public void PushSearchResult(List<ParseObject> cards){
		//Debug.Log ("Result pushed");
		foreach (ParseObject card in cards) {
			bool toSkip = false;
			for (int i = 0; i < content.childCount; i++) {
				if (content.GetChild (i).gameObject.name.Equals(card.ObjectId)) {
					toSkip = true;
					break;
				}
			}
			if (toSkip) {
				continue;
			}
			//Debug.Log ("Card found: " + card.ObjectId);
			GameObject cardBtn = Instantiate (cardObj,content) as GameObject;
			cardBtn.transform.localScale = Vector3.one;
			cardBtn.GetComponent<ResultCard> ().Initialize (card, this,cardBtn.transform.GetSiblingIndex());
			cardBtn.name = card.ObjectId;

			GameObject cardDetail = Instantiate (detailObj,detail) as GameObject;
			cardDetail.transform.localScale = Vector3.one;
			cardDetail.GetComponent<Search_Detail> ().Initialize (cardBtn.GetComponent<ResultCard> (), this,cardBtn.transform.GetSiblingIndex());
			cardDetail.name = card.ObjectId;
			cardDetail.SetActive (false);
		}
	}

	public void ShowCardDetail(int index){
		if (showingDetail >= 0) {
			detail.GetChild (showingDetail).gameObject.SetActive (false);
		}
		if (index >= 0) {
			GameObject _detail = detail.GetChild (index).gameObject;
			_detail.SetActive (true);
			_detail.GetComponent<Search_Detail> ().SetContent ();
			showingDetail = index;
		}
	}
}

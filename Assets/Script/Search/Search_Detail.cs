using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Parse;
using WWUtils.Audio;
using System.IO;

public class Search_Detail : MonoBehaviour {

	public Image img;

	public Transform lang_content;

	public GameObject langRow,nextCardBtn,prevCardBtn;

	public Text authorTxt,schoolTxt,lastUpdateTxt;

	ResultCard r_card;

	public int index;

	bool contentSet = false;

	SearchResultControl src;

	public void Initialize(ResultCard _card,SearchResultControl _src,int _index){
		r_card = _card;
		index = _index;
		src = _src;
	}

	public void SetContent(){
		if (index == transform.parent.childCount - 1) {
			nextCardBtn.SetActive (false);
		} else {
			nextCardBtn.SetActive (true);
		}
		if (index == 0) {
			prevCardBtn.SetActive (false);
		} else {
			prevCardBtn.SetActive (true);
		}
		if (contentSet) {
			return;
		}
		contentSet = true;
		img.sprite = r_card.img.sprite;
		System.DateTime dt = (System.DateTime)r_card.card.CreatedAt;
		dt.AddHours ((double)8);
		lastUpdateTxt.text = dt.ToString ("dddd, d MMM yyyy, hh:mm tt");
		foreach (ParseObject lang in r_card.cardLangs) {
			StartCoroutine (SetCardLang (lang));
		}
		StartCoroutine (GetAuthorAndSchoolName ());
	}

	IEnumerator GetAuthorAndSchoolName(){
		ParseUser auther = r_card.card.Get<ParseUser> ("author");
		string authorName = "";
		bool queryDone = false;
		auther.FetchAsync ().ContinueWith (t => {
			authorName = auther.Get<string>("realName");
			queryDone = true;
		});
		yield return new WaitUntil (() => queryDone);
		queryDone = false;
		authorTxt.text = authorName;

		ParseObject school =  auther.Get<ParseObject> ("school");
		string schoolName = "";
		school.FetchAsync ().ContinueWith (t => {
			schoolName = school.Get<string>("fullName");
			queryDone = true;
		});
		yield return new WaitUntil (() => queryDone);
		schoolTxt.text = schoolName;
	}

	IEnumerator SetCardLang(ParseObject lang){
		GameObject langObj = Instantiate (langRow, lang_content) as GameObject;
		langObj.transform.FindChild ("lbl_display").GetComponent<Text> ().text = lang.Get<string> ("name");
		langObj.transform.FindChild ("lbl_lang").GetComponent<Text> ().text = GameManager.Instance.Language.getString("lang-" + lang.Get<string> ("langKey"), CaseHandle.FirstCharUpper);

		string soundName = lang.ObjectId + "-lang-sound.wav";
		string soundPath = Path.Combine (LocalData.Instance.DirectoryPath, soundName);

		if (!File.Exists (soundPath)) {
			var soundFile = lang.Get<ParseFile> ("sound");
			var downloadLangSoundRequest = new WWW (soundFile.Url.AbsoluteUri);
			yield return downloadLangSoundRequest;
			File.WriteAllBytes (soundPath, downloadLangSoundRequest.bytes);
			//Debug.Log ("Downloaded!");
		}
		//Debug.Log ("Try to get sound from file!");
		WAV soundWAV = new WAV (soundPath);
		AudioClip audioClip = AudioClip.Create ("temp", soundWAV.SampleCount, 1, soundWAV.Frequency, false);
		audioClip.SetData (soundWAV.LeftChannel, 0);
		langObj.transform.FindChild ("btn_playSound").GetComponent<AudioSource> ().clip = audioClip;

		langRow.gameObject.SetActive (false);
	}

	public void ShowNextCard(bool next){
		if (next) {
			if (index + 1 < transform.parent.childCount) {
				src.ShowCardDetail (index + 1);
			}
		} else {
			if (index - 1 >= 0) {
				src.ShowCardDetail (index - 1);
			}
		}
	}

}
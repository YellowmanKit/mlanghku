using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Parse;
using System.IO;

public class ResultCard : MonoBehaviour {

	public Image img;

	public Text langTxt;

	public ParseObject card;

	public List<ParseObject> cardLangs = new List<ParseObject> ();

	SearchResultControl src;

	bool imgReady,langReady;

	public int index;

	public void Initialize(ParseObject _card,SearchResultControl _src,int _index){
		card = _card;
		src = _src;
		index = _index;
		StartCoroutine (GetCardImage ());
		StartCoroutine (GetCardLang ());
	}

	IEnumerator GetCardImage(){
		string imgName = card.ObjectId + "-image.jpg";
		//string imgPath = Path.Combine (LocalData.Instance.DirectoryPath, imgName);
		string imgKey = card.ObjectId + "-image";
		PlayerPrefs.DeleteKey (imgKey);
		if (!PlayerPrefs.HasKey(imgKey)) {
			var parseFile = card.Get<ParseFile> ("image");
			var imageDownloadRequest = new WWW (parseFile.Url.AbsoluteUri);
			yield return imageDownloadRequest;
			PlayerPrefs.SetString (imgKey, System.Convert.ToBase64String(imageDownloadRequest.bytes));
		}
		Texture2D imgTexture = new Texture2D (1, 1, TextureFormat.ARGB32, false);
		imgTexture.LoadImage (System.Convert.FromBase64String (PlayerPrefs.GetString (imgKey)));
		img.sprite = Sprite.Create (imgTexture, new Rect (0f, 0f, imgTexture.width, imgTexture.height), new Vector2 (0.5f, 0.5f), 100f);
		imgReady = true;
	}

	IEnumerator GetCardLang(){
		var langRelation = card.GetRelation <ParseObject>("langs");
		//var langQuery = langRelation.Query;
		bool queryDone = false;
		langRelation.Query.FindAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				//Debug.Log("Query success");
				IEnumerable<ParseObject> langs = t.Result;
				foreach(ParseObject lang in langs){
					//Debug.Log(lang.Get<string>("name"));
					cardLangs.Add(lang);
				}
			}else{
				//Debug.Log("Failed to get langs!");
			}
			queryDone = true;
		});
		yield return new WaitUntil (() => queryDone);
		if (cardLangs.Count > 0) {
			langTxt.text = cardLangs [0].Get<string> ("name");
		}
		langReady = true;
	}

	public void EventShowCardDetail(){
		if (!imgReady || !langReady) {
			return;
		}
		src.ShowCardDetail (index);
	}
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.IO;
using Parse;
using System;

public enum CardSortBy {
	Name,
	LastModify,
	Status,
	SlideNumber
}

public enum CardStatus {
	Not_Graded = 0,
	Passed = 1,
	Failed = 2,
	Featured = 3
}

public class Card {
	protected string	imageName = "";

	public string 	cardId = "";
	public string	projectId = "";	// may not exist

	public string	authorId = "";
	public string	authorName = "";
	public string	authorRealName = "";

	public int 		lastUpdatedAt = 0;
	public int 		imageLastUpdatedAt = 0;
	
	public string 	objectId = "";
	public string 	oldVersion = "";
	public string	imageUrl = "";
	
	public bool		isDeleted = false;

	// teacher use
	public List<string>			comments = null;
	public CardStatus			status = CardStatus.Not_Graded;		// 0 = not graded, 1 = passed, 2 = failed, 3 = featured

	public Dictionary<string, CardLang>		langs = null;

	public bool isSlide() {
		return langs.ContainsKey ("#SlideShow") && langs ["#SlideShow"] != null;
	}

	public int slideNumber {
		get {
			if (isSlide()) {
				return int.Parse (langs ["#SlideShow"].name);
			} else {
				return -1;
			}
		}
		set {
			if (!isSlide()) {
				langs ["#SlideShow"] = new CardLang (cardId, "#SlideShow", null);
			}
			langs ["#SlideShow"].name = "" + value;
		}
	}

	public Dictionary<string, CardLang> getLangs() {
		if (langs == null) return null;
		
		if (!langs.ContainsKey ("#SlideShow")) {
			return langs;
		} else {
			Dictionary<string, CardLang> newLangs = new Dictionary<string, CardLang> (langs);
			newLangs.Remove ("#SlideShow");
			return newLangs;
		}
	}
	
	internal bool HasComment { get { return comments != null && comments.Count > 0; } }
	internal bool HasGraded { get { return status != CardStatus.Not_Graded; } }

	internal CardLang[] 		LangsInOrder {
		get {
			Dictionary<string, CardLang> langs = getLangs ();

			//Debug.Log ("--------------");
			if (langs == null || langs.Count == 0) {
				return new CardLang[0];
			}
			string[] keys = new string[langs.Keys.Count];
			int[] values = new int[langs.Keys.Count];

			// preparation
			int idx = 0;
			foreach (string key in langs.Keys) {
				keys[idx++] = key;
			}

			for (int i = 0; i < keys.Length; i++) {
				string key = keys[i];
				if (Enum.IsDefined( typeof(LangOrder), key)) {
					values[i] = CardLang.LangKeyToIndex(key);
					//Debug.Log (key + ": " + values[i]);
				} else {
					values[i] = 99;
					Debug.LogWarning("unknown enum " + key);
				}
			}
			// sorting
			for (int i = 0; i < values.Length - 1; i++) {
				int smallestIdx = i;
				int smallest = values[i];
				for (int k = i + 1; k < values.Length; k++) {
					int v = values[k];

					if (v < smallest) {
						smallestIdx = k;
						smallest = v;
					}
				}

				string temp = keys[i];
				keys[i] = keys[smallestIdx];
				keys[smallestIdx] = temp;

				int t = values[i];
				values[i] = values[smallestIdx];
				values[smallestIdx] = t;
			}

			// build an array
			CardLang[] langsArray = new CardLang[langs.Keys.Count];
			for (int i = 0; i < keys.Length; i++) {
				string key = keys[i];
				//Debug.Log (key);
				langsArray[i] = langs[key];
			}
			return langsArray;
		}
	}
	
	public JSONNode toJSON() {
		JSONNode node = JSON.Parse("{}");

		node["cardId"] = cardId;
		node["lastUpdatedAt"].AsInt = lastUpdatedAt;
		if (!string.IsNullOrEmpty(objectId))
			node["objectId"] = objectId;

		node["isDeleted"].AsInt = isDeleted ? 1 : 0;

		if (isDeleted) {
			return node;
		}
		node["langs"] = JSON.Parse("{}");
		foreach (CardLang lang in langs.Values) {
			node["langs"][lang.langKey] = lang.toJSON();
		}

		node["imageLastUpdatedAt"].AsInt = imageLastUpdatedAt;

		if (!string.IsNullOrEmpty(oldVersion))
			node["oldVersion"] = oldVersion;
		if (!string.IsNullOrEmpty(projectId)) {
			node["project"] = JSON.Parse("{}");
			node["project"]["objectId"] = projectId;
			node["projectId"] = projectId;
		}

		// teacher
		if (comments != null)
			node["comments"] = string.Join(",", comments.ToArray());
		node["status"].AsInt = (int)status;

		return node;
	}
	internal string ImageName { get { return imageName; } }
	internal string ImagePath {
		get { return Path.Combine(LocalData.Instance.DirectoryPath, ImageName); }
	}

	internal bool	isImageExist {
		get {
			return File.Exists (ImagePath);
		}
	}
	internal bool	isReadyToUpload {
		get {
			if (isDeleted) {
				return true;
			} else {
				return !string.IsNullOrEmpty(cardId.Trim())
					&& AllLangReady
						&& isImageExist;
			}
		}
	}
	internal bool AllLangReady {
		get {
			foreach (CardLang lang in langs.Values) {
				if (!lang.isReadyToUpload) {
					return false;
				}
			}
			return true;
		}
	}
	
	public Card (JSONNode node) {
		langs = new Dictionary<string, CardLang>();

		if (node == null)
			return;

		//Debug.Log (node.ToString());

		if (node.GetKeys().Contains("cardId"))
			cardId = node["cardId"].Value;
		imageName = cardId + ".jpg";
		if (node.GetKeys().Contains("langs")) {
			foreach (JSONNode langNode in node["langs"].Childs) {
				if (!langNode.GetKeys().Contains("langKey")) {
					continue;
				}
				string key = langNode["langKey"].Value;
				CardLang lang = new CardLang(cardId, key, langNode);
				langs[key] = lang;
			}
		}

		if (node.GetKeys().Contains("lastUpdatedAt"))
			lastUpdatedAt = node["lastUpdatedAt"].AsInt;
		if (node.GetKeys().Contains("imageLastUpdatedAt"))
			imageLastUpdatedAt = node["imageLastUpdatedAt"].AsInt;
		if (node.GetKeys().Contains("isDeleted"))
			isDeleted = node["isDeleted"].AsInt == 1 ? true : false;
		
		if (node.GetKeys().Contains("objectId"))
			objectId = node["objectId"].Value;
		if (node.GetKeys().Contains("image"))
			imageUrl = node["image"] ["url"].Value;

		// Teacher parts, comments, etc
		if (node.GetKeys().Contains("comments")) {
			comments = new List<string>();
			foreach (JSONNode comment in node["comments"].Childs) {
				comments.Add (comment.Value);
			}
		} else {
			comments = new List<string>();
		}

		if (node.GetKeys().Contains("status"))
			status = (CardStatus)node["status"].AsInt ;

		///////////////////////////////////////
		// may not exist
		if (node.GetKeys().Contains("authorName"))
			authorName = node["authorName"].Value ;
		if (node.GetKeys().Contains("authorRealName"))
			authorRealName = node["authorRealName"].Value ;
		if (node.GetKeys().Contains("authorId"))
			authorId = node["authorId"].Value ;
		
		if (node.GetKeys().Contains("project"))
			projectId = node["project"]["objectId"].Value ;
		else if (node.GetKeys().Contains("projectId")) {
			projectId = node["projectId"].Value ;
		}
		//Debug.Log ("c 5");
	}

	public string CommemtToString () {
		List<string> output = new List<string>();
		foreach (string comment in comments) {
			output.Add ("- " + comment);
		}

		return string.Join("\n", output.ToArray());
	}

	internal bool					IsCreatedByMe {
		get {
			return !string.IsNullOrEmpty(authorId) && ParseUser.CurrentUser != null && authorId.Equals(ParseUser.CurrentUser.ObjectId);
		}
	}
	
	public bool Equals (Card other) {
		//Debug.Log ("Me: " + toJSON().ToString());
		//Debug.Log ("other: " + other.toJSON().ToString());
		if (other == null)
			return false;

		if (!cardId.Equals(other.cardId)) {
			Debug.Log ("card id not equal " + cardId + " vs " + other.cardId);
			return false;
		}
		if (!authorName.Equals(other.authorName)) {
			Debug.Log ("authorName not equal");
			return false;
		}
		if (!authorId.Equals(other.authorId)) {
			Debug.Log ("authorId not equal");
			return false;
		}

		// lang part

		if (other.langs.Count != langs.Count) {
			Debug.Log ("langs count not equal");
			return false;
		}

		foreach (CardLang myLang in langs.Values) {
			bool found = false;
			foreach (CardLang otherLang in other.langs.Values) {
				if (myLang.langKey.Equals(otherLang.langKey)) {
					found = true;
					// is content totally equal?
					if (!myLang.Equals(otherLang)) {
						//Debug.Log (myLang.langKey + " not equal");
					    return false;
					}
				} else {
					continue;
				}
			}
			if (!found)
				return false;
		}

		return true;
	}
	public bool IsCommentEqual (Card other) {
		foreach (string comment in comments) {
			if (!other.comments.Contains(comment)) {
				return false;
			}
		}
		foreach (string comment in other.comments) {
			if (!comments.Contains(comment)) {
				return false;
			}
		}

		if (status != other.status)
			return false;

		return true;
	}

	public CardLang GetLangByKey (string key) {
		if (langs == null)
			return null;

		foreach (CardLang lang in langs.Values) {
			if (key.Equals(lang.langKey)) {
				return lang;
			}
		}

		return null;
	}

	public string GetDefaultDisplayName () {
		string name = "";
		CardLang langEng = GetLangByKey("zh_w");
		if (langEng != null)
			name = langEng.getName();
		else {
			foreach (CardLang lang in langs.Values) {
				if (!string.IsNullOrEmpty(lang.getName())) {
					name = lang.getName();
					break;
				}
			}
		}
		return name;
	}

	public void DeleteLang (string langKey) {
		CardLang lang = GetLangByKey(langKey);
		if (lang != null) {
			lang.isDeleted = true;
			langs.Remove(lang.langKey);
		}

	}

	public int CompareBy (Card other, CardSortBy key, SortingOrder order) {
		int result = 0;

		switch(key) {
		case CardSortBy.Name: result = CompareLang (other); break;
		case CardSortBy.Status:
			result = status - other.status;
			if (result == 0) result = CompareLang (other);
			break;
		case CardSortBy.LastModify: result = lastUpdatedAt - other.lastUpdatedAt; break;
		case CardSortBy.SlideNumber: result = slideNumber - other.slideNumber; break;
		}
		
		if (order == SortingOrder.Desc)
			result *= -1;
		
		return result;
	}

	public int CompareLang (Card other) {
		if (langs.ContainsKey("en") && other.langs.ContainsKey("en")) {
			return langs["en"].name.CompareTo(other.langs["en"].name);
		} else {
			return cardId.CompareTo(other.cardId);
		}
	}
	
	public static void Sort (Card[] list, CardSortBy key, SortingOrder order) {
		if (list == null || list.Length == 0)
			return;
		MergeSort(list, key, order, 0, list.Length);
	}
	
	private static void MergeSort (Card[] list, CardSortBy key, SortingOrder order, int low, int high) {
		int N = high - low;
		if (N <= 1)
			return;
		
		int mid = low + N / 2;
		MergeSort(list, key, order, low, mid);
		MergeSort(list, key, order, mid, high);
		
		Card[] aux = new Card[N];
		int i = low, j = mid;
		for (int k = 0; k < N; k++) {
			if (i == mid)
				aux[k] = list[j++];
			else if (j == high)
				aux[k] = list[i++];
			else if (list[j].CompareBy(list[i], key, order) < 0)
				aux[k] = list[j++];
			else
				aux[k] = list[i++];
		}
		
		for (int k = 0; k < N; k++)	{
			list[low + k] = aux[k];
		}
	}

	public void SafelyDeleteFiles () {
		LocalData.Instance.SafelyDelete(ImagePath);
		foreach (CardLang lang in langs.Values) {
			LocalData.Instance.SafelyDelete(lang.SoundPath);
		}
	}
}

public class FeaturedCard : Card {
	public FeaturedCard (JSONNode node) : base(node) {
		imageName = "featured-" + cardId + ".jpg";
		foreach (CardLang lang in langs.Values) {
			lang.SetFeaturedCard();
		}
	}
}

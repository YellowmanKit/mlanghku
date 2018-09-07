using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.IO;
using Parse;
using System;

public enum LangOrder {
	//zh = 0,		// Cantonese, spoken
	zh_w = 0,
	zh = 1,// Cantonese, written
	pth = 2,		// PTH, spoken
	pth_w = 3,	// PTH, written
	en = 4,
	hi = 5,		// hindi
	ur = 6,		// Urdu
	ne = 7,		// Nepalese
	tl = 8,		// Tagalog
	jp = 9,
	es = 10,		// 西班牙语
	de = 11,		// 德语
	fr = 12,		// 法语
}


public class CardLang {
	private string 	cardId = "";
	public string	langKey = "";

	public string 	objectId = "";

	public string	name = "";
	public string getName() {
//		return DeprecatedHelper.removeTitleTag (name);
		return name;
	}

	public int 			soundLastUpdatedAt = 0;
	public string		soundUrl = "";

	public bool		isDeleted = false;
	
	protected string	soundName = "";
	internal string SoundName { get { return soundName; } }
	internal string SoundPath {
		get { return soundName=="" ? "" : Path.Combine(LocalData.Instance.DirectoryPath, soundName); }
	}
	internal bool	isSoundExist {
		get {
			return File.Exists (SoundPath);
		}
	}

	internal bool	isReadyToUpload {
		get {
			if (isDeleted) {
				return true;
			} else if (langKey.StartsWith("#")) {
				return true;
			} else {
				return !string.IsNullOrEmpty(name.Trim())
						&& isSoundExist; // 2017/01/09 TODO: why must having sound to upload?  Project requirement?
			}

		}
	}

	public JSONNode toJSON() {
		JSONNode node = JSON.Parse("{}");
		node["langKey"] = langKey;
		node["name"] = name;
		node["soundLastUpdatedAt"].AsInt = soundLastUpdatedAt;
		node["isDeleted"].AsInt = isDeleted ? 1 : 0;

		if (!string.IsNullOrEmpty(objectId))
			node["objectId"] = objectId;
		return node;
	}
	/*
	public CardLang (string _cardId) {
		cardId = _cardId;
		soundName = cardId + "-" + langKey + ".wav";
	}

*/
	public CardLang (string _cardId, string _langKey, JSONNode node) {
		cardId = _cardId;
		langKey = _langKey;

		if (!langKey.StartsWith("#")) soundName = cardId + "-" + langKey + ".wav";

		if (node == null)
			return;

		if (node.GetKeys().Contains("name"))
			name = node["name"].Value.Trim();

		if (node.GetKeys().Contains("soundLastUpdatedAt"))
			soundLastUpdatedAt = node["soundLastUpdatedAt"].AsInt;
		if (node.GetKeys().Contains("isDeleted"))
			isDeleted = node["isDeleted"].AsInt == 1 ? true : false;
		
		if (node.GetKeys().Contains("objectId"))
			objectId = node["objectId"].Value;
		if (node.GetKeys().Contains("sound"))
			soundUrl = node["sound"] ["url"].Value;
	}

	public bool Equals (CardLang other) {
		if (!langKey.Equals(other.langKey))
			return false;
		if (!objectId.Equals(other.objectId))
			return false;
		if (!name.Equals(other.name))
			return false;
		if (soundLastUpdatedAt != other.soundLastUpdatedAt)
			return false;
		if (!soundUrl.Equals(other.soundUrl))
			return false;
		if (isDeleted != other.isDeleted)
			return false;
		return true;
	}

	public void SetFeaturedCard () {
		soundName = "featured-" + cardId + "-" + langKey + ".wav";
	}


	public static int LangKeyToIndex (string key) {
		try {
			int idx = (int) (Enum.Parse(typeof(LangOrder), key, true));
			return idx;
		} catch (Exception e) {
			Debug.LogError(e.Message);
			return -99;
		}
	}

















}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Parse;

public class Search_Prefix : Search_MyCards {

	public InputField prefix;

	public void EventPrefixSearchButtonPressed(){
		searchButton.enabled = false;
		searchButton.GetComponentInChildren<Text> ().text = "Searching...";
		//SetSearchDataTime ();
		StartCoroutine (PrefixSearchRoutine ());
	}

	IEnumerator PrefixSearchRoutine(){
		var langQuery = ParseObject.GetQuery ("CardLang").WhereStartsWith ("name", prefix.text);
		var cardQuery = ParseObject.GetQuery ("Card").WhereMatchesQuery ("langs", langQuery).WhereEqualTo("toShare",true).Limit (limit);
		switch ((SortBy)sortBy.value) {
		case SortBy.FromNewestToOldest:
			cardQuery = cardQuery.OrderByDescending ("createdAt");
			break;
		case SortBy.FromOldestToNewest:
			cardQuery = cardQuery.OrderBy ("createdAt");
			break;
		}
		if (skip > 0) {
			cardQuery = cardQuery.Skip (skip);
		}

		List<ParseObject> cards = new List<ParseObject> ();
		bool queryDone = false;
		cardQuery.FindAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				IEnumerable<ParseObject> resultCards = t.Result;
				foreach(ParseObject card in resultCards){
					cards.Add(card);
					skip++;
					limit++;
				}
			}else{
				Debug.Log("Query failed");
			}
			queryDone = true;
		});
		yield return new WaitUntil (()=> queryDone);
		//Debug.Log("Query done");
		src.PushSearchResult (cards);
		searchButton.enabled = true;
		searchButton.GetComponentInChildren<Text> ().text = "Search for more result";
	}


}

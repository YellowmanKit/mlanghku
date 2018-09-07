using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Parse;

public class Search_Standard : Search_MyCards {

	public Dropdown schoolDropdown;

	public Dictionary<string,ParseObject> schoolParseObj = new Dictionary<string,ParseObject>();

	public List<string> schoolNames = new List<string>();

	public List<string> testSchoolParseObjId = new List<string>(){"dvNvpH6MbU","jv8KsEa6BC","10Htdrh9Fx","H6zQcSGHGe"};//"4aeNBO5PEG" is Test School

	bool schoolListSet;

	public void EventStandardSearchButtonPressed(){
		searchButton.enabled = false;
		searchButton.GetComponentInChildren<Text> ().text = "Searching...";
		SetSearchDataTime ();
		StartCoroutine (StandardSearchRoutine ());
	}

	IEnumerator StandardSearchRoutine(){
		var query = ParseObject.GetQuery ("Card").WhereGreaterThanOrEqualTo ("createdAt", startDate).WhereLessThanOrEqualTo ("createdAt", endDate).WhereEqualTo("toShare",true).Limit (limit);
		switch ((SortBy)sortBy.value) {
		case SortBy.FromNewestToOldest:
			query = query.OrderByDescending ("createdAt");
			break;
		case SortBy.FromOldestToNewest:
			query = query.OrderBy ("createdAt");
			break;
		}
		if (schoolDropdown.value > 0) {
			var authorQuery = ParseObject.GetQuery ("User").WhereEqualTo ("school", schoolParseObj [schoolDropdown.captionText.text]);
			query = query.WhereMatchesQuery ("author", authorQuery);
		}
		if (skip > 0) {
			query = query.Skip (skip);
		}


		List<ParseObject> cards = new List<ParseObject> ();
		bool queryDone = false;
		query.FindAsync ().ContinueWith (t => {
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

	public void SetSchoolDropdown(){
		if (!schoolListSet) {
			StartCoroutine (SetSchoolDropdownRoutine ());
			schoolListSet = true;
		}
	}

	IEnumerator SetSchoolDropdownRoutine(){
		var query = ParseObject.GetQuery ("School");
		bool queryDone = false;
		query.FindAsync ().ContinueWith (t => {
			IEnumerable<ParseObject> schools = t.Result;
			foreach(ParseObject school in schools){
				if(!SchoolIsTestAccount(school.ObjectId)){
					schoolParseObj.Add(school.Get<string>("fullName"),school);
					schoolNames.Add(school.Get<string>("fullName"));
					//Debug.Log(school.ObjectId);
				}
			}
			queryDone = true;
		});
		yield return new WaitUntil (() => queryDone);

		schoolNames.Sort ();

		List<Dropdown.OptionData> schoolDropdownOption = new List<Dropdown.OptionData>();
		foreach (string schoolName in schoolNames) {
			var schoolOption = new Dropdown.OptionData ();
			schoolOption.text = schoolName;
			schoolDropdownOption.Add (schoolOption);
		}
		schoolDropdown.AddOptions (schoolDropdownOption);
		schoolDropdown.options[0].text = "All schools";
		schoolDropdown.captionText.text = "All schools";
		schoolDropdown.value = 0;
	}

	bool SchoolIsTestAccount(string objId){
		foreach (string testObjId in testSchoolParseObjId) {
			if (testObjId.Equals (objId)) {
				return true;
			}
		}
		return false;
	}
}
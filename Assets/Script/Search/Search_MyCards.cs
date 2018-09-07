using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Parse;


public class Search_MyCards : MonoBehaviour {

	protected enum AcademicYear{
		Year2016to2017FirstSemester,
		Year2016to2017SecondSemester,
		Year2017to2018FirstSemester,
		AllTime
	}

	protected enum SortBy{
		FromNewestToOldest,
		FromOldestToNewest
	}

	enum Type{
		Featured,
		Passed,
		Failed,
		NotGraded,
		All
	}

	public Dropdown academicYear, sortBy, type;

	public SearchResultControl src;

	public Button searchButton;

	protected System.DateTime startDate;

	protected System.DateTime endDate;

	protected int skip,resultPerSearch,limit;

	void Start(){
		resultPerSearch = 18;
		ResetSkipLimit ();
	}

	public void EventSearchButtonPressed(){
		searchButton.enabled = false;
		searchButton.GetComponentInChildren<Text> ().text = "Searching...";
		SetSearchDataTime ();
		StartCoroutine (MyCardSearchRoutine ());
	}

	int GetCardStatusValue(){
		switch ((Type)type.value) {
		case Type.Featured:
			return 3;
		case Type.Passed:
			return 1;
		case Type.Failed:
			return 2;
		case Type.NotGraded:
			return 0;
		default:
			return -1;
		}
	}

	IEnumerator MyCardSearchRoutine(){
		var query = ParseObject.GetQuery ("Card").WhereGreaterThanOrEqualTo ("createdAt", startDate).WhereLessThanOrEqualTo ("createdAt", endDate).WhereEqualTo ("author", ParseUser.CurrentUser).Limit (limit);
		if (type.value != 4) {
			//Debug.Log ("Add type filter " + GetCardStatusValue());
			query = query.WhereEqualTo ("status", GetCardStatusValue());
		}
		switch ((SortBy)sortBy.value) {
		case SortBy.FromNewestToOldest:
			query = query.OrderByDescending ("createdAt");
			break;
		case SortBy.FromOldestToNewest:
			query = query.OrderBy ("createdAt");
			break;
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

	protected void SetSearchDataTime(){
		switch ((AcademicYear)academicYear.value) {
		case AcademicYear.Year2016to2017FirstSemester:
			startDate = new System.DateTime (2016, 9, 1, 0, 0, 0);
			endDate = new System.DateTime (2017, 2, 1, 0, 0, 0);
			break;
		case AcademicYear.Year2016to2017SecondSemester:
			startDate = new System.DateTime (2017, 2, 1, 0, 0, 0);
			endDate = new System.DateTime (2017, 9, 1, 0, 0, 0);
			break;
		case AcademicYear.Year2017to2018FirstSemester:
			startDate = new System.DateTime (2017, 9, 1, 0, 0, 0);
			endDate = new System.DateTime (2018, 2, 1, 0, 0, 0);
			break;
		default:
			startDate = new System.DateTime (1970, 9, 1, 0, 0, 0);
			endDate = new System.DateTime (9999, 2, 1, 0, 0, 0);
			break;
		}
	}

	public void ResetSkipLimit(){
		skip = 0;
		limit = resultPerSearch;
	}
}

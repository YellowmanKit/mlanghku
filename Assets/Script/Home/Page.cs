using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Parse;
using SimpleJSON;

public abstract class Page : MonoBehaviour {
	
	public enum PageState {
		READY,
		INIT,
		WAITING_FOR_DATA,
		DATA_RETRIEVED,
		END
	}

	public PageState state = PageState.READY;

	/*
	void Start () {
	
	}

	void Update () {
	
	}
	*/

	protected void IdentityCheck () {
		Debug.Log ("[Page] [TODO] IdentityCheck " + LocalData.Instance.UserIdentity.ToString ());
		/*
		ParseUserIdentity identity = LocalData.Instance.UserIdentity;
		
		if (identity == ParseUserIdentity.Unknown) {
			Debug.LogError ("Unknown role");
			ParseUser.LogOutAsync ();
			Fader.Instance.FadeIn (0.5f)
				.LoadLevel ("Login")
					.FadeOut (0.5f);
		} else if (identity == ParseUserIdentity.Student && !Application.loadedLevelName.Equals ("Home_Student")) {
			// student go a wrong page
			Application.LoadLevel ("Home_Student");
		} else if (identity == ParseUserIdentity.Teacher && !Application.loadedLevelName.Equals ("Home_Teacher")) {
			// teacher go a wrong page
			Application.LoadLevel ("Home_Teacher");
		}
		*/
	}

	#region Socket
	public abstract void Init (Dictionary<string, object> data);
	public abstract void OnPageShown (Dictionary<string, object> data);

	public virtual bool AllowPopEvent () {
		return true;
	}
	public virtual bool HandledEscape () {
		return false;
	}
	//public abstract void DataLoaded ();
	#endregion

	#region Parse
	protected void RenewUserData ()
	{
		Debug.Log ("[Page] RenewUserData()");
		if (LocalData.Instance.IsTestUser > 0) {
			state = PageState.DATA_RETRIEVED;
			return;
		}

		LoadingTask task = new LoadingTask("Loading Data...");
		//task.DisplayMessage = "Loading Data...";
		//task.CanCancel = true;
		GameManager.Instance.LoadingTasks.Add (task);

		Dictionary<string, object> data = new Dictionary<string, object>();
		data ["timestamp"] = GameManager.GetCurrentTimestemp();

		ParseCloud.CallFunctionAsync<string>("RenewUser", data).ContinueWith(t => {
			if (t.IsFaulted) {
				Debug.LogError("[Page] RenewUserData() Fail");
				foreach(var e in t.Exception.InnerExceptions) {
					ParseException parseException = (ParseException) e;
					if (parseException.Message.Contains("resolve host")) {
						NavigationBar.Instance.SetConnection(false);
					} else {
						int errorCode = 0;
						int.TryParse(parseException.Message, out errorCode);
						switch(errorCode) {
						case 99:
							GameManager.Instance.AlertTasks.Add (
								new AlertTask(
									GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
									, GameManager.Instance.Language.getString("incorrect-time", CaseHandle.FirstCharUpper)
									, TextAnchor.MiddleLeft));
							break;
						default:
							GameManager.Instance.AlertTasks.Add (
								new AlertTask(
									GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
									, parseException.Code + ": " + parseException.Message
									, TextAnchor.MiddleLeft));
							break;
						}
					}
				}
			} else {
				NavigationBar.Instance.SetConnection(true);
				string result = t.Result;
				Debug.Log ("[Page] RenewUser() result:[" + result + "]");
				JSONNode node = JSON.Parse(result);

				CacheManager.Instance.HandleRenewUser(node); // setup a cached version
			}
			task.Loading = false;

			state = PageState.DATA_RETRIEVED;
		});
	}
	#endregion
}

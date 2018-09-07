using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Parse;
using SimpleJSON;

public class Teacher_Course_AddProject : Page {

	private string					_dynSelectedCourseId = "";
	private string					_dynObjectId = "";				// edit?

	public Toggle 					_toggleSlideShow;

	public Text						_txtSubTitle = null;

	public Image					_imgIcon = null;
	private Texture2D				_textureIcon = null;
	public InputField				_inputProjectName = null;
	public InputField				_inputProjectDesc = null;

	public Text						_txtAllowSubmissionFrom = null;
	public Text						_txtDeadline = null;
	private Text					_dynDatePickerTextFeild = null;
	private bool					_dynImageChange = false;

	public GameObject				_btnDelete = null;

	bool shouldRemoveTexture;

	private char nameValidate(char addedChar) {
		//if (!char.IsLetterOrDigit (addedChar) && addedChar != ' ') {
		//	return '\0';
		//}
		if (!char.IsLetterOrDigit (addedChar) && addedChar != ' ' && addedChar != ',' && addedChar != '。' && addedChar != '﹐') {
			return '\0';
		}

		return addedChar;
	}

	// Use this for initialization
	void Start () {
		//InitDefaultDateTime ();

		_inputProjectName.onValidateInput += delegate(string input, int charIndex, char addedChar) { return nameValidate( addedChar ); };
	}

	void OnDestroy () {
		if (shouldRemoveTexture) {
			Destroy (_textureIcon);
			_textureIcon = null;
		}
	}

	// Update is called once per frame
	void Update () {
		switch (state) {
		case PageState.READY:
			break;
		case PageState.INIT:
			break;
		case PageState.WAITING_FOR_DATA:
			break;
		case PageState.DATA_RETRIEVED:
			NavigationBar.Instance.PopView();
			state = PageState.END;
			break;
		case PageState.END:
			break;
		}
	}

	#region Socket
	public override void OnPageShown (Dictionary<string, object> data) {

	}

	public override	void Init (Dictionary<string, object> data) {
		if (data != null) {
			if (data.ContainsKey("courseId")) {
				_dynSelectedCourseId = (string)(data ["courseId"]);

				if (data.ContainsKey("objectId")) {
					// if existed, it is modify
					_btnDelete.gameObject.SetActive(true);
					_dynObjectId = (string)(data ["objectId"]);


					JSONNode projectNode = LocalData.Instance.Data["my-projects"][_dynObjectId];
					Project project = new Project(projectNode);

					_inputProjectName.text = project.getTitle();
					_toggleSlideShow.isOn = project.isSlideShow ();
					_inputProjectDesc.text = project.desc;

					_txtDeadline.text = project.dueDate.ToString("dddd, d MMM yyyy, hh:mm tt");

					if (File.Exists(project.ImagePath)) {
//						_imgIcon.sprite = DownloadManager.FileToSprite (project.ImagePath);
						DownloadManager.Instance.AddLinkage(project.ImagePath, null, _imgIcon);
						_textureIcon = _imgIcon.sprite.texture;
					} else {
						_imgIcon.sprite = DownloadManager.Instance.spriteNoImage;
						Debug.Log ("NO image");
					}
					shouldRemoveTexture = false;

					string navigationTitle = GameManager.Instance.Language.getString("modify-project");
					NavigationBar.Instance.SetTitle (navigationTitle);

					string subTitle = String.Format(GameManager.Instance.Language.getString("modify-string"), project.getTitle());
					_txtSubTitle.text = subTitle;

				} else {
					_btnDelete.gameObject.SetActive(false);
					// create new project
					string navigationTitle = GameManager.Instance.Language.getString("add-project");
					NavigationBar.Instance.SetTitle (navigationTitle);

					string courseTitle = LocalData.Instance.Data["my-course"] [_dynSelectedCourseId] ["courseTitle"].Value;
					string subTitle = String.Format(GameManager.Instance.Language.getString("add-project-to-val"), courseTitle);
					_txtSubTitle.text = subTitle;

					InitDefaultDateTime ();
				}
			} else {
				Debug.LogWarning("TeacherCourse: SocketBecomeVisible data no key");
			}
		}
	}
	#endregion

	#region Event
	public void EventButtonSelectIcon () {
		ImagePicker.Instance.ShowImagePicker (ProjectIconSelected);
	}

	private void ProjectIconSelected (Texture2D img) {
		if (img == null) {
			Debug.LogWarning("User cancel");
			return;
		}

		if (shouldRemoveTexture) {
//			Texture2D.DestroyImmediate(_textureIcon, false);
			Destroy(_textureIcon);
		}

		_textureIcon = img;
		_dynImageChange = true;

		Sprite sprite = Sprite.Create(_textureIcon, new Rect(0, 0, _textureIcon.width, _textureIcon.height), Vector2.zero);
		_imgIcon.sprite = sprite;
		shouldRemoveTexture = true;

		//img = null;
		//Destroy (img);
		img = null;
	}

	public void EventButtonDatePicker (Text textfield) {
		_dynDatePickerTextFeild = textfield;

		DatePicker.Instance.ShowDatePicker (Convert.ToDateTime(textfield.text),DateTimeSelected);
	}

	private void DateTimeSelected (DateTime date) {
		if (_dynDatePickerTextFeild != null) {
			_dynDatePickerTextFeild.text = date.ToString("dddd, d MMM yyyy, hh:mm tt");
		} else {
			Debug.LogWarning("No textfield");
		}
	}

	public void EventButtonDelete () {
		Debug.Log ("Ask delete");
		// sure to delete?
		string[] option = new string[]{
			GameManager.Instance.Language.getString("no", CaseHandle.FirstCharUpper),
			GameManager.Instance.Language.getString("yes", CaseHandle.FirstCharUpper)
		};

		AlertTask confirm = new AlertTask(
			""
			, GameManager.Instance.Language.getString("confirm-delete-project", CaseHandle.FirstCharUpper)
			, option
			, TextAnchor.MiddleCenter);
		confirm.buttonCompletedEvent_1 += ConfirmToDelete;
		GameManager.Instance.AlertTasks.Add (confirm);
	}

	private void ConfirmToDelete () {
		Debug.Log ("ConfirmToDelete");
		SubmitData (null, 1);
	}

	public void EventButtonSubmit () {

		// validation
		int errorCount = 0;
		string error = "";//GameManager.Instance.Language.getString("error-semi", CaseHandle.FirstCharUpper);
		string projectName = _inputProjectName.text;
		//string projectDesc = _inputProjectDesc.text;
		if (string.IsNullOrEmpty(projectName)) {
			errorCount++;
			error += "\n- " + GameManager.Instance.Language.getString("project-name-empty", CaseHandle.FirstCharUpper);
		}

		// texture
		if (_textureIcon == null) {
			errorCount++;
			error += "\n- " + GameManager.Instance.Language.getString("icon-not-set", CaseHandle.FirstCharUpper);
		}
		if (string.IsNullOrEmpty(_dynSelectedCourseId)) {
			errorCount++;
			error += "\n- " + GameManager.Instance.Language.getString("unknown-course", CaseHandle.FirstCharUpper);
		}

		DateTime submissionFrom = Convert.ToDateTime (_txtAllowSubmissionFrom.text);
		DateTime dueDate = Convert.ToDateTime (_txtDeadline.text);
		if (dueDate.CompareTo(DateTime.Now) <= 0) {
			// due date earlier than now
			errorCount++;
			error += "\n- " + GameManager.Instance.Language.getString("due-date-too-early", CaseHandle.FirstCharUpper);
		}
		if (dueDate.CompareTo(submissionFrom) <= 0) {
			// due date earlier than submissionFrom
			errorCount++;
			error += "\n- " + GameManager.Instance.Language.getString("submission-date-too-late", CaseHandle.FirstCharUpper);
		}

		if (errorCount > 0) {
			error = error.Remove(error.IndexOf("\n"),"\n".Length);
			GameManager.Instance.AlertTasks.Add (
				new AlertTask(
					GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
					, error
					, TextAnchor.MiddleLeft));
			return;
		}

		// is test data?

		if (string.IsNullOrEmpty(_dynObjectId) || _dynImageChange) {
			// create new project / image changed
			// upload image
			UploadImage();

		} else {
			// modify existing project and image not changed
			SubmitData ();
		}
	}

	public void EventButtonRotate (int angle) {
		Texture t = _textureIcon;
		if (angle == -1) {
			_textureIcon = TextureRotate.rotateTextureLeft(_textureIcon);
		} else if (angle == 1) {
			_textureIcon = TextureRotate.rotateTextureRight(_textureIcon);
		}
		if (shouldRemoveTexture) Destroy(t);
		shouldRemoveTexture = true;

		Sprite sprite = Sprite.Create(_textureIcon, new Rect(0, 0, _textureIcon.width, _textureIcon.height), Vector2.zero);
		_imgIcon.sprite = sprite;

		_dynImageChange = true;
		//_currentCard.imageLastUpdatedAt = GameManager.GetCurrentTimestemp();
		sprite = null;
	}
	#endregion

	#region Helper
	private void InitDefaultDateTime () {
		DateTime date = DateTime.Now;
		date = new DateTime(date.Year, date.Month, date.Day, 
			date.Hour, Mathf.CeilToInt(date.Minute / 5) * 5, 0);

		_txtAllowSubmissionFrom.text = date.ToString("dddd, d MMM yyyy, hh:mm tt");
		_txtDeadline.text = date.AddDays(7).ToString("dddd, d MMM yyyy, hh:mm tt");
	}

	#endregion

	#region Parse
	private void UploadImage () {
		if (LocalData.Instance.IsTestUser > 0) {
			// test user mode
			SubmitData(null);
		} else {
			// realistic
			LoadingTask task = new LoadingTask("Uploading File...", false); // v1.3 default is cannot be cancelled
			//task.DisplayMessage = "Uploading File...";
			GameManager.Instance.LoadingTasks.Add (task);

//			string projectName = _inputProjectName.text;

			byte[] dataImage = _textureIcon.EncodeToJPG();
			ParseFile fileImage = new ParseFile("icon.jpg", dataImage);

			fileImage.SaveAsync().ContinueWith(t => {
				if (t.IsFaulted) {
					string error = "";
					foreach(var e in t.Exception.InnerExceptions) {
						ParseException parseException = (ParseException) e;
						error += "\n" + parseException.Code + ": " + parseException.Message;
					}
					GameManager.Instance.AlertTasks.Add (
						new AlertTask(
							GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
							, GameManager.Instance.Language.getString("image-upload-failed", CaseHandle.FirstCharUpper) + error
							, TextAnchor.MiddleLeft));
				} else {
					// save parse record
					SubmitData(fileImage);
				}
				task.Loading = false;
			});
		}
	}

	private void SubmitData (ParseFile projectIcon = null, int isDeleted = 0) {

		if (LocalData.Instance.IsTestUser > 0) {
			// test user mode
			if (isDeleted == 1) {
				LocalData.Instance.Data["my-projects"].Remove(_dynObjectId);
			} else {
				if (string.IsNullOrEmpty(_dynObjectId)) {
					_dynObjectId = "project-" + GameManager.GetCurrentTimestemp();
				}

				JSONNode projectNode = JSON.Parse("{}");
				projectNode["objectId"] = _dynObjectId;
				projectNode["projectTitle"] = _inputProjectName.text;
				if (_toggleSlideShow.isOn) projectNode["projectTitle"] += "#SlideShow";
				projectNode["projectDesc"] = _inputProjectDesc.text;
				projectNode["course"] = JSON.Parse("{}");
				projectNode["course"]["objectId"] = _dynSelectedCourseId;
				DateTime dueDate = Convert.ToDateTime (_txtDeadline.text);
				projectNode["dueDate"] = JSON.Parse("{}");
				projectNode["dueDate"]["iso"] = dueDate.ToString();

				Project p = new Project(projectNode);

				byte[] dataImage = _textureIcon.EncodeToJPG();
				File.WriteAllBytes(p.ImagePath, dataImage);
				LocalData.Instance.Data["my-projects"][_dynObjectId] = projectNode;
			}

			LocalData.Instance.Save();

			state = PageState.DATA_RETRIEVED;

		} else {
			LoadingTask task = new LoadingTask("Saving...");
			//task.DisplayMessage = "Saving...";
			//task.CanCancel = true;
			GameManager.Instance.LoadingTasks.Add (task);

			Dictionary<string, object> data = new Dictionary<string, object>();
			data ["timestamp"] = GameManager.GetCurrentTimestemp();
			data ["projectTitle"] = _inputProjectName.text;
			if (_toggleSlideShow.isOn) data["projectTitle"] += "#SlideShow";
			data ["projectDesc"] = _inputProjectDesc.text;
			if (projectIcon != null)
				data ["projectIcon"] = projectIcon;
			data ["courseId"] = _dynSelectedCourseId;

			if (!string.IsNullOrEmpty(_dynObjectId)) {
				data ["objectId"] = _dynObjectId;
			}
			data ["isDeleted"] = isDeleted.ToString();

			DateTime dueDate = Convert.ToDateTime (_txtDeadline.text).ToUniversalTime ();
			data ["dueDate"] = dueDate;

			ParseCloud.CallFunctionAsync<string>("Teacher_AddProject", data).ContinueWith(t => {
				if (t.IsFaulted) {
					Debug.LogError("Fail Teacher_AddProject");
					foreach(var e in t.Exception.InnerExceptions) {
						ParseException parseException = (ParseException) e;
						if (parseException.Message.Contains("resolve host")) {
							NavigationBar.Instance.SetConnection(false);
						} else {
							int errorCode = 0;
							int.TryParse(parseException.Message, out errorCode);
							switch(errorCode) {
							case 1:
								GameManager.Instance.AlertTasks.Add (
									new AlertTask(
										GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
										, GameManager.Instance.Language.getString("submission-date-too-late", CaseHandle.FirstCharUpper)
										, TextAnchor.MiddleLeft));
								break;
							case 2:
								GameManager.Instance.AlertTasks.Add (
									new AlertTask(
										GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
										, GameManager.Instance.Language.getString("due-date-too-early", CaseHandle.FirstCharUpper)
										, TextAnchor.MiddleLeft));
								break;
							case 3:
								GameManager.Instance.AlertTasks.Add (
									new AlertTask(
										GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
										, GameManager.Instance.Language.getString("image-upload-failed", CaseHandle.FirstCharUpper)
										, TextAnchor.MiddleLeft));
								break;
							case 6:
								GameManager.Instance.AlertTasks.Add (
									new AlertTask(
										GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
										, GameManager.Instance.Language.getString("no-permission", CaseHandle.FirstCharUpper)
										, TextAnchor.MiddleLeft));
								break;
							case 7:
								GameManager.Instance.AlertTasks.Add (
									new AlertTask(
										GameManager.Instance.Language.getString("error", CaseHandle.FirstCharUpper)
										, GameManager.Instance.Language.getString("project-duplicate-name", CaseHandle.FirstCharUpper)
										, TextAnchor.MiddleLeft));
								break;
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
					Debug.Log ("Teacher_AddProject: " + result);
					JSONNode node = JSON.Parse(result);

					CacheManager.Instance.HandleTeacherSpecificCourseData(_dynSelectedCourseId, node);
					state = PageState.DATA_RETRIEVED;
				}
				task.Loading = false;
			});
		}

	}

	#endregion
}

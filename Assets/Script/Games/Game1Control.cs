using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SimpleJSON;
using Parse;
using System.Threading.Tasks;
using WWUtils.Audio;

public class Game1Control : MonoBehaviour {

	public GameObject enlargeTextPanel,rankingObj,rankingTitle;

	public GameObject[] pages;

	public Text scoreText,questCountText,correctCountText,wrongCountText,yourScoreValue,bestScoreValue,notEnoughCardWarning,missionName;

	public BounsCounterControl BCC;

	public CanvasGroup endgameScreen,stayInFrontGroup,suddenDeadGroup;

	public AudioSource[] UIAS;

	public Animator suddenDeadAni;

	private Text[] answer,language;

	private Image[] correct,wrong,answerImg,imgCorrect,imgWrong;

	private Image questionImage;

	public GameObject startBtn,bounsStar,starPosi,addedScore,instruction;

	private GameObject nextBtn,endGameBtn,questionSound,soundQuestionLang;

	private AudioSource[] sound;

	private int index,quesCardNum,usedcard,usingPage,projTag,loopCount,score,maxQuestion,questCount,correctCount,wrongCount,suddenDeadTag,questionType;

	private List<FeaturedCard> featuredCards;

	private List<int> projTagForFeaturedCards,usedCards,usedProjId;

	private bool[] trueAns;

	private List<string> projectsId;

	private bool answered;

	private float displayScore,bounsTime;

	private Coroutine suddenDeadRoutine;

	private string[] cardIdInAns;

	private bool missionMode;

	private ParseObject mission;

	private List<string> projectsIdToUse = new List<string> ();

	private bool isTeacher;


	// Use this for initialization
	void Start () {
		InitializeMode ();
//		string userName = LocalData.Instance.Data ["user"] ["realName"].Value + " <color=#838383FF>(" + LocalData.Instance.Data ["user"] ["username"].Value + ")</color>";
//		Debug.Log(userName);
		isTeacher = (LocalData.Instance.UserIdentity == ParseUserIdentity.Teacher);
		bounsTime = 5f;
		maxQuestion = 10;
		score = 0;
		answered = true;
		usedCards = new List<int> ();
		usedProjId = new List<int> ();
		projTagForFeaturedCards = new List<int> ();
//		featuredCards = new List<Card> ();
		featuredCards = new List<FeaturedCard> ();
		projectsId = new List<string> ();
		answerImg = new Image[2];
		trueAns = new bool[3];
		answer = new Text[3];
		language = new Text[3];
		correct = new Image[3];
		wrong = new Image[3];
		imgCorrect = new Image[2];
		imgWrong = new Image[2];
		sound = new AudioSource[3];
		cardIdInAns = new string[3];
		CollectData ();
		SetPageObject (0);
		UpdateCountText ();

	}

	private void InitializeMode(){
		Game1EntryControl entryControl = GameObject.Find ("Game1EntryControl").GetComponent<Game1EntryControl> ();
		if (entryControl.missionMode) {
			missionMode = true;
			mission = entryControl.selectedMission;
			missionName.text = mission.Get<string> ("missionName");
			projectsIdToUse = entryControl.projectsIdToUseInMission;
		}
		Destroy (entryControl.gameObject);
	}

	public void AddScore(int value){
		score += value;
		addedScore.GetComponent<Text> ().text = "+" + value;
		addedScore.GetComponent<Text> ().color = Color.green;
		addedScore.GetComponent<RectTransform> ().localScale = new Vector3 (1.5f, 1.5f, 1f);
		//scoreText.text = "" + score;
	}

	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (KeyCode.Space) == true) {
			PlayerPrefs.DeleteAll ();
		}
		UpdateScore ();
		UpdateAddedScore ();
	}

	public void EventEnlargeText(int index){
		enlargeTextPanel.transform.GetChild (0).gameObject.GetComponent<Text> ().text = answer [index].text;
		enlargeTextPanel.transform.GetChild (1).gameObject.GetComponent<Text> ().text = language [index].text;
		enlargeTextPanel.SetActive (true);
	}

	public void EventCloseEnlargeText(){
		enlargeTextPanel.SetActive (false);
	}

	private void UpdateCountText(){
		questCountText.text = "" + questCount + "/" + maxQuestion;
		//correctCountText.text = "" + correctCount;
		//wrongCountText.text = "" + wrongCount;
		if(suddenDeadTag > 0){
			questCountText.color = Color.red;
		}
	}

	private void UpdateAddedScore(){
		if (addedScore.GetComponent<RectTransform> ().localScale.x > 1f) {
			float newScale = addedScore.GetComponent<RectTransform> ().localScale.x - Time.deltaTime;
			addedScore.GetComponent<RectTransform> ().localScale = new Vector3 (newScale, newScale, 1f);
		}
		if (addedScore.GetComponent<Text> ().color.a > 0f) {
			Color C = addedScore.GetComponent<Text> ().color;
			C.a -= Time.deltaTime * 2f;
			addedScore.GetComponent<Text> ().color = C;
		}
	}

	private void UpdateScore(){
		displayScore += (score - displayScore) * Time.deltaTime * 5f;
		scoreText.text = "" + Mathf.Round (displayScore);
	}

	private void SetPageObject(int pageNum){
		questionImage = pages[pageNum].transform.FindChild ("QuestionImage").gameObject.GetComponent<Image> ();
		nextBtn = pages [pageNum].transform.FindChild ("NextButton").gameObject;
		endGameBtn = pages [pageNum].transform.FindChild ("EndButton").gameObject;
		questionSound = pages [pageNum].transform.FindChild ("QuestionSound").gameObject;
		soundQuestionLang = pages [pageNum].transform.FindChild ("SoundQuestionLang").gameObject;
		answerImg [0] = pages [pageNum].transform.FindChild ("AnswerImage1").gameObject.GetComponent<Image> ();
		answerImg [1] = pages [pageNum].transform.FindChild ("AnswerImage2").gameObject.GetComponent<Image> ();
		imgCorrect [0] = answerImg [0].transform.FindChild ("correct").gameObject.GetComponent<Image> ();
		imgCorrect [1] = answerImg [1].transform.FindChild ("correct").gameObject.GetComponent<Image> ();
		imgWrong [0] = answerImg [0].transform.FindChild ("wrong").gameObject.GetComponent<Image> ();
		imgWrong [1] = answerImg [1].transform.FindChild ("wrong").gameObject.GetComponent<Image> ();

		if (questionSound == null || soundQuestionLang == null) {
			Debug.Log ("Cant find sound quest obj");
		}
		for (int i = 1; i < 4; i++) {
			Transform answerButton = pages [pageNum].transform.FindChild ("AnswerButton" + i);
			answer [i - 1] = answerButton.FindChild ("Answer Text " + i).gameObject.GetComponent<Text> ();
			language[i - 1] = answerButton.FindChild ("lang" + i).gameObject.GetComponent<Text> ();
			correct[i-1] = answerButton.FindChild ("correct" + i).gameObject.GetComponent<Image> ();
			wrong[i-1] = answerButton.FindChild ("wrong" + i).gameObject.GetComponent<Image> ();
			sound[i - 1] = answerButton.FindChild ("Sound Button " + i).gameObject.GetComponent<AudioSource> ();
		}
		usingPage = pageNum;
	}

	private void CollectData (){
		//featuredCards = new Card[100];

		foreach (JSONNode projectNode in LocalData.Instance.Data["my-projects"].Childs) {
			Project project = new Project (projectNode);
			string id = project.id;
			if (missionMode) {
				Debug.Log ("Mission mode active");
				if (projectsIdToUse.Contains (id)) {
					projectsId.Add (id);
				} else {
					Debug.Log ("Ignore proj: " + id);
				}
			} else {
				projectsId.Add (id);
			}
			//Debug.Log (id);
		}
		int count = 0;
		while (count < 10000) {
			count++;
			int randomProjIdNum = GetRandomProjIdNum ();
			if (randomProjIdNum == -1) {
				return;
			}
			usedProjId.Add (randomProjIdNum);
		

			List<FeaturedCard> FCards = LocalData.Instance.GetFeaturedCardsFromProject (projectsId [randomProjIdNum]);
			//Debug.Log ("Found local cards : " + FCards.Count + " In project no. " + projTag);
			foreach (FeaturedCard fcard in FCards) {
				//string cardKey = fcard.cardId;
				//if (UseThisCard(cardKey)) {
					featuredCards.Add (fcard);
					projTagForFeaturedCards.Add (projTag);
				//}
			}
			if (featuredCards.Count >= 100) {
				//Debug.Log ("Got enough featured cards!");
				return;
			}

			projTag++;
		}
	}

	private bool UseThisCard(string id){
		int cardAnsweredCorrectlyCount = PlayerPrefs.GetInt (id);
		int dice = Random.Range (0, 30);
		if (dice > cardAnsweredCorrectlyCount) {
			return true;
		} else {
			return false;
		}
	}

	private int GetRandomProjIdNum(){
		if (projectsId.Count == usedProjId.Count) {
			Debug.Log ("All proj are used! Projects: " + projectsId.Count + " Cards: " + featuredCards.Count);
			return -1;
		}
		int count = 0;
		while(count < 10000){
			count++;
			int returnValue = Random.Range (0, projectsId.Count);
				if(!usedProjId.Contains(returnValue)){
				return returnValue;
			}
		}
		//Debug.Log ("All proj are used!");
		return -1;
	}

	public void StartGame(){
		if (featuredCards.Count < 3) {
			StartCoroutine (NotEnoughCardWarningRoutine ());
			return;
		}
		startBtn.SetActive (false);
		instruction.SetActive (false);

		//AskQuestion ();
		//SetBounsStar ();
		StartCoroutine (TurnPage ());
	}

	IEnumerator NotEnoughCardWarningRoutine (){
		notEnoughCardWarning.enabled = true;
		yield return new WaitForSeconds (5);
		notEnoughCardWarning.enabled = false;
	}

	public void NextQuestion(){
		//ResetAnswer ();
		//AskQuestion ();
		UIAS [0].Play ();
		nextBtn.GetComponent<Button>().enabled = false;
		StartCoroutine (TurnPage ());
	}

	IEnumerator TurnPage(){
		SetPageObject ((usingPage + 1) % 2);
		AskQuestion ();
		RectTransform RTprevious = pages [(usingPage + 1) % 2].GetComponent<RectTransform> ();
		RectTransform RTusing = pages [usingPage].GetComponent<RectTransform> ();
		RTusing.rotation = Quaternion.Euler (0f, 0f, 0f);
		RTprevious.transform.SetAsLastSibling ();
		float rotate = 0f;
		while (rotate < 90f) {
			//RTprevious.rotation = Quaternion.Euler (0f, RTprevious.rotation.y + 3f, 0f);
			RTprevious.Rotate(new Vector3(0f,3f,0f));
			rotate += 3f;
			yield return new WaitForSeconds (0.03f);
		}
		yield return new WaitForSeconds (0.2f);
		if (questionType == 0) {
			SetBounsStar ();
		}else if (questionType == 1) {
			questionSound.GetComponent<AudioSource> ().Play ();
			yield return new WaitForSeconds (1.5f);
			SetBounsStar ();
		}
	}

	private void ResetAnswer(){
		for (int i = 0; i < 3; i++) {
			correct [i].enabled = false;
			wrong [i].enabled = false;
		}
		for (int i = 0; i < 2; i++) {
			imgCorrect [i].enabled = false;
			imgWrong [i].enabled = false;
		}
		answered = false;
	}

	private void AskQuestion(){
		questCount++;
		if (suddenDeadTag == 1) {
			maxQuestion++;
		}
		UpdateCountText ();
		nextBtn.SetActive (false);
		ResetAnswer ();
		while (true) {
			quesCardNum = Random.Range (0, featuredCards.Count);
			if (!usedCards.Contains (quesCardNum) || usedCards.Count >= featuredCards.Count) {
				break;
			}
		}
		if(!usedCards.Contains(quesCardNum)){
			usedCards.Add (quesCardNum);
		}
		FeaturedCard questionCard = featuredCards [quesCardNum];

		questionType= Random.Range (0, 2);

		soundQuestionLang.GetComponent<Image> ().enabled = false;
		soundQuestionLang.transform.GetChild (0).gameObject.GetComponent<Text> ().enabled = false;
		soundQuestionLang.transform.GetChild (1).gameObject.GetComponent<Text> ().enabled = false;

		if (questionType == 0) {
			questionImage.enabled = true;
			questionSound.GetComponent<Image> ().enabled = false;
			questionSound.GetComponent<Button> ().enabled = false;

			DownloadManager.Instance.AddLinkage (questionCard.ImagePath, questionCard.imageUrl, questionImage);

		} else if (questionType == 1) {
			
			questionImage.enabled = false;
			questionSound.GetComponent<Image> ().enabled = true;
			questionSound.GetComponent<Button> ().enabled = true;

			Dictionary<string, CardLang> langs = featuredCards [quesCardNum].getLangs ();
			if (langs == null) {
				Debug.Log ("This card have no lang? Impossible!");
				//answer [ansNum].text = featuredCards [cardNum].GetDefaultDisplayName ();
			} else {
				//string[] langKeys = new string[13]{"fr","de","es","jp","tl","ne","ur","hi","pth_w","pth","zh_w","zh","en"};
				List<CardLang> langList = new List<CardLang> ();
				foreach (CardLang clang in langs.Values) {
					langList.Add (clang);
				}
				int dice2 = Random.Range (0, langList.Count);
				soundQuestionLang.transform.GetChild(0).gameObject.GetComponent<Text>().text = langList [dice2].name;
				soundQuestionLang.transform.GetChild(1).gameObject.GetComponent<Text>().text =  GameManager.Instance.Language.getString("lang-" + langList[dice2].langKey, CaseHandle.FirstCharUpper);
				//_txtLangName.text = GameManager.Instance.Language.getString("lang-" + _lang.langKey, CaseHandle.FirstCharUpper);
				if(langList [dice2].isSoundExist) {
					Debug.Log ("[ViewCard_LangRow] TryToPlaySound(): sound-exist: SoundPath=" + langList [dice2].SoundPath);
					try{
						WAV wav = new WAV (langList [dice2].SoundPath);
						AudioClip audioClip = AudioClip.Create ("temp", wav.SampleCount, 1, wav.Frequency, false);
						audioClip.SetData (wav.LeftChannel, 0);
						questionSound.GetComponent<AudioSource>().clip = audioClip;
					}
					catch(System.Exception e){
						Debug.Log ("Sound Exist but cannot access it! " + e.Message);
					}
				}
				//sound[ansNum].clip = langList [dice].

			}
			//StartCoroutine (PlayQuestionSound ());
			//trueAns [ansNum] = correct;
		}

		SetAnswer ();
	}

//	IEnumerator PlayQuestionSound(){
//		yield return new WaitForSeconds (1f);
//		questionSound.GetComponent<AudioSource> ().Play ();
//		yield return null;
//	}

	private void SetBounsStar(){
		GameObject star = Instantiate (bounsStar) as GameObject;
		star.transform.SetParent (BCC.gameObject.transform);
		star.transform.localScale = new Vector3 (1f, 1f, 1f);
		//star.GetComponent<RectTransform> ().position = starPosi.GetComponent<RectTransform> ().position;
		star.transform.position = starPosi.transform.position;
		star.GetComponent<ShakeTheStar> ().enabled = true;
		if (suddenDeadTag == 1 && bounsTime > 1f) {
			bounsTime -= 0.2f;
		}
		BCC.bounsTime = bounsTime;
		BCC.STS = star.GetComponent<ShakeTheStar> ();
		SetButton (true);
	}

	private void SetAnswer(){
		for (int i = 0; i < 3; i++) {
			answer [i].transform.parent.gameObject.SetActive (false);
		}
		answerImg [0].gameObject.SetActive (false);
		answerImg [1].gameObject.SetActive (false);

		if (questionType == 0) {
			int correctAns = Random.Range (0, 3);
			AssignCardToAnswer (correctAns, quesCardNum, true);
			AssignCardToAnswer ((correctAns + 1) % 3, GetRandomCardExceptAnswer (), false);
			AssignCardToAnswer ((correctAns + 2) % 3, GetRandomCardExceptAnswer (), false);
		} else if (questionType == 1) {
			int correctAns = Random.Range (0, 2);
			AssignCardToImgAnswer (correctAns, quesCardNum, true);
			AssignCardToImgAnswer ((correctAns + 1) % 2, GetRandomCardExceptAnswer (), false);
		}
	}

	private int GetRandomCardExceptAnswer(){
		loopCount++;
		int selectedCard = Random.Range (0, featuredCards.Count);
		if ((projTagForFeaturedCards [selectedCard] == projTagForFeaturedCards [quesCardNum]) && projTag > 0 && loopCount < 100) {
			return GetRandomCardExceptAnswer ();
		}
		if (selectedCard == quesCardNum || selectedCard == usedcard) {
			return GetRandomCardExceptAnswer ();
		} 
		usedcard = selectedCard;
		loopCount = 0;
		return selectedCard;
	}

	private void AssignCardToImgAnswer(int ansNum,int cardNum,bool correct){
		answerImg [ansNum].gameObject.SetActive (true);
		DownloadManager.Instance.AddLinkage (featuredCards[cardNum].ImagePath, featuredCards[cardNum].imageUrl, answerImg[ansNum]);
		trueAns [ansNum] = correct;
		cardIdInAns [ansNum] = featuredCards [cardNum].cardId;
	}

	private void AssignCardToAnswer(int ansNum,int cardNum,bool correct){
		answer [ansNum].transform.parent.gameObject.SetActive (true);
		Dictionary<string, CardLang> langs = featuredCards [cardNum].getLangs ();
		if (langs == null) {
			Debug.Log ("This card have no lang? Impossible!");
			//answer [ansNum].text = featuredCards [cardNum].GetDefaultDisplayName ();
		} else {
			//string[] langKeys = new string[13]{"fr","de","es","jp","tl","ne","ur","hi","pth_w","pth","zh_w","zh","en"};
			List<CardLang> langList = new List<CardLang> ();
			foreach (CardLang clang in langs.Values) {
				langList.Add (clang);
				}
			int dice = Random.Range (0, langList.Count);
			answer [ansNum].text = langList [dice].name;
			language[ansNum].text =  GameManager.Instance.Language.getString("lang-" + langList[dice].langKey, CaseHandle.FirstCharUpper);
				//_txtLangName.text = GameManager.Instance.Language.getString("lang-" + _lang.langKey, CaseHandle.FirstCharUpper);
			if(langList [dice].isSoundExist) {
				Debug.Log ("[ViewCard_LangRow] TryToPlaySound(): sound-exist: SoundPath=" + langList [dice].SoundPath);
				try{
					WAV wav = new WAV (langList [dice].SoundPath);
					AudioClip audioClip = AudioClip.Create ("temp", wav.SampleCount, 1, wav.Frequency, false);
					audioClip.SetData (wav.LeftChannel, 0);
					sound[ansNum].clip = audioClip;
				}
				catch(System.Exception e){
					Debug.Log (e.Message);
				}
			}
			//sound[ansNum].clip = langList [dice].
			
		}
		trueAns [ansNum] = correct;
		cardIdInAns [ansNum] = featuredCards [cardNum].cardId;
	}

	public void EventSelectAnswer(int ans){
		if (answered) {
			return;
		}

		SetButton (false);
		if (questionType == 1) {
			soundQuestionLang.GetComponent<Image> ().enabled = true;
			soundQuestionLang.transform.GetChild (0).gameObject.GetComponent<Text> ().enabled = true;
			soundQuestionLang.transform.GetChild (1).gameObject.GetComponent<Text> ().enabled = true;
		}
		answered = true;

		if (questionType == 0) {
			for (int i = 0; i < 3; i++) {
				sound [i].Stop ();

				if (trueAns [i]) {
					//correct [i].enabled = true;
					StartCoroutine (MarkPulse (correct [i], 0.4f, (float)i * 0.35f));
					if (ans == i) {
						UIAS [2].Play ();
						AddScore (5);
						if (suddenDeadTag == 1 && BCC.bounsTime == 0) {
							suddenDeadTag = 2;
						}
						BCC.AnswerSelected (true);
						correctCount++;
						//CountCardAnsweredCorrectly (ans);
					} else {
						UIAS [3].Play ();
						BCC.AnswerSelected (false);
						wrongCount++;
						if (suddenDeadTag == 1) {
							suddenDeadTag = 2;
						}
					}
				} else {
					StartCoroutine (MarkPulse (wrong [i], 0.25f, (float)i * 0.35f));
				}
			}
		} else if (questionType == 1) {
			for (int i = 0; i < 2; i++) {
				if (trueAns [i]) {
					StartCoroutine (MarkPulse (imgCorrect [i], 0.4f, (float)i * 0.35f));
					if (ans == i) {
						UIAS [2].Play ();
						AddScore (5);
						if (suddenDeadTag == 1 && BCC.bounsTime == 0) {
							suddenDeadTag = 2;
						}
						BCC.AnswerSelected (true);
						correctCount++;
					} else {
						UIAS [3].Play ();
						BCC.AnswerSelected (false);
						wrongCount++;
						if (suddenDeadTag == 1) {
							suddenDeadTag = 2;
						}
					}
				} else {
					StartCoroutine (MarkPulse (imgWrong [i], 0.25f, (float)i * 0.35f));
				}
			}
		}
		UpdateCountText ();

		if (questCount < maxQuestion || suddenDeadTag == 1) {
			nextBtn.GetComponent<Button> ().enabled = true;
			nextBtn.SetActive (true);
		} else if (suddenDeadTag == 0 && wrongCount == 0) {
			suddenDeadAni.enabled = true;
			suddenDeadTag = 1;
			nextBtn.GetComponent<Button> ().enabled = true;
			nextBtn.SetActive (true);
			suddenDeadRoutine = StartCoroutine (StartSuddenDeadRoutine ());
		}else{
			if (suddenDeadRoutine != null) {
				StopCoroutine (suddenDeadRoutine);
			}
			endGameBtn.SetActive (true);
		}
	}

	private void CountCardAnsweredCorrectly(int ansNum){
		int count = PlayerPrefs.GetInt (cardIdInAns [ansNum]);
		count++;
		PlayerPrefs.SetInt (cardIdInAns [ansNum], count);
	}

	IEnumerator StartSuddenDeadRoutine(){
		while (suddenDeadGroup.alpha < 0.2f) {
			suddenDeadGroup.alpha += 0.01f;
			yield return new WaitForSeconds (0.02f);
		}
		int playCount = 0;
		while (playCount < 4) {
			playCount++;
			GetComponent<AudioSource> ().Play ();
			while (suddenDeadGroup.alpha < 0.4f) {
				suddenDeadGroup.alpha += 0.02f;
				yield return new WaitForSeconds (0.025f);
			}
			while (suddenDeadGroup.alpha > 0.2f) {
				suddenDeadGroup.alpha -= 0.02f;
				yield return new WaitForSeconds (0.025f);
			}
			yield return new WaitForSeconds (0.2f);
		}
	}

	IEnumerator MarkPulse(Image mark,float oriScale,float delay){
		yield return new WaitForSeconds (delay);

		mark.enabled = true;
		RectTransform RT = mark.gameObject.GetComponent<RectTransform> ();
		RT.localScale = new Vector3 (2f*oriScale, 2f*oriScale, 1f);

		while (RT.localScale.x > oriScale) {
			float step = (RT.localScale.x - oriScale) * 0.1f;
			RT.localScale = new Vector3 (RT.localScale.x - step, RT.localScale.y - step, 1f);
			yield return new WaitForSeconds (0.02f);
		}
	}

	public void EventLeaveGame(){
		Fader.Instance.FadeIn (0.5f)
			.LoadLevel ("Game1_entry")
			.FadeOut (0.5f);
	}

	private void SetButton(bool ON){
		for (int i = 0; i < 3; i++) {
			if (ON) {
				answer [i].transform.parent.gameObject.GetComponent<Button> ().enabled = true;
			} else {
				answer [i].transform.parent.gameObject.GetComponent<Button> ().enabled = false;
			}
		}
		for (int i = 0; i < 2; i++) {
			if (ON) {
				answerImg [i].gameObject.GetComponent<Button> ().enabled = true;
			} else {
				answerImg [i].gameObject.GetComponent<Button> ().enabled = false;
			}
		}
	}

	public void EventEndGame(){
		StartCoroutine (EndGameRoutine ());
	}

	IEnumerator EndGameRoutine(){
		rankingTitle.SetActive (false);

		rankingObj.SetActive (false);

		while (suddenDeadGroup.alpha > 0f) {
			suddenDeadGroup.alpha -= 0.02f;
			yield return new WaitForSeconds (0.05f);
		}

		UIAS [0].Play ();
		endgameScreen.blocksRaycasts = true;
		endgameScreen.interactable = true;

		int bestScore = 0;

		bool done = false;
		string bestScoreKey = "";

		ParseObject localhighscoreObj = new ParseObject ("Game1BestScore");

		if (!missionMode || isTeacher) {
			bestScoreKey = "BestScore-" + LocalData.Instance.Data ["user"] ["realName"].Value + "(" + LocalData.Instance.Data ["user"] ["username"].Value + ")";
			bestScore = PlayerPrefs.GetInt (bestScoreKey);
			done = true;
		} else {
			ParseQuery<ParseObject> highScoreQuery = new ParseQuery<ParseObject> ("Game1BestScore").WhereEqualTo ("inMission", mission).WhereEqualTo ("Player", ParseUser.CurrentUser);
			highScoreQuery.FindAsync ().ContinueWith (t => {
				if(!t.IsFaulted){
					IEnumerable<ParseObject> highscores = t.Result;
					foreach(var highScore in highscores){
						bestScore = highScore.Get<int>("HighScore1");
						localhighscoreObj = highScore;
					}
				}else{
					Debug.Log("Failed to get high score! Error: "+t.Exception.Message);
				}
				done = true;
			});
		}

		yield return new WaitUntil (() => done == true);

		bestScoreValue.text = "" + bestScore;

		CanvasGroup pageGroup = pages [usingPage].GetComponent<CanvasGroup> ();
		pageGroup.blocksRaycasts = false;
		pageGroup.interactable = false;
		while (stayInFrontGroup.alpha > 0f) {
			stayInFrontGroup.alpha -= 0.05f;
			yield return new WaitForSeconds (0.05f);
		}
		while (pageGroup.alpha > 0f) {
			pageGroup.alpha -= 0.05f;
			yield return new WaitForSeconds (0.05f);
		}

		while (endgameScreen.alpha < 1f) {
			endgameScreen.alpha += 0.05f;
			yield return new WaitForSeconds (0.05f);
		}

		int displayScore = 0;
		yourScoreValue.text = "" + displayScore;
		while (displayScore < score) {
			displayScore++;
			yourScoreValue.text = "" + displayScore;
			UIAS [4].Play ();
			yield return new WaitForSeconds (0.02f);
		}
		yield return new WaitForSeconds (1f);

		bool toUploadHighScore = false;

		if (bestScore < score && missionMode == true && !isTeacher) {
			toUploadHighScore = true;
		}
		while (bestScore < score) {
			bestScore++;
			bestScoreValue.text = "" + bestScore;
			UIAS [4].Play ();
			yield return new WaitForSeconds (0.02f);
		}
		if (!missionMode || isTeacher) {
			PlayerPrefs.SetInt (bestScoreKey, bestScore);
			PlayerPrefs.Save ();
		}
		if (toUploadHighScore) {
			done = false;
			localhighscoreObj ["Player"] = ParseUser.CurrentUser;
			localhighscoreObj ["HighScore1"] = bestScore;
			localhighscoreObj ["inMission"] = mission;
			localhighscoreObj.SaveAsync ().ContinueWith (t => {
				if(!t.IsFaulted){
					Debug.Log("New high score saved!");
				}else{
					Debug.Log("Failed to save new high score! Error: " + t.Exception.Message);
				}
				done = true;
			});
		}
		yield return new WaitUntil (() => done == true);
		done = false;
		ParseQuery<ParseObject> allHighscoreQuery = new ParseQuery<ParseObject> ("Game1BestScore").WhereEqualTo ("inMission", mission).Limit(20);

		List<ParseObject> hightScores = new List<ParseObject> ();

		allHighscoreQuery.FindAsync ().ContinueWith (t => {
			if(!t.IsFaulted){
				IEnumerable<ParseObject> highscores = t.Result;
				foreach(var highScore in highscores){
					hightScores.Add(highScore);
					Debug.Log("Get highscore!");
				}
			}else{
				Debug.Log("Failed to get all highscore!");
			}
			done = true;
		});
		yield return new WaitUntil (() => done == true);

		if (!missionMode || isTeacher) {
			yield break;
		}

		List<int> scores = new List<int> ();
		List<int> order = new List<int> ();

		for (int i = 0; i < hightScores.Count; i++) {
			int _score = hightScores [i].Get<int> ("HighScore1");
			//Debug.Log ("_score: " + _score);
			if (scores.Count == 0) {
				scores.Add (_score);
				order.Add (i);
			}else{
				for(int j=0;j<scores.Count;j++) {
					//Debug.Log ("scores.Count: " + scores.Count);
					//Debug.Log ("j: " + j);

					if (_score > scores[j]) {
						scores.Insert (j,_score);
						order.Insert (j,i);
						break;
					}
					if (j == scores.Count - 1) {
						scores.Add (_score);
						order.Add (i);
						break;
					}
				}
			}
//			foreach (int score in scores) {
//				Debug.Log ("scores: " + score);
//			}
			//Debug.Log ("loop: " + i);
		}
//
//		foreach (int score in scores) {
//			Debug.Log ("scores: " + score);
//		}
//		foreach (int ord in order) {
//			Debug.Log ("order: " + ord);
//		}

		Debug.Log ("Start to set rank log");

		GameObject oriRow = rankingObj.transform.Find ("row").gameObject;
		int count = 0;
		foreach(int i in order){
			GameObject log = Instantiate (oriRow,oriRow.transform.position,oriRow.transform.rotation);
			log.transform.SetParent (rankingObj.transform);
			log.transform.localScale = Vector3.one;
			//log.transform.Translate (new Vector3 (0f, tranStep*count, 0f));
			log.SetActive (true);

			done = false;
			ParseUser player = (ParseUser)hightScores [i] ["Player"];
			player.FetchAsync ().ContinueWith (t => {
				done = true;
			});
			yield return new WaitUntil (() => done == true);
			log.transform.Find ("name").gameObject.GetComponent<Text> ().text = "" + player.Get<string> ("realName");
			log.transform.Find ("score").gameObject.GetComponent<Text> ().text = "" + hightScores [i].Get<int>("HighScore1");
			log.transform.Find ("rank").gameObject.GetComponent<Text> ().text = "" + (count + 1) + ".";
			count++;
			if (player.ObjectId.Equals(LocalData.Instance.Data ["user"] ["objectId"])) {
				log.transform.Find ("name").gameObject.GetComponent<Text> ().color = Color.green;
				log.transform.Find ("score").gameObject.GetComponent<Text> ().color = Color.green;
				log.transform.Find ("rank").gameObject.GetComponent<Text> ().color = Color.green;
			}
		}
		oriRow.SetActive (false);
		rankingObj.SetActive (true);
		if (missionMode) {
			rankingTitle.SetActive (true);
		} else {
			rankingTitle.SetActive (false);
		}
		yield return null;
	}

	public void EventRestart(){
		UIAS [0].Play ();
		Fader.Instance.FadeIn (0.5f)
			.LoadLevel ("Game1")
			.FadeOut (0.5f);
	}
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class DatePicker : MonoBehaviour {
	
	public static DatePicker _instance = null;
	internal static DatePicker Instance { get { return _instance; } }
	
	public delegate void EventHandler(DateTime date);
	public event EventHandler		_responseHandler = null;

	public Text						_txtMonthYear = null;
	public Text						_txtTime = null;

	// timer set
	public GameObject				_panelTimerPopUp = null;
	public Text						_txtTimerPopUpTime = null;
	public Slider					_sliderHour = null;
	public Slider					_sliderMin = null;

	private Transform				_dynLastSelectedDayTransform = null;
	internal Transform				LastSelectedDayTransform {
		set {
			if (_dynLastSelectedDayTransform != null) {
				// disable the old
				Transform color = _dynLastSelectedDayTransform.FindChild("img_selected");
				if (color != null) {
					Image img = color.GetComponent<Image>();
					if (img != null)
						img.enabled = false;
				}

			}

			// set the new
			_dynLastSelectedDayTransform = value;
			if (_dynLastSelectedDayTransform != null) {
				Transform color = _dynLastSelectedDayTransform.FindChild("img_selected");
				if (color != null) {
					Image img = color.GetComponent<Image>();
					if (img != null)
						img.enabled = true;
				}
			}
		}
	}

	public Transform				_calendarParent = null;
	public Transform				_transCalendarDayFirstObject = null;
	public Transform[]				_transDayButtons = null;

	private DateTime				_dynSelectedDate;
	
	void Awake () {
		_instance = this;
	}
	
	// Use this for initialization
	void Start () {
		//GameObject.DontDestroyOnLoad (gameObject);
		// get the button
		_transDayButtons = new Transform[42];
		_transDayButtons [0] = _transCalendarDayFirstObject;
		
		Transform color = _transCalendarDayFirstObject.FindChild("img_selected");
		if (color != null) {
			Image img = color.GetComponent<Image>();
			if (img != null)
				img.enabled = false;
		}

		for (int i = 1; i < _transDayButtons.Length; i++) {
			_transDayButtons[i] = Instantiate(_transCalendarDayFirstObject) as Transform;
			_transDayButtons[i].SetParent(_calendarParent);
			_transDayButtons[i].localScale = Vector3.one;
			_transDayButtons[i].name = "calendar_day_" + i;
		}
		
		_panelTimerPopUp.gameObject.SetActive (false);

		_dynSelectedDate = DateTime.Now;
		InitCalendar (_dynSelectedDate);

		// place at correct position
		RectTransform t = GetComponent<RectTransform>();
		t.anchoredPosition = new Vector2(0, 0);
		
		gameObject.SetActive(false);
	}

	public void InitCalendar (DateTime date) {
		_dynSelectedDate = new DateTime (date.Year, date.Month, date.Day, date.Hour, Mathf.CeilToInt(date.Minute / 5) * 5, 0);;

		int year = _dynSelectedDate.Year;
		int month = _dynSelectedDate.Month;
		int day = _dynSelectedDate.Day;

		int totalDayInThisMonth = DateTime.DaysInMonth (year, month);
		DateTime firstDate = new DateTime (year, month, 1);
		int firstDayStartAt = (int)firstDate.DayOfWeek;

		for (int i = 0; i < _transDayButtons.Length; i++) {
			Transform transButton = _transDayButtons[i];
			Button button =  transButton.GetComponent<Button>();
			Text txtDay = transButton.FindChild("Text").GetComponent<Text>();

			if ( i < firstDayStartAt ) {
				button.interactable = false;
				txtDay.text = "";
			} else {
				int displayDay = (i - firstDayStartAt + 1);
				if (displayDay <= totalDayInThisMonth) {
					button.interactable = true;
					txtDay.text = displayDay.ToString() ;
				} else {
					button.interactable = false;
					txtDay.text = "";
				}

				if (displayDay == day) {
					LastSelectedDayTransform = transButton;
				}
			}
		}

		// month - year display
		_txtMonthYear.text = GameManager.Instance.Language.getString("month-" + month) + " " + year;

		// timer
		_panelTimerPopUp.gameObject.SetActive (false);
		int hour = _dynSelectedDate.Hour;
		int min = _dynSelectedDate.Minute;

		_txtTimerPopUpTime.text = _txtTime.text = hour.ToString("00") + ":" + min.ToString("00");
		_sliderHour.value = hour;
		_sliderMin.value = min / 5;

	}
	
	public void EventButtonPrevMonth () {
		_dynSelectedDate = _dynSelectedDate.AddMonths (-1);
		InitCalendar (_dynSelectedDate);
	}
	public void EventButtonNextMonth () {
		_dynSelectedDate = _dynSelectedDate.AddMonths (1);
		InitCalendar (_dynSelectedDate);
	}

	public void EventButtonSelectDay (GameObject btn) {
		//Debug.Log (btn.name);
		Transform display = btn.transform.FindChild ("Text");
		Text txtDisplay = display.GetComponent<Text> ();
		int day = int.Parse(txtDisplay.text);

		//Debug.Log (day);

		_dynSelectedDate = new DateTime (_dynSelectedDate.Year, _dynSelectedDate.Month, day, _dynSelectedDate.Hour, _dynSelectedDate.Minute, 0);

		LastSelectedDayTransform = btn.transform;
	}
	
	public void ShowDatePicker (DateTime date, EventHandler handler) {
		_responseHandler = handler;

		InitCalendar (date);
		
		gameObject.SetActive (true);
	}

	public void EventSliderTimeChange () {
		//Debug.Log ((int)_sliderHour.value);
		//Debug.Log ((int)_sliderMin.value);

		_dynSelectedDate = new DateTime(_dynSelectedDate.Year, _dynSelectedDate.Month, _dynSelectedDate.Day, 
		                                (int)_sliderHour.value, (int)_sliderMin.value * 5, 0);

		_txtTimerPopUpTime.text = _txtTime.text = _dynSelectedDate.Hour.ToString("00") + ":" + _dynSelectedDate.Minute.ToString("00");
	}
	
	public void EventButtonModifyTime () {
		_panelTimerPopUp.SetActive (true);
	}

	public void EventButtonTimerPopupDone () {
		_panelTimerPopUp.SetActive (false);
	}
	
	private void DateSelected (DateTime date) {
		
		if (_responseHandler != null)
			_responseHandler (date);
		gameObject.SetActive (false);
	}
	
	public void EventButtonDone () {
		if (_responseHandler != null)
			_responseHandler (_dynSelectedDate);
		gameObject.SetActive (false);
	}
}

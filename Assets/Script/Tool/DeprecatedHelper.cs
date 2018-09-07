using UnityEngine;
using System.Collections;

public class DeprecatedHelper {

	public static string removeTitleTag(string title) {
		string t;
		int index = title.IndexOf ('#');
		if (index != -1) {
			t = title.Substring (0, index);
		} else {
			t = title;
		}

		return t;
	}

	public static Card getSlideShowIndexCard(Card[] cards) {
		if (cards.Length == 0)
			return null;

		Card slideShowIndexCard = null;

		foreach (Card card in cards) {
			//Debug.LogWarning (card.comments);
			if (card.comments.Count > 0) {
				slideShowIndexCard = card;
				break;
			}
		}
		if (slideShowIndexCard == null) {
			slideShowIndexCard = cards [0];
		}

		return slideShowIndexCard;
	}

}

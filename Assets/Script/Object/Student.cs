using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.IO;
using System;

public enum StudentSortBy {
	Name,
	NumOfAttention,
	NumOfCards,
	NumOfFeatured
}

public class Student {
	public string 	username = "";
	public string 	realName = "";
	public string 	email = "";
	
	public string 	objectId = "";
	public int		numOfTeacherAttention = 0;
	public int		numOfFeaturedCards = 0;
	public int		numOfSubmittedCards = 0;
	
	internal string id { get { return objectId; } }
	/*
	public JSONNode toJSON() {
		JSONNode node = JSON.Parse("{}");
		node["username"] = name;
		if (!string.IsNullOrEmpty(email))
			node["email"] = email;
		
		if (!string.IsNullOrEmpty(objectId))
			node["objectId"] = objectId;
		
		return node;
	}
	*/
	
	public Student (JSONNode node) {
		if (node == null)
			return;

		//Debug.Log ("** " + node.ToString());
		
		if (node.GetKeys().Contains("objectId"))
			objectId = node["objectId"].Value;
		
		if (node.GetKeys().Contains("username"))
			username = node["username"].Value;
		if (node.GetKeys().Contains("realName"))
			realName = node["realName"].Value;
		if (node.GetKeys().Contains("email"))
			email = node["email"].Value;
		
		if (node.GetKeys().Contains("numOfTeacherAttention"))
			numOfTeacherAttention = node["numOfTeacherAttention"].AsInt;
		if (node.GetKeys().Contains("numOfFeaturedCards"))
			numOfFeaturedCards = node["numOfFeaturedCards"].AsInt;
		if (node.GetKeys().Contains("numOfSubmittedCards"))
			numOfSubmittedCards = node["numOfSubmittedCards"].AsInt;

	}

	public int CompareBy (Student other, StudentSortBy key, SortingOrder order) {
		
		if (key == StudentSortBy.NumOfCards) {
			if (order == SortingOrder.Asc) {
				int d = numOfSubmittedCards - other.numOfSubmittedCards;
				return d == 0 ? realName.CompareTo(other.realName) : d;
			} else {
				int d = other.numOfSubmittedCards - numOfSubmittedCards;
				return d == 0 ? realName.CompareTo(other.realName) : d;
			}
		} else if (key == StudentSortBy.NumOfFeatured) {
			if (order == SortingOrder.Asc) {
				int d = numOfFeaturedCards - other.numOfFeaturedCards;
				return d == 0 ? realName.CompareTo(other.realName) : d;
			} else {
				int d = other.numOfFeaturedCards - numOfFeaturedCards;
				return d == 0 ? realName.CompareTo(other.realName) : d;
			}
		} else if (key == StudentSortBy.NumOfAttention) {
			if (order == SortingOrder.Asc) {
				int d = numOfTeacherAttention - other.numOfTeacherAttention;
				return d == 0 ? realName.CompareTo(other.realName) : d;
			} else {
				int d = other.numOfTeacherAttention - numOfTeacherAttention;
				return d == 0 ? realName.CompareTo(other.realName) : d;
			}
		} else if (key == StudentSortBy.Name) {
			if (order == SortingOrder.Asc) {
				return realName.CompareTo(other.realName);
			} else {
				return realName.CompareTo(other.realName) * -1;
			}
		}
		return 0;
	}
	
	public static void Sort (Student[] list, StudentSortBy key, SortingOrder order) {
		MergeSort(list, key, order, 0, list.Length);
	}
	
	private static void MergeSort (Student[] list, StudentSortBy key, SortingOrder order, int low, int high) {
		int N = high - low;
		if (N <= 1)
			return;
		
		int mid = low + N / 2;
		MergeSort(list, key, order, low, mid);
		MergeSort(list, key, order, mid, high);
		
		Student[] aux = new Student[N];
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
}

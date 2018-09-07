using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.IO;
using System;

public enum StudentProjectSortBy {
	Name,
	NumOfCard,
	NumOfFeatured,
	NumOfAttention
}

public class StudentProject {
	public Student 	student = null;
	public Project 	project = null;
	public int		numOfCards = 0;
	public int		numOfFeaturedCards = 0;
	public int		numOfTeacherAttention = 0;
	public int		numOfStudentAttention = 0;
	
	public string 	objectId = "";
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
	public StudentProject () {
	}
	
	public StudentProject (JSONNode node) {
		if (node == null)
			return;

		if (node.GetKeys().Contains("objectId"))
			objectId = node["objectId"].Value;
		
		if (node.GetKeys().Contains("numOfStudentAttention"))
			numOfStudentAttention = node["numOfStudentAttention"].AsInt;
		if (node.GetKeys().Contains("numOfTeacherAttention"))
			numOfTeacherAttention = node["numOfTeacherAttention"].AsInt;
		if (node.GetKeys().Contains("numOfSubmittedCard"))
			numOfCards = node["numOfSubmittedCard"].AsInt;
		if (node.GetKeys().Contains("numOfFeaturedCards"))
			numOfFeaturedCards = node["numOfFeaturedCards"].AsInt;
		
	}

	public int CompareBy (StudentProject other, StudentProjectSortBy key, SortingOrder order) {

		if (key == StudentProjectSortBy.NumOfAttention) {
			if (order == SortingOrder.Asc) {
				int d = numOfTeacherAttention - other.numOfTeacherAttention;
				if (d == 0) {
					if (student != null) {
						return student.realName.CompareTo(other.student.realName);
					} if (project != null) {
						return project.title.CompareTo(other.project.title);
					} 
				} else {
					return d;
				}
			} else {
				int d = other.numOfTeacherAttention - numOfTeacherAttention;
				if (d == 0) {
					if (student != null) {
						return student.realName.CompareTo(other.student.realName);
					} if (project != null) {
						return project.title.CompareTo(other.project.title);
					} 
				} else {
					return d;
				}
			}
		} else if (key == StudentProjectSortBy.NumOfCard) {
			if (order == SortingOrder.Asc) {
				int d = numOfCards - other.numOfCards;
				if (d == 0) {
					if (student != null) {
						return student.realName.CompareTo(other.student.realName);
					} if (project != null) {
						return project.title.CompareTo(other.project.title);
					} 
				} else {
					return d;
				}
			} else {
				int d = other.numOfCards - numOfCards;
				if (d == 0) {
					if (student != null) {
						return student.realName.CompareTo(other.student.realName);
					} if (project != null) {
						return project.title.CompareTo(other.project.title);
					} 
				} else {
					return d;
				}
			}
		} else if (key == StudentProjectSortBy.NumOfFeatured) {
			if (order == SortingOrder.Asc) {
				int d = numOfFeaturedCards - other.numOfFeaturedCards;
				if (d == 0) {
					if (student != null) {
						return student.realName.CompareTo(other.student.realName);
					} if (project != null) {
						return project.title.CompareTo(other.project.title);
					} 
				} else {
					return d;
				}
			} else {
				int d = other.numOfCards - numOfCards;
				if (d == 0) {
					if (student != null) {
						return student.realName.CompareTo(other.student.realName);
					} if (project != null) {
						return project.title.CompareTo(other.project.title);
					} 
				} else {
					return d;
				}
			}
		} else if (key == StudentProjectSortBy.Name) {
			if (order == SortingOrder.Asc) {
				if (student != null) {
					return student.realName.CompareTo(other.student.realName);
				} else if (project != null) {
					return project.title.CompareTo(other.project.title);
				}
			} else {
				if (student != null) {
					return student.realName.CompareTo(other.student.realName) * -1;
				} else if (project != null) {
					return project.title.CompareTo(other.project.title) * -1;
				}
			}
		}
		return 0;
	}

	public static void Sort (StudentProject[] list, StudentProjectSortBy key, SortingOrder order) {
		MergeSort(list, key, order, 0, list.Length);
	}

	private static void MergeSort (StudentProject[] list, StudentProjectSortBy key, SortingOrder order, int low, int high) {
		int N = high - low;
		if (N <= 1)
			return;
		
		int mid = low + N / 2;
		MergeSort(list, key, order, low, mid);
		MergeSort(list, key, order, mid, high);

		StudentProject[] aux = new StudentProject[N];
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

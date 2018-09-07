using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.IO;
using System;

public enum CourseSortBy {
	Name,
	NumOfStudent
}

public class Course {
	public string 	courseTitle = "";
	public string	teacherName = "";
	
	public string 	objectId = "";
	public int		numOfStudents = 0;
	
	internal string id { get { return objectId; } }
	
	public Course (JSONNode node) {
		if (node == null)
			return;

		//Debug.Log ("** " + node.ToString());
		
		if (node.GetKeys().Contains("objectId"))
			objectId = node["objectId"].Value;
		
		if (node.GetKeys().Contains("courseTitle"))
			courseTitle = node["courseTitle"].Value;

		if (node.GetKeys().Contains("courseTeacher")) {
			teacherName = node["courseTeacher"]["realName"].Value;
		}
	}

	public int CompareBy (Course other, CourseSortBy key, SortingOrder order) {
		
		if (key == CourseSortBy.NumOfStudent) {
			if (order == SortingOrder.Asc) {
				int d = numOfStudents - other.numOfStudents;
				return d == 0 ? courseTitle.CompareTo(other.courseTitle) : d;
			} else {
				int d = other.numOfStudents - numOfStudents;
				return d == 0 ? courseTitle.CompareTo(other.courseTitle) : d;
			}
		} else if (key == CourseSortBy.Name) {
			if (order == SortingOrder.Asc) {
				return courseTitle.CompareTo(other.courseTitle);
			} else {
				return courseTitle.CompareTo(other.courseTitle) * -1;
			}
		}
		return 0;
	}
	
	public static void Sort (Course[] list, CourseSortBy key, SortingOrder order) {
		MergeSort(list, key, order, 0, list.Length);
	}
	
	private static void MergeSort (Course[] list, CourseSortBy key, SortingOrder order, int low, int high) {
		int N = high - low;
		if (N <= 1)
			return;
		
		int mid = low + N / 2;
		MergeSort(list, key, order, low, mid);
		MergeSort(list, key, order, mid, high);
		
		Course[] aux = new Course[N];
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

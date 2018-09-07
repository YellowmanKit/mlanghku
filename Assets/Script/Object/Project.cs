using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.IO;
using System;

public enum ProjectSortBy {
	Name,
	NumOfAttention,
	DueDate
}

public class Project {
	public string 	title = "";
	public string 	desc = "";
	public DateTime dueDate;
	public int 		numOfTeacherAttention = 0;
	public int 		numOfStudentDone = 0;
	public string	imageUrl = "";
	
	public string 	objectId = "";
	public string 	courseId = "";

	internal string id { get { return objectId; } }
	
	public JSONNode toJSON() {
		JSONNode node = JSON.Parse("{}");
		node["title"] = title;
		node["desc"] = desc;
		node["dueDate"] = dueDate.ToString();
		node["numOfTeacherAttention"].AsInt = numOfTeacherAttention;
		node["numOfStudentDone"].AsInt = numOfStudentDone;
		
		if (!string.IsNullOrEmpty(objectId))
			node["objectId"] = objectId;
		
		return node;
	}
	internal string ImageName { get { return "project-" + id + ".jpg"; } }
	internal string ImagePath {
		get { return Path.Combine(LocalData.Instance.DirectoryPath, ImageName); }
	}
	internal bool	isImageExist {
		get {
			return File.Exists (ImagePath);
		}
	}

	internal bool dueDatePassed {
		get {
			TimeSpan timeRemain = (dueDate - DateTime.Now);
			return timeRemain.TotalSeconds < 0;
		}
	}

	public string getTitle() {
		return DeprecatedHelper.removeTitleTag (title);
	}

	public bool isSlideShow() {
		return title.Contains ("#SlideShow");
	}
	
	public Project (JSONNode node) {
		if (node == null)
			return;
		
		if (node.GetKeys().Contains("projectTitle"))
			title = node["projectTitle"].Value;
		if (node.GetKeys().Contains("projectDesc"))
			desc = node["projectDesc"].Value;
		if (node.GetKeys().Contains("dueDate"))
			dueDate = Convert.ToDateTime(node["dueDate"]["iso"].Value);
		
		if (node.GetKeys().Contains("numOfTeacherAttention"))
			numOfTeacherAttention = node["numOfTeacherAttention"].AsInt;
		if (node.GetKeys().Contains("numOfStudentDone"))
			numOfStudentDone = node["numOfStudentDone"].AsInt;
		
		if (node.GetKeys().Contains("objectId"))
			objectId = node["objectId"].Value;
		if (node.GetKeys().Contains("course"))
			courseId = node["course"] ["objectId"].Value;
		if (node.GetKeys().Contains("projectIcon"))
			imageUrl = node["projectIcon"] ["url"].Value;
	}
	
	public int CompareBy (Project other, ProjectSortBy key, SortingOrder order) {
		
		if (key == ProjectSortBy.NumOfAttention) {
			if (order == SortingOrder.Asc) {
				int a = numOfTeacherAttention - other.numOfTeacherAttention;
				if (a == 0) {
					int d = dueDate.CompareTo(other.dueDate);
					return d == 0 ? title.CompareTo(other.title) : d;
				} else {
					return a;
				}
				//return a == 0 ? title.CompareTo(other.title) : a;
			} else {
				int a = other.numOfTeacherAttention - numOfTeacherAttention;
				if (a == 0) {
					int d = dueDate.CompareTo(other.dueDate);
					return d == 0 ? title.CompareTo(other.title) : d;
				} else {
					return a;
				}
				//return a == 0 ? title.CompareTo(other.title) : a;
			}
		} else if (key == ProjectSortBy.DueDate) {
			int d = dueDate.CompareTo(other.dueDate);
			if (order == SortingOrder.Asc) {
				return d == 0 ? title.CompareTo(other.title) : d;
			} else {
				return d == 0 ? title.CompareTo(other.title) : d * -1;
			}
		} else if (key == ProjectSortBy.Name) {
			if (order == SortingOrder.Asc) {
				return title.CompareTo(other.title);
			} else {
				return title.CompareTo(other.title) * -1;
			}
		}
		return 0;
	}
	
	public static void Sort (Project[] list, ProjectSortBy key, SortingOrder order) {
		MergeSort(list, key, order, 0, list.Length);
	}
	
	private static void MergeSort (Project[] list, ProjectSortBy key, SortingOrder order, int low, int high) {
		int N = high - low;
		if (N <= 1)
			return;
		
		int mid = low + N / 2;
		MergeSort(list, key, order, low, mid);
		MergeSort(list, key, order, mid, high);
		
		Project[] aux = new Project[N];
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

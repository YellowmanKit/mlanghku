using UnityEngine;
using UnityEngine.UI;
//using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class FontChanger : MonoBehaviour {
	public Font font;
	public Font[] favouriteFonts;
	public string prefabFolder;

	/*
	public static string[] GetAllPrefabPaths (string prefabFolder) {
		string[] temp = AssetDatabase.GetAllAssetPaths();
		List<string> result = new List<string>();
		string pathPrefix = "Assets/";
		if (prefabFolder.Length > 0) pathPrefix += prefabFolder+"/";
		foreach ( string s in temp ) {
			if ( s.EndsWith( ".prefab" ) && s.StartsWith(pathPrefix)) result.Add( s );
		}
		return result.ToArray();
	}

	[ContextMenu ("Change Font (Prefab)")]
	void changeFontForPrefabFolder () {
		Debug.Log ("changeFontForPrefabFolder");

		string[] paths = GetAllPrefabPaths (prefabFolder);
		
		foreach (string path in paths) {
			GameObject o = (GameObject) AssetDatabase.LoadMainAssetAtPath( path );
			Text[] ts = o.GetComponentsInChildren<Text>(true);

			bool hasChange = false;

			foreach (Text t in ts) {
				if (t.font == font)
					continue;
				
				t.font = font;
				hasChange = true;
			}

			if (hasChange) {
				EditorUtility.SetDirty (o);
				Debug.Log (path);
			}
		}

		AssetDatabase.SaveAssets ();
	}

	*/
}

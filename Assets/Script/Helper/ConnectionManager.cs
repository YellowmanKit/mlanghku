using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Parse;

public class ConnectionManager : MonoBehaviour {
	
	private static ConnectionManager _instance = null;
	internal static ConnectionManager Instance { get { return _instance; } }

}

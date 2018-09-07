#pragma warning disable 0219

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MiniJSON;

public class FBWrapper : MonoBehaviour {
	
	static FBWrapper instance = null;

	public Action OnInit;
	public Action<Dictionary<string, object>> OnLogin;
	public Action<Dictionary<string, object>> OnPostToWall;
	public Action<Dictionary<string, object>> OnImageUpload;
	public Action<Dictionary<string, object>> OnShareWithFriends;
	public Action<Dictionary<string, object>> OnGetUserDetails;

	[HideInInspector]
	public Texture2D uploadImage;
	public Action<Dictionary<string, object>> uploadCallback;

	//=============================================================================
	// Init singleton
	//=============================================================================
	
	public static FBWrapper Instance 
	{
		get {
			if(instance == null)
			{
				GameObject go = GameObject.Find("MobileToolkit");
				if(go == null)
				{
					go = new GameObject();
					go.name = "FacebookWrapper";
				}
				instance = go.AddComponent<FBWrapper>();

				bool hasFacebookSdk = false;

				#if !UNITY_WINRT

				foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies())
				{
					string lib = a.FullName.Split('.')[0].Split(',')[0];

					if(lib == "IFacebook" || lib == "FB")
					{
						Debug.Log ("facebook SDK present - " + lib);
						hasFacebookSdk = true;
						break;
					}
				}

				if(!hasFacebookSdk)
					Debug.Log ("Please download the Facebook SDK to use this feature!");

				#endif
			}
			
			return instance; 
		}
	}
	
	void Awake() 
	{
		if (instance != null && instance != this) 
		{
			Destroy(this.gameObject);
		}
	}


	//=============================================================================
	// Init Facebook
	//=============================================================================
	
	public void Init(Action callback)
	{
#if facebook
		OnInit = callback;
		FB.Init(OnInitComplete);
#endif
	}

#if facebook
	void OnInitComplete()
	{
		if(OnInit != null) OnInit();
	}
#endif


	//=============================================================================
	// Login
	//=============================================================================

	public void Login(Action<Dictionary<string, object>> callback)
	{
#if facebook
		OnLogin = callback;
		FB.Login("email,publish_actions", OnLoginComplete);
#endif
	}

#if facebook
	void OnLoginComplete(FBResult result)
	{
		if(OnLogin != null) 
			OnLogin(DeserializeFBResult(result));
	}
#endif


	//=============================================================================
	// Post to Wall
	//=============================================================================

	public void PostToWall(Action<Dictionary<string, object>> callback, string title, string caption, string description, string image, string link)
	{
#if facebook
		if(!FB.IsLoggedIn)
		{
			Debug.Log ("User not logged in!");
			return;
		}

		OnPostToWall = callback;

		FB.Feed(
			toId: "",
			link: link,
			linkName: title,
			linkCaption: caption,
			linkDescription: description,
			picture: image,
			callback: OnFeedPostComplete
		);
#endif
	}

#if facebook
	void OnFeedPostComplete(FBResult result)
	{
		if(OnPostToWall != null) 
			OnPostToWall(DeserializeFBResult(result));
	}
#endif

	
	//=============================================================================
	// Post Image to Wall
	//=============================================================================

	public void PostImageToWall(Action<Dictionary<string, object>> callback, Texture2D image)
	{
#if facebook
		if(!FB.IsLoggedIn)
		{
			Debug.Log ("User not logged in!");
			return;
		}

		Instance.uploadImage = image;
		Instance.uploadCallback = callback;

		Instantiate(Resources.Load ("FBUploadImage"));
#endif
	}
	

	//=============================================================================
	// Upload image
	//=============================================================================
	
	public void UploadImage(Action<Dictionary<string, object>> callback, Texture2D image, String message)
	{
#if facebook
		if(!FB.IsLoggedIn)
		{
			Debug.Log ("User not logged in!");
			return;
		}

		OnImageUpload = callback;

		byte[] bytes = image.EncodeToPNG();
		
		var wwwForm = new WWWForm();
		wwwForm.AddBinaryData("image", bytes, "MyImage.png");
		wwwForm.AddField("message", message);
		
		FB.API("me/photos", Facebook.HttpMethod.POST, OnImageUploaded, wwwForm);
#endif
	}

#if facebook
	void OnImageUploaded(FBResult result)
	{
		if(OnImageUpload != null) 
			OnImageUpload(DeserializeFBResult(result));
	}
#endif


	//=============================================================================
	// Share with Friends
	//=============================================================================

	public void ShareWithFriends(Action<Dictionary<string, object>> callback, string title, string message, bool nonUsers = true, int? maxRecipients = null)
	{
#if facebook
		if(!FB.IsLoggedIn)
		{
			Debug.Log ("User not logged in!");
			return;
		}

		OnShareWithFriends = callback;

		List<object> filters = null;
		if(nonUsers)
			filters = Facebook.MiniJSON.Json.Deserialize("[\"app_non_users\"]") as List<object>;
		
		FB.AppRequest(
			message,
			null,
			filters,
			null,
			maxRecipients,
			"{}",
			title,
			OnShareWithFriendsComplete
		);
#endif
	}

#if facebook
	void OnShareWithFriendsComplete(FBResult result)
	{
		if(OnShareWithFriends != null) 
			OnShareWithFriends(DeserializeFBResult(result));
	}
#endif


	//=============================================================================
	// Get profile pic
	//=============================================================================

	public void GetProfilePic(Action<Texture2D> callback)
	{
#if facebook
		if(!FB.IsLoggedIn)
		{
			Debug.Log ("User not logged in!");
			return;
		}

		StartCoroutine(DownloadProfilePic(callback));
#endif
	}

#if facebook
	IEnumerator DownloadProfilePic(Action<Texture2D> callback)
	{
		WWW www = new WWW("https://graph.facebook.com/" + FB.UserId + "/picture?type=large");
		Texture2D profilePic = new Texture2D(128, 128, TextureFormat.DXT1, false);
		
		yield return www;
		
		www.LoadImageIntoTexture(profilePic);
		callback(profilePic);
	}
#endif


	//=============================================================================
	// Get user details
	//=============================================================================
	
	public void GetUserDetails(Action<Dictionary<string, object>> callback)
	{
#if facebook
		if(!FB.IsLoggedIn)
		{
			Debug.Log ("User not logged in!");
			return;
		}

		OnGetUserDetails = callback;

		FB.API("/me?fields=id,name,first_name,last_name,email,link,gender,locale", Facebook.HttpMethod.GET, OnGetUserDetailsComplete);
#endif
	}

#if facebook
	void OnGetUserDetailsComplete(FBResult result)
	{
		if(OnGetUserDetails != null) 
			OnGetUserDetails(DeserializeFBResult(result));
	}
#endif


	//=============================================================================
	// Disconnect
	//=============================================================================

	public void Logout()
	{
#if facebook
		if(FB.IsLoggedIn) FB.Logout();
#endif	
	}


	//=============================================================================
	// Generic functions
	//=============================================================================

#if facebook
	public Dictionary<string, object> DeserializeFBResult(FBResult result)
	{
		Dictionary<string, object> data = Json.Deserialize(result.Text) as Dictionary<string, object>;
		return data;
	}
#endif

}
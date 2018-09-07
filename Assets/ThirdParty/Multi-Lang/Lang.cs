/*
The Lang Class adds easy to use multiple language support to any Unity project by parsing an XML file
containing numerous strings translated into any languages of your choice.  Refer to UMLS_Help.html and lang.xml
for more information.
 
Created by Adam T. Ryder
C# version by O. Glorieux

http://forum.unity3d.com/threads/add-multiple-language-support-to-your-unity-projects.206271/
*/

using System;
using System.Collections;
using System.IO;
using System.Xml;

using UnityEngine;

public class Lang
{
	private Hashtable table;
	
	/*
    Initialize Lang class
    path = path to XML resource example:  Path.Combine(Application.dataPath, "lang.xml")
    language = language to use example:  "English"
    web = boolean indicating if resource is local or on-line example:  true if on-line, false if local
 
    NOTE:
    If XML resource is on-line rather than local do not supply the path to the path variable as stated above
    instead use the WWW class to download the resource and then supply the resource.text to this initializer
 
    Web Example:
    var wwwXML : WWW = new WWW("http://www.exampleURL.com/lang.xml");
    yield wwwXML;
     
    var LangClass : Lang = new Lang(wwwXML.text, currentLang, true)
    */
	public Lang ( string path, string language, bool web) {
		if (!web) {
			setLanguage(path, language);
		} else {
			setLanguageWeb(path, language);
		}
	}
	
	/*
    Use the setLanguage function to swap languages after the Lang class has been initialized.
    This function is called automatically when the Lang class is initialized.
    path = path to XML resource example:  Path.Combine(Application.dataPath, "lang.xml")
    language = language to use example:  "English"
 
    NOTE:
    If the XML resource is stored on the web rather than on the local system use the
    setLanguageWeb function
    */
	public void setLanguage ( string path, string language) {
		var xml = new XmlDocument();
		xml.Load(path);
		
		table = new Hashtable();
		var element = xml.DocumentElement[language];
		if (element != null) {
			var elemEnum = element.GetEnumerator();
			while (elemEnum.MoveNext()) {
				var xmlItem = (XmlElement)elemEnum.Current;
				table.Add(xmlItem.GetAttribute("name"), xmlItem.InnerText);
			}
		} else {
			Debug.LogError("[Lang] The specified language does not exist: " + language);
		}
	}
	
	/*
    Use the setLanguageWeb function to swap languages after the Lang class has been initialized
    and the XML resource is stored on the web rather than locally.  This function is called automatically
    when the Lang class is initialized.
    xmlText = String containing all XML nodes
    language = language to use example:  "English"
 
    Example:
    var wwwXML : WWW = new WWW("http://www.exampleURL.com/lang.xml");
    yield wwwXML;
     
    var LangClass : Lang = new Lang(wwwXML.text, currentLang)
    */
	public void setLanguageWeb ( string xmlText, string language) {
		var xml = new XmlDocument();
		xml.Load(new StringReader(xmlText));
		
		table = new Hashtable();
		var element = xml.DocumentElement[language];
		if (element != null) {
			var elemEnum = element.GetEnumerator();
			while (elemEnum.MoveNext()) {
				var xmlItem = (XmlElement)elemEnum.Current;
				table.Add(xmlItem.GetAttribute("name"), xmlItem.InnerText);
			}
		} else {
			Debug.LogError("[Lang] The specified language does not exist: " + language);
		}
	}
	
	/*
    Access strings in the currently selected language by supplying this getString function with
    the name identifier for the string used in the XML resource.
 
    Example:
    XML file:
    <languages>
        <English>
            <string name="app_name">Unity Multiple Language Support</string>
            <string name="description">This script provides convenient multiple language support.</string>
        </English>
        <French>
            <string name="app_name">Unité Langue Soutien Multiple</string>
            <string name="description">Ce script fournit un soutien multilingue pratique.</string>
        </French>
    </languages>
 
    JavaScript:
    var appName : String = langClass.getString("app_name");
          */
	public string getString (string name, CaseHandle _case = CaseHandle.Normal) {
		if (!table.ContainsKey(name)) {
			Debug.LogWarning("[Lang] The specified string does not exist: " + name);
			
			return "\"" + name + "\"";
		}

		string lang = (string)table[name];
		switch (_case) {
		case CaseHandle.Upper:
			lang = lang.ToUpper();
			break;
		case CaseHandle.Lower:
			lang = lang.ToLower();
			break;
		case CaseHandle.FirstCharUpper:
			lang = lang[0].ToString().ToUpper() + lang.Substring(1).ToLower();
			break;
		case CaseHandle.FirstCharUpperForEachWord:
			string[] words = lang.Split(' ');
			for(int i =0; i < words.Length; i++) {
				words[i] = words[i][0].ToString().ToUpper() + words[i].Substring(1).ToLower();
			}
			lang = string.Join(" ", words);
			break;
		}
		
		return lang.Replace("\\n", "\n");
	}
	
}
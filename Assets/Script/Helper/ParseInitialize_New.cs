// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

//-----------------------------------------------------
// mLang v1.3
// backup hku server's path and app_id
// app_id  : Y9orBaF45mjkyT8vlWL1DFlysGwG0v15Q7NpYFW7
// .netkey : 9izk9NqDVccOqQLDdqjQDxui9iU3YpQrbK6wvV5A
// server  : http://147.8.219.237:1337/parse/
//-----------------------------------------------------
//using Parse.Common.Internal;
using Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ParseInitialize_New : ParseInitializeBehaviour {

    public static ParseInitialize_New instance;
    private static bool isInitialized = false;
    
    [SerializeField]
    public string server;
    string defaultServerURL = null;
    
    public override void Awake() 
	{
		Debug.Log ("[ParseInitialize_New] Awake()");
        if (isInitialized) {
            return;
        }

        instance = this;

        isInitialized = true;

        //hack the param to avoid base init
        //but we still have to call base awake to setup Dispatcher
        string appID = this.applicationID;
        this.applicationID = "";
        string dk = this.dotnetKey;
        this.dotnetKey = "";

        base.Awake();

        this.applicationID = appID;
        this.dotnetKey = dk;

        defaultServerURL = string.IsNullOrEmpty(server) ? null : server;

        init();
    }

    public void init() 
	{
		server = string.IsNullOrEmpty(server) ? defaultServerURL : server == "parse" ? null : server;
		Debug.Log ("[ParseInitialize_New] init() server:[" + server + "]");

        //manual init
        //it should be processed after base awake as tested
        ParseClient.Initialize(new ParseClient.Configuration {
            ApplicationId = applicationID,
            WindowsKey = dotnetKey,
            Server = server
        });
    }
}

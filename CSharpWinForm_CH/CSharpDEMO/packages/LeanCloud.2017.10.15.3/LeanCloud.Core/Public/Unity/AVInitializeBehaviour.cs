// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This Source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this Source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LeanCloud
{
    /// <summary>
    /// Mandatory MonoBehaviour for scenes that use LeanCloud. Set the application ID and .NET key
    /// in the editor.
    /// </summary>
    // TODO (hallucinogen): somehow because of Push, we need this class to be added in a GameObject
    // called `AVInitializeBehaviour`. We might want to fix this.
    public class AVInitializeBehaviour : MonoBehaviour
    {
        private static bool isInitialized = false;

        [SerializeField]
        /// <summary>
        /// The LeanCloud applicationId used in this app. You can get this value from the LeanCloud website.
        /// </summary>
        public string applicationID;


        [SerializeField]
        /// <summary>
        /// The LeanCloud applicationKey used in this app. You can get this value from the LeanCloud website.
        /// </summary>
        public string applicationKey;

        [SerializeField]
        /// <summary>
        /// The service region.
        /// </summary>
        public AVClient.Configuration.AVRegion region;


        [SerializeField]
        /// <summary>
        /// Use this uri as cloud function server host. This is used for local development.
        /// </summary>
        public string engineServer;

        [SerializeField]
        /// <summary>
        /// Whether use production stage to process request or not.
        /// </summary>
        public bool useProduction = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:LeanCloud.AVInitializeBehaviour"/> is web player.
        /// </summary>
        /// <value><c>true</c> if is web player; otherwise, <c>false</c>.</value>
        public static bool IsWebPlayer { get; set; }

        /// <summary>
        /// Initializes the LeanCloud SDK and begins running network requests created by LeanCloud.
        /// </summary>
        public virtual void Awake()
        {
            IsWebPlayer = Application.isWebPlayer;

            Initialize();

            // Force the name to be `AVInitializeBehaviour` in runtime.
            gameObject.name = "AVInitializeBehaviour";
        }

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;
            // Keep this gameObject around, even when the scene changes.
            GameObject.DontDestroyOnLoad(gameObject);

            AVClient.Initialize(new AVClient.Configuration
            {
                ApplicationId = applicationID,
                ApplicationKey = applicationKey,
                Region = region,
                EngineServer = (!string.IsNullOrEmpty(engineServer)) ? new Uri(engineServer) : null,
            });

            AVClient.UseProduction = useProduction;

            Dispatcher.Instance.GameObject = gameObject;

            // Kick off the dispatcher.
            StartCoroutine(Dispatcher.Instance.DispatcherCoroutine);
        }
    }
}

// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This Source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this Source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LeanCloud.Storage.Internal;
using System.Linq;

namespace LeanCloud.Core.Internal
{
    /// <summary>
    /// AVCommand is an <see cref="HttpRequest"/> with pre-populated
    /// headers.
    /// </summary>
    public class AVCommand : HttpRequest
    {
        public IDictionary<string, object> DataObject { get; private set; }
        public override Stream Data
        {
            get
            {
                if (base.Data != null)
                {
                    return base.Data;
                }

                return base.Data = (DataObject != null
                  ? new MemoryStream(Encoding.UTF8.GetBytes(Json.Encode(DataObject)))
                  : null);
            }
            set { base.Data = value; }
        }

        public AVCommand(string relativeUri,
            string method,
            string sessionToken = null,
            IList<KeyValuePair<string, string>> headers = null,
            IDictionary<string, object> data = null) : this(relativeUri: relativeUri,
                method: method,
                sessionToken: sessionToken,
                headers: headers,
                stream: null,
                contentType: data != null ? "application/json" : null)
        {
            DataObject = data;
        }

        public AVCommand(string relativeUri,
                string method,
                string sessionToken = null,
                IList<KeyValuePair<string, string>> headers = null,
                Stream stream = null,
                string contentType = null)
        {
            var state = AVPlugins.Instance.AppRouterController.Get();
            var urlTemplate = "https://{0}/{1}/{2}";
            AVClient.Configuration configuration = AVClient.CurrentConfiguration;
            var apiVersion = "1.1";
            if (relativeUri.StartsWith("push") || relativeUri.StartsWith("installations"))
            {
                Uri = new Uri(string.Format(urlTemplate, state.PushServer, apiVersion, relativeUri));
            }
            else if (relativeUri.StartsWith("stats") || relativeUri.StartsWith("always_collect") || relativeUri.StartsWith("statistics"))
            {
                Uri = new Uri(string.Format(urlTemplate, state.StatsServer, apiVersion, relativeUri));
            }
            else if (relativeUri.StartsWith("functions") || relativeUri.StartsWith("call"))
            {
                Uri = new Uri(string.Format(urlTemplate, state.EngineServer, apiVersion, relativeUri));

                if (configuration.EngineServer != null)
                {
                    Uri = new Uri(string.Format("{0}/{1}/{2}", configuration.EngineServer, apiVersion, relativeUri));
                }
            }
            else
            {
                Uri = new Uri(string.Format(urlTemplate, state.ApiServer, apiVersion, relativeUri));
            }
            Method = method;
            Data = stream;
            Headers = new List<KeyValuePair<string, string>>(headers ?? Enumerable.Empty<KeyValuePair<string, string>>());

            string useProduction = AVClient.UseProduction ? "1" : "0";
            Headers.Add(new KeyValuePair<string, string>("X-LC-Prod", useProduction));

            if (!string.IsNullOrEmpty(sessionToken))
            {
                Headers.Add(new KeyValuePair<string, string>("X-LC-Session", sessionToken));
            }
            if (!string.IsNullOrEmpty(contentType))
            {
                Headers.Add(new KeyValuePair<string, string>("Content-Type", contentType));
            }
        }

        public AVCommand(AVCommand other)
        {
            this.Uri = other.Uri;
            this.Method = other.Method;
            this.DataObject = other.DataObject;
            this.Headers = new List<KeyValuePair<string, string>>(other.Headers);
            this.Data = other.Data;
        }

        public string ToLog()
        {
            StringBuilder sb = new StringBuilder();
            var start = "===HTTP Request Start===";
            sb.AppendLine(start);
            var urlLog = "Url: " + this.Uri;
            sb.AppendLine(urlLog);

            var methodLog = "Method: " + this.Method;
            sb.AppendLine(methodLog);

            try
            {
                var headers = this.Headers.ToDictionary(x => x.Key, x => x.Value as object);
                var headersLog = "Headers: " + Json.Encode(headers);
                sb.AppendLine(headersLog);
            }
            catch (Exception eh)
            {
            }

            try
            {
                if (this.DataObject != null)
                {
                    var bodyLog = "Body:" + Json.Encode(this.DataObject);
                    sb.AppendLine(bodyLog);
                }
            }
            catch (Exception eb)
            {
            }

            var end = "===HTTP Request End===";
            sb.AppendLine(end);
            return sb.ToString();
        }
    }
}

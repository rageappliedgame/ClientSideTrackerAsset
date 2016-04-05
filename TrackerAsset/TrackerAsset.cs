/*
 * Copyright 2016 Open University of the Netherlands
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * This project has received funding from the European Union’s Horizon
 * 2020 research and innovation programme under grant agreement No 644187.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
namespace AssetPackage
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A tracker asset.
    /// 
    /// <list type="number">
    /// <item><term>TODO</term><desciption> - Add method to return the mime-type/content-type.</desciption></item>
    /// <item><term>TODO</term><desciption> - Add method to return the accept-type.</desciption></item>
    /// 
    /// <item><term>TODO</term><desciption> - Find a solution for the Connect inside Flush (in UCM's Code).</desciption></item>
    /// 
    /// <item><term>TODO</term><desciption> - Check disk based storage (local).</desciption></item>
    /// <item><term>TODO</term><desciption> - Serialize Queue for later submission.</desciption></item>
    /// <item><term>TODO</term><desciption> - Check if ObjectRegEx works un Unity (does so in .NET 3.5).</desciption></item>
    /// 
    /// <item><term>TODO</term><desciption> - Prevent csv/xml/json from net storage and xapi from local storage.</desciption></item>
    /// 
    /// <item><term>TODO</term><desciption> - Add context to IWebServiceRequest (so we know what TrackEvents to remove in Success or re-add in Error).</desciption></item>  
    /// </list>
    /// </summary>
    public class TrackerAsset : BaseAsset, IWebServiceResponse
    {
        #region Fields

        /// <summary>
        /// The RegEx to extract a JSON Object. Used to extract 'actor'.
        /// </summary>
        ///
        /// <remarks>
        /// NOTE: This regex handles matching brackets by using balancing groups. This should be tested in Mono if it works there too.<br />
        /// NOTE: {} brackets must be escaped as {{ and }} for String.Format statements.<br />
        /// NOTE: \ must be escaped as \\ in strings.<br />
        /// </remarks>
        private const string ObjectRegEx =
            "\"{0}\":(" +                   // {0} is replaced by the proprty name, capture only its value in {} brackets.
            "\\{{" +                        // Start with a opening brackets.
            "(?>" +
            "    [^{{}}]+" +                // Capture each non bracket chracter.
            "    |    \\{{ (?<number>)" +   // +1 for opening bracket.
            "    |    \\}} (?<-number>)" +  // -1 for closing bracket.
            ")*" +
            "(?(number)(?!))" +             // Handle unaccounted left brackets with a fail.
            "\\}})"; // Stop at matching bracket.

        //private const string ObjectRegEx = "\"{0}\":(\\{{(?:.+?)\\}},)";
        /// <summary>
        /// Filename of the settings file.
        /// </summary>
        const String SettingsFileName = "TrackerAssetSettings.xml";

        /// <summary>
        /// The TimeStamp Format.
        /// </summary>
        private const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        /// <summary>
        /// The RegEx to extract a plain quoted JSON Value. Used to extract 'token'.
        /// </summary>
        private const string TokenRegEx = "\"{0}\":\"(.+?)\"";

        /// <summary>
        /// The instance.
        /// </summary>
        static readonly TrackerAsset _instance = new TrackerAsset();

        /// <summary>
        /// The actor object.
        /// 
        /// Extracted from JSON inside Success().
        /// </summary>
        private static String ActorObject = String.Empty;

        /// <summary>
        /// Identifier for the object.
        /// 
        /// Extracted from JSON inside Success().
        /// </summary>
        private static String ObjectId = String.Empty;

        /// <summary>
        /// A Regex to extact the actor object from JSON.
        /// </summary>
        private Regex jsonActor = new Regex(String.Format(ObjectRegEx, "actor"), RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// A Regex to extact the authentication token value from JSON.
        /// </summary>
        private Regex jsonAuthToken = new Regex(String.Format(TokenRegEx, "authToken"), RegexOptions.Singleline);

        /// <summary>
        /// A Regex to extact the objectId value from JSON.
        /// </summary>
        private Regex jsonObjectId = new Regex(String.Format(TokenRegEx, "objectId"), RegexOptions.Singleline);

        /// <summary>
        /// A Regex to extact the token value from JSON.
        /// </summary>
        private Regex jsonToken = new Regex(String.Format(TokenRegEx, "token"), RegexOptions.Singleline);

        /// <summary>
        /// A Regex to extact the status value from JSON.
        /// </summary>
        private Regex jsonHealth = new Regex(String.Format(TokenRegEx, "status"), RegexOptions.Singleline);

        /// <summary>
        /// The queue of TrackerEvents to Send.
        /// </summary>
        private Queue<TrackerEvent> queue = new Queue<TrackerEvent>();

        /// <summary>
        /// Options for controlling the operation.
        /// </summary>
        private TrackerAssetSettings settings = null;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Explicit static constructor tells # compiler not to mark type as
        /// beforefieldinit.
        /// </summary>
        static TrackerAsset()
        {
            // Nothing
        }

        /// <summary>
        /// Prevents a default instance of the TrackerAsset class from being created.
        /// </summary>
        private TrackerAsset()
            : base()
        {
            settings = new TrackerAssetSettings();

            if (LoadSettings(SettingsFileName))
            {
                // ok
            }
            else
            {
                settings.Secure = false;
                settings.Host = "127.0.0.1";
                settings.Port = 3000;
                settings.BasePath = "/api/";

                settings.UserToken = "a:";
                settings.TrackingCode = String.Empty;
                settings.StorageType = StorageTypes.net;
                settings.TraceFormat = TraceFormats.xapi;

                SaveSettings(SettingsFileName);
            }
        }

        #endregion Constructors

        #region Enumerations

        /// <summary>
        /// Values that represent events.
        /// </summary>
        public enum Events
        {
            /// <summary>
            /// An enum constant representing the choice option.
            /// </summary>
            choice,
            /// <summary>
            /// An enum constant representing the click option.
            /// </summary>
            click,
            /// <summary>
            /// An enum constant representing the screen option.
            /// </summary>
            screen,
            /// <summary>
            /// An enum constant representing the variable option.
            /// </summary>
            var,
            /// <summary>
            /// An enum constant representing the zone option.
            /// </summary>
            zone,
        }

        /// <summary>
        /// Values that represent storage types.
        /// </summary>
        public enum StorageTypes
        {
            /// <summary>
            /// An enum constant representing the network option.
            /// </summary>
            net,

            /// <summary>
            /// An enum constant representing the local option.
            /// </summary>
            local
        }

        /// <summary>
        /// Values that represent trace formats.
        /// </summary>
        public enum TraceFormats
        {
            /// <summary>
            /// An enum constant representing the JSON option.
            /// </summary>
            json,
            /// <summary>
            /// An enum constant representing the XML option.
            /// </summary>
            xml,
            /// <summary>
            /// An enum constant representing the xAPI option.
            /// </summary>
            xapi,
            /// <summary>
            /// An enum constant representing the CSV option.
            /// </summary>
            csv,
        }

        #endregion Enumerations

        #region Properties

        /// <summary>
        /// Visible when reflecting.
        /// </summary>
        ///
        /// <value>
        /// The instance.
        /// </value>
        public static TrackerAsset Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the connection active (ie the ActorObject
        /// and ObjectId have been extracted).
        /// </summary>
        ///
        /// <value>
        /// true if active, false if not.
        /// </value>
        public Boolean Active { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the connected (ie a UserToken is present and no Fail() has occurred).
        /// </summary>
        ///
        /// <value>
        /// true if connected, false if not.
        /// </value>
        public Boolean Connected { get; private set; }

        /// <summary>
        /// Gets the health.
        /// </summary>
        ///
        /// <value>
        /// The health.
        /// </value>
        public String Health { get; private set; }

        /// <summary>
        /// Gets or sets options for controlling the operation.
        /// </summary>
        ///
        /// <remarks>   Besides the toXml() and fromXml() methods, we never use this property but use
        ///                it's correctly typed backing field 'settings' instead. </remarks>
        /// <remarks> This property should go into each asset having Settings of its own. </remarks>
        /// <remarks>   The actual class used should be derived from BaseAsset (and not directly from
        ///             ISetting). </remarks>
        ///
        /// <value>
        /// The settings.
        /// </value>
        public override ISettings Settings
        {
            get
            {
                return settings;
            }
            set
            {
                settings = (value as TrackerAssetSettings);
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Checks the health of the UCM Tracker.
        /// </summary>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        public Boolean CheckHealth()
        {
            RequestResponse response = IssueRequest2("health", "GET");

            if (response.ResultAllowed)
            {
                if (jsonHealth.IsMatch(response.body))
                {
                    Health = jsonHealth.Match(response.body).Groups[1].Value;

                    Log(Severity.Information, "Health Status={0}", Health);
                }
            }
            else
            {
                Log(Severity.Error, "Request Error: {0}-{1}", response.responseCode, response.responsMessage);
            }

            return response.ResultAllowed;
        }

        /// <summary>
        /// Player selected an option in a presented choice
        /// </summary>
        /// <param name="choiceId">Choice identifier.</param>
        /// <param name="optionId">Option identifier.</param>
        public void Choice(string choiceId, string optionId)
        {
            Trace(new TrackerEvent()
            {
                Event = TrackerEvent.CHOICE,
                Target = choiceId,
                Value = optionId
            });
        }

        /// <summary>
        /// Clicks.
        /// </summary>
        ///
        /// <param name="x"> The x coordinate. </param>
        /// <param name="y"> The y coordinate. </param>
        public void Click(float x, float y)
        {
            Click(x, y, String.Empty);
        }

        /// <summary>
        /// Clicks.
        /// </summary>
        ///
        /// <param name="x">      The x coordinate. </param>
        /// <param name="y">      The y coordinate. </param>
        /// <param name="target"> Target for the. </param>
        public void Click(float x, float y, string target)
        {
            Trace(new TrackerEvent()
            {
                Event = TrackerEvent.CLICK,
                Target = target,
                Value = String.Format("{0}x{1}", x, y)
            });
        }

        /// <summary>
        /// Errors.
        /// </summary>
        ///
        /// <param name="url"> URL of the document. </param>
        /// <param name="msg"> The error message. </param>
        public void Error(string url, string msg)
        {
            //Log(Severity.Error, "{0} - [{1}]", msg, url);

            //Connected = false;
        }

        /// <summary>
        /// Flushes the queue.
        /// </summary>
        public void Flush()
        {
            if (!Connected)
            {
                Log(Severity.Verbose, "Not connected yet, Can't flush.");

                // Start();
            }
            else
            {
                ProcessQueue();
            }
        }

        /// <summary>
        /// Login with a Username and Password.
        ///
        /// After this call, the Success method will extract the token from the returned .
        /// </summary>
        ///
        /// <param name="username"> The username. </param>
        /// <param name="password"> The password. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        public Boolean Login(string username, string password)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();

            headers.Add("Content-Type", "application/json");
            headers.Add("Accept", "application/json");

            RequestResponse response = IssueRequest2("login", "POST", headers,
                String.Format("{{\r\n \"username\": \"{0}\",\r\n \"password\": \"{1}\"\r\n}}",
                username, password));

            if (response.ResultAllowed)
            {
                if (jsonToken.IsMatch(response.body))
                {
                    settings.UserToken = jsonToken.Match(response.body).Groups[1].Value;
                    if (settings.UserToken.StartsWith("Bearer "))
                    {
                        settings.UserToken.Remove(0, "Bearer ".Length);
                    }
                    Log(Severity.Information, "Token= {0}", settings.UserToken);

                    Connected = true;
                }
            }
            else
            {
                Log(Severity.Error, "Request Error: {0}-{1}", response.responseCode, response.responsMessage);

                Connected = false;
            }

            return Connected;
        }

        /// <summary>
        /// Screens.
        /// </summary>
        ///
        /// <param name="screenId"> Identifier for the screen. </param>
        public void Screen(string screenId)
        {
            Trace(new TrackerEvent()
            {
                Event = TrackerEvent.SCREEN,
                Target = screenId
            });
        }

        /// <summary>
        /// Starts with a userToken and trackingCode.
        /// </summary>
        ///
        /// <param name="userToken">    The user token. </param>
        /// <param name="trackingCode"> The tracking code. </param>
        public void Start(String userToken, String trackingCode)
        {
            settings.UserToken = userToken;
            settings.TrackingCode = trackingCode;

            Start();
        }

        /// <summary>
        /// Starts with a trackingCode (and with the already extracted UserToken).
        /// </summary>
        ///
        /// <param name="trackingCode"> The tracking code. </param>
        public void Start(String trackingCode)
        {
            settings.TrackingCode = trackingCode;

            Start();
        }

        /// <summary>
        /// Starts Tracking with: 1) An already extracted UserToken (from Login) and
        /// 2) TrackingCode (Shown at Game on a2 server).
        /// </summary>
        public void Start()
        {
            switch (settings.StorageType)
            {
                case StorageTypes.net:
                    Dictionary<string, string> headers = new Dictionary<string, string>();

                    //! The UserToken might get swapped for a better one during response
                    //! processing. 
                    headers["Authorization"] = String.Format("Bearer {0}", settings.UserToken);

                    RequestResponse response = IssueRequest2(String.Format("proxy/gleaner/collector/start/{0}", settings.TrackingCode), "POST", headers, String.Empty);

                    if (response.ResultAllowed)
                    {
                        Log(Severity.Information, "");

                        // Extract AuthToken.
                        //
                        if (jsonAuthToken.IsMatch(response.body))
                        {
                            settings.UserToken = jsonAuthToken.Match(response.body).Groups[1].Value;
                            if (settings.UserToken.StartsWith("Bearer "))
                            {
                                //! Update UserToken.
                                settings.UserToken = settings.UserToken = settings.UserToken.Remove(0, "Bearer ".Length);
                            }
                            Log(Severity.Information, "AuthToken= {0}", settings.UserToken);

                            Connected = true;
                        }

                        // Extract AuthToken.
                        //
                        if (jsonObjectId.IsMatch(response.body))
                        {
                            ObjectId = jsonObjectId.Match(response.body).Groups[1].Value;

                            if (!ObjectId.EndsWith("/"))
                            {
                                ObjectId += "/";
                            }

                            Log(Severity.Information, "ObjectId= {0}", ObjectId);
                        }

                        // Extract Actor Json Object.
                        //
                        if (jsonActor.IsMatch(response.body))
                        {
                            ActorObject = jsonActor.Match(response.body).Groups[1].Value;

                            Log(Severity.Information, "Actor= {0}", ActorObject);

                            Active = true;
                        }
                    }
                    else
                    {
                        Log(Severity.Error, "Request Error: {0}-{1}", response.responseCode, response.responsMessage);

                        Active = false;
                        Connected = false;
                    }

                    break;

                case StorageTypes.local:
                    {
                        // Allow LocalStorage if a Bridge is implementing IDataStorage.
                        // 
                        IDataStorage tmp = getInterface<IDataStorage>();

                        Connected = tmp != null;
                        Active = tmp != null;
                    }
                    break;
            }
        }

        /// <summary>
        /// Success.
        /// </summary>
        ///
        /// <remarks>
        /// This method also extracts information from the returned body (token,
        /// authToken, objectId and actor).
        /// </remarks>
        ///
        /// <param name="url">     URL of the document. </param>
        /// <param name="code">    The code. </param>
        /// <param name="headers"> The headers. </param>
        /// <param name="body">    The body. </param>
        public void Success(string url, int code, Dictionary<string, string> headers, string body)
        {
            //Log(Severity.Verbose, "Success: {0} - [{1}]", code, url);

            //foreach (KeyValuePair<string, string> kvp in headers)
            //{
            //    Log(Severity.Verbose, "{0}: {1}", kvp.Key, kvp.Value);
            //}
            //Log(Severity.Verbose, body);
            //Log(Severity.Verbose, "");

            //#warning the following code should be improved (is partially caused by the use of JSON instead of XML).

            // Flow:
            // 1a) If we use a: as Authorization value on the /start/ call (and do not login),
            // 1b) We have to take the 'authToken' value from the /start/ request for subsequent calls.
            // 2a) If we login with username/password, we get a temporary Authorization value from the 'token' value.
            // 2b) This Authorization value we use for /start/ and replace it inside success() with the 'authToken' value for subsequent calls.
            // 3a) The 'token' value from 2a) can also be used directly for a start() call.

            ////! /HEALTH/
            ////
            //if (url.EndsWith("/health"))
            //{
            //    Log(Severity.Information, "Health= {0}", body);
            //}

            //! /LOGIN/
            //
            //if (url.EndsWith("/login") && jsonToken.IsMatch(body))
            //{
            //    settings.UserToken = jsonToken.Match(body).Groups[1].Value;
            //    if (settings.UserToken.StartsWith("Bearer "))
            //    {
            //        settings.UserToken.Remove(0, "Bearer ".Length);
            //    }
            //    Log(Severity.Information, "Token= {0}", settings.UserToken);

            //    Connected = true;
            //}

            //! /START/
            //
            //if (url.EndsWith(String.Format("/start/{0}", (Settings as TrackerAssetSettings).TrackingCode)))
            //{
            //    Log(Severity.Information, "");

            //    // Extract AuthToken.
            //    //
            //    if (jsonAuthToken.IsMatch(body))
            //    {
            //        settings.UserToken = jsonAuthToken.Match(body).Groups[1].Value;
            //        if (settings.UserToken.StartsWith("Bearer "))
            //        {
            //            settings.UserToken = settings.UserToken = settings.UserToken.Remove(0, "Bearer ".Length);
            //        }
            //        Log(Severity.Information, "AuthToken= {0}", settings.UserToken);

            //        Connected = true;
            //    }

            //    // Extract AuthToken.
            //    //
            //    if (jsonObjectId.IsMatch(body))
            //    {
            //        ObjectId = jsonObjectId.Match(body).Groups[1].Value;

            //        if (!ObjectId.EndsWith("/"))
            //        {
            //            ObjectId += "/";
            //        }

            //        Log(Severity.Information, "ObjectId= {0}", ObjectId);
            //    }

            //    // Extract Actor Json Object.
            //    //
            //    if (jsonActor.IsMatch(body))
            //    {
            //        ActorObject = jsonActor.Match(body).Groups[1].Value;

            //        Log(Severity.Information, "Actor= {0}", ActorObject);

            //        Active = true;
            //    }
            //}

            if (url.EndsWith("/track"))
            {
                Log(Severity.Information, "Track= {0}", body);
            }

            Active = !(String.IsNullOrEmpty(ActorObject) || String.IsNullOrEmpty(ObjectId));
        }

        /// <summary>
        /// Adds the given value to the Queue.
        /// </summary>
        ///
        /// <param name="value"> New value for the variable. </param>
        public void Trace(TrackerEvent value)
        {
            queue.Enqueue(value);
        }

        /// <summary>
        /// A meaningful variable was updated in the game.
        /// </summary>
        /// <param name="varName">Variable name.</param>
        /// <param name="value">New value for the variable.</param>
        public void Var(string varName, System.Object value)
        {
            Trace(new TrackerEvent()
            {
                Event = TrackerEvent.VAR,
                Target = varName,
                Value = value
            });
        }

        /// <summary>
        /// Zones.
        /// </summary>
        ///
        /// <param name="zoneId"> Identifier for the zone. </param>
        public void Zone(string zoneId)
        {
            Trace(new TrackerEvent()
            {
                Event = TrackerEvent.ZONE,
                Target = zoneId
            });
        }

        /// <summary>
        /// Issue a HTTP Webrequest.
        /// </summary>
        ///
        /// <param name="path">   Full pathname of the file. </param>
        /// <param name="method"> The method. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        private bool IssueRequest(string path, string method)
        {
            return IssueRequest(path, method, new Dictionary<string, string>(), String.Empty);
        }

        /// <summary>
        /// Issue a HTTP Webrequest.
        /// </summary>
        ///
        /// <param name="path">    Full pathname of the file. </param>
        /// <param name="method">  The method. </param>
        /// <param name="headers"> The headers. </param>
        /// <param name="body">    The body. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        private bool IssueRequest(string path, string method, Dictionary<string, string> headers, string body)
        {
            return IssueRequest(path, method, headers, body, settings.Port);
        }

        /// <summary>
        /// Issue a HTTP Webrequest.
        /// </summary>
        ///
        /// <param name="path">    Full pathname of the file. </param>
        /// <param name="method">  The method. </param>
        /// <param name="headers"> The headers. </param>
        /// <param name="body">    The body. </param>
        /// <param name="port">    The port. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        private bool IssueRequest(string path, string method, Dictionary<string, string> headers, string body, Int32 port)
        {
            IWebServiceRequest ds = getInterface<IWebServiceRequest>();

            if (ds != null)
            {
                //Log(LogLevel.Verbose, "****");

                Uri uri = new Uri(string.Format("http{0}://{1}{2}{3}/{4}",
                    settings.Secure ? "s" : String.Empty,
                    settings.Host,
                    port == 80 ? String.Empty : String.Format(":{0}", port),
                    String.IsNullOrEmpty(settings.BasePath.TrimEnd('/')) ? "" : settings.BasePath.TrimEnd('/'),
                    path.TrimStart('/')));

                Log(Severity.Verbose, "{0} [{1}]", method, uri.ToString());

                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    Log(Severity.Verbose, "{0}: {1}", kvp.Key, kvp.Value);
                }

                if (!string.IsNullOrEmpty(body))
                {
                    Log(Severity.Verbose, body);
                }

                ds.WebServiceRequest(
                    method,
                    uri,
                    headers,
                    body,
                    this);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Issue a HTTP Webrequest.
        /// </summary>
        ///
        /// <param name="path">   Full pathname of the file. </param>
        /// <param name="method"> The method. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        private RequestResponse IssueRequest2(string path, string method)
        {
            return IssueRequest2(path, method, new Dictionary<string, string>(), String.Empty);
        }

        /// <summary>
        /// Issue a HTTP Webrequest.
        /// </summary>
        ///
        /// <param name="path">    Full pathname of the file. </param>
        /// <param name="method">  The method. </param>
        /// <param name="headers"> The headers. </param>
        /// <param name="body">    The body. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        private RequestResponse IssueRequest2(string path, string method, Dictionary<string, string> headers, string body)
        {
            return IssueRequest2(path, method, headers, body, settings.Port);
        }

        /// <summary>
        /// Query if this object issue request 2.
        /// </summary>
        ///
        /// <param name="path">    Full pathname of the file. </param>
        /// <param name="method">  The method. </param>
        /// <param name="headers"> The headers. </param>
        /// <param name="body">    The body. </param>
        /// <param name="port">    The port. </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>
        private RequestResponse IssueRequest2(string path, string method, Dictionary<string, string> headers, string body, Int32 port)
        {
            IWebServiceRequest2 ds = getInterface<IWebServiceRequest2>();

            RequestResponse response = new RequestResponse();

            if (ds != null)
            {
                ds.WebServiceRequest(
                   new RequestSetttings
                   {
                       method = method,
                       uri = new Uri(string.Format("http{0}://{1}{2}{3}/{4}",
                                   settings.Secure ? "s" : String.Empty,
                                   settings.Host,
                                   port == 80 ? String.Empty : String.Format(":{0}", port),
                                   String.IsNullOrEmpty(settings.BasePath.TrimEnd('/')) ? "" : settings.BasePath.TrimEnd('/'),
                                   path.TrimStart('/')
                                   )),
                       requestHeaders = headers,
                       //! allowedResponsCodes,     // default is ok
                       body = body, // or method.Equals("GET")?string.Empty:body
                   }, out response);
            }

            return response;
        }

        /// <summary>
        /// Process the queue.
        /// </summary>
        private void ProcessQueue()
        {
            if (queue.Count > 0)
            {
                List<string> sb = new List<string>();

                UInt32 cnt = settings.BatchSize == 0 ? UInt32.MaxValue : settings.BatchSize;

                while (queue.Count > 0 && cnt > 0)
                {
                    TrackerEvent item = queue.Dequeue();

                    cnt -= 1;

                    switch (settings.TraceFormat)
                    {
                        case TraceFormats.json:
                            sb.Add(item.ToJson());
                            break;
                        case TraceFormats.xml:
                            sb.Add(item.ToXml());
                            break;
                        case TraceFormats.xapi:
                            sb.Add(item.ToXapi());
                            break;
                        default:
                            sb.Add(item.ToCsv());
                            break;
                    }
                }

                String data = String.Empty;

                switch (settings.TraceFormat)
                {
                    case TraceFormats.json:
                        data = "[\r\n" + String.Join(",\r\n", sb.ToArray()) + "\r\n]";
                        break;
                    case TraceFormats.xml:
                        data = "<TrackEvents>\r\n" + String.Join("\r\n", sb.ToArray()) + "\r\n</TrackEvent>";
                        break;
                    case TraceFormats.xapi:
                        data = "[\r\n" + String.Join(",\r\n", sb.ToArray()) + "\r\n]";
                        break;
                    default:
                        data = String.Join("\r\n", sb.ToArray());
                        break;
                }

                sb.Clear();

                Log(Severity.Verbose, data);

                switch (settings.StorageType)
                {
                    case StorageTypes.local:
                        IDataStorage storage = getInterface<IDataStorage>();

                        if (storage != null)
                        {
                            String previous = storage.Exists(settings.LogFile) ? storage.Load(settings.LogFile) : String.Empty;

#warning TODO Add Append() to IDataStorage using File.AppendAllText().
#warning TODO Append is not 100% correct for XML and JSON (we need to insert it into the array).

                            storage.Save(settings.LogFile, previous + data);
                        }

                        break;
                    case StorageTypes.net:
                        Dictionary<string, string> headers = new Dictionary<string, string>();

                        headers.Add("Content-Type", "application/json");
                        headers.Add("Authorization", String.Format("Bearer {0}", settings.UserToken));

                        Log(Severity.Information, "\r\n" + data);

                        RequestResponse response = IssueRequest2("proxy/gleaner/collector/track",
                                "POST", headers, data);

                        if (response.ResultAllowed)
                        {
                            Log(Severity.Information, "Track= {0}", response.body);
                        }
                        else
                        {
                            Log(Severity.Error, "Request Error: {0}-{1}", response.responseCode, response.responsMessage);

                            Active = false;
                            Connected = false;
                        }

                        break;
                }
            }
            else
            {
                Log(Severity.Information, "Nothing to flush");
            }
        }

        #endregion Methods

        #region Nested Types

        /// <summary>
        /// A tracker event.
        /// </summary>
        public class TrackerEvent
        {
            #region Fields

            /// <summary>
            /// A choice.
            /// </summary>
            public const string CHOICE = "choice";

            /// <summary>
            /// A click.
            /// </summary>
            public const string CLICK = "click";

            /// <summary>
            /// A screen.
            /// </summary>
            public const string SCREEN = "screen";

            /// <summary>
            /// A variable.
            /// </summary>
            public const string VAR = "var";

            /// <summary>
            /// The zone.
            /// </summary>
            public const string ZONE = "zone";

            /// <summary>
            /// The extension prefix.
            /// </summary>
            private const string EXT_PREFIX = VOCAB_PREFIX + "ext/";

            /// <summary>
            /// The verb prefix.
            /// </summary>
            private const string VERB_PREFIX = VOCAB_PREFIX + "verbs/";

            /// <summary>
            /// purl URL prefix.
            /// </summary>
            private const string VOCAB_PREFIX = "http://purl.org/xapi/games/";

            #endregion Fields

            #region Constructors

            public TrackerEvent()
            {
                this.TimeStamp = DateTime.Now;
            }

            #endregion Constructors

            #region Properties

            /// <summary>
            /// Gets or sets the event.
            /// </summary>
            ///
            /// <value>
            /// The event.
            /// </value>
            [DefaultValue("")]
            public string Event { get; set; }

            /// <summary>
            /// Gets or sets the Target for the.
            /// </summary>
            ///
            /// <value>
            /// The target.
            /// </value>
            [DefaultValue("")]
            public string Target { get; set; }

            /// <summary>
            /// Gets the Date/Time of the time stamp.
            /// </summary>
            ///
            /// <value>
            /// The time stamp.
            /// </value>
            public DateTime TimeStamp { get; private set; }

            /// <summary>
            /// Gets or sets the value.
            /// </summary>
            ///
            /// <value>
            /// The value.
            /// </value>
            [DefaultValue(null)]
            public object Value { get; set; }

            #endregion Properties

            #region Methods

            /// <summary>
            /// Converts this object to a CSV Item.
            /// </summary>
            ///
            /// <returns>
            /// This object as a string.
            /// </returns>
            public string ToCsv()
            {
                return this.TimeStamp.ToString(TimeFormat) + ", " +
                       Enquote(this.Event) + ", " + Enquote(this.Target) +
                       (this.Value == null || String.IsNullOrEmpty(this.Value.ToString()) ?
                       String.Empty :
                       ", " + Enquote(this.Value.ToString()));
            }

            /// <summary>
            /// Converts this object to a JSON Item.
            /// </summary>
            ///
            /// <returns>
            /// This object as a string.
            /// </returns>
            public string ToJson()
            {
#warning Use proper JSON Encoding.
                return "{\r\n  \"timestamp\":\"" + this.TimeStamp.ToString(TimeFormat) + "\"" +
                       ",\r\n  \"event\":\"" + this.Event + "\"" +
                       ",\r\n  \"target\":\"" + this.Target + "\"" +
                       (this.Value == null || String.IsNullOrEmpty(this.Value.ToString()) ?
                       String.Empty :
                       ",\r\n  \"value\":\"" + this.Value.ToString() + "\"") +
                       "\r\n}";
            }

            /// <summary>
            /// Converts this object to an XML Item.
            /// </summary>
            ///
            /// <returns>
            /// This object as a string.
            /// </returns>
            public string ToXml()
            {
#warning Use XMLSerializer else use proper XML Encoding.
                return "<TrackEvent \"timestamp\"=\"" + this.TimeStamp.ToString(TimeFormat) + "\"" +
                       " \"event\"=\"" + this.Event + "\"" +
                       " \"target\"=\"" + this.Target + "\"" +
                       (this.Value == null || String.IsNullOrEmpty(this.Value.ToString()) ?
                       " />" :
                       "><![CDATA[" + this.Value.ToString() + "]]></TrackEvent>");
            }

            /// <summary>
            /// Converts this object to an xapi.
            /// </summary>
            ///
            /// <returns>
            /// This object as a string.
            /// </returns>
            internal string ToXapi()
            {
#warning Use proper JSON Encoding.
                return "{\"timestamp\":\"" + this.TimeStamp.ToString(TimeFormat) + "\",\r\n" +
                        "\"actor\":" + ActorObject + ",\r\n" +
                        "\"verb\":" + CreatexApiVerb() + ",\r\n" +
                        "\"object\":" + CreatexApiActivity() +
                        ((this.Value == null || String.IsNullOrEmpty(this.Value.ToString())) ? "\r\n" : ",\r\n\"result\":" + CreateResult()) +
                    "}";
            }

            private String CreateResult()
            {
                return "{\"extensions\":{\"" + EXT_PREFIX + "value\":\"" + this.Value + "\"}}";
            }

            /// <summary>
            /// Creates the activity.
            /// </summary>
            ///
            /// <returns>
            /// The new activity.
            /// </returns>
            private String CreatexApiActivity()
            {
                string ev = this.Event;

                string id;

                if (CHOICE.Equals(ev))
                {
                    id = "choice";
                }
                else if (SCREEN.Equals(ev))
                {
                    id = "screen";
                }
                else if (ZONE.Equals(ev))
                {
                    id = "zone";
                }
                else if (VAR.Equals(ev))
                {
                    id = "variable";
                }
                else
                {
                    //click...
                    id = ev;
                }

                return "{\"id\":\"" + ObjectId + id + "/" + this.Target + "\"}";
            }

            /// <summary>
            /// Creates the verb.
            /// </summary>
            ///
            /// <returns>
            /// The new verb.
            /// </returns>
            private String CreatexApiVerb()
            {
                string ev = this.Event;

                string id;
                if (CHOICE.Equals(ev))
                {
                    id = "choose";
                }
                else if (SCREEN.Equals(ev))
                {
                    id = "viewed";
                }
                else if (ZONE.Equals(ev))
                {
                    id = "entered";
                }
                else if (VAR.Equals(ev))
                {
                    id = "updated";
                }
                else
                {
                    //click...
                    id = ev;
                }

                return "{\"id\":\"" + VERB_PREFIX + id + "\"}";
            }

            /// <summary>
            /// Enquotes.
            /// </summary>
            ///
            /// <remarks>
            /// Both checks could be combined.
            /// </remarks>
            ///
            /// <param name="value"> The value. </param>
            ///
            /// <returns>
            /// A string.
            /// </returns>
            private string Enquote(string value)
            {
                if (value.Contains("\""))
                {
                    //1) Replace one quote by two quotes and enquote the whole string.
                    return string.Format("\"{0}\"", value.Replace("\"", "\"\""));
                }
                else if (value.Contains("\r\n") || value.Contains(","))
                {
                    // 2) If the string contains a CRLF or , enquote the whole string.
                    return string.Format("\"{0}\"", value);
                }

                return value;
            }

            #endregion Methods
        }

        #endregion Nested Types
    }
}
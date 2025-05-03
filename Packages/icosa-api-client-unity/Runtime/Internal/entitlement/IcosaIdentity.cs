// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

/// Handle accessing OAuth2 based web services.

namespace IcosaClientInternal.entitlement
{
    [ExecuteInEditMode]
    public class IcosaIdentity : MonoBehaviour
    {
        public class UserInfo
        {
            public string id;
            public string name;
            public string email;
            public Sprite icon;
        }

        private const string DEFAULT_AUTH_RESPONSE_TEXT =
            "<html><body><p>Successfully signed in.</p>" +
            "<p>You can now close this browser tab or window and return to the application.</p></body></html>";

        private string m_ServiceName;
        private string m_ClientId;
        private string m_ClientSecret;
        private const string m_DeviceCodeUri = "https://icosa.gallery/device";
        private const string m_AccessTokenUri = "https://api.icosa.gallery/v1/login/device_login";

        private const string m_UserInfoUri =
            "https://api.icosa.gallery/v1/users/me";

        private const string m_ReplaceHeadset = "ReplaceHeadset";
        private string m_CallbackFailedMessage = "Sorry!";

        // User avatar pixel density. This is the number of pixels that correspond to one unit in world space.
        // Larger values will make a smaller (more dense) avatar. Smaller values will make it larger (less dense).
        private const int USER_AVATAR_PIXELS_PER_UNIT = 30;

        private static Color UI_BACKGROUND_COLOR = Color.clear;

        public static IcosaIdentity Instance;

        public static event Action OnProfileUpdated
        {
            add
            {
                if (Instance != null)
                {
                    value(); // Call the event once for the current profile.
                }

                m_OnProfileUpdated += value;
            }
            remove { m_OnProfileUpdated -= value; }
        }

        private static event Action m_OnProfileUpdated;
        private static string PLAYER_PREF_REFRESH_KEY_SUFFIX = "PTOAuthRefreshKey";
        private string m_PlayerPrefRefreshKey;
        private const string kIconSizeSuffix = "?sz=128";

        private string m_AccessToken;
        private string m_RefreshToken;
        private UserInfo m_User = null;

        private HttpListener m_HttpListener;
        private int m_HttpPort;
        private bool m_WaitingOnAuthorization;
        private string m_VerificationCode;
        private Boolean m_VerificationError;

        public UserInfo Profile
        {
            get { return m_User; }
            set
            {
                m_User = value;
                if (m_OnProfileUpdated != null)
                {
                    m_OnProfileUpdated();
                }
            }
        }

        public bool LoggedIn
        {
            // We don't consider us logged in until we have the UserInfo
            get { return m_RefreshToken != null && Profile != null; }
        }

        public bool HasAccessToken
        {
            get { return m_AccessToken != null; }
        }

        public string AccessToken
        {
            get { return m_AccessToken; }
        }

        public bool WaitingOnAuthorization
        {
            get { return m_WaitingOnAuthorization; }
        }

        public string RefreshToken
        {
            get { return m_RefreshToken; }
        }

        /// <summary>
        /// Must be called to initialize.
        /// </summary>
        /// <param name="additionalScopes">Additional scopes to include in authentication.</param>
        /// <param name="clientId">OAuth2 Client ID</param>
        /// <param name="clientSecret">OAuth2 Client Secret</param>
        /// <param name="serviceName">Service name. For regular apps that only use one set of authentication
        /// credentials (typical case), pass null. However, if you need to have different auth "silos",
        /// each with one set of credentials, use this to distinguish between different
        /// authentication "silos" in your app. OAuth2Identity is a singleton, so you can only use one
        /// "silo" at a time, but passing different service names for your different silos will
        /// cause this class to, for example, store per-silo refresh keys separately.</param>
        public void Setup(string serviceName, string clientId, string clientSecret, string additionalScopes)
        {
            m_ServiceName = serviceName ?? "";
            m_ClientId = clientId;
            m_ClientSecret = clientSecret;

            Instance = this;
            m_PlayerPrefRefreshKey = String.Format("{0}_{1}", m_ServiceName, PLAYER_PREF_REFRESH_KEY_SUFFIX);

            if (PlayerPrefs.HasKey(m_PlayerPrefRefreshKey))
            {
                m_RefreshToken = PlayerPrefs.GetString(m_PlayerPrefRefreshKey);
            }
        }

        // Use Google Account Chooser to open a url with the current account.
        public void OpenURL(string url)
        {
            if (LoggedIn)
            {
                url = string.Format("https://accounts.google.com/AccountChooser?Email={0}&continue={1}",
                    Profile.email, url);
            }

            Application.OpenURL(url);
        }

        public void Login(System.Action onSuccess, System.Action onFailure, bool launchSignInFlowIfNeeded)
        {
            CoroutineRunner.StartCoroutine(this, Authorize(onSuccess, onFailure, launchSignInFlowIfNeeded));
        }

        public void LoginWithTokens(Action onSuccess, Action onFailure, string accessToken, string refreshToken)
        {
            CoroutineRunner.StartCoroutine(this, AuthorizeWithTokens(onSuccess, onFailure, accessToken, refreshToken));
        }

        public void CancelLogin()
        {
            if (m_WaitingOnAuthorization)
            {
                m_VerificationError = true;
                m_VerificationCode = "Aborted by caller";
                StopHttpListener();
            }
        }

        public void Logout()
        {
            if (m_RefreshToken != null)
            {
                m_RefreshToken = null;
                m_AccessToken = null;
                Profile = null;
                PlayerPrefs.DeleteKey(m_PlayerPrefRefreshKey);
            }
        }

        /// Sign an outgoing request.
        public void Authenticate(UnityWebRequest www)
        {
            www.SetRequestHeader("Authorization", String.Format("Bearer {0}", m_AccessToken));
        }

        private IEnumerator GetUserInfo()
        {
            if (String.IsNullOrEmpty(m_RefreshToken))
            {
                yield break;
            }

            UserInfo user = new UserInfo();
            for (int i = 0; i < 2; i++)
            {
                string uri = m_UserInfoUri + "&key=" + WWW.EscapeURL(IcosaMainInternal.Instance.apiKey);
                using (UnityWebRequest www = UnityWebRequest.Get(uri))
                {
                    Authenticate(www);
                    yield return UnityCompat.SendWebRequest(www);
                    if (www.responseCode == 200)
                    {
                        JObject json = JObject.Parse(www.downloadHandler.text);
                        user.id = json["resourceName"].ToString();
                        user.name = json["names"][0]["displayName"].ToString();
                        string iconUri = json["photos"][0]["url"].ToString();
                        if (json["emailAddresses"] != null)
                        {
                            foreach (var email in json["emailAddresses"])
                            {
                                var primary = email["metadata"]["primary"];
                                if (primary != null && primary.Value<bool>())
                                {
                                    user.email = email["value"].ToString();
                                    break;
                                }
                            }
                        }

                        Profile = user;
                        yield return LoadProfileIcon(iconUri);

                        yield break;
                    }
                    else if (www.responseCode == 401)
                    {
                        yield return Reauthorize(() => { }, (error) => { Debug.LogError(error); });
                    }
                    else
                    {
                        Debug.Log(www.responseCode);
                        Debug.Log(www.error);
                        Debug.Log(www.downloadHandler.text);
                    }
                }
            }

            Profile = null;
        }

        // I have a refresh token, I need an access token.
        public IEnumerator<object> Reauthorize(Action successCallback, Action<string> failureCallback)
        {
            m_AccessToken = null;
            if (!String.IsNullOrEmpty(m_RefreshToken))
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("client_id", m_ClientId);
                parameters.Add("client_secret", m_ClientSecret);
                parameters.Add("refresh_token", m_RefreshToken);
                parameters.Add("grant_type", "refresh_token");
                using (UnityWebRequest www = UnityWebRequest.Post(m_AccessTokenUri, parameters))
                {
                    yield return UnityCompat.SendWebRequest(www);
                    if (UnityCompat.IsNetworkError(www))
                    {
                        failureCallback("Network error");
                        yield break;
                    }

                    if (www.responseCode == 400 || www.responseCode == 401)
                    {
                        // Refresh token revoked or expired - forget it
                        m_RefreshToken = null;
                        PlayerPrefs.DeleteKey(m_PlayerPrefRefreshKey);
                        failureCallback("No valid refresh token, could not reauthorize");
                    }
                    else
                    {
                        JObject json = JObject.Parse(www.downloadHandler.text);
                        m_AccessToken = json["access_token"].ToString();
                        successCallback();
                    }
                }
            }
        }

        /// I have nothing.  Open browser to authorize permissions then get refresh and access tokens.
        public IEnumerator<object> Authorize(System.Action onSuccess, System.Action onFailure,
            bool launchSignInFlowIfNeeded)
        {
            if (String.IsNullOrEmpty(m_RefreshToken))
            {
                if (!launchSignInFlowIfNeeded)
                {
                    // We need to launch the sign-in flow. If we are not allowed to, then we have failed.
                    onFailure();
                    yield break;
                }

                // Something about the url makes OpenURL() not work on OSX, so use a workaround
                if (Application.platform == RuntimePlatform.OSXEditor ||
                    Application.platform == RuntimePlatform.OSXPlayer)
                {
                    System.Diagnostics.Process.Start(m_DeviceCodeUri);
                }
                else
                {
                    Application.OpenURL(m_DeviceCodeUri);
                }

                if (m_WaitingOnAuthorization)
                {
                    // A previous attempt is already waiting
                    yield break;
                }

                m_WaitingOnAuthorization = true;
                m_VerificationCode = null;
                m_VerificationError = false;

                // Wait for verification
                while (m_VerificationCode == null && !m_VerificationError)
                {
                    yield return null;
                }

                if (m_VerificationError)
                {
                    Debug.LogError("Account verification failed");
                    Debug.LogFormat("Verification error {0}", m_VerificationCode);
                    m_WaitingOnAuthorization = false;
                    yield break;
                }

                // Exchange for tokens
                var parameters = new Dictionary<string, string>();
                parameters.Add("code", m_VerificationCode);

                UnityWebRequest www = UnityWebRequest.Post(m_AccessTokenUri, parameters);

                yield return UnityCompat.SendWebRequest(www);
                if (UnityCompat.IsNetworkError(www))
                {
                    Debug.LogError("Network error");
                    m_WaitingOnAuthorization = false;
                    yield break;
                }
                else if (www.responseCode >= 400)
                {
                    Debug.LogError("Authorization failed");
                    Debug.LogFormat("Authorization error {0}", www.downloadHandler.text);
                    m_WaitingOnAuthorization = false;
                    yield break;
                }

                JObject json = JObject.Parse(www.downloadHandler.text);
                if (json != null)
                {
                    SetTokens(json["access_token"].ToString(), json["refresh_token"].ToString());
                }

                m_WaitingOnAuthorization = false;
            }

            yield return GetUserInfo();

            if (LoggedIn)
            {
                onSuccess();
            }
            else
            {
                onFailure();
            }
        }

        public IEnumerator<object> ExchangeDeviceCodeForTokens(string deviceCode)
        {
            // Exchange for tokens
            var parameters = new Dictionary<string, string>
            {
                { "code", deviceCode }
            };

            UnityWebRequest www = UnityWebRequest.Post(m_AccessTokenUri, parameters);

            yield return UnityCompat.SendWebRequest(www);
            if (UnityCompat.IsNetworkError(www))
            {
                Debug.LogError("Network error");
                m_WaitingOnAuthorization = false;
                yield break;
            }

            if (www.responseCode >= 400)
            {
                Debug.LogError("Authorization failed");
                Debug.LogFormat("Authorization error {0}", www.downloadHandler.text);
                m_WaitingOnAuthorization = false;
                yield break;
            }

            JObject json = JObject.Parse(www.downloadHandler.text);
            if (json != null)
            {
                SetTokens(json["access_token"].ToString(), json["refresh_token"].ToString());
            }

            m_WaitingOnAuthorization = false;
        }

        private void SetTokens(string accessToken, string refreshToken)
        {
            m_AccessToken = accessToken;
            m_RefreshToken = refreshToken;
            PlayerPrefs.SetString(m_PlayerPrefRefreshKey, m_RefreshToken);
        }

        /// <summary>
        /// Alternate form of Authorize() that allows setting the access and refresh tokens directly.
        /// This form will use those tokens instead of launching the auth flow.
        /// </summary>
        public IEnumerator<object> AuthorizeWithTokens(Action onSuccess, Action onFailure,
            string accessToken, string refreshToken)
        {
            SetTokens(accessToken, refreshToken);
            return Authorize(onSuccess, onFailure, launchSignInFlowIfNeeded: false);
        }

        private void StopHttpListener()
        {
            if (m_HttpListener != null)
            {
                m_HttpListener.Abort();
                m_HttpListener = null;
                m_HttpPort = 0;
            }
        }

        private IEnumerator LoadProfileIcon(string uri)
        {
            if (Profile == null)
            {
                yield break;
            }

            using (UnityWebRequest www = UnityCompat.GetTexture(uri + kIconSizeSuffix))
            {
                yield return UnityCompat.SendWebRequest(www);
                if (UnityCompat.IsNetworkError(www) || www.responseCode >= 400)
                {
                    Debug.LogErrorFormat("Error downloading {0}, error {1}", uri, www.responseCode);
                    Profile.icon = null;
                }
                else
                {
                    // Convert the texture to a circle and set it as the user's avatar in the UI.
                    Texture2D profileImage = DownloadHandlerTexture.GetContent(www);
                    Profile.icon = Sprite.Create(CropSquareTextureToCircle(profileImage),
                        new Rect(0, 0, profileImage.width, profileImage.height), new Vector2(0.5f, 0.5f),
                        USER_AVATAR_PIXELS_PER_UNIT);
                }

                if (m_OnProfileUpdated != null)
                {
                    m_OnProfileUpdated();
                }
            }
        }

        private Texture2D CropSquareTextureToCircle(Texture2D squareTexture)
        {
            float width = squareTexture.width;
            float height = squareTexture.height;
            float radius = width / 2;
            float centerX = squareTexture.width / 2;
            float centerY = squareTexture.height / 2;
            Color[] c = squareTexture.GetPixels(0, 0, (int)width, (int)height);
            Texture2D circleTexture = new Texture2D((int)height, (int)width);
            for (int i = 0; i < height * width; i++)
            {
                int y = Mathf.FloorToInt(i / width);
                int x = Mathf.FloorToInt(i - (y * width));
                if (radius * radius >= (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY))
                {
                    circleTexture.SetPixel(x, y, c[i]);
                }
                else
                {
                    circleTexture.SetPixel(x, y, UI_BACKGROUND_COLOR);
                }
            }

            circleTexture.Apply();
            return circleTexture;
        }
    }
}
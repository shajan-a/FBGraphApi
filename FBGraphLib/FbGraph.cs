using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FBGraphLib
{
    public class FbGraph
    {
        #region FIELDS

        private string _path = string.Empty;
        private string _filenameWithExtension;
        private string _contentType;
        private List<string> _lstFiles;
        private readonly CookieContainer _cookieContainer = new CookieContainer();
        private string _redirectUrl;

        #endregion

        #region PROPERTIES
        
        public string Email { get; set; }
        public string Password { get; set; }
        public string AccessToken { get; set; }
        public List<string> AllAlbumNames { get; set; } = new List<string>();

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Login to the Facebook account.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns>State</returns>
        public State LogIn(State state, string email, string password)
        {
            if (state.IsloggedIn && state.Cookies.Count > 0)
            {
                MessageBox.Show("Already Logged In");
                return state;
            }
            return LoginMethod(state.Cookies, email, password);
        }

        /// <summary>
        /// Generate User Access Token for logged in user.
        /// </summary>
        /// <param name="state"></param>
        /// <returns>string</returns>
        public string GetUserAccessToken(State state)
        {
            if (state.Cookies.Count <= 0)
            {
                MessageBox.Show("Not Logged In");
                return "INVALID TOKEN";
            }
            return GetUserAccessToken(state.Cookies ?? new CookieCollection());
        }

        /// <summary>
        /// List all the album names in Facebook account.
        /// </summary>
        /// <param name="state"></param>
        /// <returns>List&lt;string&gt;</returns>
        public List<string> GetAlbums(State state)
        {
            if (string.IsNullOrEmpty(state.AccessToken))
            {
                MessageBox.Show("Token Not Valid");
                return new List<string>();
            }
            return GetAlbum(state);
        }
        
        /// <summary>
        /// Upload image/video file(s) into Facebook account.
        /// </summary>
        /// <param name="state"></param>
        /// <returns>Upload</returns>
        public Upload UploadMediaFile(State state)
        {
            if (string.IsNullOrEmpty(state.AccessToken))
            {
                MessageBox.Show("Token Not Valid");
                return null;
            }
            return UploadMediaFile(state.Cookies, state.AccessToken);
        }

        /// <summary>
        /// Create an Album and upload one or more image/video file(s) into it.
        /// </summary>
        /// <param name="state"></param>
        /// <returns>Album</returns>
        public Album UploadToAlbum(State state)
        {
            if (string.IsNullOrEmpty(state.AccessToken))
            {
                MessageBox.Show("Token Not Valid");
                return null;
            }
            return UploadToAlbum(state.Cookies,state.AccessToken);
        }

        /// <summary>
        /// Logout from the Facebook account
        /// </summary>
        /// <param name="state"></param>
        /// <returns>State</returns>
        public State Logout(State state)
        {
            state.IsloggedIn = false;
            state.AccessToken = null;
            state.Cookies = new CookieCollection();
            MessageBox.Show("Logged out successfully");
            return state;
        }

        #endregion

        #region PRIVATE METHODS

        private State LoginMethod(CookieCollection cookies, string email, string password)
        {
            State state = new State();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.facebook.com");
            request.Proxy = new WebProxy();
            request.ServicePoint.ConnectionLimit = 10;
            /*--------------------------------------------*/
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; rv:11.0) Gecko/20100101 Firefox/15.0";
            request.Accept = "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Headers.Add("Accept-Language: en-us,en;q=0.5");
            request.Headers.Add("Accept-Encoding: gzip, deflate");
            request.KeepAlive = true;
            /*--------------------------------------------*/
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(cookies);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            cookies = response.Cookies.Count > 0 ? response.Cookies : cookies;

            string logInUrl = "https://www.facebook.com/login.php?login_attempt=1";
            string postData = String.Format("email={0}&pass={1}", email, password);
            HttpWebRequest logInRequest = (HttpWebRequest)WebRequest.Create(logInUrl);
            logInRequest.Proxy = new WebProxy();
            logInRequest.CookieContainer = new CookieContainer();
            logInRequest.CookieContainer.Add(cookies); //recover cookies First request

            logInRequest.Method = WebRequestMethods.Http.Post;
            logInRequest.ProtocolVersion = HttpVersion.Version11;
            /*-------------------------------------------*/
            logInRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; rv:11.0) Gecko/20100101 Firefox/15.0";
            logInRequest.Accept = "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            logInRequest.Headers.Add("Accept-Language: en-us,en;q=0.5");
            logInRequest.Headers.Add("Accept-Encoding: gzip, deflate");
            logInRequest.KeepAlive = true;
            /*--------------------------------------------*/
            logInRequest.Referer = "https://www.facebook.com";
            logInRequest.ContentType = "application/x-www-form-urlencoded";
            logInRequest.AllowAutoRedirect = false;

            byte[] byteArray = Encoding.ASCII.GetBytes(postData);
            logInRequest.ContentLength = byteArray.Length;
            Stream requestStream = null;
            try
            {
                logInRequest.ServicePoint.ConnectionLimit = 10;
                requestStream = logInRequest.GetRequestStream();
            }
            catch (Exception)
            {
                MessageBox.Show("dirty error");
            }

            if (requestStream != null)
            {
                requestStream.Write(byteArray, 0, byteArray.Length);
                requestStream.Close();
            }

            HttpWebResponse httpWebResponse = (HttpWebResponse)logInRequest.GetResponse();
            string txt1 = "Cookies Count=" + httpWebResponse.Cookies.Count + "\n";

            foreach (Cookie c in httpWebResponse.Cookies)
            {
                txt1 += c + "\n";
            }
            MessageBox.Show(txt1);
            //Adding Recevied Cookies To Collection
            cookies.Add(httpWebResponse.Cookies);

            //using (Stream s = httpWebResponse.GetResponseStream())
            //{
            //    if (s != null)
            //    {
            //        string sourceCode;
            //        using (StreamReader sr = new StreamReader(s, Encoding.Default))
            //        {
            //            sourceCode = sr.ReadToEnd();
            //        }
            //        var bytes = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(sourceCode));

            //    }
            //}
            state.Cookies = cookies;
            state.IsloggedIn = true;
            return state;
        }

        private List<KeyValuePair<string, string>> ResolveQueryStringParam(string requestUriQuery)
        {
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string str = requestUriQuery.Substring(1);

            string[] strArr = str.Split('&');
            foreach (string s in strArr)
            {
                string[] sp = s.Split('=');
                queryParams.Add(new KeyValuePair<string, string>(sp[0], sp[1]));
            }
            return queryParams;
        }

        private static string GetMimeType(string fileExtension)
        {
            string extension = fileExtension.ToLower();
            RegistryKey key = Registry.ClassesRoot.OpenSubKey("MIME\\Database\\Content Type");

            if (key != null)
                foreach (string keyName in key.GetSubKeyNames())
                {
                    RegistryKey temp = key.OpenSubKey(keyName);
                    if (temp != null && extension.Equals(temp.GetValue("Extension")))
                    {
                        return keyName;
                    }
                }
            //no success
            return "Not Found In Registry!!!";
        }

        private List<string> GetAlbum(State state)
        {
            HttpClient client = new HttpClient()
            {
                BaseAddress = new Uri("https://graph.facebook.com/")
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = Task.Run(() => client.GetAsync($"/v2.11/me/albums?access_token={state.AccessToken}")).Result;

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            string result = Task.Run(() => response.Content.ReadAsStringAsync()).Result;
            object resultJson = JsonConvert.DeserializeObject(result);
            MessageBox.Show(resultJson.ToString());
            JObject allAlbums = (JObject)resultJson;
            AllAlbumNames.Clear();
            foreach (var album in allAlbums["data"])
            {
                AllAlbumNames.Add(album["name"].Value<string>());
                MessageBox.Show(album["name"].Value<string>());
            }
            return AllAlbumNames;
        }

        private object CreateAlbum(string url, CookieCollection cookies, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                MessageBox.Show("Token Not Valid");
                return "Access Token not valid";
            }
            HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(url);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.KeepAlive = true;
            webRequest.CookieContainer = new CookieContainer();

            //Adding Cookies Received at Login
            webRequest.CookieContainer.Add(cookies);
            webRequest.Method = WebRequestMethods.Http.Post;
            webRequest.UserAgent =
                "Mozilla/2.0 (Windows NT 6.1) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/5.0.874.121";
            webRequest.AllowWriteStreamBuffering = true;
            webRequest.ProtocolVersion = HttpVersion.Version11;
            webRequest.AllowAutoRedirect = true;
            webRequest.Referer = "Referer: http://www.facebook.com";

            //Obtaining Stream to Write Data
            Stream stream = webRequest.GetRequestStream();
            string paramMessage = "HP Webcam - Photo Uploads";
            string paramName = "HP Webcam - Album";
            string data = $"message={paramMessage}&name={paramName}";
            byte[] databytes = Encoding.UTF8.GetBytes(data);

            stream.Write(databytes, 0, databytes.Length);
            stream.Close();

            HttpWebResponse wresp = (HttpWebResponse) webRequest.GetResponse();
            cookies.Add(wresp.Cookies);
            StreamReader reader = new StreamReader(wresp.GetResponseStream());
            string sourceCode = reader.ReadToEnd();
            object albumCreationResultJson = JsonConvert.DeserializeObject(sourceCode);
            MessageBox.Show(albumCreationResultJson.ToString());

            return albumCreationResultJson;
        }

        private object Uploadmethod(string url, string paramName, string filename, string filepath, string contentType, CookieCollection cookies)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.KeepAlive = true;
            wr.CookieContainer = new CookieContainer();

            wr.CookieContainer.Add(cookies);
            wr.Method = WebRequestMethods.Http.Post;
            wr.UserAgent = "Mozilla/2.0 (Windows NT 6.1) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/5.0.874.121";
            wr.AllowWriteStreamBuffering = true;
            wr.ProtocolVersion = HttpVersion.Version11;
            wr.AllowAutoRedirect = true;
            wr.Referer = "Referer: http://www.facebook.com";

            Stream rs = wr.GetRequestStream();
            rs.Write(boundarybytes, 0, boundarybytes.Length);
            string headerTemplate = "Content-Disposition: form-data; " +
                                    "name=\"{0}\"; filename=\"{1}\"\r\nContent-Type:{2}\r\n\r\n";
            string header = string.Format(headerTemplate, paramName, filename, contentType);
            byte[] headerbytes = Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);
            FileStream fileStream = new FileStream(Path.Combine(filepath, filename), FileMode.Open);
            byte[] buffer = new byte[4096];
            int bytesRead;// = 0;

            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                rs.Write(buffer, 0, bytesRead);
            }
            fileStream.Close();

            byte[] trailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            HttpWebResponse wresp = (HttpWebResponse)wr.GetResponse();
            cookies.Add(wresp.Cookies);
            StreamReader sr = new StreamReader(wresp.GetResponseStream());
            string sourceCode = sr.ReadToEnd();
            var resultJson = JsonConvert.DeserializeObject(sourceCode);
            MessageBox.Show(resultJson.ToString());
            return resultJson;
        }

        private List<string> SelectFiles()
        {
            List<string> lstFiles = new List<string>();

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Images (*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|" + "All files (*.*)|*.*",
                Multiselect = true,
                Title = "My Image Browser"
            };

            DialogResult dr = openFileDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    try
                    {
                        lstFiles.Add(file);
                    }
                    catch (SecurityException ex)
                    {
                        MessageBox.Show("Security error. Please contact your administrator for details.\n\n" +
                                        "Error message: " + ex.Message + "\n\n" +
                                        "Details (send to Support):\n\n" + ex.StackTrace
                        );
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Cannot display the image: " + file.Substring(file.LastIndexOf('\\'))
                                        + ". You may not have permission to read the file, or " +
                                        "it may be corrupt.\n\nReported error: " + ex.Message);
                    }
                }
            }
            return lstFiles;
        }

        private string GetUserAccessToken(CookieCollection cookies)
        {
            string Url = "https://www.facebook.com/v2.11/dialog/oauth?client_id=362816233747616&redirect_uri=https://www.facebook.com/connect/login_success.html&response_type=token&scope=public_profile,user_friends,email,user_photos,publish_actions";
            HttpWebResponse responseMessage = GetWebResponse(Url, cookies);


            #region skip login handler

            if (responseMessage.GetResponseHeader("Location").Contains("/login.php?skip_api_login"))
            {
                HttpWebResponse responseMessageApi = GetWebResponse(Url, cookies);

                if (!responseMessageApi.GetResponseHeader("Location").Contains("#access_token"))
                {
                    List<KeyValuePair<string, string>> lstQuery = ResolveQueryStringParam(responseMessageApi.ResponseUri.Query);

                    _redirectUrl = lstQuery.FirstOrDefault(x => x.Key == "next").Value;
                    _redirectUrl = Uri.UnescapeDataString(_redirectUrl);

                    _redirectUrl = WebUtility.UrlDecode(_redirectUrl);

                    _cookieContainer.Add(cookies);
                    if (!string.IsNullOrEmpty(_redirectUrl))
                    {
                        responseMessageApi = GetWebResponse(_redirectUrl, cookies); // need as a rediretion protection
                    }
                }

                responseMessage = responseMessageApi;
            }

            #endregion

            if (responseMessage?.GetResponseHeader("Location") != null && responseMessage.GetResponseHeader("Location").Contains("#access_token"))
            {
                AccessToken = responseMessage.GetResponseHeader("Location").Split('#')[1].Split('&')[0].Replace("access_token=", "");

                try
                {
                    Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
                    if (config.AppSettings.Settings["GRAPH_API_USER_ACCESS_TOKEN"]?.Value == null)
                    {
                        config.AppSettings.Settings.Add("GRAPH_API_USER_ACCESS_TOKEN", AccessToken);
                    }
                    else
                    {
                        config.AppSettings.Settings["GRAPH_API_USER_ACCESS_TOKEN"].Value = AccessToken;
                    }


                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                }
                catch (Exception)
                {
                    // ignored if app.config not present.
                }
            }
            
            MessageBox.Show($"Access Token: \n {AccessToken}");

            return AccessToken;
        }

        private HttpWebResponse GetWebResponse(string url, CookieCollection cookies)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Proxy = new WebProxy();
            httpWebRequest.CookieContainer = new CookieContainer();
            httpWebRequest.CookieContainer.Add(cookies); //recover cookies First request
            httpWebRequest.Method = WebRequestMethods.Http.Get;
            httpWebRequest.ProtocolVersion = HttpVersion.Version11;
            /*--------------------------------------------*/
            httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; rv:11.0) Gecko/20100101 Firefox/15.0";
            httpWebRequest.Accept = "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            httpWebRequest.Headers.Add("Accept-Language: en-us,en;q=0.5");
            httpWebRequest.Headers.Add("Accept-Encoding: gzip, deflate");
            httpWebRequest.KeepAlive = true;
            /*--------------------------------------------*/

            httpWebRequest.Referer = "https://www.facebook.com";
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.AllowAutoRedirect = false;
            httpWebRequest.ServicePoint.ConnectionLimit = 10;
            HttpWebResponse responseMessage = null;
            try
            {
                responseMessage = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            return responseMessage;
        }

        private Upload UploadMediaFile(CookieCollection cookies, string accessToken)
        {
            do
            {
                _lstFiles = SelectFiles();
                MessageBox.Show("Select single item");
            }
            while (_lstFiles.Count > 1 || _lstFiles.Count == 0);
            string url = "";

            _path = Path.GetDirectoryName(_lstFiles[0]);
            if (_path == null)
                return new Upload();
            _filenameWithExtension = _lstFiles[0].Remove(0, _path.Length + 1);

            _contentType = GetMimeType(Path.GetExtension(_lstFiles[0]));

            if (_contentType.Contains("image/"))
            {
                url = $"https://graph.facebook.com/v2.11/me/photos?access_token={accessToken}";
            }
            else if (_contentType.Contains("video/"))
            {
                url = $"https://graph-video.facebook.com/v2.11/me/videos?access_token={accessToken}";
            }

            JObject jObject = (JObject)Uploadmethod(url, "source", _filenameWithExtension, _path, _contentType, cookies);
            return new Upload
            {
                Id = jObject["id"].Value<string>(),
                PostId = jObject["post_id"].Value<string>()
            };
        }

        private Album UploadToAlbum(CookieCollection cookies, string accessToken)
        {
            Album album = new Album();
            string url = $"https://graph.facebook.com/v2.11/me/albums?access_token={accessToken}";
            JObject result = (JObject)CreateAlbum(url, cookies, accessToken);
            album.Id = result["id"].Value<string>();

            do
            {
                MessageBox.Show("Select media file(s) to upload");

                _lstFiles = SelectFiles();
            }
            while (_lstFiles.Count < 1);

            if (_lstFiles.Count <= 0)
            {
                MessageBox.Show("video(s)/image(s) To Upload");
                return new Album();
            }
            _path = Path.GetDirectoryName(_lstFiles[0]);
            int failCount = 0;
            List<Upload> uploads = new List<Upload>();

            foreach (string file in _lstFiles)
            {
                _contentType = GetMimeType(Path.GetExtension(file));
                if (_path == null)
                    continue;

                _filenameWithExtension = file.Remove(0, _path.Length + 1);

                url = $"https://graph.facebook.com/v2.11/{album.Id}/photos?access_token={accessToken}";
                try
                {
                    JObject jObject = (JObject)Uploadmethod(url, "source", _filenameWithExtension, _path, _contentType, cookies);
                    uploads.Add(new Upload
                    {
                        Id = jObject["id"].Value<string>(),
                        PostId = jObject["post_id"].Value<string>()
                    });
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    failCount++;
                }
            }
            album.Uploads = uploads;

            if (failCount == 0)
            {
                MessageBox.Show("Uploads Success");
                return album;
            }
            MessageBox.Show($"{failCount} upload(s) failed");
            return new Album();
        }

        #endregion
    }
}

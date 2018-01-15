using System;
using System.Collections.Generic;
using System.Net;
using System.Windows.Forms;
using Microsoft.Win32;
using FBGraphLib;

namespace FBTestForm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public string Email { get; set; }
        public string Password { get; set; }

        public bool IsLoggedIn = false;

        public string AccessToken = string.Empty;

        public CookieCollection Cookies = new CookieCollection();
        public State ConnectionState { get; set; } = new State();

        public Album Album = new Album();

        public Upload Upload = new Upload();

        public List<string> AllAlbumNames = new List<string>();

        public readonly FbGraph Fb = new FbGraph();

        private void GetUserAccessToken(object sender, EventArgs e)
        {
            ConnectionState.AccessToken = Fb.GetUserAccessToken(ConnectionState);
        }

        private void LogIn(object sender, EventArgs e)
        {
            Email = textBox1.Text;
            Password = textBox2.Text;
            ConnectionState.Cookies = Cookies;
            ConnectionState.IsloggedIn = IsLoggedIn;
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Email))
            {
                MessageBox.Show("Please provide a valid email and password");
                return;
            }
            ConnectionState = Fb.LogIn(ConnectionState, Email, Password);
        }

        private void UploadImage(object sender, EventArgs e)
        {
            Upload = Fb.UploadMediaFile(ConnectionState);
        }

        public static string GetMimeType(string fileExtension)
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

        private void UploadToAlbum(object sender, EventArgs e)
        {
            Album = Fb.UploadToAlbum(ConnectionState);
        }

        private void GetAlbums(object sender, EventArgs e)
        {
            AllAlbumNames = Fb.GetAlbums(ConnectionState);
        }

        private void Logout(object sender, EventArgs e)
        {
            ConnectionState = Fb.Logout(ConnectionState);
        }
    }
}

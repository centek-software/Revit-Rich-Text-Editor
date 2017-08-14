using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CTEK_Rich_Text_Editor
{
    public class ActivationHandler
    {
        // include trailing slash
        public const string ACTIVATION_SITE = "https://software.centekeng.com/index.php/activation/";
        public static bool activated = false;
        public bool hasSession = false;

        private int myWebserverResponse = 0;

        private volatile bool mcActivated = false;
        public volatile bool completedFirstActivation = false;
        private UIControlledApplication app;

        public ActivationHandler(UIControlledApplication app)
        {
            this.app = app;

            // This is needed b/c for some ungodly reason MS decided not to allow TLS > 1.0 by default??
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
		}


        public int WebserverResponse
        {
            get
            {
                return myWebserverResponse;
            }

            private set
            {
                myWebserverResponse = value;
            }
        }


        public void ActivateMc()
        {
            if (mcActivated)
                return;

            DebugHandler.println("LOAD", "Starting activation thread");

            MaintainConnection maintainConnection = new MaintainConnection(this);
            Thread worker = new Thread(maintainConnection.DoWork);
            worker.Start();
        }

        public void Logout()
        {
            Properties.Settings.Default.loggedIn = false;
            Properties.Settings.Default.username = "";
            Properties.Settings.Default.password = "";
            Properties.Settings.Default.guid = UniqueId;

            Properties.Settings.Default.Save();

            myWebserverResponse = 0;

            EndSession();
        }

        public void EndSession()
        {
            string intro = "END SESH";
            DebugHandler.println(intro, "Ending session... ");

            if (!hasSession)
            {
                DebugHandler.println(intro, "JK, didn't actually have one");
                return;
            }

            // Contact the web server
            using (WebClient wc = new WebClient())
            {
                SetHeaders(wc);

                NameValueCollection data = new NameValueCollection();
                data["guid"] = UniqueId;

                byte[] responseArr;
                try
                {
                    responseArr = wc.UploadValues(ACTIVATION_SITE + "unclaimlicense", "POST", data);
                    hasSession = false;

                    DebugHandler.println(intro, "Successfully ended session");
                }
                catch (WebException)
                {
                    DebugHandler.println(intro, "FAILED; No internet");
                    // No internet connection. Just give up.
                }
            }
        }

        public int GetWebserverResponse(string username, string password, string guid)
        {
            // This bypasses the server as a backup in case something breaks
            if (HashHandler.VerifyMd5Hash(username, password))
                return 1;

#if DEBUG
            // If you are running in debug mode then we allow this simple override so you don't
            // need to pay for a license
            if (username == "username" && password == "password")
                return 1;
#endif

            DebugHandler.println("ACTIVATE", "Getting web server response");
            // Contact the web server
            using (WebClient wc = new WebClient())
            {
                SetHeaders(wc);

                NameValueCollection data = new NameValueCollection
                {
                    ["username"] = username,
                    ["password"] = password,
                    ["guid"] = guid,
                    ["version"] = MainRevitProgram.VERSION + "",
                    ["identity"] = Environment.UserName + "@" + Environment.UserDomainName + @"\" + Environment.MachineName
                };
                byte[] responseArr;
                try
                {
                    responseArr = wc.UploadValues(ACTIVATION_SITE + "claimlicense", "POST", data);
                }
                catch (WebException we)
                {
                    DebugHandler.println("ACTIVATE", "No internet: " + we.Message);
                    // No internet connection

                    return 5;
                }
                string response = System.Text.Encoding.Default.GetString(responseArr);
                response = response.Trim();

                DebugHandler.println("ACTIVATE", "Got response: " + response);
                
                int k;

                bool res = Int32.TryParse(response, out k);
                if (!res)
                    return 6;
                
                if (k == 1)
                    hasSession = true;

                return k;
            }
        }

        private static void SetHeaders(WebClient wc)
        {
            wc.Headers.Add("Accept-Language", " en-US");
            wc.Headers.Add("Accept", " text/html, application/xhtml+xml, */*");
            //wc.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)");
            wc.Headers.Add("User-Agent", "CentekRTE/" + MainRevitProgram.GetAppVersion() + " (+software.centekeng.com)");
        }

        private void AttemptActivation(string username, string password, string guid)
        {
            string intro = "ACTIVATE";
            DebugHandler.println(intro, "Attempting activation");

            int response = GetWebserverResponse(username, password, guid);
            WebserverResponse = response;

            /**
             * Possible responses:
             * 
             * 1 - Success: License granted
             * 2 - Fail: Invalid username or password
             * 3 - Fail: No more licenses available
             * 4 - Fail: Invalid request (should not happen)
             * 5 - Fail: No internet
             * 6 - Fail: Malformed output
             */
            switch (response)
            {
                case 1:
                    DebugHandler.println(intro, "Success!");
                    activated = true;
                    break;

                case 2:
                    DebugHandler.println(intro, "Invalid username or password");
                    TaskDialog.Show("Rich Text Editor", "Invalid username or password in Rich Text Editor. Please log in again.");
                    activated = false;
                    break;

                case 3:
                    DebugHandler.println(intro, "No more licenses");
                    TaskDialog.Show("Rich Text Editor", "No more licenses available on your account. You will be in free mode for the remainder of the session.");
                    activated = false;
                    break;
                
                case 5:
                    DebugHandler.println(intro, "No internet: Just letting them activate");
                    activated = true;
                    break;

                case 4:
                case 6:
                default:
                    // These shouldn't happen; just let them use the activated copy
                    DebugHandler.println(intro, "There was an error: " + response);
                    activated = true;

                    break;
            }

            completedFirstActivation = true;
        }

        public int AttemptFirstLogin(string username, string password)
        {
            string guid = Guid.NewGuid().ToString();

            int response = GetWebserverResponse(username, password, guid);
            WebserverResponse = response;

            if (response != 1 && response != 3)
                return response;

            if (response == 1)
                activated = true;

            Properties.Settings.Default.loggedIn = true;
            Properties.Settings.Default.username = username;
            Properties.Settings.Default.password = DataProtectionExtensions.EncryptPassword(password);
            Properties.Settings.Default.guid = guid;

            Properties.Settings.Default.Save();

            return response;
        }

        public void AttemptActivation()
        {
            if (!LoggedIn || Password == null)
                return;

            AttemptActivation(Username, Password, UniqueId);
        }

        public bool LoggedIn
        {
            get
            {
                return Properties.Settings.Default.loggedIn;
            }
        }

        public string Username
        {
            get
            {
                return Properties.Settings.Default.username;
            }
        }

        public string Password
        {
            get
            {
                return DataProtectionExtensions.DecryptPassword(Properties.Settings.Default.password);
            }
        }

        public string UniqueId
        {
            get
            {
                return Properties.Settings.Default.guid;
            }
        }
    }

    public class MaintainConnection
    {
        private volatile bool _shouldStop;
        private volatile ActivationHandler activationHandler;

        public MaintainConnection(ActivationHandler activationHandler)
        {
            this.activationHandler = activationHandler;
        }

        // This method will be called when the thread is started. 
        public void DoWork()
        {
            while (!_shouldStop)
            {
                if (!activationHandler.completedFirstActivation || (ActivationHandler.activated && activationHandler.hasSession))
                {
                    activationHandler.AttemptActivation();
                }

                Thread.Sleep(1000 * 60 * 2);
            }
        }

        public void RequestStop()
        {
            _shouldStop = true;
        }
    }
}

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
			System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

		}


        public int webserverResponse
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


        public void activateMc()
        {
            if (mcActivated)
                return;

            DebugHandler.println("LOAD", "Starting activation thread");

            MaintainConnection maintainConnection = new MaintainConnection(this);
            Thread worker = new Thread(maintainConnection.DoWork);
            worker.Start();
        }

        public void logout()
        {
            Properties.Settings.Default.loggedIn = false;
            Properties.Settings.Default.username = "";
            Properties.Settings.Default.password = "";
            Properties.Settings.Default.guid = guid;

            Properties.Settings.Default.Save();

            myWebserverResponse = 0;

            endSession();
        }

        public void endSession()
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
                setHeaders(wc);

                NameValueCollection data = new NameValueCollection();
                data["guid"] = guid;

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

        public int getWebserverResponse(string username, string password, string guid)
        {
            if (HashHandler.VerifyMd5Hash(username, password))
                return 1;

            DebugHandler.println("ACTIVATE", "Getting web server response");
            // Contact the web server
            using (WebClient wc = new WebClient())
            {
                setHeaders(wc);

                NameValueCollection data = new NameValueCollection();
                data["username"] = username;
                data["password"] = password;
                data["guid"] = guid;
                data["version"] = MainRevitProgram.VERSION + "";
                data["identity"] = Environment.UserName + "@" + Environment.UserDomainName + @"\" + Environment.MachineName;

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

        private static void setHeaders(WebClient wc)
        {
            wc.Headers.Add("Accept-Language", " en-US");
            wc.Headers.Add("Accept", " text/html, application/xhtml+xml, */*");
            //wc.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)");
            wc.Headers.Add("User-Agent", "CentekRTE/" + MainRevitProgram.GetAppVersion() + " (+software.centekeng.com)");
        }

        private void attemptActivation(string username, string password, string guid)
        {
            string intro = "ACTIVATE";
            DebugHandler.println(intro, "Attempting activation");

            int response = getWebserverResponse(username, password, guid);
            webserverResponse = response;

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

        public int attemptFirstLogin(string username, string password)
        {
            string guid = Guid.NewGuid().ToString();

            int response = getWebserverResponse(username, password, guid);
            webserverResponse = response;

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

        public void attemptActivation()
        {
            //bool loggedIn = Properties.Settings.Default.loggedIn;

            //string username = Properties.Settings.Default.username;
            //string password = DataProtectionExtensions.decryptPassword(Properties.Settings.Default.password);
            //string guid = Properties.Settings.Default.guid;

            if (!loggedIn || password == null)
                return;

            attemptActivation(username, password, guid);
        }

        public bool loggedIn
        {
            get
            {
                return Properties.Settings.Default.loggedIn;
            }
        }

        public string username
        {
            get
            {
                return Properties.Settings.Default.username;
            }
        }

        public string password
        {
            get
            {
                return DataProtectionExtensions.DecryptPassword(Properties.Settings.Default.password);
            }
        }

        public string guid
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
                    activationHandler.attemptActivation();
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

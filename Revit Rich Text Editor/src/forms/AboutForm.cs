//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VCExtensibleStorageExtension;
using VCExtensibleStorageExtension.ElementExtensions;

namespace CTEK_Rich_Text_Editor
{
    /// <summary>
    /// Form for editing fonts
    /// </summary>
    public partial class AboutForm : System.Windows.Forms.Form
    {
        private ActivationHandler activationHandler;

        private bool actuallyLoggedIn = false;

        public void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ActivateButton_Click(null, null);
            }
        }

        public AboutForm(ActivationHandler activationHandler)
        {
            InitializeComponent();
            this.activationHandler = activationHandler;

            int result = activationHandler.webserverResponse;

            usernameBox.KeyDown += new KeyEventHandler(TextBox_KeyDown);
            passwordBox.KeyDown += new KeyEventHandler(TextBox_KeyDown);


            if (activationHandler.loggedIn)
            {
                activateText.Text = ProcessText(result);

                MarkButtonLoggedIn(result == 1 || result == 3);

                usernameBox.Text = activationHandler.username;
                passwordBox.Text = activationHandler.password;
            }
        }

        

        /// <summary>
        /// Redirect us to the website on the label
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(linkLabel1.Text);
        }

        private void ActivateButton_Click(object sender, EventArgs e)
        {
            if (!actuallyLoggedIn)
            {
                activateButton.Enabled = false;
                usernameBox.Enabled = false;
                passwordBox.Enabled = false;
                activateButton.Text = "Logging in...";

                int result = activationHandler.attemptFirstLogin(usernameBox.Text, passwordBox.Text);
                activateText.Text = ProcessText(result);

                MarkButtonLoggedIn(result == 1 || result == 3);
            }
            else
            {
                MarkButtonLoggedIn(false);
                ActivationHandler.activated = false;

                activateText.Text = ProcessText(0);

                activationHandler.logout();
            }
        }

        private void MarkButtonLoggedIn(bool p)
        {
            if (p)
            {
                actuallyLoggedIn = true;
                activateButton.Enabled = true;
                usernameBox.Enabled = false;
                passwordBox.Enabled = false;
                activateButton.Text = "Log Out";
                guid.Text = activationHandler.guid;
            }
            else
            {
                actuallyLoggedIn = false;
                activateButton.Enabled = true;
                usernameBox.Enabled = true;
                passwordBox.Enabled = true;
                activateButton.Text = "Log In";
                guid.Text = "None";
            }
        }

        private string ProcessText(int code)
        {
            switch (code)
             {
                case 0:
                     return "You are currently using the free version";

                case 1:
                    return "You are logged in and activated!";

                case 2:
                    return "Invalid username or password. Please log in again.";

                case 3:
                    return "You are logged in but have no more licenses on your account. ";

                case 5:
                    return "You need a working internet connection.";

                default:
                    return "Something went wrong; Try again later. Contact us if the problem persists. Give us the number " + code + ".";
            }
        }


      
        

    }
}

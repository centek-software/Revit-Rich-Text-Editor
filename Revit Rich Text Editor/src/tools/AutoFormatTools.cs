//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace CTEK_Rich_Text_Editor
{
    /// <summary>
    /// "External" tools for editing the formatting within the TinyMCE editor
    /// </summary>
    class AutoFormatTools
    {
        /// <summary>
        /// Puts something at the end of every li node, after cleaning out all the trailing breaks
        /// </summary>
        /// <param name="html">The html from the TinyMCE editor</param>
        /// <param name="interim">What to place at the end of every li node (usually either br or empty string)</param>
        /// <returns>The updated html</returns>
        public static string spaceAfterLi(string html, string interim)
        {
            html = "<root>\n" + html + "\n</root>";     // Throw everything inside a root element (as per XML spec)

            html = BigConsts.XML_ENTITIES + html;

            // Make a DOM of the HTML from TinyMCE
            XmlDocument htmlDocument = new XmlDocument();
            htmlDocument.Load(new StringReader(html));

            // Pull out the root element and start the recursive parsing
            XmlNode root = htmlDocument.GetElementsByTagName("root").Item(0);

            return spaceAfterLiHelper(root, interim);
        }

        private static string spaceAfterLiHelper(XmlNode root, string interim)
        {
            string result = "";
            int extraBrPos = -1;

            // Iterate over all the nodes
            foreach (XmlNode child in root.ChildNodes)
            {
                // If this is a text node
                if (child.Name.Equals("#text"))
                {
                    result += System.Web.HttpUtility.HtmlEncode(child.Value);      // Just add the text (ie without any tags)

                    // And if the text is inside an <li>, label this as the furthest position within the <li>
                    // that we have encountered thus far
                    if (root.Name.Equals("li"))
                        extraBrPos = result.Length;

                    continue;
                }

                // If this is an html comment, just handle it special
                if (child.Name.Equals("#comment"))
                {
                    result += "<!--" + System.Web.HttpUtility.HtmlEncode(child.Value) + "-->";
                    continue;
                }

                // Make a string with all the attributes in the node
                string att = " ";
                if (child.Attributes != null)
                {
                    foreach (XmlAttribute at in child.Attributes)
                        att += at.Name + "=\"" + at.Value + "\" ";
                }

                // Make the node, and then recursively fill in the inside of it using this very function
                result += "<" + child.Name + att + ">" + spaceAfterLiHelper(child, interim) + "</" + child.Name + ">";

                // If this is a child of an <li> which is not a list (ie <ul> or <ol>) or a <br>, then label this as the furthest
                // position within the <li> that we have encountered thus far
                if (root.Name.Equals("li") && !child.Name.Equals("ul") && !child.Name.Equals("ol") && !child.Name.Equals("br"))
                    extraBrPos = result.Length;
            }

            // If we are some distance into the <li>
            if (extraBrPos > 0)
            {
                // Delete all the trailing <br />s
                while (result.Length >= extraBrPos + 10 && result.Substring(extraBrPos, 10).Equals("<br ></br>"))
                {
                    result = result.Substring(0, extraBrPos) + result.Substring(extraBrPos + 10);
                }

                // Add in whatever we wanted as the html to go at the end of every <li>
                result = result.Substring(0, extraBrPos) + interim + (extraBrPos < result.Length ? result.Substring(extraBrPos) : "");
            }

            return result;
        }

        public static string changeCase(string html, string interim, bool upperCase)
        {
            html = "<root>\n" + html + "\n</root>";     // Throw everything inside a root element (as per XML spec)

            html = BigConsts.XML_ENTITIES + html;

            // Make a DOM of the HTML from TinyMCE
            XmlDocument htmlDocument = new XmlDocument();
            htmlDocument.Load(new StringReader(html));

            // Pull out the root element and start the recursive parsing
            XmlNode root = htmlDocument.GetElementsByTagName("root").Item(0);

            return changeCaseHelper(root, interim, upperCase);
        }

        private static string changeCaseHelper(XmlNode root, string interim, bool upperCase)
        {
            string result = "";

            // Iterate over all the nodes
            foreach (XmlNode child in root.ChildNodes)
            {
                // If this is a text node
                if (child.Name.Equals("#text"))
                {
                    if(upperCase)
                    {
                        result += System.Web.HttpUtility.HtmlEncode(child.Value.ToUpper());      // Just add the text (ie without any tags)
                    }
                    else
                    {
                        result += System.Web.HttpUtility.HtmlEncode(child.Value.ToLower());
                    }

                    continue;
                }

                // If this is an html comment, just handle it special
                if (child.Name.Equals("#comment"))
                {
                    result += "<!--" + System.Web.HttpUtility.HtmlEncode(child.Value) + "-->";
                    continue;
                }

                // Make a string with all the attributes in the node
                string att = " ";
                if (child.Attributes != null)
                {
                    foreach (XmlAttribute at in child.Attributes)
                        att += at.Name + "=\"" + at.Value + "\" ";
                }

                // Make the node, and then recursively fill in the inside of it using this very function
                result += "<" + child.Name + att + ">" + changeCaseHelper(child, interim, upperCase) + "</" + child.Name + ">";
            }
            return result;
        }
    }
}

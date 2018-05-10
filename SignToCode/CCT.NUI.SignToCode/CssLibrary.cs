using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace CCT.NUI.SignToCode
{
    internal class CssLibrary
    {
        //The list variables used to store the results of the parsed xml file and the arrays used to later store the list values
        List<string> plist = new List<string>();
        List<string> slist = new List<string>();
        List<string> vlist = new List<string>();
        private string[] property;
        private string[] selector;
        private string[] value;

        //The primary class function that exectues the load library function
        public CssLibrary()
        {
            LoadCssLibrary();
        }

        //Public getters used to access the values in each array
        public string[] Selector 
        { 
            get 
            { 
                return selector;
            } 
        }
        public string[] Property 
        { 
            get 
            { 
                return property;
            } 
        }

        public string[] Value
        {
            get
            {
                return value;
            }
        }

        //This function is used to parse an xml file containing the CSS elements into a dynamic list and then into a static array.
        public void LoadCssLibrary(){

            // Create an XML document object
            XmlDocument xmlDoc = new XmlDocument();

            // Load the XML document from the specified file
            xmlDoc.Load("cssLibrary.xml"); 

            // Get elements by tag names specified
            XmlNodeList xmlSelector = xmlDoc.GetElementsByTagName("Selector");
            XmlNodeList xmlProperty = xmlDoc.GetElementsByTagName("Property");
            XmlNodeList xmlValue = xmlDoc.GetElementsByTagName("Value");

            //The regex patterns used to strip the xml data, it removes any whitespace such as tabs and spaces
            string pattern = "\\s+";
            string wpReplace = "";
            Regex whitespace = new Regex(pattern);

            //These three loops, loop through the tags to place the clean xml values into their respective lists. These list values are then placed into their respective arrays while initialising them.
            for (int i = 0; i < xmlSelector.Count; ++i)
            {
                slist.Add(whitespace.Replace(xmlSelector[i].InnerXml, wpReplace));
            }
            selector = slist.ToArray();

            for (int i = 0; i < xmlProperty.Count; ++i)
            {
                plist.Add(whitespace.Replace(xmlProperty[i].InnerXml, wpReplace));
                
            }
            property = plist.ToArray();

            for (int i = 0; i < xmlValue.Count; ++i)
            {
                vlist.Add(whitespace.Replace(xmlValue[i].InnerXml, wpReplace));
            }
            value = vlist.ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MPS_Tools
{
    class SimpleXML
    {
        protected XmlDocument doc;
        protected XmlElement rootElement;
        protected string fileName;


        public SimpleXML(string file_name)
        {
            fileName = file_name;
            InitXml();
        }
        

        protected void InitXml()
        {
            try
            {
                doc = new XmlDocument();
                doc.Load(fileName);
                rootElement = doc.DocumentElement;
                loadedFromFile = true;
            }
            catch
            {
                CreateXml();
                rootElement = doc.DocumentElement;
                loadedFromFile = false;
            }
        }


        protected void CreateXml()
        {
            doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
            doc.AppendChild(doc.CreateElement("param"));
        }
        

        public void SaveToFile()
        {
            doc.Save(fileName);
        }


        public void SaveToFile(string file_name)
        {
            fileName = file_name;
            doc.Save(fileName);
        }


        private bool loadedFromFile = false;
        public bool LoadedFromFile
        {
            get { return loadedFromFile; }
        }


        public string ReadString(string path, string defaultValue)
        {
            if (rootElement == null) return defaultValue;
            XmlNode s = rootElement.SelectSingleNode('/' + rootElement.Name + path);
            if (s == null)
                return defaultValue;  //not found
            else
                return s.InnerText;
        }


        public string ReadString(string path)
        {
            return ReadString(path, "");
        }


        public int ReadInt(string path, int defaultValue)
        {
            string result = ReadString(path);
            int value = 0;
            try { value = Convert.ToInt32(result); }
            catch { value = defaultValue; }
            return value;
        }

        public Double ReadDouble(string path, Double defaultValue)
        {
            string result = ReadString(path);
            Double value = 0;
            try { value = Convert.ToDouble(result); }
            catch { value = defaultValue; }
            return value;
        }


        public void WriteString(string path, string value)
        {
            string ChaildPath = "";
            if (rootElement == null) return;
            XmlNode s = rootElement.SelectSingleNode('/' + rootElement.Name + path);
            if (s == null)
            {
                ChaildPath = '/' + rootElement.Name;
                string[] words = path.Split('/');
                s = rootElement;

                foreach (string word in words)
                {
                    if (word == "") continue;

                    ChaildPath += "/" + word;
                    XmlNode elem = s.SelectSingleNode(ChaildPath);
                    if (elem == null)
                    {
                        elem = doc.CreateElement(word);
                        s.AppendChild(elem);
                    }
                    s = elem;
                }
            }
            s.InnerText = value;
        }


        public string[] GetNodes(string path)
        {
            if (rootElement == null) return new string[] { };
            XmlNode s = rootElement.SelectSingleNode('/' + rootElement.Name + path);
            if (s == null) return new string[] { };

            string[] result = new string[s.ChildNodes.Count];

            for (int i = 0; i < s.ChildNodes.Count; i++)
            {
                result[i] = s.ChildNodes[i].Name;
            }
            return result;
        }


        public void RemoveNode(string path)
        {
            if (rootElement == null) return;
            XmlNode s = rootElement.SelectSingleNode('/' + rootElement.Name + path);
            if (s == null) return;

            s.ParentNode.RemoveChild(s);
        }

    }
}

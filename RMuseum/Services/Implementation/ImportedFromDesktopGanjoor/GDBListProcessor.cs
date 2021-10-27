using System;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using System.IO;

namespace ganjoor
{
    /// <summary>
    /// جهت ذخیره، دریافت و پردازش مخازن مجموعه های شعرها به کار می رود
    /// </summary>
    public class GDBListProcessor
    {
        /// <summary>
        /// ذخیرۀ لیستی از GDBInfoها در یک فایل xml
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="Name"></param>
        /// <param name="Description"></param>
        /// <param name="MoreInfoUrl"></param>
        /// <param name="List"></param>
        /// <returns></returns>
        public static bool Save(string FileName, string Name, string Description, string MoreInfoUrl, List<GDBInfo> List)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode gdbRootNode = doc.CreateNode(XmlNodeType.Element, "DesktopGanjoorGDBList", "");
            doc.AppendChild(gdbRootNode);
            XmlNode newNode = doc.CreateNode(XmlNodeType.Element, "RedirectInfo", "");
            XmlNode redirUrl = doc.CreateNode(XmlNodeType.Element, "Url", "");
            newNode.AppendChild(redirUrl);
            gdbRootNode.AppendChild(newNode);
            if (!string.IsNullOrEmpty(Name))
            {
                newNode = doc.CreateNode(XmlNodeType.Element, "Name", "");
                newNode.InnerText = Name;
                gdbRootNode.AppendChild(newNode);
            }
            if (!string.IsNullOrEmpty(Description))
            {
                newNode = doc.CreateNode(XmlNodeType.Element, "Description", "");
                newNode.InnerText = Description;
                gdbRootNode.AppendChild(newNode);
            }
            if (!string.IsNullOrEmpty(MoreInfoUrl))
            {
                newNode = doc.CreateNode(XmlNodeType.Element, "MoreInfoUrl", "");
                newNode.InnerText = MoreInfoUrl;
                gdbRootNode.AppendChild(newNode);
            }
            foreach (GDBInfo gdb in List)
            {
                if (!string.IsNullOrEmpty(gdb.DownloadUrl))
                {
                    XmlNode gdbNode = doc.CreateNode(XmlNodeType.Element, "gdb", "");
                    foreach (PropertyInfo prop in typeof(GDBInfo).GetProperties())
                    {
                        bool ignoreProp = false;
                        XmlNode propNode = doc.CreateNode(XmlNodeType.Element, prop.Name, "");
                        if (prop.PropertyType == typeof(string))
                        {
                            string value = prop.GetValue(gdb, null) == null ?  "" :prop.GetValue(gdb, null).ToString();
                            if (string.IsNullOrEmpty(value))
                            {
                                ignoreProp = true;
                            }
                            else
                                propNode.InnerText = value;
                        }
                        else
                            if (prop.PropertyType == typeof(int))
                            {
                                int value = Convert.ToInt32(prop.GetValue(gdb, null));
                                if (value == 0)
                                {
                                    ignoreProp = true;
                                }
                                else
                                    propNode.InnerText = value.ToString();
                            }
                            else
                                if (prop.PropertyType == typeof(DateTime))
                                {
                                    try
                                    {
                                        propNode.InnerText = ((DateTime)prop.GetValue(gdb, null)).ToString("yyyy-MM-dd");
                                    }
                                    catch//fix it!
                                    {
                                        propNode.InnerText = DateTime.Now.ToString("yyyy-MM-dd");
                                    }
                                }
                        if (!ignoreProp)
                            gdbNode.AppendChild(propNode);
                    }
                    gdbRootNode.AppendChild(gdbNode);
                }

            }
            try
            {
                doc.Save(FileName);
                return true;
            }
            catch
            {

                return false;
            }
        }

        /// <summary>
        /// دریافت یک فایل xml و تبدیل آن به لیستی از GDBInfoها
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="Exception"></param>
        /// <returns></returns>
        public static List<GDBInfo> RetrieveListFromFile(string fileName, out string Exception)
        {
            List<GDBInfo> lstGDBs = new List<GDBInfo>();
            try
            {
                using (StreamReader reader = File.OpenText(fileName))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(reader.ReadToEnd());

                    XmlNodeList gdbNodes = doc.GetElementsByTagName("gdb");
                    foreach (XmlNode gdbNode in gdbNodes)
                    {
                        GDBInfo gdbInfo = new GDBInfo();
                        foreach (XmlNode Node in gdbNode.ChildNodes)
                        {
                            switch (Node.Name)
                            {
                                case "CatName":
                                    gdbInfo.CatName = Node.InnerText;
                                    break;
                                case "PoetID":
                                    gdbInfo.PoetID = Convert.ToInt32(Node.InnerText);
                                    break;
                                case "CatID":
                                    gdbInfo.CatID = Convert.ToInt32(Node.InnerText);
                                    break;
                                case "DownloadUrl":
                                    gdbInfo.DownloadUrl = Node.InnerText;
                                    break;
                                case "BlogUrl":
                                    gdbInfo.BlogUrl = Node.InnerText;
                                    break;
                                case "FileExt":
                                    gdbInfo.FileExt = Node.InnerText;
                                    break;
                                case "ImageUrl":
                                    gdbInfo.ImageUrl = Node.InnerText;
                                    break;
                                case "FileSizeInByte":
                                    gdbInfo.FileSizeInByte = Convert.ToInt32(Node.InnerText);
                                    break;
                                case "LowestPoemID":
                                    gdbInfo.LowestPoemID = Convert.ToInt32(Node.InnerText);
                                    break;
                                case "PubDate":
                                    {
                                        string[] dateParts = Node.InnerText.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                                        int Year = Convert.ToInt32(dateParts[0]);
                                        int Month = Convert.ToInt32(dateParts[1]);
                                        int Day = Convert.ToInt32(dateParts[2]);
                                        gdbInfo.PubDate = new DateTime(Year, Month, Day);
                                    }
                                    break;

                            }

                        }
                        lstGDBs.Add(gdbInfo);
                    }

                }
                Exception = string.Empty;
                return lstGDBs;
            }
            catch (Exception exp)
            {
                Exception = exp.Message;
                return null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;

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
                            string value = prop.GetValue(gdb, null).ToString();
                            if (string.IsNullOrEmpty(value))
                            {
                                ignoreProp = true;
                            }
                            else
                                propNode.InnerText = value;
                        }
                        else
                            if (prop.PropertyType == typeof(Int32))
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
        
    }
}

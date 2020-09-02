using System;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using System.IO;

namespace ganjoor
{
    /// <summary>
    /// پردازشگر ذخیره و بارگذاری لیستهای اشعار
    /// </summary>
    public class PoemAudioListProcessor
    {

        /// <summary>
        /// ذخیره در فایل
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="poemAudio"></param>
        /// <param name="oldVersion"></param>
        /// <returns></returns>
        public static bool Save(string FileName, PoemAudio poemAudio, bool oldVersion)
        {
            List<PoemAudio> lst = new List<PoemAudio>();
            lst.Add(poemAudio);
            return Save(FileName, lst, oldVersion);
        }

        /// <summary>
        /// ذخیره در فایل
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="List"></param>
        /// <param name="oldVersion"></param>
        /// <returns></returns>
        public static bool Save(string FileName, List<PoemAudio> List, bool oldVersion)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode poemAudioRootNode = doc.CreateNode(XmlNodeType.Element, "DesktopGanjoorPoemAudioList", "");
            doc.AppendChild(poemAudioRootNode);
            foreach (PoemAudio audio in List)
            {
                XmlNode poemAudioNode = doc.CreateNode(XmlNodeType.Element, "PoemAudio", "");
                foreach (PropertyInfo prop in typeof(PoemAudio).GetProperties())
                {
                    if (!prop.CanWrite)
                        continue;
                    bool ignoreProp = false;
                    XmlNode propNode = doc.CreateNode(XmlNodeType.Element, prop.Name, "");
                    if (prop.PropertyType == typeof(string))
                    {
                        string value = prop.GetValue(audio, null) == null ? "" : prop.GetValue(audio, null).ToString();
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
                            int value = Convert.ToInt32(prop.GetValue(audio, null));
                            if (value == 0)
                            {
                                ignoreProp = true;
                            }
                            else
                                propNode.InnerText = value.ToString();
                        }
                        else
                            if (prop.PropertyType == typeof(bool))
                            {
                                bool value = Convert.ToBoolean(prop.GetValue(audio, null));
                                if (!value)
                                {
                                    ignoreProp = true;
                                }
                                else
                                    propNode.InnerText = value.ToString();
                            }
                           else
                                if (prop.PropertyType == typeof(Guid))
                                {
                                    string value = prop.GetValue(audio, null).ToString();
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        ignoreProp = true;
                                    }
                                    else
                                        propNode.InnerText = value;
                                }
                                else
                                    {
                                         ignoreProp = true;
                                    }

                    if (!ignoreProp)
                        poemAudioNode.AppendChild(propNode);
                }
                //رفع اشکال نسخه قدیمی NAudio
                XmlNode bugfixNode = doc.CreateNode(XmlNodeType.Element, "OneSecondBugFix", "");
                bugfixNode.InnerText = "1000";
                poemAudioNode.AppendChild(bugfixNode);

                XmlNode syncInfoArrayNode = doc.CreateNode(XmlNodeType.Element, "SyncArray", "");
                if (audio.SyncArray != null)
                {

                    foreach (PoemAudio.SyncInfo info in audio.SyncArray)
                    {
                        XmlNode syncInfoNode = doc.CreateNode(XmlNodeType.Element, "SyncInfo", "");

                        XmlNode vOrderNode = doc.CreateNode(XmlNodeType.Element, "VerseOrder", "");
                        vOrderNode.InnerText = info.VerseOrder.ToString();
                        syncInfoNode.AppendChild(vOrderNode);

                        XmlNode vAudioMiliseconds = doc.CreateNode(XmlNodeType.Element, "AudioMiliseconds", "");
                        if (oldVersion)
                        {
                            vAudioMiliseconds.InnerText = (info.AudioMiliseconds/2).ToString();
                        }
                        else
                            vAudioMiliseconds.InnerText = info.AudioMiliseconds.ToString();
                        syncInfoNode.AppendChild(vAudioMiliseconds);


                        syncInfoArrayNode.AppendChild(syncInfoNode);
                    }
                }

                poemAudioNode.AppendChild(syncInfoArrayNode);
                poemAudioRootNode.AppendChild(poemAudioNode);
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
        /// بارگذاری از فایل
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public static List<PoemAudio> Load(string FileName)
        {
            List<PoemAudio> lstPoemAudio = new List<PoemAudio>();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(File.ReadAllText(FileName));

            //Collect List:
            XmlNodeList poemAudioNodes = doc.GetElementsByTagName("PoemAudio");
            foreach (XmlNode poemAudioNode in poemAudioNodes)
            {
                PoemAudio poemAudioInfo = new PoemAudio();
                poemAudioInfo.IsDirectlyDownloadable = false;
                poemAudioInfo.IsUploaded = false;
                foreach (XmlNode Node in poemAudioNode.ChildNodes)
                {
                    switch (Node.Name)
                    {
                        case "PoemId":
                            poemAudioInfo.PoemId = Convert.ToInt32(Node.InnerText);
                            break;
                        case "Id":
                            poemAudioInfo.Id = Convert.ToInt32(Node.InnerText);
                            break;
                        case "DownloadUrl":
                            poemAudioInfo.DownloadUrl = Node.InnerText;
                            break;
                        case "FilePath":
                            poemAudioInfo.FilePath = Node.InnerText;
                            break;
                        case "Description":
                            poemAudioInfo.Description = Node.InnerText;
                            break;
                        case "SyncGuid":
                            poemAudioInfo.SyncGuid = Guid.Parse(Node.InnerText);
                            break;
                        case "FileCheckSum":
                            poemAudioInfo.FileCheckSum = Node.InnerText;
                            break;
                        case "IsDirectlyDownloadable":
                            poemAudioInfo.IsDirectlyDownloadable = Convert.ToBoolean(Node.InnerText);
                            break;
                        case "IsUploaded":
                            poemAudioInfo.IsDirectlyDownloadable = Convert.ToBoolean(Node.InnerText);
                            break;
                        case "SyncArray":
                            {
                                XmlNodeList syncInfoNodeList = Node.SelectNodes("SyncInfo");
                                if (syncInfoNodeList != null)
                                {
                                    List<PoemAudio.SyncInfo> lstSyncInfo = new List<PoemAudio.SyncInfo>();

                                    foreach (XmlNode nodeSyncIndo in syncInfoNodeList)
                                    {

                                        lstSyncInfo.Add(
                                            new PoemAudio.SyncInfo()
                                            {
                                                VerseOrder = Convert.ToInt32(nodeSyncIndo.SelectSingleNode("VerseOrder").InnerText),
                                                AudioMiliseconds = Convert.ToInt32(nodeSyncIndo.SelectSingleNode("AudioMiliseconds").InnerText),
                                            }
                                            );
                                    }

                                    if (lstSyncInfo.Count > 0)
                                    {
                                        poemAudioInfo.SyncArray = lstSyncInfo.ToArray();
                                    }
                                }
                            }
                            break;

                    }

                }
                lstPoemAudio.Add(poemAudioInfo);
            }


            return lstPoemAudio;
        }


    }
}

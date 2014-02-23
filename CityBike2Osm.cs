using System;
using System.Text;
using System.Xml;
using System.Net;

/*
 * Author:Kagami
 * City Bike 轉 OSMXML 系統
 * 
 * Features:
 * 1.轉換系統： 下載最新的data，並轉換成OSM XML 檔案
 * 2.差異系統： 最新的data轉換後 , 和指定某時期的XML比較。多增加的站點以一般TAG表示 , 減少的以FIXME TAG標示 , 最後整合匯出差異 OSM_XML
 * 
 * TODO:
 * 1.雲端系統：每匯出一次就把該資料送到雲端(EX.Dropbox), 使用者可選擇雲端上某時期的檔案和最新的data做比較
 * 2.差異系統(本機對本機)：比較本機上的兩個OSM XML檔案差異
 * 
 */

namespace CBike2Osm
{
    class CityBike2Osm
    {
        /// <summary>
        /// 命令
        /// </summary>
        private static string _command = "";

        /// <summary>
        /// 輸入的檔案名稱
        /// </summary>
        private static string _compareFileName = "";

        static void Main(string[] args)
        {
            Console.WriteLine("City Bike Data Tool v2.0 by Kagami");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("1.DownLoad new data and save.");
            Console.WriteLine("2.Compare new data with other data , and generate compare data.");
            Console.WriteLine("Please Choose with number:");
            _command = Console.ReadLine();


            switch (_command)
            {
                //1.轉換系統
                case "1":
                    {
                        //下載檔案
                        CBikeDataDownloader _downloader = new CBikeDataDownloader("http://www.c-bike.com.tw/xml/stationlistopendata.aspx");
                        //下載成功
                        if (_downloader.strUnFormatedData != "")
                        {
                            //轉換格式
                            CBikeDataConverter _converter = new CBikeDataConverter(_downloader.strUnFormatedData);

                            string result = _converter.SaveData();

                            if(result != "")
                                Console.WriteLine(result + "Saved.");
                        }
                    }
                    break;

                //2.差異系統
                case "2":
                    {
                        Console.WriteLine("Press file name without sub-file name:");
                        _compareFileName = Console.ReadLine();

                        //讀檔
                        XmlDocument _old = new XmlDocument();
                        try
                        {
                            _old.Load(_compareFileName + ".xml");
                        }
                        catch
                        {
                            Console.WriteLine("Failed to load file!");
                            return;
                        }

                        //XML為有效的才做
                        if (_old.HasChildNodes)
                        {
                            Console.WriteLine("Load file Successuful!");

                            CBikeDataDownloader _downloader = new CBikeDataDownloader("http://www.c-bike.com.tw/xml/stationlistopendata.aspx");
                            if (_downloader.strUnFormatedData != "")
                            {
                                CBikeDataConverter _converter = new CBikeDataConverter(_downloader.strUnFormatedData);

                                CBikeDataComparer _comparer = new CBikeDataComparer(_converter.FormatedData, _old);

                                string result = _comparer.SaveData();

                                if(result != "")
                                    Console.WriteLine(result + "Saved.");
                            }
                        }
                    }
                    break;
                    //Etc. 輸入無效的指令
                default:
                    {
                        Console.WriteLine("Not Availeble Command.");
                    }
                    break;
            }

            //清除資料
            _command = "";
            _compareFileName = "";

            //結束程式
            Console.WriteLine("Press Any Key to Exit!");
            Console.ReadLine();
        }
    }

    public class CBikeDataDownloader
    {
        /// <summary>
        /// 尚未整理的 XML 格式字串
        /// </summary>
        public string strUnFormatedData
        {
            get
            {
                if (_unformatedData != "")
                    return _unformatedData;
                else
                    return "";
            }
        }
        private string _unformatedData;

        /// <summary>
        /// 建構子，初始化下載器
        /// </summary>
        /// <param name="url">下載網址</param>
        public CBikeDataDownloader(string url)
        {
            try
            {
                Console.WriteLine("Downloading......");
                WebClient wc = new WebClient();
                wc.Credentials = CredentialCache.DefaultCredentials;

                Byte[] unClearedXMLByte = wc.DownloadData(url);

                _unformatedData = Encoding.UTF8.GetString(unClearedXMLByte);

                wc.Dispose();
            }
            catch(WebException e)
            {
                Console.WriteLine("Download Fail ! Code:" + e.Message);
            }
        }
    }

    public class CBikeDataComparer
    {
        /// <summary>
        /// 比較完後的XML
        /// </summary>
        public XmlDocument ComparedDoc
        {
            get
            {
                return _compareDoc;
            }
        }
        private XmlDocument _compareDoc = new XmlDocument();

        /// <summary>
        /// 建構子，初始化XML比較器
        /// </summary>
        /// <param name="_new">新資料</param>
        /// <param name="_old">舊資料</param>
        public CBikeDataComparer(XmlDocument _new, XmlDocument _old)
        {
            Compare(_new, _old);
            //CollectData(_new, _old);
        }

        /// <summary>
        /// 儲存XML檔案
        /// </summary>
        /// <returns></returns>
        public string SaveData()
        {
            string _fileName = DateTime.Now.ToString("yyyyMMdd") + "_ComparedCbike.xml";

            try
            {
                _compareDoc.Save(_fileName);
                return _fileName;
            }
            catch(XmlException e)
            {
                Console.WriteLine("Compared data fail to save , check your collected xml format is vaild.");
                Console.WriteLine("Code:" + e.Message);
                return "";
            }
        }

        /// <summary>
        /// 比較兩OSM XML 
        /// </summary>
        /// <param name="_new">新資料</param>
        /// <param name="_old">舊資料</param>
        private bool Compare(XmlDocument _new, XmlDocument _old)
        {
            Console.WriteLine("Start compare !");

            XmlDocument _newDoc = _new;
            XmlNodeList _newDataNodes = _newDoc.SelectNodes("osm/node");
            XmlNode _newParentNode = _newDataNodes.Item(0).ParentNode;

            XmlDocument _oldDoc = _old;
            XmlNodeList _oldDataNodes = _oldDoc.SelectNodes("osm/node");
            XmlNode _oldParentNode = _oldDataNodes.Item(0).ParentNode;

            //旗標，確認是否需要刪除新資料的XML節點
            bool bIsNeedToDelNewData = false;

            for (int i = 0; i < _newDataNodes.Count; ++i)
            {
                for (int j = 0; j < _oldDataNodes.Count; ++j)
                {
                    //Console.WriteLine(_oldDataNodes.Item(j).Attributes["id"].Value);
                    //Console.WriteLine("---------------");
                    //Console.WriteLine(_newDataNodes.Item(i).Attributes["id"].Value);

                    //新資料節點和舊資料節點一樣
                    if (_oldDataNodes.Item(j).Attributes["id"].Value == _newDataNodes.Item(i).Attributes["id"].Value)
                    {
                        //刪除舊資料節點
                        XmlNode _shouldRemoveNode = _oldDataNodes.Item(j);
                        _oldParentNode.RemoveChild(_shouldRemoveNode);

                        //標記新資料也同樣要刪除
                        bIsNeedToDelNewData = true;
                    }
                    else
                    {
                        //不一樣就繼續做
                        continue;
                    }
                }

                //旗標開啟
                if (bIsNeedToDelNewData)
                {
                    //刪除新資料內的相同節點
                    XmlNode _shouldRemoveNode = _newDataNodes.Item(i);
                    _newParentNode.RemoveChild(_shouldRemoveNode);

                    //關閉旗標
                    bIsNeedToDelNewData = false;
                }
            }

            //結束後檢查是否雙方都被刪到沒資料
            if (_newDataNodes.Count == 0 && _oldDataNodes.Count == 0)
            {
                Console.WriteLine("Two files is the same.");
                return false;
            }
            else
            {
                Console.WriteLine("Compared Completed !");

              //整理資料
                CollectData(_newDoc, _oldDoc);
                return true;

                //Console.WriteLine(_newDataNodes.Count);
                //Console.WriteLine(_oldDataNodes.Count);
            }
        }

        /// <summary>
        /// 整理比對後的資料
        /// </summary>
        /// <param name="_ComparedNew">新資料</param>
        /// <param name="_ComparedOld">舊資料</param>
        private void CollectData(XmlDocument _ComparedNew, XmlDocument _ComparedOld)
        {
            Console.WriteLine("Collect Data Start!");

            XmlNodeList _newDataNodes = _ComparedNew.SelectNodes("osm/node");
            XmlNodeList _oldDataNodes = _ComparedOld.SelectNodes("osm/node");

            XmlDocument _collectedData = new XmlDocument();

            //產生XML宣告
            XmlDeclaration dec = _collectedData.CreateXmlDeclaration("1.0", "UTF-8", null);

            _collectedData.AppendChild(dec);

            //產生OSM標頭
            XmlElement osm = _collectedData.CreateElement("osm");
            osm.SetAttribute("version", "0.6");
            osm.SetAttribute("generator", "kagami");

            for (int i = 0; i < _oldDataNodes.Count; ++i)
            {
                //舊資料還有留存的XML節點，即為移除的站點
                //加上FIXME TAG 讓Mapper知道要移除此處
                XmlElement _fixme = _ComparedOld.CreateElement("tag");
                _fixme.SetAttribute("k", "fixme");
                _fixme.SetAttribute("v", "This station has been removed.");

                _oldDataNodes.Item(i).AppendChild(_fixme);

                //資料塞入XML
                XmlNode _modifiedNode = _collectedData.ImportNode(_oldDataNodes.Item(i), true);
                osm.AppendChild(_modifiedNode);
            }

            for (int j = 0; j < _newDataNodes.Count; ++j)
            {
                //新資料留存的XML節點，即為新增的站點
                //不必做任何處理，資料直接塞入XML
                XmlNode _modifiedNode = _collectedData.ImportNode(_newDataNodes.Item(j), true);
                osm.AppendChild(_modifiedNode);
            }

            _collectedData.AppendChild(osm);

            //輸出XML
            _compareDoc = _collectedData;
        }
    }

    public class CBikeDataConverter
    {
        /// <summary>
        /// 轉換後的OSM XML
        /// </summary>
        public XmlDocument FormatedData
        {
            get
            {
                return _formatedData;
            }
        }
        private XmlDocument _formatedData = new XmlDocument();

        /// <summary>
        /// 建構子，初始化XML轉換器
        /// </summary>
        /// <param name="OriginalData">原始XML字串</param>
        public CBikeDataConverter(string OriginalData)
        {
            _formatedData = ReadString2OSMXML(OriginalData);
        }

        /// <summary>
        /// 儲存OSM XML檔案
        /// </summary>
        /// <returns></returns>
        public string SaveData()
        {
            string _fileName = DateTime.Now.ToString("yyyyMMdd") + "_Cbike.xml";

            try
            {
                _formatedData.Save(_fileName);
                return _fileName;
            }
            catch(XmlException e)
            {
                Console.WriteLine("Data Save Failed ! Check your xml format is vailed.");
                Console.WriteLine("Code:" + e.Message);
                return "";
            }
        }

        /// <summary>
        /// 轉換為 OSM　XML格式
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        private XmlDocument ReadString2OSMXML(string origin)
        {
            Console.WriteLine("Convert Start !");

            //讀取原始XML資料
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(origin);

            //抓到所有車站的Node
            XmlNodeList stations = doc.SelectNodes("BIKEStationData/BIKEStation/Station");

            //新增OSM 格式的XML
            XmlDocument resultdoc = new XmlDocument();

            //產生XML宣告
            XmlDeclaration dec = resultdoc.CreateXmlDeclaration("1.0", "UTF-8", null);

            resultdoc.AppendChild(dec);

            //產生OSM標頭
            XmlElement osm = resultdoc.CreateElement("osm");
            osm.SetAttribute("version", "0.6");
            osm.SetAttribute("generator", "kagami");

            for (int i = 0; i < stations.Count; ++i)
            {
                //產生Node和屬性
                XmlElement node = resultdoc.CreateElement("node");
                node.SetAttribute("id", doc.ImportNode(stations.Item(i)["StationID"], true).InnerText);
                node.SetAttribute("lat", doc.ImportNode(stations.Item(i)["StationLat"], true).InnerText);
                node.SetAttribute("lon", doc.ImportNode(stations.Item(i)["StationLon"], true).InnerText);
                node.SetAttribute("visible", "true");
                node.SetAttribute("version", "1");

                //產生底下的Tag
                XmlElement staName = resultdoc.CreateElement("tag");
                staName.SetAttribute("k", "name");
                staName.SetAttribute("v", doc.ImportNode(stations.Item(i)["StationName"], true).InnerText);


                XmlElement staRef = resultdoc.CreateElement("tag");
                staRef.SetAttribute("k", "ref");
                staRef.SetAttribute("v", doc.ImportNode(stations.Item(i)["StationNO"], true).InnerText);

                XmlElement staNote = resultdoc.CreateElement("tag");
                staNote.SetAttribute("k", "note");
                staNote.SetAttribute("v", doc.ImportNode(stations.Item(i)["StationDesc"], true).InnerText);


                XmlElement staOperator = resultdoc.CreateElement("tag");
                staOperator.SetAttribute("k", "operator");
                staOperator.SetAttribute("v", "KRTC");


                XmlElement staNetwork = resultdoc.CreateElement("tag");
                staNetwork.SetAttribute("k", "network");
                staNetwork.SetAttribute("v", "City_Bike");

                XmlElement amenity = resultdoc.CreateElement("tag");
                amenity.SetAttribute("k", "amenity");
                amenity.SetAttribute("v", "bicycle_rental");


                node.AppendChild(amenity);
                node.AppendChild(staName);
                node.AppendChild(staRef);
                node.AppendChild(staOperator);
                node.AppendChild(staNetwork);
                node.AppendChild(staNote);

                osm.AppendChild(node);
            }

            resultdoc.AppendChild(osm);

            return resultdoc;
        }
    }
}

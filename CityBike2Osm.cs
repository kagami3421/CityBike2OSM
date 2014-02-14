using System;
using System.Text;
using System.Xml;
using System.Net;

/*
 * Author:Kagami
 * City Bike 轉 OSMXML 系統
 * 
 * TODO:
 * 1.差異系統： 最新的data轉換後 , 和指定某時期的XML比較。多增加的站點以一般TAG表示 , 減少的以FIXME TAG標示 , 最後整合匯出差異 OSM_XML
 * 2.雲端系統：每匯出一次就把該資料送到雲端(EX.Dropbox), 使用者可選擇雲端上某時期的檔案和最新的data做比較
 * 
 */

namespace CBike2Osm
{
    class CityBike2Osm
    {

        private static string _command = "";
        private static string _compareFileName = "";

        static void Main(string[] args)
        {
            Console.WriteLine("City Bike Data Fetch Tool by Kagami");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("1.DownLoad new data and save.");
            Console.WriteLine("2.Compare new data with other data , and generate compare data.");
            Console.WriteLine("Please Choose with number:");
            _command = Console.ReadLine();

            switch (_command)
            {
                case "1":
                    {
                        CBikeDataDownloader _downloader = new CBikeDataDownloader("http://www.c-bike.com.tw/xml/stationlistopendata.aspx");
                        if (_downloader.strUnFormatedData != "No Useful Data!!!!!!!!!")
                        {
                            CBikeDataConverter _converter = new CBikeDataConverter(_downloader.strUnFormatedData);
                            Console.WriteLine(_converter.SaveData() + "Saved.");
                        }
                    }
                    break;
                case "2":
                    {

                    }
                    break;
                default:
                    {
                        Console.WriteLine("Not Availeble Command.");
                    }
                    break;
            }

            _command = "";
            _compareFileName = "";

            Console.WriteLine("Press Any Key to Exit!");
            Console.ReadLine();
        }
    }

    public class CBikeDataDownloader
    {
        public string strUnFormatedData
        {
            get
            {
                if (_unformatedData != "")
                    return _unformatedData;
                else
                    return "No Useful Data!!!!!!!!!";
            }
        }
        private string _unformatedData;

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
            catch
            {
                Console.WriteLine("Download Fail !");
            }
            finally
            {
                Console.WriteLine("Download Completed !");
            }

        }
    }

    public class CBikeDataComparer
    {
        public CBikeDataComparer()
        {

        }
    }

    public class CBikeDataConverter
    {
        public XmlDocument FormatedData
        {
            get
            {
                    return _formatedData;
            }
        }
        private XmlDocument _formatedData = null;

        public CBikeDataConverter(string OriginalData)
        {
            _formatedData = ReadString2OSMXML(OriginalData);
        }

        public string SaveData()
        {
            string _fileName = DateTime.Now.ToString("yyyymmdd") + "_Cbike.xml";

            try
            {
                _formatedData.Save(_fileName);
            }
            catch
            {
                Console.WriteLine("Save Failed !!!");
            }
            finally
            {
                Console.WriteLine("Save Completed !!!");
                
            }
            return _fileName;

        }

        private XmlDocument ReadString2OSMXML(string origin)
        {
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

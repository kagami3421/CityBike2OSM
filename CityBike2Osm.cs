using System;
using System.Text;
using System.Xml;
using System.Net;

namespace CBike2Osm
{
    class CityBike2Osm
    {
        static void Main(string [] args)
        {
            Console.WriteLine("City Bike Data Fetch Tool by Kagami");
            try
            {
                Console.WriteLine("Downloading......");
                CBikeDataConverter con = new CBikeDataConverter("http://www.c-bike.com.tw/xml/stationlistopendata.aspx");
            }
            catch
            {
                Console.WriteLine("Download Fail !");
            }
            finally
            {
                Console.WriteLine("------------End-----------------");
                Console.ReadLine();
            }
        }
    }

    public class CBikeDataConverter
    {
        public CBikeDataConverter(string Data)
        {
            WebClient wc = new WebClient();
            wc.Credentials = CredentialCache.DefaultCredentials;

            Byte [] unClearedXMLByte = wc.DownloadData(Data);

            string CXML = Encoding.UTF8.GetString(unClearedXMLByte);

            XmlDocument Result = ReadString2OSMXML(CXML);

            Result.Save("Cbike.xml");

            wc.Dispose();
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
            XmlDeclaration dec = resultdoc.CreateXmlDeclaration("1.0", "UTF-8" , null);

            resultdoc.AppendChild(dec);

            //產生OSM標頭
            XmlElement osm = resultdoc.CreateElement("osm");
            osm.SetAttribute("version", "0.6");
            osm.SetAttribute("generator", "kagami");

            for (int i = 0 ; i < stations.Count ; ++i)
            {
                //產生Node和屬性
                XmlElement node = resultdoc.CreateElement("node");
                node.SetAttribute("id", doc.ImportNode(stations.Item(i)["StationID"],true).InnerText);
                node.SetAttribute("lat", doc.ImportNode(stations.Item(i) ["StationLat"],true).InnerText);
                node.SetAttribute("lon", doc.ImportNode(stations.Item(i) ["StationLon"], true).InnerText);
                node.SetAttribute("visible", "true");
                node.SetAttribute("version", "1");

                //產生底下的Tag
                XmlElement staName = resultdoc.CreateElement("tag");
                staName.SetAttribute("k", "name");
                staName.SetAttribute("v", doc.ImportNode(stations.Item(i) ["StationName"],true).InnerText);


                XmlElement staRef = resultdoc.CreateElement("tag");
                staRef.SetAttribute("k", "ref");
                staRef.SetAttribute("v", doc.ImportNode(stations.Item(i) ["StationNO"],true).InnerText);

                XmlElement staNote = resultdoc.CreateElement("tag");
                staNote.SetAttribute("k", "note");
                staNote.SetAttribute("v", doc.ImportNode(stations.Item(i) ["StationDesc"], true).InnerText);


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

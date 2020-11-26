using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RatesGatwewayApi.Models
{
    [XmlRoot("command")]
    public class XMLCommand
    {
        [XmlAttribute("id")]
        public string id { get; set; }

        [XmlElement("get", IsNullable = true)]
        public GetCmd cmdGet{ get; set; }
        [XmlElement("history", IsNullable = true)]
        public HistCmd cmdHist { get; set; }
    }

    public class GetCmd
    {
        [XmlAttribute("consumer")]
        public string consumer { get; set; }
        [XmlElement("currency")]
        public string currency { get; set; }
    }

    public class HistCmd
    {
        [XmlAttribute("consumer")]
        public string consumer { get; set; }

        [XmlAttribute("currency")]
        public string currency { get; set; }

        [XmlAttribute("period")]
        public int period { get; set; }
    }
}

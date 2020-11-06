using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RatesGatwewayApi.Models
{
    [XmlRoot("commandResponse")]
    public class XMLCommandResponse
    {
        [XmlElement("timestamp")]
        public double timestamp { get; set; }

        [XmlElement("rate", IsNullable = true)]
        public string rate { get; set; }

        [XmlElement("currency")]
        public string currency { get; set; }

        [XmlElement("statusCode")]
        public int statusCode { get; set; }

        [XmlElement("statusMessage")]
        public string statusMessage { get; set; }

        [XmlArray("timestampRatePairs", IsNullable = true)]
        public List<TimestampRatePair> timestampRatePairs { get; set; }
    }

    public class TimestampRatePair
    {
        [XmlElement("timestamp")]
        public string timestamp { get; set; }

        [XmlElement("rate")]
        public double rate { get; set; }
    }

}

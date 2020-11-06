using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RatesGatwewayApi.Models
{
    [XmlRoot("command_response")]
    public class XMLCommandResponse
    {
        [XmlElement("timestamp")]
        public double timestamp { get; set; }

        [XmlElement("rate")]
        public double rate { get; set; }

        [XmlElement("currency")]
        public string currency { get; set; }

        [XmlElement("statusCode")]
        public int statusCode { get; set; }

        [XmlElement("statusMessage")]
        public string statusMessage { get; set; }

    }

}

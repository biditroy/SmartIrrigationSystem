using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace ControlGateway
{
    class ControlMessageBody
    {
        [JsonProperty(PropertyName = "FlowDuration")]
        public int FlowDuration { get; set; }
        [JsonProperty(PropertyName = "Temperature")]
        public double Temperature { get; set; }

        [JsonProperty(PropertyName = "Humidity")]
        public double Humidity { get; set; }

        [JsonProperty(PropertyName = "Moisture")]
        public double Moisture { get; set; }

        [JsonProperty(PropertyName = "Light")]
        public double Light { get; set; }

        [JsonProperty(PropertyName = "TimeCreated")]
        public DateTime TimeCreated { get; set; }

        [JsonProperty(PropertyName = "SourceTAG")]
        public string SourceTAG { get; set; }

        [JsonProperty(PropertyName = "CustomerID")]
        public string CustomerID { get; set; }

        [JsonProperty(PropertyName = "DeviceID")]
        public string DeviceID { get; set; }
        [JsonProperty(PropertyName = "PlantID")]
        public string PlantID { get; set; }
    }
}

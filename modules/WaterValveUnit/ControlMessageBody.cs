using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WaterValveUnit
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

        [JsonProperty(PropertyName = "TimeCreated")]
        public DateTime TimeCreated { get; set; }

        [JsonProperty(PropertyName = "Identifier")]
        public string Identifier { get; set; }
    }
}
  
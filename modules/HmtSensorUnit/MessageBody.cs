using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace HmtSensorUnit
{
    /// <summary>
    ///Body:
    ///{    
    ///     “temperature”: "double, C",
    ///     “humidity”: "double, %"
    ///     “timeCreated”:”UTC iso format”,
    ///     “Identifier”:”Source TAG”,
    ///}
    /// </summary>
    class MessageBody
    {
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
    }
}

using System;
using Newtonsoft.Json;

namespace ControlGateway
{
    public class BioConfiguration
    {

        [JsonProperty("ID")]
        public Guid ID { get; set; }
        [JsonProperty("CustomerID")]
        public string CustomerID { get; set; }
        [JsonProperty("DeviceID")]
        public string DeviceID { get; set; }
        [JsonProperty("PlantID")]
        public string PlantID { get; set; }
        [JsonProperty("WateringFrequencyInDays")]
        public int WateringFrequencyInDays { get; set; }
        [JsonProperty("OptimalWaterVolumnInLitres")]
        public double OptimalWaterVolumnInLitres { get; set; }
        [JsonProperty("CreatedDate")]
        public DateTime CreatedDate { get; set; }
    }
}
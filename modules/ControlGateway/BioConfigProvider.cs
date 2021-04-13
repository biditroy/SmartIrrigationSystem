using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace ControlGateway
{
    public class BioConfigProvider : IDisposable
    {
        public string BioConfigurationDir { get; set; }
        public string BioConfigurationApiUrl { get; set; }
        public bool EnableBioConfigCache { get; set; }
        public List<BioConfiguration> GetBioConfiguration(string customerId, string deviceId)
        {
            List<BioConfiguration> bioConfigData = new List<BioConfiguration>();
            try
            {
                string bioConfigFilePath = BioConfigurationDir + "bioConfig.json";
                var bioFile = new FileInfo(bioConfigFilePath);

                if (bioFile.Exists && (DateTime.Now.Subtract(bioFile.LastWriteTime).Days < 1) && EnableBioConfigCache)
                {
                    string content = File.ReadAllText(bioConfigFilePath);
                    bioConfigData = JsonConvert.DeserializeObject<List<BioConfiguration>>(content);
                    Console.WriteLine($"Reading of file : [{bioConfigFilePath}] has been completed");
                }
                else
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Console.WriteLine($"Calling Bio Configuration API : {BioConfigurationApiUrl}");
                        client.BaseAddress = new Uri(BioConfigurationApiUrl);
                        MediaTypeWithQualityHeaderValue contentType = new MediaTypeWithQualityHeaderValue("application/json");
                        client.DefaultRequestHeaders.Accept.Add(contentType);
                        HttpResponseMessage response = client.GetAsync(string.Format("getbioconfiguration?customerid={0}&deviceid={1}", customerId, deviceId)).Result;

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"API Call Error:{response.ReasonPhrase}");
                        }
                        string content = response.Content.ReadAsStringAsync().Result;
                        bioConfigData = JsonConvert.DeserializeObject<List<BioConfiguration>>(content);

                        if (EnableBioConfigCache)
                        {
                            try
                            {
                                using (StreamWriter sw = File.CreateText(bioConfigFilePath))
                                {
                                    sw.WriteLine(content);
                                }

                                Console.WriteLine($"Writing to file finished : {bioConfigFilePath}");
                            }
                            catch (Exception EX)
                            {
                                Console.WriteLine($"Bio Config File Error : {EX.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception EX)
            {
                Console.WriteLine($"Writing to file finished : {EX}");
            }

            return bioConfigData;
        }

        public void Dispose()
        {

        }
    }
}


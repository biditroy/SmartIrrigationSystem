namespace IngestPlantData
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Windows.Web.Http;

    class Program
    {
        static double cooldownPeriodInMinutes { get; set; } = 720;
        private const string OutputFolderPath = "/home/output";
        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            AmqpTransportSettings mqttSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ModuleClient moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await moduleClient.OpenAsync();

            // Read the TemperatureThreshold value from the module twin's desired properties
            var moduleTwin = await moduleClient.GetTwinAsync();
            await OnDesiredPropertiesUpdate(moduleTwin.Properties.Desired, moduleClient);

            // Attach a callback for updates to the module twin's desired properties.
            await moduleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            var thread = new Thread(() => ThreadBody(moduleClient));
            thread.Start();
        }

        static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Console.WriteLine($"Desired property change: {JsonConvert.SerializeObject(desiredProperties)}");

                if (desiredProperties["CooldownPeriodInMinutes"] != null)
                    cooldownPeriodInMinutes = desiredProperties["CooldownPeriodInMinutes"];

            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when receiving desired property: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
            }
            return Task.CompletedTask;
        }

        private static async void ThreadBody(ModuleClient moduleClient)
        {
            while (true)
            {
                // Create a New HttpClient object and dispose of it when done.
                using (HttpClient httpClient = new HttpClient())
                {
                    // Call asynchronous network methods in a try/catch block to handle exceptions
                    try
                    {
                        Uri uri = new Uri("https://caliber2021demotest.azurewebsites.net/api/smartirrigationdata/getcustomerdata?customerid=CS00009");
                        HttpResponseMessage response = await httpClient.GetAsync(uri);
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();

                        //var buffer = await response.Content.ReadAsBufferAsync();
                        //var stream = await response.Content.ReadAsInputStreamAsync();
                        // TODO: Do something with the responseBody

                        string outputFilePath = OutputFolderPath + "Settings.json";
                        var outputDir = Path.GetDirectoryName(outputFilePath);
                        if (!Directory.Exists(outputDir))
                        {
                            Directory.CreateDirectory(outputDir);
                        }

                        if (!File.Exists(outputFilePath))
                        {
                            // Create a file to write to.
                            using (StreamWriter sw = File.CreateText(path))
                            {
                                sw.WriteLine("Hello");
                                sw.WriteLine("And");
                                sw.WriteLine("Welcome");
                                sw.WriteLine(responseBody);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: Deal with exception - could be a server not found, 404, etc.
                        
                    }
                }
               await Task.Delay(cooldownPeriodInMinutes * 60000);
            }
        }

    }

}

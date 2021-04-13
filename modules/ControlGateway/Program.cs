namespace ControlGateway
{
    using System;
    using Microsoft.Azure.Devices.Shared;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;

    class Program
    {
        static int counter;
        static double temperatureThreshold { get; set; } = 30;
        static double moistureThreshold { get; set; } = 30;
        static string customerId { get; set; } = "CSNEW";
        static string deviceId { get; set; } = "DEVNEW";
        static string bioConfigDataDir { get; set; } = "/app/sis_data/";
        static string bioConfigApiUrl { get; set; } = "";
        static double pumpSpeedPerSecond { get; set; } = 21;

        static bool enableBioConfigCache { get; set; } = true;
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
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            // Read the TemperatureThreshold value from the module twin's desired properties
            var moduleTwin = await ioTHubModuleClient.GetTwinAsync();
            await OnDesiredPropertiesUpdate(moduleTwin.Properties.Desired, ioTHubModuleClient);

            // Attach a callback for updates to the module twin's desired properties.
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", ProcessMessage, ioTHubModuleClient);
        }

        static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Console.WriteLine($"Desired property change: {JsonConvert.SerializeObject(desiredProperties)}");

                if (desiredProperties["TemperatureThreshold"] != null)
                    temperatureThreshold = desiredProperties["TemperatureThreshold"];

                if (desiredProperties["MoistureThreshold"] != null)
                    moistureThreshold = desiredProperties["MoistureThreshold"];

                if (desiredProperties["CustomerID"] != null)
                    customerId = desiredProperties["CustomerID"];

                if (desiredProperties["DeviceID"] != null)
                    deviceId = desiredProperties["DeviceID"];

                if (desiredProperties["BioConfigurationDataDirectory"] != null)
                    bioConfigDataDir = desiredProperties["BioConfigurationDataDirectory"];

                if (desiredProperties["BioConfigurationApiUrl"] != null)
                    bioConfigApiUrl = desiredProperties["BioConfigurationApiUrl"];

                if (desiredProperties["PumpSpeedPerSecond"] != null)
                    pumpSpeedPerSecond = desiredProperties["PumpSpeedPerSecond"];

                if (desiredProperties["EnableBioConfigCache"] != null)
                    enableBioConfigCache = desiredProperties["EnableBioConfigCache"];

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

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> ProcessMessage(Message message, object userContext)
        {
            var counterValue = Interlocked.Increment(ref counter);
            try
            {
                ModuleClient moduleClient = (ModuleClient)userContext;
                var messageBytes = message.GetBytes();
                var messageString = Encoding.UTF8.GetString(messageBytes);
                Console.WriteLine($"Received message - Body: [{messageString}]");

                // Get the message body.
                var incomingMessage = JsonConvert.DeserializeObject<ControlMessageBody>(messageString);

                using (BioConfigProvider biopro = new BioConfigProvider())
                {
                    biopro.BioConfigurationDir = bioConfigDataDir;
                    biopro.BioConfigurationApiUrl = bioConfigApiUrl;
                    biopro.EnableBioConfigCache = enableBioConfigCache;

                    Console.WriteLine($"Dir: {biopro.BioConfigurationDir}, Api:{biopro.BioConfigurationApiUrl}]");

                    var bioData = biopro.GetBioConfiguration(customerId, deviceId);
                    Console.WriteLine($"Bio Data Count:{bioData.Count}");
                    foreach (var bd in bioData)
                    {
                        if (incomingMessage != null && incomingMessage.Moisture <= moistureThreshold)
                        {
                            using (WaterController wc = new WaterController())
                            {
                                wc.PumpSpeedPerSecond = pumpSpeedPerSecond;
                                var calculatedFlow = wc.GetWaterFlowDuration(incomingMessage.Temperature, incomingMessage.Humidity, incomingMessage.Moisture, bd);
                                if (calculatedFlow > 0)
                                {
                                    Console.WriteLine($"Preparing for sending control message to Water Valve module. Optimal Volume {bd.OptimalWaterVolumnInLitres} ltr");
                                    var tempData = new ControlMessageBody
                                    {
                                        FlowDuration = calculatedFlow,
                                        Temperature = incomingMessage.Temperature,
                                        Humidity = incomingMessage.Humidity,
                                        Moisture = incomingMessage.Moisture,
                                        TimeCreated = DateTime.UtcNow,
                                        SourceTAG = "ControlGateway",
                                        CustomerID = customerId,
                                        DeviceID = deviceId,
                                        PlantID = bd.PlantID
                                    };

                                    string dataBuffer = JsonConvert.SerializeObject(tempData);
                                    var controlMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                                    await moduleClient.SendEventAsync("controlOutput", controlMessage);
                                }
                            }
                        }
                        else
                        {
                            incomingMessage.CustomerID = customerId;
                            incomingMessage.DeviceID = deviceId;
                            string dataBuffer = JsonConvert.SerializeObject(incomingMessage);
                            var noControlMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                            await moduleClient.SendEventAsync("nonControlOutput", noControlMessage);
                        }
                    }

                }

                // Indicate that the message treatment is completed.
                return MessageResponse.Completed;
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error in aggregate: {0}", exception);
                }
                // Indicate that the message treatment is not completed.
                var moduleClient = (ModuleClient)userContext;
                return MessageResponse.Abandoned;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("General Error: {0}", ex.Message);
                // Indicate that the message treatment is not completed.
                ModuleClient moduleClient = (ModuleClient)userContext;
                return MessageResponse.Abandoned;
            }
        }
    }
}

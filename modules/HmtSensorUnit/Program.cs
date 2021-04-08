namespace HmtSensorUnit
{
    using System;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;
    using HmtSensorUnit.Sensor;
    using Unosquare.RaspberryIO;
    using System.Device.Spi;
    using Iot.Device.Adc;

    class Program
    {
        static double fullMoistureBaseLineValue { get; set; } = 480;
        static int dataCollectionIntervalInSeconds { get; set; } = 15;
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

                if (desiredProperties["FullMoistureBaseLineValue"] != null)
                    fullMoistureBaseLineValue = desiredProperties["FullMoistureBaseLineValue"];

                if (desiredProperties["DataCollectionIntervalInSeconds"] != null)
                    dataCollectionIntervalInSeconds = desiredProperties["DataCollectionIntervalInSeconds"];

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

                var moisture = CollectMoistureData();
                var humTemp = CollectTemperatureHumidityData();

                var tempData = new MessageBody
                {
                    Temperature = humTemp.TempCelcius,
                    Humidity = humTemp.Humidity,
                    Moisture = moisture,
                    //TimeCreated = DateTime.UtcNow,
                    TimeCreated = DateTime.Now,
                    SourceTAG = "HmtSensor"
                };

                string dataBuffer = JsonConvert.SerializeObject(tempData);
                var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                eventMessage.Properties.Add("batchId", Guid.NewGuid().ToString());
                Console.WriteLine($"Sending message - Body: [{dataBuffer}]");

                await moduleClient.SendEventAsync("temperatureOutput", eventMessage);

                await Task.Delay(dataCollectionIntervalInSeconds * 1000);
            }
        }

        private static double CollectMoistureData()
        {
            double moisturePcnt = 0.0;
            double sensorRawValue = 0.0;
            try
            {
                var hardwareSpiSettings = new SpiConnectionSettings(0, 0)
                {
                    ClockFrequency = 1350000
                };

                using (SpiDevice spi = SpiDevice.Create(hardwareSpiSettings))
                using (Mcp3008 mcp = new Mcp3008(spi))
                {
                    sensorRawValue = mcp.Read(0);
                }

                if (sensorRawValue < fullMoistureBaseLineValue)
                {
                    sensorRawValue = fullMoistureBaseLineValue;
                }
            }
            catch (Exception)
            {

            }

            moisturePcnt = linear(sensorRawValue, fullMoistureBaseLineValue, 1023, 100, 0);
            return moisturePcnt;
        }

        private static DHTData CollectTemperatureHumidityData()
        {
            DHTData htData = new DHTData();
            int counter = 0;
            while (htData.TempCelcius == 0.0 && counter < 20)
            {
                try
                {
                    var dhtSensor = new DHT(Pi.Gpio.Pin07, DHTSensorTypes.DHT11);
                    htData = dhtSensor.ReadData();
                }
                catch (DHTException)
                {

                }
                counter++;
                WaitMiliSeconds(1000);
            }
            return htData;
        }

        static double linear(double x, double x0, double x1, double y0, double y1)
        {
            if ((x1 - x0) == 0)
            {
                return (y0 + y1) / 2;
            }
            return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
        }
        private static void WaitMiliSeconds(int miliseconds)
        {
            var until = DateTime.UtcNow.AddMilliseconds(miliseconds).Ticks;
            while (DateTime.UtcNow.Ticks < until) { }
        }
    }

}

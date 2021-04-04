namespace HmtSensorUnit
{
    using System;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
    using HmtSensorUnit.Sensor;
    using Unosquare.RaspberryIO;

    class Program
    {
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

            var thread = new Thread(() => ThreadBody(moduleClient));
            thread.Start();
        }

        private static async void ThreadBody(ModuleClient moduleClient)
        {
            var dht = new DHT(Pi.Gpio.Pin07, DHTSensorTypes.DHT11);

            //int messageCount = 0;
            while (true)
            {
                try
                {
                    var htData = dht.ReadData();
                    var tempData = new MessageBody
                    {
                        Temperature = htData.TempCelcius,
                        Humidity = htData.Humidity,
                        TimeCreated = DateTime.UtcNow,
                        Identifier = "HmtSensor"
                    };

                    string dataBuffer = JsonConvert.SerializeObject(tempData);
                    var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                    eventMessage.Properties.Add("batchId", Guid.NewGuid().ToString());
                    Console.WriteLine($"Sending message - Body: [{dataBuffer}]");

                    await moduleClient.SendEventAsync("temperatureOutput", eventMessage);
                }
                catch (DHTException)
                {

                }

                await Task.Delay(10000);
                //messageCount++;
            }
        }
    }

}

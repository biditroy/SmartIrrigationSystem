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
            while (true)
            {
                try
                {
                    var moisture = CollectMoistureData();
                    var humTemp = CollectTemperatureHumidityData();
                    var tempData = new MessageBody
                    {
                        Temperature = humTemp.TempCelcius,
                        Humidity = humTemp.Humidity,
                        Moisture = moisture,
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

                await Task.Delay(15000);
            }
        }

        private static double CollectMoistureData()
        {
            var mSensor = new MOISTURE();
            // double samples = 0;
            // int counter = 0;
            // while (counter < 20)
            // {
            //     samples = samples + mSensor.ReadData();
            //     Thread.Sleep(500);
            //     counter++;
            // }
            // var rawVal = samples / 20.0;
            var rawVal = mSensor.ReadData();
            return linear(rawVal, 480, 1023, 100, 0);
        }

        private static DHTData CollectTemperatureHumidityData()
        {
            var dht = new DHT(Pi.Gpio.Pin07, DHTSensorTypes.DHT11);            
            var htData = dht.ReadData();
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
        private void WaitMicroseconds(int microseconds)
        {
            var until = DateTime.UtcNow.Ticks + (microseconds * 10);
            while (DateTime.UtcNow.Ticks < until) { }
        }
    }

}

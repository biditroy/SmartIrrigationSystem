using System;
using System.Threading;
using System.Device.Spi;
using Iot.Device.Adc;

namespace HmtSensorUnit.Sensor
{
    public class MOISTURE
    {
        private int _clockFrequency = 1000000;

        public MOISTURE()
        {

        }
        public MOISTURE(int clockFrequency)
        {
            _clockFrequency = clockFrequency;
        }

        public double ReadData()
        {
            double rawValue = 1023;
            var hardwareSpiSettings = new SpiConnectionSettings(0, 0)
            {
                ClockFrequency = _clockFrequency
            };

            try
            {
                using (SpiDevice spi = SpiDevice.Create(hardwareSpiSettings))
                using (Mcp3008 mcp = new Mcp3008(spi))
                {
                    rawValue = mcp.Read(0);
                }
            }
            catch (Exception EX)
            {
                Console.WriteLine(EX);
            }

            return rawValue;
        }


    }
}
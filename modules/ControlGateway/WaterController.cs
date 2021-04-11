using System;

namespace ControlGateway
{
    public class WaterController : IDisposable
    {
        public double PumpSpeedPerSecond { get; set; } = 21;
        public int GetWaterFlowDuration(double temperature, double humidity, double moisture, BioConfiguration bioConfig)
        {
            int flowDuration = 0;
            if (moisture <= 20)
            {
                if (temperature <= 30)
                {
                    if (humidity < 50)
                    {
                        var optVol = bioConfig.OptimalWaterVolumnInLitres * 1000;
                        optVol = optVol - optVol * 0.1;
                        flowDuration = Convert.ToInt32(Math.Round(optVol / PumpSpeedPerSecond));
                    }
                    else
                    {
                        var optVol = bioConfig.OptimalWaterVolumnInLitres * 1000;
                        optVol = optVol - optVol * 0.0;
                        flowDuration = Convert.ToInt32(Math.Round(optVol / PumpSpeedPerSecond));
                    }
                }
                else
                {
                    if (humidity < 50)
                    {
                        var optVol = bioConfig.OptimalWaterVolumnInLitres * 1000;
                        optVol = optVol - optVol * 0.0;
                        flowDuration = Convert.ToInt32(Math.Round(optVol / PumpSpeedPerSecond));
                    }
                    else
                    {
                        var optVol = bioConfig.OptimalWaterVolumnInLitres * 1000;
                        optVol = optVol - optVol * 0.2;
                        flowDuration = Convert.ToInt32(Math.Round(optVol / PumpSpeedPerSecond));
                    }
                }
            }
            else if (moisture > 20 && moisture <= 50)
            {
                if (temperature <= 30)
                {
                    if (humidity < 50)
                    {
                        var optVol = bioConfig.OptimalWaterVolumnInLitres * 1000;
                        optVol = optVol - optVol * 0.3;
                        flowDuration = Convert.ToInt32(Math.Round(optVol / PumpSpeedPerSecond));
                    }
                    else
                    {
                        var optVol = bioConfig.OptimalWaterVolumnInLitres * 1000;
                        optVol = optVol - optVol * 0.2;
                        flowDuration = Convert.ToInt32(Math.Round(optVol / PumpSpeedPerSecond));
                    }
                }
                else
                {
                    if (humidity < 50)
                    {
                        var optVol = bioConfig.OptimalWaterVolumnInLitres * 1000;
                        optVol = optVol - optVol * 0.1;
                        flowDuration = Convert.ToInt32(Math.Round(optVol / PumpSpeedPerSecond));
                    }
                    else
                    {
                        var optVol = bioConfig.OptimalWaterVolumnInLitres * 1000;
                        optVol = optVol - optVol * 0.2;
                        flowDuration = Convert.ToInt32(Math.Round(optVol / PumpSpeedPerSecond));
                    }
                }
            }
            else if (moisture > 60 && moisture <= 90)
            {
                if (temperature <= 30)
                {
                    if (humidity < 50)
                    {
                        var optVol = bioConfig.OptimalWaterVolumnInLitres * 1000;
                        optVol = optVol - optVol * 0.9;
                        flowDuration = Convert.ToInt32(Math.Round(optVol / PumpSpeedPerSecond));
                    }
                    else
                    {
                        var optVol = bioConfig.OptimalWaterVolumnInLitres * 1000;
                        optVol = optVol - optVol * 0.8;
                        flowDuration = Convert.ToInt32(Math.Round(optVol / PumpSpeedPerSecond));
                    }
                }
                else
                {
                    if (humidity < 50)
                    {
                        var optVol = bioConfig.OptimalWaterVolumnInLitres * 1000;
                        optVol = optVol - optVol * 0.7;
                        flowDuration = Convert.ToInt32(Math.Round(optVol / PumpSpeedPerSecond));
                    }
                    else
                    {
                        var optVol = bioConfig.OptimalWaterVolumnInLitres * 1000;
                        optVol = optVol - optVol * 0.6;
                        flowDuration = Convert.ToInt32(Math.Round(optVol / PumpSpeedPerSecond));
                    }
                }
            }
            else
            {
                flowDuration = 0;
            }

            return flowDuration;
        }

        public void Dispose()
        {

        }
    }
}
using System;
using System.Device.Gpio;

namespace nanoFramework.EncButton
{
    public class Utils
    {
        public static int EB_UPTIME()
        {
            return (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond);
        }
        
        public static bool EBread(int pin, GpioController gpioController)
        {
            return gpioController.Read(pin) == PinValue.High;
        }
    }
}
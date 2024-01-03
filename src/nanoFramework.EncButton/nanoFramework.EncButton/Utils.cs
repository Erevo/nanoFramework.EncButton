using System;
using System.Device.Gpio;

namespace nanoFramework.EncButton
{
    public class Utils
    {
        public static long EB_UPTIME()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }
        
        public static bool EBread(int pin, GpioController gpioController)
        {
            return gpioController.Read(pin) == PinValue.High;
        }
    }
}
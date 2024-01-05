using System;
using System.Device.Gpio;

namespace nanoFramework.EncButton
{
    public static class Utils
    {
        public static long GetUptime()
        {
            return System.Environment.TickCount64;
        }
        
        public static bool ReadPin(int pin, GpioController gpioController)
        {
            return gpioController.Read(pin) == PinValue.High;
        }
    }
}
using System.Device.Gpio;
using nanoFramework.EncButton.Enums;

namespace nanoFramework.EncButton.Core
{
    public class Button : VirtButton
    {
        private GpioController _gpioController;

        public Button(int npin = 0, PinMode pinMode = PinMode.InputPullUp, PinValue btnLevel = default,
            GpioController? gpioController = null)
        {
            _gpioController = gpioController ?? new GpioController();

            init(npin, pinMode, btnLevel);
        }


        public int pin { get; set; }


        // указать пин и его режим работы
        void init(int npin = 0, PinMode mode = PinMode.InputPullUp, PinValue btnLevel = default)
        {
            pin = npin;
            _gpioController.SetPinMode(pin, mode);
            setBtnLevel(btnLevel);
        }

        // прочитать текущее значение кнопки (без дебаунса)
        bool read()
        {
            return Utils.EBread(pin, _gpioController) ^ read_bf(EncButtonPackFlag.INV);
        }

        // функция обработки, вызывать в loop
        public bool tick()
        {
            return base.tick(Utils.EBread(pin, _gpioController));
        }

        // обработка кнопки без сброса событий и вызова коллбэка
        bool tickRaw()
        {
            return base.tickRaw(Utils.EBread(pin, _gpioController));
        }
    }
}
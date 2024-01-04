using System;
using System.Collections;
using System.Device.Gpio;
using System.Threading;
using nanoFramework.EncButton.Enums;

namespace nanoFramework.EncButton.Core
{
    public class Button : VirtButton
    {
        private GpioController _gpioController;

        private static ArrayList _buttons = new ArrayList();
        private static Thread? _handlerThread;

        public Button(int npin = 0, PinMode pinMode = PinMode.InputPullUp, PinValue btnLevel = default,
            GpioController? gpioController = null)
        {
            if (_handlerThread == null)
            {
                _handlerThread = new Thread(() =>
                {
                    while (true)
                    {
                        foreach (var btn in _buttons)
                        {
                            if (btn is Button button)
                            {
                                button.tick();
                            }
                        }
                    }
                });
                _handlerThread.Start();
            }

            _gpioController = gpioController ?? new GpioController();

            _buttons.Add(this);
            init(npin, pinMode, btnLevel);
        }


        public int pin { get; set; }


        // указать пин и его режим работы
        void init(int npin, PinMode pinMode = PinMode.InputPullUp, PinValue btnLevel = default)
        {
            pin = npin;
            SetBtnLevel(btnLevel);

            if ((pinMode == PinMode.Input) | (pinMode == PinMode.InputPullDown) | (pinMode == PinMode.InputPullUp))
            {
                _gpioController.OpenPin(npin, pinMode);
                //_gpioController.RegisterCallbackForPinValueChangedEvent(_buttonPin, PinEventTypes.Rising | PinEventTypes.Falling, PinStateChanged);
                return;
            }

            throw new ArgumentException("GPIO pin can only be set to input, not to output.");
        }

        // прочитать текущее значение кнопки (без дебаунса)
        bool read()
        {
            return Utils.EBread(pin, _gpioController) ^ ReadFlag(EncButtonPackFlag.INV);
        }

        // функция обработки, вызывать в loop
        public bool tick()
        {
            return base.Tick(Utils.EBread(pin, _gpioController));
        }

        // обработка кнопки без сброса событий и вызова коллбэка
        bool tickRaw()
        {
            return base.TickRaw(Utils.EBread(pin, _gpioController));
        }
    }
}
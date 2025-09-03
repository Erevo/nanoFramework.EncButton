using System;
using System.Collections;
using System.Device;
using System.Device.Gpio;
using System.Threading;
using nanoFramework.EncButton.Enums;

namespace nanoFramework.EncButton.Core
{
    public class Button : VirtButton
    {
        private GpioController _gpioController;

        private static readonly ArrayList AllButtons = new ArrayList();
        private static Timer? _tickTimer;

        public Button(int nPin, PinMode pinMode = PinMode.InputPullUp, PinValue btnLevel = default,
            GpioController? gpioController = null)
        {
            _tickTimer ??= new Timer(GlobalTick, null, 0, 10);

            _gpioController = gpioController ?? new GpioController();

            AllButtons.Add(this);
            Init(nPin, pinMode, btnLevel);
        }

        public int Pin { get; private set; }

        // прочитать текущее значение кнопки (без дебаунса)
        public bool ReadRaw()
        {
            var readPinValue = Utils.ReadPin(Pin, _gpioController);
            return readPinValue ^ ReadFlag(EncButtonPackFlag.INV);
        }

        // указать пин и его режим работы
        private void Init(int nPin, PinMode pinMode = PinMode.InputPullUp, PinValue btnLevel = default)
        {
            Pin = nPin;
            SetBtnLevel(btnLevel);

            if ((pinMode == PinMode.Input) | (pinMode == PinMode.InputPullDown) | (pinMode == PinMode.InputPullUp))
            {
                _gpioController.OpenPin(nPin, pinMode);
                //_gpioController.RegisterCallbackForPinValueChangedEvent(_buttonPin, PinEventTypes.Rising | PinEventTypes.Falling, PinStateChanged);
                return;
            }

            throw new ArgumentException("GPIO pin can only be set to input, not to output.");
        }


        // функция обработки, вызывать в loop
        private bool Tick()
        {
            return base.Tick(Utils.ReadPin(Pin, _gpioController));
        }

        // обработка кнопки без сброса событий и вызова коллбэка
        private bool TickRaw()
        {
            return base.TickRaw(Utils.ReadPin(Pin, _gpioController));
        }

        private void GlobalTick(object state)
        {
            foreach (var btn in AllButtons)
            {
                if (btn is Button button)
                {
                    button.Tick();
                }
            }
        }
    }
}
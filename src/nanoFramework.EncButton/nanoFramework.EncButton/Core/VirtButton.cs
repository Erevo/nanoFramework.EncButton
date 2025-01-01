using System;
using System.Device.Gpio;
using System.Diagnostics;
using nanoFramework.EncButton.Enums;

namespace nanoFramework.EncButton.Core
{
    public class VirtButton
    {
        private EncButtonPackFlag flags;

        public const int EB_SHIFT = 4;

        public int EB_DEB_T { get; set; } = 70;
        public int EB_CLICK_T { get; set; } = 200;// >> EB_SHIFT;
        public int EB_HOLD_T = 600;// >> EB_SHIFT;
        public int EB_STEP_T = 200 ;//>> EB_SHIFT;

        //public event Action<EncButtonFlag>? Action;
        public delegate void ButtonActionDelegate(object sender, EncButtonFlag encButtonFlag);

        public event ButtonActionDelegate? ButtonAction;

        public long _timer = 0;
        public long _fTimer = 0;

        public int Clicks { get; private set; } = 0;

        // ====================== SET ======================
        // установить таймаут удержания, умолч. 600 (макс. 4000 мс)
        public void SetHoldTimeout(int timeout)
        {
            EB_HOLD_T = timeout;
        }

        // установить таймаут импульсного удержания, умолч. 200 (макс. 4000 мс)
        public void SetStepTimeout(int timeout)
        {
            EB_STEP_T = timeout;
        }

        // установить таймаут ожидания кликов, умолч. 500 (макс. 4000 мс)
        public void SetClickTimeout(int timeout)
        {
            EB_CLICK_T = timeout;
        }

        // установить таймаут антидребезга, умолч. 50 (макс. 255 мс)
        public void SetDebTimeout(int timeout)
        {
            EB_DEB_T = timeout;
        }

        // установить уровень кнопки (HIGH - кнопка замыкает VCC, LOW - замыкает GND)
        protected void SetBtnLevel(PinValue level)
        {
            WriteFlag(EncButtonPackFlag.INV, level == PinValue.Low);
        }

        // кнопка нажата в прерывании (не учитывает btnLevel!)
        private void PressIsr()
        {
            if (!ReadFlag(EncButtonPackFlag.DEB)) _timer = Utils.GetUptime();
            SetFlag(EncButtonPackFlag.DEB | EncButtonPackFlag.BISR);
        }

        // сбросить системные флаги (принудительно закончить обработку)
        private void Reset()
        {
            Clicks = 0;
            ClearFlag(~EncButtonPackFlag.INV);
        }

        // принудительно сбросить флаги событий
        private void Clear()
        {
            if (ReadFlag(EncButtonPackFlag.ClicksRelease)) Clicks = 0;
            if (ReadFlag(EncButtonPackFlag.ClicksRelease | EncButtonPackFlag.StepRelease |
                         EncButtonPackFlag.PressRelease | EncButtonPackFlag.HoldRelease | EncButtonPackFlag.REL_R))
            {
                ClearFlag(EncButtonPackFlag.ClicksRelease | EncButtonPackFlag.StepRelease |
                          EncButtonPackFlag.PressRelease | EncButtonPackFlag.HoldRelease | EncButtonPackFlag.REL_R);
            }
        }

        // ====================== GET ======================
        // кнопка нажата [событие]
        public bool IsPressed()
        {
            return ReadFlag(EncButtonPackFlag.PressRelease);
        }

        // кнопка отпущена (в любом случае) [событие]
        public bool Release()
        {
            return EqFlag(EncButtonPackFlag.REL_R | EncButtonPackFlag.Release,
                EncButtonPackFlag.REL_R | EncButtonPackFlag.Release);
        }

        // клик по кнопке (отпущена без удержания) [событие]
        public bool Click()
        {
            return EqFlag(EncButtonPackFlag.REL_R | EncButtonPackFlag.Release | EncButtonPackFlag.Hold,
                EncButtonPackFlag.REL_R);
        }

        // кнопка зажата (между press() и release()) [состояние]
        public bool IsPressing()
        {
            return ReadFlag(EncButtonPackFlag.Press);
        }

        // кнопка была удержана (больше таймаута) [событие]
        public bool Hold()
        {
            return ReadFlag(EncButtonPackFlag.HoldRelease);
        }

        // кнопка была удержана (больше таймаута) с предварительными кликами [событие]
        public bool Hold(int num)
        {
            return Clicks == num && Hold();
        }

        // кнопка удерживается (больше таймаута) [состояние]
        public bool Holding()
        {
            return EqFlag(EncButtonPackFlag.Press | EncButtonPackFlag.Hold,
                EncButtonPackFlag.Press | EncButtonPackFlag.Hold);
        }

        // кнопка удерживается (больше таймаута) с предварительными кликами [состояние]
        public bool Holding(int num)
        {
            return Clicks == num && Holding();
        }

        // импульсное удержание [событие]
        public bool Step()
        {
            return ReadFlag(EncButtonPackFlag.StepRelease);
        }

        // импульсное удержание с предварительными кликами [событие]
        public bool Step(int num)
        {
            return Clicks == num && Step();
        }

        // зафиксировано несколько кликов [событие]
        private bool HasClicks()
        {
            return EqFlag(EncButtonPackFlag.ClicksRelease | EncButtonPackFlag.Hold, EncButtonPackFlag.ClicksRelease);
        }

        // зафиксировано указанное количество кликов [событие]
        public bool HasClicks(int num)
        {
            return Clicks == num && HasClicks();
        }

        // получить количество кликов
        public int GetClicks()
        {
            return Clicks;
        }

        // получить количество степов
        public long GetSteps()
        {
            return _fTimer > 0 ? ((StepFor() + EB_STEP_T - 1) / EB_STEP_T) : 0; // (x + y - 1) / y
        }

        // кнопка отпущена после удержания [событие]
        private bool ReleaseHold()
        {
            return EqFlag(
                EncButtonPackFlag.REL_R | EncButtonPackFlag.Release | EncButtonPackFlag.Hold | EncButtonPackFlag.Step,
                EncButtonPackFlag.REL_R | EncButtonPackFlag.Hold);
        }

        // кнопка отпущена после удержания с предварительными кликами [событие]
        private bool ReleaseHold(int num)
        {
            return Clicks == num && EqFlag(
                EncButtonPackFlag.ClicksRelease | EncButtonPackFlag.Hold | EncButtonPackFlag.Step,
                EncButtonPackFlag.ClicksRelease | EncButtonPackFlag.Hold);
        }

        // кнопка отпущена после импульсного удержания [событие]
        private bool ReleaseStep()
        {
            return EqFlag(EncButtonPackFlag.REL_R | EncButtonPackFlag.Release | EncButtonPackFlag.Step,
                EncButtonPackFlag.REL_R | EncButtonPackFlag.Step);
        }

        // кнопка отпущена после импульсного удержания с предварительными кликами [событие]
        private bool ReleaseStep(int num)
        {
            return Clicks == num && EqFlag(EncButtonPackFlag.ClicksRelease | EncButtonPackFlag.Step,
                EncButtonPackFlag.ClicksRelease | EncButtonPackFlag.Step);
        }

        // кнопка ожидает повторных кликов [состояние]
        private bool Waiting()
        {
            return Clicks > 0 && EqFlag(EncButtonPackFlag.Press | EncButtonPackFlag.Release, 0);
        }

        // идёт обработка [состояние]
        private bool Busy()
        {
            return ReadFlag(EncButtonPackFlag.Busy);
        }

        // было действие с кнопки, вернёт код события [событие]
        private EncButtonFlag Action()
        {
            switch (flags & (EncButtonPackFlag)0b111111111)
            {
                case (EncButtonPackFlag.Press | EncButtonPackFlag.PressRelease):
                    return EncButtonFlag.Press;
                case (EncButtonPackFlag.Press | EncButtonPackFlag.Hold | EncButtonPackFlag.HoldRelease):
                    return EncButtonFlag.Hold;
                case (EncButtonPackFlag.Press | EncButtonPackFlag.Hold | EncButtonPackFlag.Step |
                      EncButtonPackFlag.StepRelease):
                    return EncButtonFlag.Step;
                case (EncButtonPackFlag.Release | EncButtonPackFlag.REL_R):
                case (EncButtonPackFlag.Release | EncButtonPackFlag.REL_R | EncButtonPackFlag.Hold):
                case (EncButtonPackFlag.Release | EncButtonPackFlag.REL_R | EncButtonPackFlag.Hold |
                      EncButtonPackFlag.Step):
                    return EncButtonFlag.Release;
                case (EncButtonPackFlag.REL_R):
                    return EncButtonFlag.Click;
                case (EncButtonPackFlag.ClicksRelease):
                    return EncButtonFlag.Clicks;
                case (EncButtonPackFlag.REL_R | EncButtonPackFlag.Hold):
                    return EncButtonFlag.Hold;
                case (EncButtonPackFlag.ClicksRelease | EncButtonPackFlag.Hold):
                    return EncButtonFlag.ReleaseHoldClicks;
                case (EncButtonPackFlag.REL_R | EncButtonPackFlag.Hold | EncButtonPackFlag.Step):
                    return EncButtonFlag.ReleaseStep;
                case (EncButtonPackFlag.ClicksRelease | EncButtonPackFlag.Hold | EncButtonPackFlag.Step):
                    return EncButtonFlag.ReleaseStepClicks;
            }

            return 0;
        }

        // ====================== TIME ======================
        // после взаимодействия с кнопкой (или энкодером EncButton) прошло указанное время, мс [событие]
        private bool Timeout(int tout)
        {
            if (ReadFlag(EncButtonPackFlag.Timeout) && (int)((int)Utils.GetUptime() - _timer) > tout)
            {
                ClearFlag(EncButtonPackFlag.Timeout);
                return true;
            }

            return false;
        }

        // время, которое кнопка удерживается (с начала нажатия), мс
        private long PressFor()
        {
            if (_fTimer > 0)
            {
                return Utils.GetUptime() - _fTimer;
            }

            return 0;
        }

        // кнопка удерживается дольше чем (с начала нажатия), мс [состояние]
        private bool PressFor(int ms)
        {
            return PressFor() > ms;
        }

        // время, которое кнопка удерживается (с начала удержания), мс
        private long HoldFor()
        {
            if (ReadFlag(EncButtonPackFlag.Hold))
            {
                return PressFor() - EB_HOLD_T;
            }

            return 0;
        }

        // кнопка удерживается дольше чем (с начала удержания), мс [состояние]
        private bool HoldFor(int ms)
        {
            return HoldFor() > ms;
        }

        // время, которое кнопка удерживается (с начала степа), мс
        private long StepFor()
        {
            if (ReadFlag(EncButtonPackFlag.Step))
            {
                return PressFor() - EB_HOLD_T * 2;
            }

            return 0;
        }

        // кнопка удерживается дольше чем (с начала степа), мс [состояние]
        private bool StepFor(int ms)
        {
            return StepFor() > ms;
        }

        // ====================== POLL ======================
        // обработка виртуальной кнопки как одновременное нажатие двух других кнопок
        protected bool Tick(VirtButton b0, VirtButton b1)
        {
            if (ReadFlag(EncButtonPackFlag.Both))
            {
                if (!b0.IsPressing() && !b1.IsPressing()) ClearFlag(EncButtonPackFlag.Both);
                if (!b0.IsPressing()) b0.Reset();
                if (!b1.IsPressing()) b1.Reset();
                b0.Clear();
                b1.Clear();
                return Tick(true);
            }
            else
            {
                if (b0.IsPressing() && b1.IsPressing()) SetFlag(EncButtonPackFlag.Both);
                return Tick(false);
            }
        }

        // обработка кнопки значением
        protected bool Tick(bool rawButtonState)
        {
            Clear();
            rawButtonState = PollBtn(rawButtonState);

            if (rawButtonState)
            {
                ButtonAction?.Invoke(this, Action());
            }

            return rawButtonState;
        }

        // обработка кнопки без сброса событий и вызова коллбэка
        protected bool TickRaw(bool rawButtonState)
        {
            return PollBtn(rawButtonState);
        }

        private bool PollBtn(bool rawButtonState)
        {
            if (ReadFlag(EncButtonPackFlag.BISR))
            {
                ClearFlag(EncButtonPackFlag.BISR);
                rawButtonState = true;
            }
            else rawButtonState ^= ReadFlag(EncButtonPackFlag.INV);

            if (!ReadFlag(EncButtonPackFlag.Busy))
            {
                if (rawButtonState) SetFlag(EncButtonPackFlag.Busy);
                else return false;
            }

            long ms = Utils.GetUptime();
            long deb = ms - _timer;
            if (rawButtonState)
            {
                // кнопка нажата
                if (!ReadFlag(EncButtonPackFlag.Press))
                {
                    // кнопка не была нажата ранее
                    if (!ReadFlag(EncButtonPackFlag.DEB) && EB_DEB_T > 0)
                    {
                        // дебаунс ещё не сработал
                        SetFlag(EncButtonPackFlag.DEB); // будем ждать дебаунс
                        _timer = ms; // сброс таймаута
                    }
                    else
                    {
                        // первое нажатие
                        if (deb >= EB_DEB_T || EB_DEB_T == 0)
                        {
                            // ждём EB_DEB_TIME
                            SetFlag(EncButtonPackFlag.Press | EncButtonPackFlag.PressRelease); // флаг на нажатие

                            _fTimer = ms;

                            _timer = ms; // сброс таймаута
                        }
                    }
                }
                else
                {
                    // кнопка уже была нажата
                    if (!ReadFlag(EncButtonPackFlag.EHLD))
                    {
                        if (!ReadFlag(EncButtonPackFlag.Hold))
                        {
                            // удержание ещё не зафиксировано

                            if (deb >= (int)EB_HOLD_T) // ждём EB_HOLD_TIME - это удержание
                            {
                                SetFlag(EncButtonPackFlag.HoldRelease |
                                        EncButtonPackFlag.Hold); // флаг что было удержание
                                _timer = ms; // сброс таймаута
                            }
                        }
                        else
                        {
                            // удержание зафиксировано
                            if (deb >= (int)(ReadFlag(EncButtonPackFlag.Step) ? EB_STEP_T : EB_HOLD_T))
                            {
                                SetFlag(EncButtonPackFlag.Step | EncButtonPackFlag.StepRelease); // флаг степ
                                _timer = ms; // сброс таймаута
                            }
                        }
                    }
                }
            }
            else
            {
                // кнопка не нажата
                if (ReadFlag(EncButtonPackFlag.Press))
                {
                    // но была нажата
                    if (deb >= EB_DEB_T)
                    {
                        // ждём EB_DEB_TIME
                        if (!ReadFlag(EncButtonPackFlag.Hold)) Clicks++; // не удерживали - это клик
                        if (ReadFlag(EncButtonPackFlag.EHLD)) Clicks = 0; //
                        SetFlag(EncButtonPackFlag.Release | EncButtonPackFlag.REL_R); // флаг release
                        ClearFlag(EncButtonPackFlag.Press); // кнопка отпущена
                    }
                    else
                    {
                        Debug.WriteLine($"Отработал антидребезг на отпускание {deb}");
                    }
                }
                else if (ReadFlag(EncButtonPackFlag.Release))
                {
                    if (!ReadFlag(EncButtonPackFlag.EHLD))
                    {
                        SetFlag(EncButtonPackFlag.REL_R); // флаг releaseHold / releaseStep
                    }

                    ClearFlag(EncButtonPackFlag.Release | EncButtonPackFlag.EHLD);
                    _timer = ms; // сброс таймаута
                }
                else if (Clicks > 0)
                {
                    // есть клики, ждём EB_CLICK_TIME

                    if (ReadFlag(EncButtonPackFlag.Hold | EncButtonPackFlag.Step) || deb >= (int)EB_CLICK_T)
                        SetFlag(EncButtonPackFlag.ClicksRelease); // флаг clicks

                    else if (_fTimer > 0) _fTimer = 0;
                }
                else if (ReadFlag(EncButtonPackFlag.Busy))
                {
                    ClearFlag(EncButtonPackFlag.Hold | EncButtonPackFlag.Step | EncButtonPackFlag.Busy);
                    SetFlag(EncButtonPackFlag.Timeout);

                    _fTimer = 0;

                    _timer = ms; // test!!
                }

                if (ReadFlag(EncButtonPackFlag.DEB))
                    ClearFlag(EncButtonPackFlag.DEB); // сброс ожидания нажатия (дебаунс)
            }

            return ReadFlag(EncButtonPackFlag.ClicksRelease | EncButtonPackFlag.PressRelease |
                            EncButtonPackFlag.HoldRelease |
                            EncButtonPackFlag.StepRelease | EncButtonPackFlag.REL_R);
        }

        private void SetFlag(EncButtonPackFlag x)
        {
            flags |= x;
        }

        private void ClearFlag(EncButtonPackFlag x)
        {
            flags &= ~x;
        }

        protected bool ReadFlag(EncButtonPackFlag x)
        {
            return flags.HasFlag(x);
        }

        private void WriteFlag(EncButtonPackFlag x, bool v)
        {
            if (v) SetFlag(x);
            else ClearFlag(x);
        }

        private bool EqFlag(EncButtonPackFlag x, EncButtonPackFlag y)
        {
            return (flags & x) == y;
        }
    }
}
using System;
using System.Device.Gpio;
using nanoFramework.EncButton.Enums;

namespace nanoFramework.EncButton.Core
{
    public class VirtButton
    {
        private EncButtonPackFlag flags;

        public const int EB_SHIFT = 4;

        public int EB_DEB_T = 50;
        public int EB_CLICK_T = 500 >> EB_SHIFT;
        public int EB_HOLD_T = 600 >> EB_SHIFT;
        public int EB_STEP_T = 200 >> EB_SHIFT;

        //public event Action<EncButtonFlag>? Action;
        public delegate void ButtonActionDelegate(object sender, EncButtonFlag encButtonFlag);
        public event ButtonActionDelegate? ButtonAction;

        public int _timer  = 0;
        public int _fTimer = 0;

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
            if (!ReadFlag(EncButtonPackFlag.DEB)) _timer = Utils.EB_UPTIME();
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
            if (ReadFlag(EncButtonPackFlag.CLKS_R)) Clicks = 0;
            if (ReadFlag(EncButtonPackFlag.CLKS_R | EncButtonPackFlag.STP_R | EncButtonPackFlag.PRS_R | EncButtonPackFlag.HLD_R | EncButtonPackFlag.REL_R))
            {
                ClearFlag(EncButtonPackFlag.CLKS_R | EncButtonPackFlag.STP_R | EncButtonPackFlag.PRS_R | EncButtonPackFlag.HLD_R | EncButtonPackFlag.REL_R);
            }
        }

        // ====================== GET ======================
        // кнопка нажата [событие]
        public bool Press()
        {
            return ReadFlag(EncButtonPackFlag.PRS_R);
        }

        // кнопка отпущена (в любом случае) [событие]
        public bool Release()
        {
            return EqFlag(EncButtonPackFlag.REL_R | EncButtonPackFlag.REL,
                EncButtonPackFlag.REL_R | EncButtonPackFlag.REL);
        }

        // клик по кнопке (отпущена без удержания) [событие]
        public bool Click()
        {
            return EqFlag(EncButtonPackFlag.REL_R | EncButtonPackFlag.REL | EncButtonPackFlag.HLD,
                EncButtonPackFlag.REL_R);
        }

        // кнопка зажата (между press() и release()) [состояние]
        private bool Pressing()
        {
            return ReadFlag(EncButtonPackFlag.PRS);
        }

        // кнопка была удержана (больше таймаута) [событие]
        public bool Hold()
        {
            return ReadFlag(EncButtonPackFlag.HLD_R);
        }

        // кнопка была удержана (больше таймаута) с предварительными кликами [событие]
        public bool Hold(int num)
        {
            return Clicks == num && Hold();
        }

        // кнопка удерживается (больше таймаута) [состояние]
        public bool Holding()
        {
            return EqFlag(EncButtonPackFlag.PRS | EncButtonPackFlag.HLD, EncButtonPackFlag.PRS | EncButtonPackFlag.HLD);
        }

        // кнопка удерживается (больше таймаута) с предварительными кликами [состояние]
        public bool Holding(int num)
        {
            return Clicks == num && Holding();
        }

        // импульсное удержание [событие]
        public bool Step()
        {
            return ReadFlag(EncButtonPackFlag.STP_R);
        }

        // импульсное удержание с предварительными кликами [событие]
        public bool Step(int num)
        {
            return Clicks == num && Step();
        }

        // зафиксировано несколько кликов [событие]
        private bool HasClicks()
        {
            return EqFlag(EncButtonPackFlag.CLKS_R | EncButtonPackFlag.HLD, EncButtonPackFlag.CLKS_R);
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
        public int GetSteps()
        {
            return _fTimer > 0 ? ((StepFor() + EB_STEP_T - 1) / EB_STEP_T) : 0; // (x + y - 1) / y
        }

        // кнопка отпущена после удержания [событие]
        private bool ReleaseHold()
        {
            return EqFlag(
                EncButtonPackFlag.REL_R | EncButtonPackFlag.REL | EncButtonPackFlag.HLD | EncButtonPackFlag.STP,
                EncButtonPackFlag.REL_R | EncButtonPackFlag.HLD);
        }

        // кнопка отпущена после удержания с предварительными кликами [событие]
        private bool ReleaseHold(int num)
        {
            return Clicks == num && EqFlag(EncButtonPackFlag.CLKS_R | EncButtonPackFlag.HLD | EncButtonPackFlag.STP,
                EncButtonPackFlag.CLKS_R | EncButtonPackFlag.HLD);
        }

        // кнопка отпущена после импульсного удержания [событие]
        private bool ReleaseStep()
        {
            return EqFlag(EncButtonPackFlag.REL_R | EncButtonPackFlag.REL | EncButtonPackFlag.STP,
                EncButtonPackFlag.REL_R | EncButtonPackFlag.STP);
        }

        // кнопка отпущена после импульсного удержания с предварительными кликами [событие]
        private bool ReleaseStep(int num)
        {
            return Clicks == num && EqFlag(EncButtonPackFlag.CLKS_R | EncButtonPackFlag.STP,
                EncButtonPackFlag.CLKS_R | EncButtonPackFlag.STP);
        }

        // кнопка ожидает повторных кликов [состояние]
        private bool Waiting()
        {
            return Clicks > 0 && EqFlag(EncButtonPackFlag.PRS | EncButtonPackFlag.REL, 0);
        }

        // идёт обработка [состояние]
        private bool Busy()
        {
            return ReadFlag(EncButtonPackFlag.BUSY);
        }

        // было действие с кнопки, вернёт код события [событие]
        private EncButtonFlag Action()
        {
            switch (flags & (EncButtonPackFlag)0b111111111)
            {
                case (EncButtonPackFlag.PRS | EncButtonPackFlag.PRS_R):
                    return EncButtonFlag.Press;
                case (EncButtonPackFlag.PRS | EncButtonPackFlag.HLD | EncButtonPackFlag.HLD_R):
                    return EncButtonFlag.Hold;
                case (EncButtonPackFlag.PRS | EncButtonPackFlag.HLD | EncButtonPackFlag.STP | EncButtonPackFlag.STP_R):
                    return EncButtonFlag.Step;
                case (EncButtonPackFlag.REL | EncButtonPackFlag.REL_R):
                case (EncButtonPackFlag.REL | EncButtonPackFlag.REL_R | EncButtonPackFlag.HLD):
                case (EncButtonPackFlag.REL | EncButtonPackFlag.REL_R | EncButtonPackFlag.HLD | EncButtonPackFlag.STP):
                    return EncButtonFlag.Release;
                case (EncButtonPackFlag.REL_R):
                    return EncButtonFlag.Click;
                case (EncButtonPackFlag.CLKS_R):
                    return EncButtonFlag.Clicks;
                case (EncButtonPackFlag.REL_R | EncButtonPackFlag.HLD):
                    return EncButtonFlag.Hold;
                case (EncButtonPackFlag.CLKS_R | EncButtonPackFlag.HLD):
                    return EncButtonFlag.ReleaseHoldClicks;
                case (EncButtonPackFlag.REL_R | EncButtonPackFlag.HLD | EncButtonPackFlag.STP):
                    return EncButtonFlag.ReleaseStep;
                case (EncButtonPackFlag.CLKS_R | EncButtonPackFlag.HLD | EncButtonPackFlag.STP):
                    return EncButtonFlag.ReleaseStepClicks;
            }

            return 0;
        }

        // ====================== TIME ======================
        // после взаимодействия с кнопкой (или энкодером EncButton) прошло указанное время, мс [событие]
        private bool Timeout(int tout)
        {
            if (ReadFlag(EncButtonPackFlag.TOUT) && (int)((int)Utils.EB_UPTIME() - _timer) > tout)
            {
                ClearFlag(EncButtonPackFlag.TOUT);
                return true;
            }

            return false;
        }

        // время, которое кнопка удерживается (с начала нажатия), мс
        private int PressFor()
        {
            if (_fTimer > 0)
            {
                return Utils.EB_UPTIME() - _fTimer;
            }

            return 0;
        }

        // кнопка удерживается дольше чем (с начала нажатия), мс [состояние]
        private bool PressFor(int ms)
        {
            return PressFor() > ms;
        }

        // время, которое кнопка удерживается (с начала удержания), мс
        private int HoldFor()
        {
            if (ReadFlag(EncButtonPackFlag.HLD))
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
        private int StepFor()
        {
            if (ReadFlag(EncButtonPackFlag.STP))
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
            if (ReadFlag(EncButtonPackFlag.BOTH))
            {
                if (!b0.Pressing() && !b1.Pressing()) ClearFlag(EncButtonPackFlag.BOTH);
                if (!b0.Pressing()) b0.Reset();
                if (!b1.Pressing()) b1.Reset();
                b0.Clear();
                b1.Clear();
                return Tick(true);
            }
            else
            {
                if (b0.Pressing() && b1.Pressing()) SetFlag(EncButtonPackFlag.BOTH);
                return Tick(false);
            }
        }

        // обработка кнопки значением
        protected bool Tick(bool s)
        {
            Clear();
            s = PollBtn(s);

            if (s)
            {
                ButtonAction?.Invoke(this, Action());
            }

            return s;
        }

        // обработка кнопки без сброса событий и вызова коллбэка
        protected bool TickRaw(bool s)
        {
            return PollBtn(s);
        }

        private bool PollBtn(bool s)
        {
            if (ReadFlag(EncButtonPackFlag.BISR))
            {
                ClearFlag(EncButtonPackFlag.BISR);
                s = true;
            }
            else s ^= ReadFlag(EncButtonPackFlag.INV);

            if (!ReadFlag(EncButtonPackFlag.BUSY))
            {
                if (s) SetFlag(EncButtonPackFlag.BUSY);
                else return false;
            }

            int ms = Utils.EB_UPTIME();
            int deb = ms - _timer;

            if (s)
            {
                // кнопка нажата
                if (!ReadFlag(EncButtonPackFlag.PRS))
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
                            SetFlag(EncButtonPackFlag.PRS | EncButtonPackFlag.PRS_R); // флаг на нажатие

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
                        if (!ReadFlag(EncButtonPackFlag.HLD))
                        {
                            // удержание ещё не зафиксировано

                            if (deb >= (int)EB_HOLD_T) // ждём EB_HOLD_TIME - это удержание
                            {
                                SetFlag(EncButtonPackFlag.HLD_R | EncButtonPackFlag.HLD); // флаг что было удержание
                                _timer = ms; // сброс таймаута
                            }
                        }
                        else
                        {
                            // удержание зафиксировано
                            if (deb >= (int)(ReadFlag(EncButtonPackFlag.STP) ? EB_STEP_T : EB_HOLD_T))
                            {
                                SetFlag(EncButtonPackFlag.STP | EncButtonPackFlag.STP_R); // флаг степ
                                _timer = ms; // сброс таймаута
                            }
                        }
                    }
                }
            }
            else
            {
                // кнопка не нажата
                if (ReadFlag(EncButtonPackFlag.PRS))
                {
                    // но была нажата
                    if (deb >= EB_DEB_T)
                    {
                        // ждём EB_DEB_TIME
                        if (!ReadFlag(EncButtonPackFlag.HLD)) Clicks++; // не удерживали - это клик
                        if (ReadFlag(EncButtonPackFlag.EHLD)) Clicks = 0; //
                        SetFlag(EncButtonPackFlag.REL | EncButtonPackFlag.REL_R); // флаг release
                        ClearFlag(EncButtonPackFlag.PRS); // кнопка отпущена
                    }
                }
                else if (ReadFlag(EncButtonPackFlag.REL))
                {
                    if (!ReadFlag(EncButtonPackFlag.EHLD))
                    {
                        SetFlag(EncButtonPackFlag.REL_R); // флаг releaseHold / releaseStep
                    }

                    ClearFlag(EncButtonPackFlag.REL | EncButtonPackFlag.EHLD);
                    _timer = ms; // сброс таймаута
                }
                else if (Clicks > 0)
                {
                    // есть клики, ждём EB_CLICK_TIME

                    if (ReadFlag(EncButtonPackFlag.HLD | EncButtonPackFlag.STP) || deb >= (int)EB_CLICK_T)
                        SetFlag(EncButtonPackFlag.CLKS_R); // флаг clicks

                    else if (_fTimer > 0) _fTimer = 0;
                }
                else if (ReadFlag(EncButtonPackFlag.BUSY))
                {
                    ClearFlag(EncButtonPackFlag.HLD | EncButtonPackFlag.STP | EncButtonPackFlag.BUSY);
                    SetFlag(EncButtonPackFlag.TOUT);

                    _fTimer = 0;

                    _timer = ms; // test!!
                }

                if (ReadFlag(EncButtonPackFlag.DEB)) ClearFlag(EncButtonPackFlag.DEB); // сброс ожидания нажатия (дебаунс)
            }

            return ReadFlag(EncButtonPackFlag.CLKS_R | EncButtonPackFlag.PRS_R | EncButtonPackFlag.HLD_R |
                           EncButtonPackFlag.STP_R | EncButtonPackFlag.REL_R);
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
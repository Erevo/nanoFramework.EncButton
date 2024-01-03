using System;
using System.Device.Gpio;
using nanoFramework.EncButton.Enums;

namespace nanoFramework.EncButton.Core
{
    public class VirtButton
    {
        private EncButtonPackFlag flags;

        public const int EB_SHIFT = 4;

        public int EB_DEB_T { get; set; } = 50;
        public int EB_CLICK_T { get; set; } = 500 >> EB_SHIFT;
        public int EB_HOLD_T { get; set; } = 600 >> EB_SHIFT;
        public int EB_STEP_T { get; set; } = 200 >> EB_SHIFT;

        public event Action<EncButtonFlag>? Action;

        public int tmr { get; set; } = 0;
        public int ftmr = 0;

        public int clicks { get; set; } = 0;

        // ====================== SET ======================
        // установить таймаут удержания, умолч. 600 (макс. 4000 мс)
        public void setHoldTimeout(int timeout)
        {
            EB_HOLD_T = timeout;
        }

        // установить таймаут импульсного удержания, умолч. 200 (макс. 4000 мс)
        public void setStepTimeout(int timeout)
        {
            EB_STEP_T = timeout;
        }

        // установить таймаут ожидания кликов, умолч. 500 (макс. 4000 мс)
        public void setClickTimeout(int timeout)
        {
            EB_CLICK_T = timeout;
        }

        // установить таймаут антидребезга, умолч. 50 (макс. 255 мс)
        public void setDebTimeout(int timeout)
        {
            EB_DEB_T = timeout;
        }

        // установить уровень кнопки (HIGH - кнопка замыкает VCC, LOW - замыкает GND)
        protected void setBtnLevel(PinValue level) {
            write_bf(EncButtonPackFlag.INV, level == PinValue.Low);
        }

        // кнопка нажата в прерывании (не учитывает btnLevel!)
        void pressISR() {
            if (!read_bf(EncButtonPackFlag.DEB)) tmr = Utils.EB_UPTIME();
            set_bf(EncButtonPackFlag.DEB | EncButtonPackFlag.BISR);
        }
        
        // сбросить системные флаги (принудительно закончить обработку)
        void reset() {
            clicks = 0;
            clr_bf(~EncButtonPackFlag.INV);
        }

        // принудительно сбросить флаги событий
        void clear() {
            if (read_bf(EncButtonPackFlag.CLKS_R)) clicks = 0;
            if (read_bf(EncButtonPackFlag.CLKS_R | EncButtonPackFlag.STP_R | EncButtonPackFlag.PRS_R | EncButtonPackFlag.HLD_R | EncButtonPackFlag.REL_R)) {
                clr_bf(EncButtonPackFlag.CLKS_R | EncButtonPackFlag.STP_R | EncButtonPackFlag.PRS_R | EncButtonPackFlag.HLD_R | EncButtonPackFlag.REL_R);
            }
        }
        
        // ====================== GET ======================
        // кнопка нажата [событие]
        public bool press()
        {
            return read_bf(EncButtonPackFlag.PRS_R);
        }

        // кнопка отпущена (в любом случае) [событие]
        public bool release()
        {
            return eq_bf(EncButtonPackFlag.REL_R | EncButtonPackFlag.REL,
                EncButtonPackFlag.REL_R | EncButtonPackFlag.REL);
        }

        // клик по кнопке (отпущена без удержания) [событие]
        public bool click()
        {
            return eq_bf(EncButtonPackFlag.REL_R | EncButtonPackFlag.REL | EncButtonPackFlag.HLD,
                EncButtonPackFlag.REL_R);
        }

        // кнопка зажата (между press() и release()) [состояние]
        public bool pressing()
        {
            return read_bf(EncButtonPackFlag.PRS);
        }

        // кнопка была удержана (больше таймаута) [событие]
        public bool hold()
        {
            return read_bf(EncButtonPackFlag.HLD_R);
        }

        // кнопка была удержана (больше таймаута) с предварительными кликами [событие]
        public bool hold(int num)
        {
            return clicks == num && hold();
        }

        // кнопка удерживается (больше таймаута) [состояние]
        public bool holding()
        {
            return eq_bf(EncButtonPackFlag.PRS | EncButtonPackFlag.HLD, EncButtonPackFlag.PRS | EncButtonPackFlag.HLD);
        }

        // кнопка удерживается (больше таймаута) с предварительными кликами [состояние]
        public bool holding(int num)
        {
            return clicks == num && holding();
        }

        // импульсное удержание [событие]
        public bool step()
        {
            return read_bf(EncButtonPackFlag.STP_R);
        }

        // импульсное удержание с предварительными кликами [событие]
        public bool step(int num)
        {
            return clicks == num && step();
        }

        // зафиксировано несколько кликов [событие]
        public bool hasClicks()
        {
            return eq_bf(EncButtonPackFlag.CLKS_R | EncButtonPackFlag.HLD, EncButtonPackFlag.CLKS_R);
        }

        // зафиксировано указанное количество кликов [событие]
        public bool hasClicks(int num)
        {
            return clicks == num && hasClicks();
        }

        // получить количество кликов
        public int getClicks()
        {
            return clicks;
        }

        // получить количество степов
        public int getSteps()
        {
            return ftmr > 0 ? ((stepFor() + EB_STEP_T - 1) / EB_STEP_T) : 0; // (x + y - 1) / y
        }

        // кнопка отпущена после удержания [событие]
        bool releaseHold()
        {
            return eq_bf(
                EncButtonPackFlag.REL_R | EncButtonPackFlag.REL | EncButtonPackFlag.HLD | EncButtonPackFlag.STP,
                EncButtonPackFlag.REL_R | EncButtonPackFlag.HLD);
        }

        // кнопка отпущена после удержания с предварительными кликами [событие]
        bool releaseHold(int num)
        {
            return clicks == num && eq_bf(EncButtonPackFlag.CLKS_R | EncButtonPackFlag.HLD | EncButtonPackFlag.STP,
                EncButtonPackFlag.CLKS_R | EncButtonPackFlag.HLD);
        }

        // кнопка отпущена после импульсного удержания [событие]
        bool releaseStep()
        {
            return eq_bf(EncButtonPackFlag.REL_R | EncButtonPackFlag.REL | EncButtonPackFlag.STP,
                EncButtonPackFlag.REL_R | EncButtonPackFlag.STP);
        }

        // кнопка отпущена после импульсного удержания с предварительными кликами [событие]
        bool releaseStep(int num)
        {
            return clicks == num && eq_bf(EncButtonPackFlag.CLKS_R | EncButtonPackFlag.STP,
                EncButtonPackFlag.CLKS_R | EncButtonPackFlag.STP);
        }

        // кнопка ожидает повторных кликов [состояние]
        bool waiting()
        {
            return clicks > 0 && eq_bf(EncButtonPackFlag.PRS | EncButtonPackFlag.REL, 0);
        }

        // идёт обработка [состояние]
        bool busy()
        {
            return read_bf(EncButtonPackFlag.BUSY);
        }

        // было действие с кнопки, вернёт код события [событие]
        EncButtonFlag action()
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
        bool timeout(int tout)
        {
            if (read_bf(EncButtonPackFlag.TOUT) && (int)((int)Utils.EB_UPTIME() - tmr) > tout)
            {
                clr_bf(EncButtonPackFlag.TOUT);
                return true;
            }

            return false;
        }

        // время, которое кнопка удерживается (с начала нажатия), мс
        int pressFor()
        {
            if (ftmr > 0)
            {
                return Utils.EB_UPTIME() - ftmr;
            }

            return 0;
        }

        // кнопка удерживается дольше чем (с начала нажатия), мс [состояние]
        bool pressFor(int ms)
        {
            return pressFor() > ms;
        }

        // время, которое кнопка удерживается (с начала удержания), мс
        int holdFor()
        {
            if (read_bf(EncButtonPackFlag.HLD))
            {
                return pressFor() - EB_HOLD_T;
            }

            return 0;
        }

        // кнопка удерживается дольше чем (с начала удержания), мс [состояние]
        bool holdFor(int ms)
        {
            return holdFor() > ms;
        }

        // время, которое кнопка удерживается (с начала степа), мс
        int stepFor()
        {
            if (read_bf(EncButtonPackFlag.STP))
            {
                return pressFor() - EB_HOLD_T * 2;
            }

            return 0;
        }

        // кнопка удерживается дольше чем (с начала степа), мс [состояние]
        bool stepFor(int ms)
        {
            return stepFor() > ms;
        }

        // ====================== POLL ======================
        // обработка виртуальной кнопки как одновременное нажатие двух других кнопок
        protected bool tick(VirtButton b0, VirtButton b1)
        {
            if (read_bf(EncButtonPackFlag.BOTH))
            {
                if (!b0.pressing() && !b1.pressing()) clr_bf(EncButtonPackFlag.BOTH);
                if (!b0.pressing()) b0.reset();
                if (!b1.pressing()) b1.reset();
                b0.clear();
                b1.clear();
                return tick(true);
            }
            else
            {
                if (b0.pressing() && b1.pressing()) set_bf(EncButtonPackFlag.BOTH);
                return tick(false);
            }
        }

        // обработка кнопки значением
        protected bool tick(bool s)
        {
            clear();
            s = pollBtn(s);

            if(s) Action?.Invoke(action());

            return s;
        }

        // обработка кнопки без сброса событий и вызова коллбэка
        protected bool tickRaw(bool s)
        {
            return pollBtn(s);
        }

        bool pollBtn(bool s)
        {
            if (read_bf(EncButtonPackFlag.BISR))
            {
                clr_bf(EncButtonPackFlag.BISR);
                s = true;
            }
            else s ^= read_bf(EncButtonPackFlag.INV);

            if (!read_bf(EncButtonPackFlag.BUSY))
            {
                if (s) set_bf(EncButtonPackFlag.BUSY);
                else return false;
            }

            int ms = Utils.EB_UPTIME();
            int deb = ms - tmr;

            if (s)
            {
                // кнопка нажата
                if (!read_bf(EncButtonPackFlag.PRS))
                {
                    // кнопка не была нажата ранее
                    if (!read_bf(EncButtonPackFlag.DEB) && EB_DEB_T > 0)
                    {
                        // дебаунс ещё не сработал
                        set_bf(EncButtonPackFlag.DEB); // будем ждать дебаунс
                        tmr = ms; // сброс таймаута
                    }
                    else
                    {
                        // первое нажатие
                        if (deb >= EB_DEB_T || EB_DEB_T == 0)
                        {
                            // ждём EB_DEB_TIME
                            set_bf(EncButtonPackFlag.PRS | EncButtonPackFlag.PRS_R); // флаг на нажатие

                            ftmr = ms;

                            tmr = ms; // сброс таймаута
                        }
                    }
                }
                else
                {
                    // кнопка уже была нажата
                    if (!read_bf(EncButtonPackFlag.EHLD))
                    {
                        if (!read_bf(EncButtonPackFlag.HLD))
                        {
                            // удержание ещё не зафиксировано

                            if (deb >= (int)EB_HOLD_T) // ждём EB_HOLD_TIME - это удержание
                            {
                                set_bf(EncButtonPackFlag.HLD_R | EncButtonPackFlag.HLD); // флаг что было удержание
                                tmr = ms; // сброс таймаута
                            }
                        }
                        else
                        {
                            // удержание зафиксировано
                            if (deb >= (int)(read_bf(EncButtonPackFlag.STP) ? EB_STEP_T : EB_HOLD_T))
                            {
                                set_bf(EncButtonPackFlag.STP | EncButtonPackFlag.STP_R); // флаг степ
                                tmr = ms; // сброс таймаута
                            }
                        }
                    }
                }
            }
            else
            {
                // кнопка не нажата
                if (read_bf(EncButtonPackFlag.PRS))
                {
                    // но была нажата
                    if (deb >= EB_DEB_T)
                    {
                        // ждём EB_DEB_TIME
                        if (!read_bf(EncButtonPackFlag.HLD)) clicks++; // не удерживали - это клик
                        if (read_bf(EncButtonPackFlag.EHLD)) clicks = 0; //
                        set_bf(EncButtonPackFlag.REL | EncButtonPackFlag.REL_R); // флаг release
                        clr_bf(EncButtonPackFlag.PRS); // кнопка отпущена
                    }
                }
                else if (read_bf(EncButtonPackFlag.REL))
                {
                    if (!read_bf(EncButtonPackFlag.EHLD))
                    {
                        set_bf(EncButtonPackFlag.REL_R); // флаг releaseHold / releaseStep
                    }

                    clr_bf(EncButtonPackFlag.REL | EncButtonPackFlag.EHLD);
                    tmr = ms; // сброс таймаута
                }
                else if (clicks > 0)
                {
                    // есть клики, ждём EB_CLICK_TIME

                    if (read_bf(EncButtonPackFlag.HLD | EncButtonPackFlag.STP) || deb >= (int)EB_CLICK_T)
                        set_bf(EncButtonPackFlag.CLKS_R); // флаг clicks

                    else if (ftmr > 0) ftmr = 0;
                }
                else if (read_bf(EncButtonPackFlag.BUSY))
                {
                    clr_bf(EncButtonPackFlag.HLD | EncButtonPackFlag.STP | EncButtonPackFlag.BUSY);
                    set_bf(EncButtonPackFlag.TOUT);

                    ftmr = 0;

                    tmr = ms; // test!!
                }

                if (read_bf(EncButtonPackFlag.DEB)) clr_bf(EncButtonPackFlag.DEB); // сброс ожидания нажатия (дебаунс)
            }

            return read_bf(EncButtonPackFlag.CLKS_R | EncButtonPackFlag.PRS_R | EncButtonPackFlag.HLD_R |
                           EncButtonPackFlag.STP_R | EncButtonPackFlag.REL_R);
        }

        public void set_bf(EncButtonPackFlag x)
        {
            flags |= x;
        }

        public void clr_bf(EncButtonPackFlag x)
        {
            flags &= ~x;
        }

        public bool read_bf(EncButtonPackFlag x)
        {
            return flags.HasFlag(x);
        }

        public void write_bf(EncButtonPackFlag x, bool v)
        {
            if (v) set_bf(x);
            else clr_bf(x);
        }

        public bool eq_bf(EncButtonPackFlag x, EncButtonPackFlag y)
        {
            return (flags & x) == y;
        }
    }
}
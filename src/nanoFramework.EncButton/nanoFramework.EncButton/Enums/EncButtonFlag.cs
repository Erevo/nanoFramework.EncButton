using System;

namespace nanoFramework.EncButton.Enums
{
    [Flags]
    public enum EncButtonFlag
    {
        Press = 1 << 0,        // нажатие на кнопку
        Hold = 1 << 1,         // кнопка удержана
        Step = 1 << 2,         // импульсное удержание
        Release = 1 << 3,      // кнопка отпущена
        Click = 1 << 4,        // одиночный клик
        Clicks = 1 << 5,       // сигнал о нескольких кликах
        Turn = 1 << 6,         // поворот энкодера
        ReleaseHold = 1 << 7,     // кнопка отпущена после удержания
        ReleaseHoldClicks = 1 << 8,   // кнопка отпущена после удержания с предв. кликами
        ReleaseStep = 1 << 9,     // кнопка отпущена после степа
        ReleaseStepClicks = 1 << 10,  // кнопка отпущена после степа с предв. кликами
    }
}
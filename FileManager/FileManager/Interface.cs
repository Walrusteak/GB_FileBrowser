using System;
using System.Configuration;
using System.Runtime.InteropServices;
using static System.Console;

namespace FileManager
{
    public static class Interface
    {
        const int commandBlockHeight = 1;
        const int infoBlockHeight = 4;

        private static readonly bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly int offset = isWindows ? -1 : 0;  //кроссплатформенный костыль

        private static bool autoSize = true;
        private static int defaultHeight = 30;
        
        public static int Width { get; private set; }
        public static int Height { get; private set; }

        public static int AreaWidth => Width - 2 + offset;  //дети, не перебарщивайте с кроссплатформой

        public static int ListAreaHeight => Height - commandBlockHeight - infoBlockHeight - 4;
        public static int InfoBlockY => Height - commandBlockHeight - infoBlockHeight - 2;

        static Interface()
        {
            Boolean.TryParse(ConfigurationManager.AppSettings["AutoSize"], out autoSize);
            Int32.TryParse(ConfigurationManager.AppSettings["DefaultHeight"], out defaultHeight);

            if (!isWindows) //под маком консоль не дает рисовать вне границ
                autoSize = true;
        }

        public static void GetWindowSize()
        {
            Width = WindowWidth + offset;   //когда это заработало, я чуть не заплакал
            Height = autoSize ? WindowHeight : defaultHeight;
        }

        public static void PrintBounds()
        {
            if (Height < 10)
            {
                SetCursorPosition(0, 0);
                Write("Слишком маленькое окно");
                return;
            }

            #region Corners
            SetCursorPosition(0, 0);
            Write(Graphics.TopLeft);
            SetCursorPosition(Width, 0);
            Write(Graphics.TopRight);
            SetCursorPosition(0, Height + offset);
            Write(Graphics.BottomLeft);
            SetCursorPosition(Width, Height + offset);
            Write(Graphics.BottomRight);
            #endregion Corners

            #region Horizontal
            SetCursorPosition(1, 0);
            Write(new string(Graphics.Horizontal, AreaWidth));
            SetCursorPosition(1, Height - commandBlockHeight - 2);
            Write(new string(Graphics.SingleHorizontal, AreaWidth));
            SetCursorPosition(1, Height - infoBlockHeight - 4);
            Write(new string(Graphics.SingleHorizontal, AreaWidth));
            SetCursorPosition(1, Height + offset);
            Write(new string(Graphics.Horizontal, AreaWidth));
            #endregion Horizontal

            #region Vertial
            for (int i = 1; i < Height - 1; i++)
            {
                if (i == Height - commandBlockHeight - 2 || i == Height - infoBlockHeight - 4)
                {
                    SetCursorPosition(0, i);
                    Write(Graphics.CenterLeft);
                    SetCursorPosition(Width, i);
                    Write(Graphics.CenterRight);
                }
                else
                {
                    SetCursorPosition(0, i);
                    Write(Graphics.Vertical);
                    SetCursorPosition(Width, i);
                    Write(Graphics.Vertical);
                }
            }
            #endregion Vertical
        }

        public static void PrintPageNumber(int current, int total)
        {
            SetCursorPosition(2, 0);
            Write($"{current}/{total}".PadRight(Width - 3, Graphics.Horizontal));
        }

        public static void ClearCommand()
        {
            SetCursorPosition(1, Height - 2);
            Write(new string(' ', Width - 2));
        }

        public static void ClearInfo()
        {
            for (int i = ListAreaHeight + 2; i <= ListAreaHeight + infoBlockHeight + 1; i++)
            {
                SetCursorPosition(1, i);
                Write(new string(' ', AreaWidth));
            }
        }

        public static void ClearList()
        {
            for (int i = 1; i <= ListAreaHeight; i++)
            {
                SetCursorPosition(1, i);
                Write(new string(' ', AreaWidth));
            }
        }

        public static void PrepareCommandField()
        {
            ClearCommand();
            SetCursorPosition(1, Height - 2);
        }
    }
}

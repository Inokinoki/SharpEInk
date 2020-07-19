using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace WinIoTEPDInk
{
    public enum EpdModel
    {
        EPD1IN54 = 0,
        EPD1IN54B,
        EPD1IN54C,
        EPD2IN13,
        EPD2IN13B,
        EPD2IN7,
        EPD2IN7B,
        EPD2IN9,
        EPD2IN9B,
        EPD4IN2,
        EPD4IN2B,
        EPD7IN5,
        EPD7IN5B
    }

    internal sealed class EpdResolution
    {
        public static int[] WIDTH = new int[]
        {
            200,    // EPD1IN54
            200,    // EPD1IN54B
            200,    // EPD1IN54C
            128,    // EPD2IN13
            128,    // EPD2IN13B
            176,    // EPD2IN7
            176,    // EPD2IN7B
            128,    // EPD2IN9
            128,    // EPD2IN9B
            400,    // EPD4IN2
            400,    // EPD4IN2B
            640,    // EPD7IN5
            640,    // EPD7IN5B
        };

        public static int[] HEIGHT = new int[]
        {
            200,    // EPD1IN54
            200,    // EPD1IN54B
            200,    // EPD1IN54C
            250,    // EPD2IN13
            250,    // EPD2IN13B
            264,    // EPD2IN7
            264,    // EPD2IN7B
            296,    // EPD2IN9
            296,    // EPD2IN9B
            300,    // EPD4IN2
            300,    // EPD4IN2B
            384,    // EPD7IN5
            384,    // EPD7IN5B
        };
    }

    interface IEpd
    {
        /**
         * Functional
         */
        void Reset();
        void DisplayFrameAsync();
        void Sleep();

        /**
         * Set image
         */
        void SetFrameMemory(byte[] image_buffer, int image_width, int image_height, int startX, int startY);
        void SetFrameMemory(byte[] image_buffer);

        /**
         * Clear image
         */
        void ClearFrameMemory(byte color);
    }
}

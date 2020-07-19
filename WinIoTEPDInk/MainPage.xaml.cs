using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace WinIoTEPDInk
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            epd = new FakeEpd(EpdModel.EPD2IN9, display);
            
            epd.SetFrameMemory(TestData.RANDOM_SAMPLE, 128, 296, 0, 0);
            epd.DisplayFrame();

            realEpd = new SPIEpd(EpdModel.EPD2IN9, reset_pin: 17, cs_pin: 8, dc_pin: 27, busy_pin: 22);
        }

        private SPIEpd realEpd;
        private FakeEpd epd;

        private double originWidth = -1;
        private double originHeight = -1;

        private void SizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (originHeight <= 0 || originHeight <= 0)
            {
                // Avoid the call on creation
                if (display != null)
                {
                    originHeight = display.Height;
                    originWidth = display.Width;
                }
            }

            // Avoid the call on creation
            if (display != null)
            {
                display.Width = originWidth * e.NewValue / 100;
                display.Height = originHeight * e.NewValue / 100;
            }
        }

        private void SizeResetButton_Click(object sender, RoutedEventArgs e)
        {
            sizeSlider.Value = 100;
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            realEpd.connectSPIAsync();
        }

        private void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            Sync();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO
            Reset();
        }

        private async Task Sync()
        {
            await realEpd.SetFrameMemoryAsync(TestData.RANDOM_SAMPLE);
            await realEpd.DisplayFrameAsync();
        }

        private async Task Reset()
        {
            await realEpd.ClearFrameMemoryAsync(0xAA);
            await realEpd.DisplayFrameAsync();
            await realEpd.ClearFrameMemoryAsync(0xFF);
            await realEpd.DisplayFrameAsync();
        }
    }
}

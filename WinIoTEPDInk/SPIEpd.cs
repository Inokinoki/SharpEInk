using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using WinIoTEPDInk.Impl;

namespace WinIoTEPDInk
{
    /**
     * Only use Epd Default Connection in this namespace
     */
    internal sealed class SPIEpdDefaultConnection
    {
        public const int RST_PIN = 8;
        public const int DC_PIN = 9;
        public const int CS_PIN = 10;
        public const int BUSY_PIN = 7;
    }

    public sealed class SPIEpd : IEpd
    {
        // private byte lut[];

        public int ResetPin { get; }
        public int DCPin { get; }
        public int ChipSelectPin { get; }
        public int BusyPin { get; }

        public int Width { get; }
        public int Height { get; }

        public bool Sleeping { get; set; }
        public bool Busy { get; set; }
        public bool Connected { get; set; }

        private GpioPin _reset;
        private GpioPin _dc;
        private GpioPin _chipselect;
        private GpioPin _busy;

        private SpiDevice _epd_ink_screen = null;

        public EpdModel Model { get; }

        private IEpdImplBase _impl = null;

        public SPIEpd(EpdModel model,
            int reset_pin = SPIEpdDefaultConnection.RST_PIN,
            int dc_pin = SPIEpdDefaultConnection.DC_PIN,
            int cs_pin = SPIEpdDefaultConnection.CS_PIN,
            int busy_pin = SPIEpdDefaultConnection.BUSY_PIN)
        {
            ResetPin        = reset_pin;
            DCPin           = dc_pin;
            ChipSelectPin   = cs_pin;
            BusyPin         = busy_pin;
            Width   = EpdResolution.WIDTH[(int)model];
            Height  = EpdResolution.HEIGHT[(int)model];

            Model = model;

            switch (Model)
            {
                case EpdModel.EPD1IN54:
                    _impl = new EpdFallback();
                    break;
                case EpdModel.EPD1IN54B:
                    _impl = new EpdFallback();
                    break;
                case EpdModel.EPD1IN54C:
                    _impl = new EpdFallback();
                    break;
                case EpdModel.EPD2IN13:
                    _impl = new EpdFallback();
                    break;
                case EpdModel.EPD2IN13B:
                    _impl = new EpdFallback();
                    break;
                case EpdModel.EPD2IN7:
                    _impl = new EpdFallback();
                    break;
                case EpdModel.EPD2IN7B:
                    _impl = new EpdFallback();
                    break;
                case EpdModel.EPD2IN9:
                    _impl = new Epd2in9(this);
                    break;
                case EpdModel.EPD2IN9B:
                    _impl = new EpdFallback();
                    break;
                case EpdModel.EPD4IN2:
                    _impl = new Epd4in2(this);
                    break;
                case EpdModel.EPD4IN2B:
                    _impl = new EpdFallback();
                    break;
                case EpdModel.EPD7IN5:
                    _impl = new EpdFallback();
                    break;
                case EpdModel.EPD7IN5B:
                    _impl = new EpdFallback();
                    break;
                default:
                    _impl = new EpdFallback();
                    break;
            }

            /* init Gpio Pins */
            var controller = GpioController.GetDefault();

            _reset = controller.OpenPin(ResetPin);
            _reset.SetDriveMode(GpioPinDriveMode.Output);

            _dc = controller.OpenPin(DCPin);
            _dc.SetDriveMode(GpioPinDriveMode.Output);

            //_chipselect = controller.OpenPin(ChipSelectPin);
            //_chipselect.SetDriveMode(GpioPinDriveMode.Output);

            _busy = controller.OpenPin(BusyPin);
            _busy.SetDriveMode(GpioPinDriveMode.Input);

            /* add busy state listener */
            _busy.ValueChanged += _busy_ValueChanged;
        }

        public void connectSPI()
        {
            connectSPIAsync().Wait();
        }

        public async Task connectSPIAsync()
        {
            try
            {
                /* init SPI settings */
                var settings = new SpiConnectionSettings(0);
                settings.ClockFrequency = 2000000;
                settings.Mode = SpiMode.Mode0;

                /* try to get the device */
                var deviceSelector = SpiDevice.GetDeviceSelector();
                IReadOnlyList<DeviceInformation> devices = await DeviceInformation.FindAllAsync(deviceSelector);
                if (devices.Count > 0)
                {
                    _epd_ink_screen = await SpiDevice.FromIdAsync(devices[0].Id, settings);
                    Connected = true;

                    return;
                }
                Connected = false;
            }
            catch (Exception ex)
            {
                Connected = false;
                Debug.WriteLine("SPI connection Failed" + ex.Message);
            }
        }

        public void Init()
        {
            _impl.InitAsync().Wait();
        }

        public void InitAsync()
        {
            _impl.InitAsync();
        }

        private void _busy_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (args.Edge == GpioPinEdge.RisingEdge)
            {
                Debug.WriteLine("Device busy");
                Busy = true;
            }
            else
            {
                Debug.WriteLine("Device released");
                Busy = false;
            }
        }

        ~SPIEpd()
        {
            /* dispose Gpio Pins */
            _reset.Dispose();
            _dc.Dispose();
            //_chipselect.Dispose();
            _busy.Dispose();

            if (_epd_ink_screen != null)
            {
                _epd_ink_screen.Dispose();
            }
        }

        public void Reset()
        {
            _impl.ResetAsync().Wait();
        }

        public void ResetAsync()
        {
            _impl.ResetAsync().Wait();
        }

        internal async Task ResetImplAsync()
        {
            if (!Connected)
                return;

            _reset.Write(GpioPinValue.Low);
            await Task.Delay(200);
            _reset.Write(GpioPinValue.High);
            await Task.Delay(200);
        }

        public void DisplayFrame()
        {
            _impl.DisplayFrameAsync().Wait();
        }

        public void Sleep()
        {
            _impl.SleepAsync().Wait();
        }

        public void SleepAsync()
        {
            _impl.SleepAsync();
        }

        public void SetFrameMemory(byte[] imageBuffer, int imageWidth, int imageHeight)
        {
            _impl.SetFrameMemoryAsync(imageBuffer, imageWidth, imageHeight).Wait();
        }

        public void SetFrameMemoryAsync(byte[] imageBuffer, int imageWidth, int imageHeight)
        {
            SetFrameMemoryAsync(imageBuffer, imageWidth, imageHeight, 0, 0);
        }

        public void SetFrameMemory(byte[] imageBuffer, int imageWidth, int imageHeight, int startX, int startY)
        {
            _impl.SetFrameMemoryAsync(imageBuffer, imageWidth, imageHeight, 0, 0).Wait();
        }

        public void SetFrameMemoryAsync(byte[] imageBuffer, int imageWidth, int imageHeight, int startX, int startY)
        {
            _impl.SetFrameMemoryAsync(imageBuffer, imageWidth, imageHeight, 0, 0);
        }

        public void SetFrameMemory(byte[] imageBuffer)
        {
            _impl.SetFrameMemoryAsync(imageBuffer).Wait();
        }

        public void SetFrameMemoryAsync(byte[] imageBuffer)
        {
            _impl.SetFrameMemoryAsync(imageBuffer);
        }

        public void ClearFrameMemory(byte color)
        {
            _impl.ClearFrameMemoryAsync(color).Wait();
        }

        public void ClearFrameMemoryAsync(byte color)
        {
            _impl.ClearFrameMemoryAsync(color);
        }

        private void SetMemoryArea(int xStart, int yStart, int xEnd, int yEnd)
        {
            _impl.SetMemoryAreaAsync(xStart, yStart, xEnd, yEnd).Wait();
        }

        private void SetMemoryAreaAsync(int xStart, int yStart, int xEnd, int yEnd)
        {
            _impl.SetMemoryAreaAsync(xStart, yStart, xEnd, yEnd);
        }

        private void SetMemoryPointerPosition(int x, int y)
        {
            _impl.SetMemoryPointerPositionAsync(x, y).Wait();
        }

        private void SetMemoryPointerPositionAsync(int x, int y)
        {
            _impl.SetMemoryPointerPositionAsync(x, y);
        }

        internal void SendCommand(byte command)
        {
            if (_epd_ink_screen == null)
            {
                Debug.WriteLine("Device not created");
                return;
            }
            if (!Connected)
            {
                Debug.WriteLine("Device not connected");
                return;
            }

            _dc.Write(GpioPinValue.Low);
            Transfer(command);
        }

        internal void SendData(byte data)
        {
            if (_epd_ink_screen == null)
            {
                Debug.WriteLine("Device not created");
                return;
            }
            if (!Connected)
            {
                Debug.WriteLine("Device not connected");
                return;
            }

            _dc.Write(GpioPinValue.High);
            Transfer(data);
        }

        internal void SendData(byte[] data)
        {
            if (_epd_ink_screen == null)
            {
                Debug.WriteLine("Device not created");
                return;
            }
            if (!Connected)
            {
                Debug.WriteLine("Device not connected");
                return;
            }

            _dc.Write(GpioPinValue.High);
            Transfer(data);
        }

        private void Transfer(byte data)
        {
            if (_epd_ink_screen == null)
            {
                Debug.WriteLine("Device not created");
                return;
            }
            if (!Connected)
            {
                Debug.WriteLine("Device not connected");
                return;
            }

            byte[] dataArray = new byte[] { data };
            //_chipselect.Write(GpioPinValue.Low);
            _epd_ink_screen.Write(dataArray);
            //_chipselect.Write(GpioPinValue.High);
            // Debug.WriteLine("Wrote 1 byte");
        }

        private void Transfer(byte[] data)
        {
            if (_epd_ink_screen == null)
            {
                Debug.WriteLine("Device not created");
                return;
            }
            if (!Connected)
            {
                Debug.WriteLine("Device not connected");
                return;
            }

            //_chipselect.Write(GpioPinValue.Low);
            _epd_ink_screen.Write(data);
            // Debug.WriteLine("Wrote " + data.Length + " bytes");
            //_chipselect.Write(GpioPinValue.High);
        }
    }
}

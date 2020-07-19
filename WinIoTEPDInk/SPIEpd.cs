using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using WinIoTEPDInk;

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

                    // Init
                    await InitAsync();
                    return;
                }
                Connected = false;
            }
            catch (Exception ex)
            {
                Connected = false;
                Debug.WriteLine("SPI Initialization Failed");
            }
        }

        public async Task Init()
        {
            InitAsync();
        }

        public async Task InitAsync()
        {
            await ResetAsync();

            // GD = 0; SM = 0; TB = 0;
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.DRIVER_OUTPUT_CONTROL));
            byte[] temp = new byte[3]
            {
                (byte)((Height - 1) & 0xFF),
                (byte)(((Height - 1) >> 8) & 0xFF),
                0x00
            };
            SendData(temp);

            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.BOOSTER_SOFT_START_CONTROL));
            temp[0] = 0xD7;
            temp[1] = 0xD6;
            temp[2] = 0x9D;
            SendData(temp);

            // VCOM 7C
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.WRITE_VCOM_REGISTER));
            SendData(0xA8);

            // 4 dummy lines per gate
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.SET_DUMMY_LINE_PERIOD));
            SendData(0x1A);

            // 2us per line
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.SET_GATE_TIME));
            SendData(0x08);

            // X increment; Y increment
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.DATA_ENTRY_MODE_SETTING));
            SendData(0x03);

            SetLookupTableRegister(SPIEpdLookupTable.LookupTableFullUpdate);
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
            ResetAsync();
        }

        public async Task ResetAsync()
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
            DisplayFrameAsync();
        }
        public async Task DisplayFrameAsync()
        {
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.DISPLAY_UPDATE_CONTROL_2));
            SendData(0xC4);
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.MASTER_ACTIVATION));
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.TERMINATE_FRAME_READ_WRITE));
            while (Busy)
            {
                await Task.Delay(100);
            }
        }

        public void Sleep()
        {
            SleepAsync();
        }
        public async Task SleepAsync()
        {
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.DEEP_SLEEP_MODE));
            while (Busy)
            {
                await Task.Delay(100);
            }
        }

        public void SetFrameMemory(byte[] imageBuffer, int imageWidth, int imageHeight)
        {
            SetFrameMemory(imageBuffer, imageWidth, imageHeight, 0, 0);
        }

        public void SetFrameMemory(byte[] imageBuffer, int imageWidth, int imageHeight, int startX, int startY)
        {
            throw new NotImplementedException();
        }

        public void SetFrameMemory(byte[] imageBuffer)
        {
            SetFrameMemoryAsync(imageBuffer);
        }

        public async Task SetFrameMemoryAsync(byte[] imageBuffer)
        {
            await SetMemoryAreaAsync(0, 0, Width - 1, Height - 1);
            await SetMemoryPointerPositionAsync(0, 0);
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.WRITE_RAM));
            /* send the image data */
            SendData(imageBuffer);
        }

        public void ClearFrameMemory(byte color)
        {
            ClearFrameMemoryAsync(color);
        }

        public async Task ClearFrameMemoryAsync(byte color)
        {
            await SetMemoryAreaAsync(0, 0, Width - 1, Height - 1);
            await SetMemoryPointerPositionAsync(0, 0);
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.WRITE_RAM));
            /* send the color data */
            for (int i = 0; i < Width / 8 * Height; i++)
            {
                SendData(color);
            }
        }

        private void SetMemoryArea(int xStart, int yStart, int xEnd, int yEnd)
        {
            SetMemoryAreaAsync(xStart, yStart, xEnd, yEnd);
        }

        private async Task SetMemoryAreaAsync(int xStart, int yStart, int xEnd, int yEnd)
        {
            byte[] temp = new byte[2] { (byte)((xStart >> 3) & 0xFF), (byte)((xEnd >> 3) & 0xFF) };
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.SET_RAM_X_ADDRESS_START_END_POSITION));
            SendData(temp);

            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.SET_RAM_Y_ADDRESS_START_END_POSITION));
            temp = new byte[4] 
            {
                (byte)(yStart & 0xFF),
                (byte)((yStart >> 8) & 0xFF),
                (byte)(yEnd & 0xFF),
                (byte)((yEnd >> 8) & 0xFF)
            };
            SendData(temp);
        }

        private void SetMemoryPointerPosition(int x, int y)
        {
            SetMemoryPointerPositionAsync(x, y);
        }

        private async Task SetMemoryPointerPositionAsync(int x, int y)
        {
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.SET_RAM_X_ADDRESS_COUNTER));
            SendData((byte)((x >> 3) & 0xFF));
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.SET_RAM_Y_ADDRESS_COUNTER));
            byte[] temp = new byte[2] { (byte)(y & 0xFF), (byte)((y >> 8) & 0xFF) };
            SendData(temp);

            while (Busy)
            {
                await Task.Delay(100);
            }
        }

        private void SetLookupTableRegister(byte[] lookupTable)
        {
            SendCommand(SPIEpdCommandHelper.GetCommand(Model, SPIEpdCommand.WRITE_LUT_REGISTER));
            SendData(lookupTable);
        }

        private void SendCommand(byte command)
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

        private void SendData(byte data)
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

        private void SendData(byte[] data)
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
            Debug.WriteLine("Wrote 1 byte");
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
            Debug.WriteLine("Wrote " + data.Length + " bytes");
            //_chipselect.Write(GpioPinValue.High);
        }
    }

    internal sealed class SPIEpdCommandHelper
    {
        public static byte GetCommand(EpdModel model, SPIEpdCommand command)
        {
            switch (model)
            {
                case EpdModel.EPD2IN9:
                    switch (command)
                    {
                        case SPIEpdCommand.DRIVER_OUTPUT_CONTROL:
                            return 0x01;
                        case SPIEpdCommand.BOOSTER_SOFT_START_CONTROL:
                            return 0x0C;
                        case SPIEpdCommand.GATE_SCAN_START_POSITION:
                            return 0x0F;
                        case SPIEpdCommand.DEEP_SLEEP_MODE:
                            return 0x10;
                        case SPIEpdCommand.DATA_ENTRY_MODE_SETTING:
                            return 0x11;
                        case SPIEpdCommand.SW_RESET:
                            return 0x12;
                        case SPIEpdCommand.TEMPERATURE_SENSOR_CONTROL:
                            return 0x1A;
                        case SPIEpdCommand.MASTER_ACTIVATION:
                            return 0x20;
                        case SPIEpdCommand.DISPLAY_UPDATE_CONTROL_1:
                            return 0x21;
                        case SPIEpdCommand.DISPLAY_UPDATE_CONTROL_2:
                            return 0x22;
                        case SPIEpdCommand.WRITE_RAM:
                            return 0x24;
                        case SPIEpdCommand.WRITE_VCOM_REGISTER:
                            return 0x2C;
                        case SPIEpdCommand.WRITE_LUT_REGISTER:
                            return 0x32;
                        case SPIEpdCommand.SET_DUMMY_LINE_PERIOD:
                            return 0x3A;
                        case SPIEpdCommand.SET_GATE_TIME:
                            return 0x3B;
                        case SPIEpdCommand.BORDER_WAVEFORM_CONTROL:
                            return 0x3C;
                        case SPIEpdCommand.SET_RAM_X_ADDRESS_START_END_POSITION:
                            return 0x44;
                        case SPIEpdCommand.SET_RAM_Y_ADDRESS_START_END_POSITION:
                            return 0x45;
                        case SPIEpdCommand.SET_RAM_X_ADDRESS_COUNTER:
                            return 0x4E;
                        case SPIEpdCommand.SET_RAM_Y_ADDRESS_COUNTER:
                            return 0x4F;
                        case SPIEpdCommand.TERMINATE_FRAME_READ_WRITE:
                            return 0xFF;
                    }
                    break;
            }
            return 0;
        }
    }

    internal enum SPIEpdCommand
    {
        UNKNOWN = 0,
        DRIVER_OUTPUT_CONTROL,
        BOOSTER_SOFT_START_CONTROL,
        GATE_SCAN_START_POSITION,
        DEEP_SLEEP_MODE,
        DATA_ENTRY_MODE_SETTING,
        SW_RESET,
        TEMPERATURE_SENSOR_CONTROL,
        MASTER_ACTIVATION,
        DISPLAY_UPDATE_CONTROL_1,
        DISPLAY_UPDATE_CONTROL_2,
        WRITE_RAM,
        WRITE_VCOM_REGISTER,
        WRITE_LUT_REGISTER,
        SET_DUMMY_LINE_PERIOD,
        SET_GATE_TIME,
        BORDER_WAVEFORM_CONTROL,
        SET_RAM_X_ADDRESS_START_END_POSITION,
        SET_RAM_Y_ADDRESS_START_END_POSITION,
        SET_RAM_X_ADDRESS_COUNTER,
        SET_RAM_Y_ADDRESS_COUNTER,
        TERMINATE_FRAME_READ_WRITE
    }

    internal class SPIEpdLookupTable
    {
        public static byte[] LookupTableFullUpdate = new byte[]
        {
            0x02, 0x02, 0x01, 0x11, 0x12, 0x12, 0x22, 0x22,
            0x66, 0x69, 0x69, 0x59, 0x58, 0x99, 0x99, 0x88,
            0x00, 0x00, 0x00, 0x00, 0xF8, 0xB4, 0x13, 0x51,
            0x35, 0x51, 0x51, 0x19, 0x01, 0x00
        };

        public static byte[] LookupTablePartialUpdate = new byte[]
        {
            0x10, 0x18, 0x18, 0x08, 0x18, 0x18, 0x08, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x13, 0x14, 0x44, 0x12,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
    }
}

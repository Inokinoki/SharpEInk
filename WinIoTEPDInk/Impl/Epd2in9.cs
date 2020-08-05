using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinIoTEPDInk.Impl
{
    class Epd2in9 : IEpdImplBase
    {
        /**
         * SPI Command
         * 
         */
        private enum SpiCommand
        {
            DRIVER_OUTPUT_CONTROL = 0x01,
            BOOSTER_SOFT_START_CONTROL = 0x0C,
            GATE_SCAN_START_POSITION = 0x0F,
            DEEP_SLEEP_MODE = 0x10,
            DATA_ENTRY_MODE_SETTING = 0x11,
            SW_RESET = 0x12,
            TEMPERATURE_SENSOR_CONTROL = 0x1A,
            MASTER_ACTIVATION = 0x20,
            DISPLAY_UPDATE_CONTROL_1 = 0x21,
            DISPLAY_UPDATE_CONTROL_2 = 0x22,
            WRITE_RAM = 0x24,
            WRITE_VCOM_REGISTER = 0x2C,
            WRITE_LUT_REGISTER = 0x32,
            SET_DUMMY_LINE_PERIOD = 0x3A,
            SET_GATE_TIME = 0x3B,
            BORDER_WAVEFORM_CONTROL = 0x3C,
            SET_RAM_X_ADDRESS_START_END_POSITION = 0x44,
            SET_RAM_Y_ADDRESS_START_END_POSITION = 0x45,
            SET_RAM_X_ADDRESS_COUNTER = 0x4E,
            SET_RAM_Y_ADDRESS_COUNTER = 0x4F,
            TERMINATE_FRAME_READ_WRITE = 0xFF
        }

        /**
         * Lookup Table
         */
        private static byte[] LookupTableFullUpdate = new byte[]
        {
            0x02, 0x02, 0x01, 0x11, 0x12, 0x12, 0x22, 0x22,
            0x66, 0x69, 0x69, 0x59, 0x58, 0x99, 0x99, 0x88,
            0x00, 0x00, 0x00, 0x00, 0xF8, 0xB4, 0x13, 0x51,
            0x35, 0x51, 0x51, 0x19, 0x01, 0x00
        };

        private static byte[] LookupTablePartialUpdate = new byte[]
        {
            0x10, 0x18, 0x18, 0x08, 0x18, 0x18, 0x08, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x13, 0x14, 0x44, 0x12,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        // SPIEpd instance
        private SPIEpd _epd = null;

        public Epd2in9(SPIEpd epd)
        {
            _epd = epd;
        }

        public async Task DisplayFrameAsync()
        {
            _epd.SendCommand((byte)SpiCommand.DISPLAY_UPDATE_CONTROL_2);
            _epd.SendData(0xC4);
            _epd.SendCommand((byte)SpiCommand.MASTER_ACTIVATION);
            _epd.SendCommand((byte)SpiCommand.TERMINATE_FRAME_READ_WRITE);
            while (_epd.Busy)
            {
                await Task.Delay(100);
            }
        }

        public async Task SleepAsync()
        {
            _epd.SendCommand((byte)SpiCommand.DEEP_SLEEP_MODE);
            while (_epd.Busy)
            {
                await Task.Delay(100);
            }
        }

        public async Task SetFrameMemoryAsync(byte[] imageBuffer)
        {
            await SetMemoryAreaAsync(0, 0, _epd.Width - 1, _epd.Height - 1);
            await SetMemoryPointerPositionAsync(0, 0);
            _epd.SendCommand((byte)SpiCommand.WRITE_RAM);
            /* send the image data */
            _epd.SendData(imageBuffer);
        }

        public async Task ClearFrameMemoryAsync(byte color)
        {
            await SetMemoryAreaAsync(0, 0, _epd.Width - 1, _epd.Height - 1);
            await SetMemoryPointerPositionAsync(0, 0);
            _epd.SendCommand((byte)SpiCommand.WRITE_RAM);
            /* send the color data */
            for (int i = 0; i < _epd.Width / 8 * _epd.Height; i++)
            {
                _epd.SendData(color);
            }
        }

        public async Task SetMemoryAreaAsync(int xStart, int yStart, int xEnd, int yEnd)
        {
            byte[] temp = new byte[2] { (byte)((xStart >> 3) & 0xFF), (byte)((xEnd >> 3) & 0xFF) };
            _epd.SendCommand((byte)SpiCommand.SET_RAM_X_ADDRESS_START_END_POSITION);
            _epd.SendData(temp);

            _epd.SendCommand((byte)SpiCommand.SET_RAM_Y_ADDRESS_START_END_POSITION);
            temp = new byte[4]
            {
                (byte)(yStart & 0xFF),
                (byte)((yStart >> 8) & 0xFF),
                (byte)(yEnd & 0xFF),
                (byte)((yEnd >> 8) & 0xFF)
            };
            _epd.SendData(temp);
        }

        public async Task SetMemoryPointerPositionAsync(int x, int y)
        {
            _epd.SendCommand((byte)SpiCommand.SET_RAM_X_ADDRESS_COUNTER);
            _epd.SendData((byte)((x >> 3) & 0xFF));
            _epd.SendCommand((byte)SpiCommand.SET_RAM_Y_ADDRESS_COUNTER);
            byte[] temp = new byte[2] { (byte)(y & 0xFF), (byte)((y >> 8) & 0xFF) };
            _epd.SendData(temp);

            while (_epd.Busy)
            {
                await Task.Delay(100);
            }
        }

        private void SetLookupTableRegister(byte[] lookupTable)
        {
            _epd.SendCommand((byte)SpiCommand.WRITE_LUT_REGISTER);
            _epd.SendData(lookupTable);
        }

        public Task SetFrameMemoryAsync(byte[] imageBuffer, int imageWidth, int imageHeight, int startX, int startY)
        {
            throw new NotImplementedException();
        }

        public Task SetFrameMemoryAsync(byte[] imageBuffer, int imageWidth, int imageHeight)
        {
            throw new NotImplementedException();
        }

        public async Task InitAsync()
        {
            await ResetAsync();

            // GD = 0; SM = 0; TB = 0;
            _epd.SendCommand((byte)SpiCommand.DRIVER_OUTPUT_CONTROL);
            byte[] temp = new byte[3]
            {
                (byte)((_epd.Height - 1) & 0xFF),
                (byte)(((_epd.Height - 1) >> 8) & 0xFF),
                0x00
            };
            _epd.SendData(temp);

            _epd.SendCommand((byte)SpiCommand.BOOSTER_SOFT_START_CONTROL);
            temp[0] = 0xD7;
            temp[1] = 0xD6;
            temp[2] = 0x9D;
            _epd.SendData(temp);

            // VCOM 7C
            _epd.SendCommand((byte)SpiCommand.WRITE_VCOM_REGISTER);
            _epd.SendData(0xA8);

            // 4 dummy lines per gate
            _epd.SendCommand((byte)SpiCommand.SET_DUMMY_LINE_PERIOD);
            _epd.SendData(0x1A);

            // 2us per line
            _epd.SendCommand((byte)SpiCommand.SET_GATE_TIME);
            _epd.SendData(0x08);

            // X increment; Y increment
            _epd.SendCommand((byte)SpiCommand.DATA_ENTRY_MODE_SETTING);
            _epd.SendData(0x03);

            SetLookupTableRegister(LookupTableFullUpdate);
        }

        public async Task ResetAsync()
        {
            await _epd.ResetImplAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinIoTEPDInk.Impl
{
    class Epd4in2 : IEpdImplBase
    {
        /**
         * SPI Command
         * 
         */
        private enum SpiCommand
        {
            PANEL_SETTING = 0x00,
            POWER_SETTING = 0x01,
            POWER_OFF = 0x02,
            POWER_OFF_SEQUENCE_SETTING = 0x03,
            POWER_ON = 0x04,
            POWER_ON_MEASURE = 0x05,
            BOOSTER_SOFT_START = 0x06,
            DEEP_SLEEP = 0x07,
            DATA_START_TRANSMISSION_1 = 0x10,
            DATA_STOP = 0x11,
            DISPLAY_REFRESH = 0x12,
            DATA_START_TRANSMISSION_2 = 0x13,
            LUT_FOR_VCOM = 0x20,
            LUT_WHITE_TO_WHITE = 0x21,
            LUT_BLACK_TO_WHITE = 0x22,
            LUT_WHITE_TO_BLACK = 0x23,
            LUT_BLACK_TO_BLACK = 0x24,
            PLL_CONTROL = 0x30,
            TEMPERATURE_SENSOR_COMMAND = 0x40,
            TEMPERATURE_SENSOR_SELECTION = 0x41,
            TEMPERATURE_SENSOR_WRITE = 0x42,
            TEMPERATURE_SENSOR_READ = 0x43,
            VCOM_AND_DATA_INTERVAL_SETTING = 0x50,
            LOW_POWER_DETECTION = 0x51,
            TCON_SETTING = 0x60,
            RESOLUTION_SETTING = 0x61,
            GSST_SETTING = 0x65,
            GET_STATUS = 0x71,
            AUTO_MEASUREMENT_VCOM = 0x80,
            READ_VCOM_VALUE = 0x81,
            VCM_DC_SETTING = 0x82,
            PARTIAL_WINDOW = 0x90,
            PARTIAL_IN = 0x91,
            PARTIAL_OUT = 0x92,
            PROGRAM_MODE = 0xA0,
            ACTIVE_PROGRAMMING = 0xA1,
            READ_OTP = 0xA2,
            POWER_SAVING = 0xE3
        }

        /*
         * Lookup Table
         */
        private static byte[] LookupTableVCOM0 = new byte[]
        {
            0x00, 0x17, 0x00, 0x00, 0x00, 0x02,
            0x00, 0x17, 0x17, 0x00, 0x00, 0x02,
            0x00, 0x0A, 0x01, 0x00, 0x00, 0x01,
            0x00, 0x0E, 0x0E, 0x00, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };
        private static byte[] LookupTableWW = new byte[]
        {
            0x40, 0x17, 0x00, 0x00, 0x00, 0x02,
            0x90, 0x17, 0x17, 0x00, 0x00, 0x02,
            0x40, 0x0A, 0x01, 0x00, 0x00, 0x01,
            0xA0, 0x0E, 0x0E, 0x00, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };
        private static byte[] LookupTableBW = new byte[]
        {
            0x40, 0x17, 0x00, 0x00, 0x00, 0x02,
            0x90, 0x17, 0x17, 0x00, 0x00, 0x02,
            0x40, 0x0A, 0x01, 0x00, 0x00, 0x01,
            0xA0, 0x0E, 0x0E, 0x00, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };
        private static byte[] LookupTableBB = new byte[]
        {
            0x80, 0x17, 0x00, 0x00, 0x00, 0x02,
            0x90, 0x17, 0x17, 0x00, 0x00, 0x02,
            0x80, 0x0A, 0x01, 0x00, 0x00, 0x01,
            0x50, 0x0E, 0x0E, 0x00, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };
        private static byte[] LookupTableWB = new byte[]
        {
            0x80, 0x17, 0x00, 0x00, 0x00, 0x02,
            0x90, 0x17, 0x17, 0x00, 0x00, 0x02,
            0x80, 0x0A, 0x01, 0x00, 0x00, 0x01,
            0x50, 0x0E, 0x0E, 0x00, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private SPIEpd _epd = null;

        public Epd4in2(SPIEpd epd)
        {
            _epd = epd;
        }

        public async Task DisplayFrameAsync()
        {
            SetLut();
            _epd.SendCommand((byte)SpiCommand.DISPLAY_REFRESH);
            while (_epd.Busy)
            {
                await Task.Delay(100);
            }
        }

        public async Task SleepAsync()
        {
            _epd.SendCommand((byte)SpiCommand.VCOM_AND_DATA_INTERVAL_SETTING);
            _epd.SendData(0x17);                                        //border floating    
            _epd.SendCommand((byte)SpiCommand.VCM_DC_SETTING);          //VCOM to 0V
            _epd.SendCommand((byte)SpiCommand.PANEL_SETTING);
            await Task.Delay(100);

            _epd.SendCommand((byte)SpiCommand.POWER_SETTING);           //VG&VS to 0V fast
            _epd.SendData(0x00);
            _epd.SendData(0x00);
            _epd.SendData(0x00);
            _epd.SendData(0x00);
            _epd.SendData(0x00);
            await Task.Delay(100);

            _epd.SendCommand((byte)SpiCommand.POWER_OFF);          //power off
            while (_epd.Busy)
            {
                await Task.Delay(100);
            }

            _epd.SendCommand((byte)SpiCommand.DEEP_SLEEP);         //deep sleep
            _epd.SendData(0xA5);
        }

        public async Task SetFrameMemoryAsync(byte[] imageBuffer)
        {
            _epd.SendCommand((byte)SpiCommand.RESOLUTION_SETTING);
            _epd.SendData((byte)(_epd.Width >> 8));
            _epd.SendData((byte)(_epd.Width & 0xff));
            _epd.SendData((byte)(_epd.Height >> 8));
            _epd.SendData((byte)(_epd.Height & 0xff));

            _epd.SendCommand((byte)SpiCommand.VCM_DC_SETTING);
            _epd.SendData(0x12);

            _epd.SendCommand((byte)SpiCommand.VCOM_AND_DATA_INTERVAL_SETTING);
            _epd.SendCommand(0x97);    //VBDF 17|D7 VBDW 97  VBDB 57  VBDF F7  VBDW 77  VBDB 37  VBDR B7

            if (imageBuffer.Length > 0)
            {
                _epd.SendCommand((byte)SpiCommand.DATA_START_TRANSMISSION_1);
                for (int i = 0; i < _epd.Width / 8 * _epd.Height; i++)
                {
                    _epd.SendData(0xFF);      // bit set: white, bit reset: black
                }
                await Task.Delay(2);
                _epd.SendCommand((byte)SpiCommand.DATA_START_TRANSMISSION_2);
                _epd.SendData(imageBuffer);
                await Task.Delay(2);
            }

            _epd.SendCommand((byte)SpiCommand.DISPLAY_REFRESH);
            while (_epd.Busy)
            {
                await Task.Delay(100);
            }
        }

        public async Task ClearFrameMemoryAsync(byte color)
        {
            _epd.SendCommand((byte)SpiCommand.RESOLUTION_SETTING);
            _epd.SendData((byte)(_epd.Width >> 8));
            _epd.SendData((byte)(_epd.Width & 0xff));
            _epd.SendData((byte)(_epd.Height >> 8));
            _epd.SendData((byte)(_epd.Height & 0xff));

            _epd.SendCommand((byte)SpiCommand.DATA_START_TRANSMISSION_1);
            await Task.Delay(2);
            for (int i = 0; i < _epd.Width / 8 * _epd.Height; i++)
            {
                _epd.SendData(0xFF);
            }
            await Task.Delay(2);
            _epd.SendCommand((byte)SpiCommand.DATA_START_TRANSMISSION_2);
            await Task.Delay(2);
            for (int i = 0; i < _epd.Width / 8 * _epd.Height; i++)
            {
                _epd.SendData(0xFF);
            }
            await Task.Delay(2);
        }

        public async Task SetMemoryAreaAsync(int xStart, int yStart, int xEnd, int yEnd)
        {
        }

        public async Task SetMemoryPointerPositionAsync(int x, int y)
        {
        }

        private void SetLookupTableRegister(byte[] lookupTable)
        {
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

            _epd.SendCommand((byte)SpiCommand.POWER_SETTING);
            _epd.SendData(0x03);                  // VDS_EN, VDG_EN
            _epd.SendData(0x00);                  // VCOM_HV, VGHL_LV[1], VGHL_LV[0]
            _epd.SendData(0x2b);                  // VDH
            _epd.SendData(0x2b);                  // VDL
            _epd.SendData(0xff);                  // VDHR

            //07 0f 17 1f 27 2F 37 2f
            _epd.SendCommand((byte)SpiCommand.BOOSTER_SOFT_START);
            _epd.SendData(0x17);
            _epd.SendData(0x17);
            _epd.SendData(0x17);

            _epd.SendCommand((byte)SpiCommand.POWER_ON);
            while (_epd.Busy)
            {
                await Task.Delay(100);
            }

            // KW-BF   KWR-AF  BWROTP 0f
            _epd.SendCommand((byte)SpiCommand.PANEL_SETTING);
            _epd.SendData(0xbf);
            _epd.SendData(0x0b);

            // 3A 100HZ   29 150Hz 39 200HZ  31 171HZ
            _epd.SendCommand((byte)SpiCommand.PLL_CONTROL);
            _epd.SendData(0x3c);
        }

        public async Task ResetAsync()
        {
            await _epd.ResetImplAsync();
        }

        private void SetLut()
        {
            _epd.SendCommand((byte)SpiCommand.LUT_FOR_VCOM);        //vcom
            _epd.SendData(LookupTableVCOM0);

            _epd.SendCommand((byte)SpiCommand.LUT_WHITE_TO_WHITE);  //ww --
            _epd.SendData(LookupTableWW);

            _epd.SendCommand((byte)SpiCommand.LUT_BLACK_TO_WHITE);  //bw r
            _epd.SendData(LookupTableBW);

            _epd.SendCommand((byte)SpiCommand.LUT_WHITE_TO_BLACK);  //wb w
            _epd.SendData(LookupTableBB);

            _epd.SendCommand((byte)SpiCommand.LUT_BLACK_TO_BLACK);  //bb b
            _epd.SendData(LookupTableWB);
        }
    }
}

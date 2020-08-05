using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinIoTEPDInk.Impl
{
    interface IEpdImplBase
    {
        Task ClearFrameMemoryAsync(byte color);

        Task DisplayFrameAsync();

        Task SleepAsync();

        Task SetFrameMemoryAsync(byte[] imageBuffer);

        Task SetFrameMemoryAsync(byte[] imageBuffer, int imageWidth, int imageHeight);

        Task SetFrameMemoryAsync(byte[] imageBuffer, int imageWidth, int imageHeight, int startX, int startY);

        Task SetMemoryAreaAsync(int xStart, int yStart, int xEnd, int yEnd);

        Task SetMemoryPointerPositionAsync(int x, int y);

        Task InitAsync();

        Task ResetAsync();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinIoTEPDInk.Impl
{
    class EpdFallback : IEpdImplBase
    {
        public Task ClearFrameMemoryAsync(byte color)
        {
            throw new NotImplementedException();
        }

        public Task DisplayFrameAsync()
        {
            throw new NotImplementedException();
        }

        public Task InitAsync()
        {
            throw new NotImplementedException();
        }

        public Task ResetAsync()
        {
            throw new NotImplementedException();
        }

        public Task SetFrameMemoryAsync(byte[] imageBuffer)
        {
            throw new NotImplementedException();
        }

        public Task SetFrameMemoryAsync(byte[] imageBuffer, int imageWidth, int imageHeight)
        {
            throw new NotImplementedException();
        }

        public Task SetFrameMemoryAsync(byte[] imageBuffer, int imageWidth, int imageHeight, int startX, int startY)
        {
            throw new NotImplementedException();
        }

        public Task SetMemoryAreaAsync(int xStart, int yStart, int xEnd, int yEnd)
        {
            throw new NotImplementedException();
        }

        public Task SetMemoryPointerPositionAsync(int x, int y)
        {
            throw new NotImplementedException();
        }

        public Task SleepAsync()
        {
            throw new NotImplementedException();
        }
    }
}

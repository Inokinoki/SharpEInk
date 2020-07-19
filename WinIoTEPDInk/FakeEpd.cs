using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace WinIoTEPDInk
{
    class FakeEpd : IEpd
    {
        private Image _image = null;

        public int Width { get; }
        public int Height { get; }

        public bool Sleeping { get; }

        private byte[] buffer;

        public FakeEpd(EpdModel model, Image image)
        {
            Width   = EpdResolution.WIDTH[(int)model];
            Height  = EpdResolution.HEIGHT[(int)model];

            buffer = new byte[Width / 8 * Height];  // Create a buffer for image
            for (int i = 0; i < Width / 8 * Height; ++i)
            {
                buffer[i] = 0xFF;
            }

            _image = image;     // EPD preview will be updated to this control
        }

        public void ClearFrameMemory(byte data)
        {
            for (int i = 0; i < Width / 8 * Height; i++)
            {
                buffer[i] = data;
            }
        }

        public async void DisplayFrameAsync()
        {
            if (Sleeping)
            {
                Debug.WriteLine(@"Fake Module sleeping, I'll do nothing");
                return;
            }

            if (_image != null)
            {
                /* Generate gray image */
                byte[] generatedImage = new byte[4 * Width * Height];
                for (int i = 0; i < Width * Height; ++i)
                {
                    if ((buffer[i / 8] & (0x80 >> (i % 8))) == 0)
                    {
                        generatedImage[4 * i] = 0;
                        generatedImage[4 * i + 1] = 0;
                        generatedImage[4 * i + 2] = 0;
                        generatedImage[4 * i + 3] = 255;
                    }
                    else
                    {
                        generatedImage[4 * i] = 255;
                        generatedImage[4 * i + 1] = 255;
                        generatedImage[4 * i + 2] = 255;
                        generatedImage[4 * i + 3] = 255;
                    }
                }

                IStorageFolder applicationfolder = ApplicationData.Current.LocalFolder;
                Debug.WriteLine(applicationfolder.Path);
                IStorageFile saveFile = await applicationfolder.CreateFileAsync("test.png", CreationCollisionOption.ReplaceExisting);
                using (var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                    encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore,
                       (uint)Width, (uint)Height, 75, 75, generatedImage.ToArray());
                    await encoder.FlushAsync();
                }
                
                /* Create a bitmap source */
                Stream s = await applicationfolder.OpenStreamForReadAsync("test.png");
                BitmapImage imageSource = new BitmapImage();
                await imageSource.SetSourceAsync(s.AsRandomAccessStream());

                BitmapImage One = new BitmapImage(new Uri("ms-appx:///Assets/StoreLogo.png"));

                /* Update image to the control */
                _image.Source = imageSource;
                _image.Width = Width;
                _image.Height = Height;
            }
        }

        public void Reset()
        {
            if (Sleeping)
            {
                Debug.WriteLine(@"Fake Module sleeping, I'll do nothing");
                return;
            }

            Debug.WriteLine(@"Fake Module reset");

            // FIXME: what should happen when reset
        }

        public void SetFrameMemory(byte[] imageBuffer, int imageWidth, int imageHeight, int startX, int startY)
        {
            if (Sleeping)
            {
                Debug.WriteLine(@"Fake Module sleeping, I'll do nothing");
                return;
            }

            int xEnd, yEnd;
            int offset = startX % 8;

            if (imageBuffer.Length <= 0 || startX < 0 || imageWidth < 0 || startY < 0 || imageHeight < 0)
            {
                return;
            }
            
            /* x point must be the multiple of 8 or the last 3 bits will be ignored */
            startX &= 0xF8;
            imageWidth &= 0xF8;

            /* validate x and width */
            if (startX + imageWidth >= Width)
            {
                xEnd = Width - 1;
            }
            else
            {
                xEnd = startX + imageWidth - 1;
            }

            /* validate y and height */
            if (startY + imageHeight >= Height)
            {
                yEnd = Height - 1;
            }
            else
            {
                yEnd = startY + imageHeight - 1;
            }

            /* set image data */
            for (int j = 0; j < yEnd - startY + 1; j++)
            {
                for (int i = 0; i < (xEnd - startX + 1) / 8; i++)
                {
                    if (i == 0)
                    {
                        /* clear offset bits */
                        buffer[startX / 8 + (imageWidth / 8) * (startY + j)] &= (byte)(0xFF << offset);

                        /* assign offset bits */
                        buffer[startX / 8 + (imageWidth / 8) * (startY + j)] |= (byte)(imageBuffer[i + (startY + j) * (imageWidth / 8)] >> (offset == 0 ? 0 : 8 - offset));
                    }
                    else
                    {
                        if (offset == 0)
                        {
                            // Normal assignment
                            buffer[startX / 8 + i + (imageWidth / 8) * (startY + j)] = imageBuffer[i + (startY + j) * (imageWidth / 8)];
                        }
                        else
                        {
                            buffer[startX / 8 + i + (imageWidth / 8) * (startY + j)] =
                                (byte)((imageBuffer[i + (startY + j) * (imageWidth / 8)] << offset) | (imageBuffer[i + 1 + (startY + j) * (imageWidth / 8)] >> (8 - offset)));
                        }
                    }
                }
            }
        }

        public void SetFrameMemory(byte[] imageBuffer, int imageWidth, int imageHeight)
        {
            if (Sleeping)
            {
                Debug.WriteLine(@"Fake Module sleeping, I'll do nothing");
                return;
            }

            SetFrameMemory(imageBuffer, imageWidth, imageHeight, 0, 0);
        }

        public void SetFrameMemory(byte[] imageBuffer)
        {
            if (Sleeping)
            {
                Debug.WriteLine(@"Fake Module sleeping, I'll do nothing");
                return;
            }

            for (int i = 0; i < Width / 8 * Height && i < imageBuffer.Length; i++)
            {
                // Fill the buffer
                buffer[i] = imageBuffer[i];
            }
        }

        public void Sleep()
        {
            Debug.WriteLine(@"Fake Module sleeping...");
        }
    }
}

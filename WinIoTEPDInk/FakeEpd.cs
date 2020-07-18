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
                    generatedImage[4 * i] = 255;
                    generatedImage[4 * i + 1] = 255;
                    generatedImage[4 * i + 2] = 255;
                    generatedImage[4 * i + 3] = 255;
                    if ((buffer[i / 8] & (0x80 >> (i % 8))) == 1)
                    {
                        generatedImage[4 * i] = 0;
                        generatedImage[4 * i + 1] = 0;
                        generatedImage[4 * i + 2] = 0;
                        generatedImage[4 * i + 3] = 0;
                    }
                    else
                    {
                        generatedImage[4 * i] = 255;
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

                /* Create a stream to store encoded bitmap data */
                //IRandomAccessStream stream = new InMemoryRandomAccessStream();
                //stream.Size = (ulong)(4 * Width * Height);

                // Encode image
                //var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                //encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore,
                //    (uint)Width, (uint)Height, 75, 75, generatedImage.ToArray());
                //await stream.FlushAsync();
                //stream.Seek(0);

                //await stream.WriteAsync(generatedImage.AsBuffer());
                //stream.Seek(0);

                //Debug.WriteLine(stream.Size);
                Stream s = await applicationfolder.OpenStreamForReadAsync("test.png");

                /* Create a bitmap source */
                BitmapImage imageSource = new BitmapImage();
                await imageSource.SetSourceAsync(s.AsRandomAccessStream());
                //imageSource.SetSource(stream);
                // await imageSource.SetSourceAsync(stream);

                BitmapImage One = new BitmapImage(new Uri("ms-appx:///Assets/StoreLogo.png"));

                /* Update image to the control */
                _image.Source = imageSource;
                _image.Width = Width;
                _image.Height = Height;
                Debug.WriteLine("Loaded to image OK");
                Debug.WriteLine(_image.Name);
                Debug.WriteLine(Width);
                Debug.WriteLine(Height);
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

        public void SetFrameMemory(byte[] image_buffer, int startX, int startY, int image_width, int image_height)
        {
            if (Sleeping)
            {
                Debug.WriteLine(@"Fake Module sleeping, I'll do nothing");
                return;
            }

        }

        public void SetFrameMemory(byte[] image_buffer, int image_width, int image_height)
        {
            if (Sleeping)
            {
                Debug.WriteLine(@"Fake Module sleeping, I'll do nothing");
                return;
            }

            SetFrameMemory(image_buffer, 0, 0, image_width, image_height);
        }

        public void SetFrameMemory(byte[] image_buffer)
        {
            if (Sleeping)
            {
                Debug.WriteLine(@"Fake Module sleeping, I'll do nothing");
                return;
            }

            for (int i = 0; i < Width / 8 * Height && i < image_buffer.Length; i++)
            {
                // Fill the buffer
                buffer[i] = image_buffer[i];
            }
        }

        public void Sleep()
        {
            Debug.WriteLine(@"Fake Module sleeping...");
        }
    }
}

using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Linq;

namespace TP_PDI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SubmitImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Filter = "Image files|*.jpg;*.png",
                FilterIndex = 1,
            };
            if (dialog.ShowDialog() == true)
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri(dialog.FileName));
                imagePicture.Source = bitmapImage;

                BitmapSource bitmapSource = ConvertToGrayScale(bitmapImage);
                grayScaleImagePicture.Source = bitmapSource;

                int[] histogram = CalculateHistogram(bitmapImage);
                DrawHistogram(histogram);

                BitmapSource negativeBitmapSource = ConvertToNegative(bitmapSource);
                // resultImage.Source = negativeBitmapSource;

                BitmapSource meanBitmapSource = MeanFilter(bitmapSource);
                //  resultImage.Source = meanBitmapSource;

                BitmapSource logaritmSource = LogarithmicFilter(bitmapSource);
                //resultImage.Source = logaritmSource;

                BitmapSource inverselogaritmSource = InverseLogaritmFilter(bitmapSource);
                //resultImage.Source = inverselogaritmSource;

                BitmapSource medianSource = MedianFilter(bitmapSource);
            //  resultImage.Source = medianSource;

                BitmapSource modeSource = ModeFilter(bitmapSource);
            //  resultImage.Source = modeSource;

                BitmapSource minSource = MinFilter(bitmapSource);
            //  resultImage.Source = minSource;

                BitmapSource maxSource = MaxFilter(bitmapSource);
           //   resultImage.Source = maxSource;
            }
        }

        private BitmapSource ConvertToGrayScale(BitmapImage image)
        {
            WriteableBitmap writeableBitmap = new WriteableBitmap(image);

            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;
            int[] pixels = new int[width * height];
            writeableBitmap.CopyPixels(pixels, width * 4, 0);

            for (int i = 0; i < pixels.Length; i++)
            {
                byte a = (byte)((pixels[i] >> 24) & 0xff); // Alpha
                byte r = (byte)((pixels[i] >> 16) & 0xff); // Red
                byte g = (byte)((pixels[i] >> 8) & 0xff);  // Green
                byte b = (byte)(pixels[i] & 0xff);         // Blue

                byte gray = (byte)(0.299 * r + 0.587 * g + 0.114 * b);

                pixels[i] = (a << 24) | (gray << 16) | (gray << 8) | gray;
            }

            WriteableBitmap grayWriteableBitmap = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            grayWriteableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            return grayWriteableBitmap;
        }
        private BitmapSource ConvertToNegative(BitmapSource image)
        {

            WriteableBitmap writeableBitmap = new WriteableBitmap(image);


            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;

            int[] pixels = new int[width * height];
            writeableBitmap.CopyPixels(pixels, width * 4, 0);

            for (int i = 0; i < pixels.Length; i++)
            {
                byte a = (byte)((pixels[i] >> 24) & 0xff); // Alpha (transparência)
                byte r = (byte)((pixels[i] >> 16) & 0xff); // Red
                byte g = (byte)((pixels[i] >> 8) & 0xff);  // Green
                byte b = (byte)(pixels[i] & 0xff);         // Blue

                byte negativeR = (byte)(255 - r);
                byte negativeG = (byte)(255 - g);
                byte negativeB = (byte)(255 - b);


                pixels[i] = (a << 24) | (negativeR << 16) | (negativeG << 8) | negativeB;
            }


            WriteableBitmap negativeWriteableBitmap = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            negativeWriteableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);


            return negativeWriteableBitmap;
        }
        private BitmapSource MeanFilter(BitmapSource image)
        {

            WriteableBitmap writeableBitmap = new WriteableBitmap(image);

            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;


            int[] originalPixels = new int[width * height];
            int[] filteredPixels = new int[width * height];
            writeableBitmap.CopyPixels(originalPixels, width * 4, 0);
            int kernelSize = 3;
            int radius = kernelSize / 2;


            for (int y = radius; y < height - radius; y++)
            {
                for (int x = radius; x < width - radius; x++)
                {
                    int rSum = 0, gSum = 0, bSum = 0;
                    int pixelCount = 0;


                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int pixel = originalPixels[(y + ky) * width + (x + kx)];
                            byte r = (byte)((pixel >> 16) & 0xff);
                            byte g = (byte)((pixel >> 8) & 0xff);
                            byte b = (byte)(pixel & 0xff);

                            rSum += r;
                            gSum += g;
                            bSum += b;
                            pixelCount++;
                        }
                    }


                    byte rAvg = (byte)(rSum / pixelCount);
                    byte gAvg = (byte)(gSum / pixelCount);
                    byte bAvg = (byte)(bSum / pixelCount);


                    filteredPixels[y * width + x] = (255 << 24) | (rAvg << 16) | (gAvg << 8) | bAvg;
                }
            }


            WriteableBitmap filteredBitmap = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            filteredBitmap.WritePixels(new Int32Rect(0, 0, width, height), filteredPixels, width * 4, 0);

            return filteredBitmap;
        }
        private BitmapSource LogarithmicFilter(BitmapSource image)
        {

            double c = 255 / Math.Log(1 + 255);


            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;


            int[] originalPixels = new int[width * height];
            int[] logPixels = new int[width * height];
            writeableBitmap.CopyPixels(originalPixels, width * 4, 0);


            for (int i = 0; i < originalPixels.Length; i++)
            {
                byte a = (byte)((originalPixels[i] >> 24) & 0xff);
                byte r = (byte)((originalPixels[i] >> 16) & 0xff);
                byte g = (byte)((originalPixels[i] >> 8) & 0xff);
                byte b = (byte)(originalPixels[i] & 0xff);


                byte logR = (byte)(c * Math.Log(1 + r));
                byte logG = (byte)(c * Math.Log(1 + g));
                byte logB = (byte)(c * Math.Log(1 + b));


                logPixels[i] = (a << 24) | (logR << 16) | (logG << 8) | logB;
            }


            WriteableBitmap logBitmap = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            logBitmap.WritePixels(new Int32Rect(0, 0, width, height), logPixels, width * 4, 0);

            return logBitmap;
        }
        private BitmapSource InverseLogaritmFilter(BitmapSource image)
        {

            double c = 255 / (Math.Exp(1) - 1);


            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;


            int[] originalPixels = new int[width * height];
            int[] expPixels = new int[width * height];
            writeableBitmap.CopyPixels(originalPixels, width * 4, 0);


            for (int i = 0; i < originalPixels.Length; i++)
            {
                byte a = (byte)((originalPixels[i] >> 24) & 0xff);
                byte r = (byte)((originalPixels[i] >> 16) & 0xff);
                byte g = (byte)((originalPixels[i] >> 8) & 0xff);
                byte b = (byte)(originalPixels[i] & 0xff);


                byte expR = (byte)(c * (Math.Exp(r / 255.0) - 1));
                byte expG = (byte)(c * (Math.Exp(g / 255.0) - 1));
                byte expB = (byte)(c * (Math.Exp(b / 255.0) - 1));


                expPixels[i] = (a << 24) | (expR << 16) | (expG << 8) | expB;
            }


            WriteableBitmap expBitmap = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            expBitmap.WritePixels(new Int32Rect(0, 0, width, height), expPixels, width * 4, 0);

            return expBitmap;
        }
        private BitmapSource MedianFilter(BitmapSource image)
        {

            int kernelSize = 3;
            int radius = kernelSize / 2;


            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;


            int[] originalPixels = new int[width * height];
            int[] medianPixels = new int[width * height];
            writeableBitmap.CopyPixels(originalPixels, width * 4, 0);


            for (int y = radius; y < height - radius; y++)
            {
                for (int x = radius; x < width - radius; x++)
                {
                    List<byte> rList = new List<byte>();
                    List<byte> gList = new List<byte>();
                    List<byte> bList = new List<byte>();


                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int pixel = originalPixels[(y + ky) * width + (x + kx)];
                            byte r = (byte)((pixel >> 16) & 0xff);
                            byte g = (byte)((pixel >> 8) & 0xff);
                            byte b = (byte)(pixel & 0xff);

                            rList.Add(r);
                            gList.Add(g);
                            bList.Add(b);
                        }
                    }


                    rList.Sort();
                    gList.Sort();
                    bList.Sort();
                    byte rMedian = rList[rList.Count / 2];
                    byte gMedian = gList[gList.Count / 2];
                    byte bMedian = bList[bList.Count / 2];


                    medianPixels[y * width + x] = (255 << 24) | (rMedian << 16) | (gMedian << 8) | bMedian;
                }
            }


            WriteableBitmap medianBitmap = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            medianBitmap.WritePixels(new Int32Rect(0, 0, width, height), medianPixels, width * 4, 0);

            return medianBitmap;
        }
        private BitmapSource ModeFilter(BitmapSource image)
        {

            int kernelSize = 3;
            int radius = kernelSize / 2;


            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;


            int[] originalPixels = new int[width * height];
            int[] modePixels = new int[width * height];
            writeableBitmap.CopyPixels(originalPixels, width * 4, 0);


            for (int y = radius; y < height - radius; y++)
            {
                for (int x = radius; x < width - radius; x++)
                {
                    Dictionary<byte, int> rDict = new Dictionary<byte, int>();
                    Dictionary<byte, int> gDict = new Dictionary<byte, int>();
                    Dictionary<byte, int> bDict = new Dictionary<byte, int>();


                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int pixel = originalPixels[(y + ky) * width + (x + kx)];
                            byte r = (byte)((pixel >> 16) & 0xff);
                            byte g = (byte)((pixel >> 8) & 0xff);
                            byte b = (byte)(pixel & 0xff);


                            if (rDict.ContainsKey(r)) rDict[r]++;
                            else rDict[r] = 1;
                            if (gDict.ContainsKey(g)) gDict[g]++;
                            else gDict[g] = 1;
                            if (bDict.ContainsKey(b)) bDict[b]++;
                            else bDict[b] = 1;
                        }
                    }


                    byte rMode = rDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                    byte gMode = gDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                    byte bMode = bDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;


                    modePixels[y * width + x] = (255 << 24) | (rMode << 16) | (gMode << 8) | bMode;
                }
            }


            WriteableBitmap modeBitmap = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            modeBitmap.WritePixels(new Int32Rect(0, 0, width, height), modePixels, width * 4, 0);

            return modeBitmap;
        }
        private BitmapSource MinFilter(BitmapSource image)
        {
            int kernelSize = 3;
            int radius = kernelSize / 2;

            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;

            int[] originalPixels = new int[width * height];
            int[] minPixels = new int[width * height];
            writeableBitmap.CopyPixels(originalPixels, width * 4, 0);

            for (int y = radius; y < height - radius; y++)
            {
                for (int x = radius; x < width - radius; x++)
                {
                    List<byte> rList = new List<byte>();
                    List<byte> gList = new List<byte>();
                    List<byte> bList = new List<byte>();

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int pixel = originalPixels[(y + ky) * width + (x + kx)];
                            byte r = (byte)((pixel >> 16) & 0xff);
                            byte g = (byte)((pixel >> 8) & 0xff);
                            byte b = (byte)(pixel & 0xff);

                            rList.Add(r);
                            gList.Add(g);
                            bList.Add(b);
                        }
                    }


                    byte rMin = rList.Min();
                    byte gMin = gList.Min();
                    byte bMin = bList.Min();

                    minPixels[y * width + x] = (255 << 24) | (rMin << 16) | (gMin << 8) | bMin;
                }
            }

            WriteableBitmap minBitmap = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            minBitmap.WritePixels(new Int32Rect(0, 0, width, height), minPixels, width * 4, 0);

            return minBitmap;
        }
        private BitmapSource MaxFilter(BitmapSource image)
        {

            int kernelSize = 3;
            int radius = kernelSize / 2;


            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;


            int[] originalPixels = new int[width * height];
            int[] maxPixels = new int[width * height];
            writeableBitmap.CopyPixels(originalPixels, width * 4, 0);


            for (int y = radius; y < height - radius; y++)
            {
                for (int x = radius; x < width - radius; x++)
                {

                    List<byte> rList = new List<byte>();
                    List<byte> gList = new List<byte>();
                    List<byte> bList = new List<byte>();


                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int pixel = originalPixels[(y + ky) * width + (x + kx)];
                            byte r = (byte)((pixel >> 16) & 0xff);
                            byte g = (byte)((pixel >> 8) & 0xff);
                            byte b = (byte)(pixel & 0xff);


                            rList.Add(r);
                            gList.Add(g);
                            bList.Add(b);
                        }
                    }


                    byte rMax = rList.Max();
                    byte gMax = gList.Max();
                    byte bMax = bList.Max();


                    maxPixels[y * width + x] = (255 << 24) | (rMax << 16) | (gMax << 8) | bMax;
                }
            }


            WriteableBitmap maxBitmap = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            maxBitmap.WritePixels(new Int32Rect(0, 0, width, height), maxPixels, width * 4, 0);

            return maxBitmap;
        }

        private int[] CalculateHistogram(BitmapSource bitmapSource)
        {
            int[] histogram = new int[256];

            WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapSource);
            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;
            byte[] pixels = new byte[width * height * 4];

            writeableBitmap.CopyPixels(pixels, width * 4, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte gray = pixels[i]; 
                histogram[gray]++;
            }

            return histogram;
        }
        private void DrawHistogram(int[] histogram)
        {
            
            histogramCanvas.Children.Clear();

            
            int max = histogram.Max();

           
            double scale = 200.0 / max;

            for (int i = 0; i < histogram.Length; i++)
            {
                
                Rectangle bar = new Rectangle
                {
                    Width = 2,
                    Height = histogram[i] * scale,
                    Fill = System.Windows.Media.Brushes.Black
                };

                
                Canvas.SetLeft(bar, i * 3); 
                Canvas.SetBottom(bar, 0);

                
                histogramCanvas.Children.Add(bar);
            }
        }


    }
}

using System.Windows;
using System.Windows.Media.Imaging;
using TP_PDI.Interfaces;
using System.Drawing.Imaging;
using System.IO;
using SixLabors.ImageSharp.PixelFormats;
using System.Windows.Media;
using SixLabors.ImageSharp;
using Microsoft.Win32;
using SixLabors.ImageSharp.Processing;
using TP_PDI.Enuns;

namespace TP_PDI.Entities
{
    public class ImageProcessor : IImageProcessor
    {
        public BitmapImage? BitmapImage { get; set; }
        public BitmapImage? GrayScaleImage { get; set; }
        public WriteableBitmap? ResultImage { get; set; }

        public void GetGrayScale(OpenFileDialog dialog)
        {
            using var image = Image.Load<Rgba32>(dialog.FileName);
            image.Mutate(x => x.Grayscale());

            using var ms = new MemoryStream();
            image.SaveAsBmp(ms);
            ms.Seek(0, SeekOrigin.Begin);

            BitmapImage grayScaleImage = new();
            grayScaleImage.BeginInit();
            grayScaleImage.StreamSource = ms;
            grayScaleImage.CacheOption = BitmapCacheOption.OnLoad;
            grayScaleImage.EndInit();

            GrayScaleImage = grayScaleImage;
        }

        public BitmapSource ConvertToNegative()
        {
            WriteableBitmap writeableBitmap = new(GrayScaleImage);
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

        public BitmapSource MeanFilter()
        {

            WriteableBitmap writeableBitmap = new WriteableBitmap(GrayScaleImage);

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


            ResultImage = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            ResultImage.WritePixels(new Int32Rect(0, 0, width, height), filteredPixels, width * 4, 0);

            return ResultImage;
        }

        public BitmapSource LogarithmicFilter()
        {

            double c = 255 / Math.Log(1 + 255);


            WriteableBitmap writeableBitmap = new WriteableBitmap(GrayScaleImage);
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


            ResultImage = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            ResultImage.WritePixels(new Int32Rect(0, 0, width, height), logPixels, width * 4, 0);

            return ResultImage;
        }

        public BitmapSource InverseLogaritmFilter()
        {

            double c = 255 / (Math.Exp(1) - 1);


            WriteableBitmap writeableBitmap = new WriteableBitmap(GrayScaleImage);
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


            ResultImage = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            ResultImage.WritePixels(new Int32Rect(0, 0, width, height), expPixels, width * 4, 0);

            return ResultImage;
        }

        public BitmapSource MedianFilter()
        {

            int kernelSize = 3;
            int radius = kernelSize / 2;


            WriteableBitmap writeableBitmap = new WriteableBitmap(GrayScaleImage);
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


            ResultImage = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            ResultImage.WritePixels(new Int32Rect(0, 0, width, height), medianPixels, width * 4, 0);

            return ResultImage;
        }

        public BitmapSource ModeFilter()
        {

            int kernelSize = 3;
            int radius = kernelSize / 2;


            WriteableBitmap writeableBitmap = new WriteableBitmap(GrayScaleImage);
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


            ResultImage = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            ResultImage.WritePixels(new Int32Rect(0, 0, width, height), modePixels, width * 4, 0);

            return ResultImage;
        }

        public BitmapSource MinFilter()
        {
            int kernelSize = 3;
            int radius = kernelSize / 2;

            WriteableBitmap writeableBitmap = new WriteableBitmap(GrayScaleImage);
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

            ResultImage = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            ResultImage.WritePixels(new Int32Rect(0, 0, width, height), minPixels, width * 4, 0);

            return ResultImage;
        }

        public BitmapSource MaxFilter()
        {

            int kernelSize = 3;
            int radius = kernelSize / 2;


            WriteableBitmap writeableBitmap = new WriteableBitmap(GrayScaleImage);
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


            ResultImage = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            ResultImage.WritePixels(new Int32Rect(0, 0, width, height), maxPixels, width * 4, 0);

            return ResultImage;
        }

        public BitmapSource ExpansionFilter()
        {
            throw new NotImplementedException();
        }

        public List<TransformedBitmap> DegreesFilter(int degrees)
        {
            if (BitmapImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            int[] positiveAndNegativeDegrees = [
                degrees,
                degrees * - 1
            ];

            List<TransformedBitmap> tranformedImages = [];

            foreach (int value in positiveAndNegativeDegrees)
                tranformedImages.Add(new TransformedBitmap(GrayScaleImage, new RotateTransform(value)));


            return tranformedImages;
        }

        public TransformedBitmap MirroringFilter(EProcess mirroringProcess)
        {
            if (BitmapImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            TransformedBitmap transformedImage;

            if(mirroringProcess == EProcess.Vertical)
                transformedImage = new(GrayScaleImage, new ScaleTransform(1, -1));
            else 
                transformedImage = new(GrayScaleImage, new ScaleTransform(-1, 1));

            return transformedImage;
        }

        #region Métodos privados
        private static int[] GetValuesFromInputs(string input)
        {

            char separator = input.Contains('x') ? 'x' : ',';
            
            string[] stringValues = input.Split(separator);

            int[] values = new int[stringValues.Length];

            for(int i = 0; i < stringValues.Length; i++)
            {
                if (int.TryParse(stringValues[i], out int result))
                    values[i] = result;
                else
                    throw new Exception("Erro ao recuperar o valor da string. Confira se está digitando corretamente.");
            }

            return values;
        }

        private BitmapSource ConvertToBitmapSource(Image<Rgba32> image)
        {
            using var ms = new MemoryStream();

            image.SaveAsBmp(ms);
            ms.Seek(0, SeekOrigin.Begin);

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            return bitmap;
        }
        #endregion
    }
}

using System.Windows;
using System.Windows.Media.Imaging;
using TP_PDI.Interfaces;
using System.Drawing.Imaging;
using System.IO;
using SixLabors.ImageSharp.PixelFormats;
using System.Windows.Media;
using Microsoft.Win32;
using SixLabors.ImageSharp.Processing;
using TP_PDI.Enuns;
using SixLabors.ImageSharp;

namespace TP_PDI.Entities
{
    public class ImageProcessor : IImageProcessor
    {
        public BitmapImage? BitmapImage { get; set; }
        public BitmapImage? GrayScaleImage { get; set; }

        public BitmapSource NegativeFilter()
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

        public BitmapSource PowerAndRootFilter(double gamma)
        {
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Gray8, null, 0);
            int width = source.PixelWidth, height = source.PixelHeight, stride = width;

            byte[] pixels = new byte[width * height];
            source.CopyPixels(pixels, stride, 0);

            byte[] outputPixels = new byte[pixels.Length];
            double c = 255.0 / Math.Pow(255, gamma);

            for (int i = 0; i < pixels.Length; i++)
            {
                double powerValue = c * Math.Pow(pixels[i], gamma);
                outputPixels[i] = (byte)Math.Clamp(powerValue, 0, 255); // Garantir que o valor esteja entre 0 e 255
            }

            WriteableBitmap resultBitmap = new(width, height, source.DpiX, source.DpiY, PixelFormats.Gray8, null);
            resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), outputPixels, stride, 0);

            return resultBitmap;
        }

        public BitmapSource LaplacianFilter()
        {
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            var writableBitmap = new WriteableBitmap(GrayScaleImage);
            int width = writableBitmap.PixelWidth, height = writableBitmap.PixelHeight;

            byte[] newPixels = new byte[width * height];

            writableBitmap.Lock();
            unsafe
            {
                byte* pixels = (byte*)writableBitmap.BackBuffer;

                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int laplacianValue = -pixels[(y - 1) * width + (x - 1)] +
                                             -pixels[(y - 1) * width + x] +
                                             -pixels[(y - 1) * width + (x + 1)] +
                                             -pixels[y * width + (x - 1)] +
                                             8 * pixels[y * width + x] +
                                             -pixels[y * width + (x + 1)] +
                                             -pixels[(y + 1) * width + (x - 1)] +
                                             -pixels[(y + 1) * width + x] +
                                             -pixels[(y + 1) * width + (x + 1)];

                        newPixels[y * width + x] = (byte)Math.Clamp(laplacianValue, 0, 255);
                    }
                }
            }

            writableBitmap.Unlock();
            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, newPixels, width);
        }

        public BitmapSource HighBoostFilter()
        {
            if (GrayScaleImage == null) throw new InvalidOperationException("A imagem em níveis de cinza deve estar definida.");

            WriteableBitmap writableBitmap = new (GrayScaleImage);
            int width = writableBitmap.PixelWidth, height = writableBitmap.PixelHeight;

            WriteableBitmap resultBitmap = new (width, height, writableBitmap.DpiX, writableBitmap.DpiY, PixelFormats.Gray8, null);

            int k = 1, kernelSum = 16; ; 
            int[,] lowPassKernel = {
                { 1, 2, 1 },
                { 2, 4, 2 },
                { 1, 2, 1 }
            };

            byte[] pixels = new byte[width * height];
            writableBitmap.CopyPixels(pixels, writableBitmap.BackBufferStride, 0);

            byte[] resultPixels = new byte[width * height];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int lowFrequencyValue = 0;

                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                            lowFrequencyValue += pixels[(x + kx) + (y + ky) * width] * lowPassKernel[ky + 1, kx + 1];
                    }

                    int originalValue = pixels[x + y * width], highBoostValue = originalValue + k * (originalValue - lowFrequencyValue / kernelSum);

                    resultPixels[x + y * width] = (byte)Math.Max(0, Math.Min(255, highBoostValue));
                }
            }
            resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width, 0);
            return resultBitmap;
        }

        public BitmapSource LogarithmicFilter()
        {
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Gray8, null, 0);
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width;
            byte[] pixels = new byte[width * height];

            source.CopyPixels(pixels, stride, 0);

            byte[] outputPixels = new byte[pixels.Length];
            double c = 255.0 / Math.Log(1 + 255);

            for (int i = 0; i < pixels.Length; i++)
            {
                outputPixels[i] = (byte)(c * Math.Log(1 + pixels[i]));
            }

            WriteableBitmap resultBitmap = new(width, height, source.DpiX, source.DpiY, PixelFormats.Gray8, null);
            resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), outputPixels, stride, 0);

            return resultBitmap;
        }

        public BitmapSource InverseLogarithmFilter()
        {
            if (GrayScaleImage == null) return null;

            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Gray8, null, 0);
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width;
            byte[] pixels = new byte[width * height];

            source.CopyPixels(pixels, stride, 0);

            byte[] outputPixels = new byte[pixels.Length];
            double c = 255.0; 

            for (int i = 0; i < pixels.Length; i++)
            {
                outputPixels[i] = (byte)(c / (1 + Math.Log(1 + pixels[i])));
            }

            WriteableBitmap resultBitmap = new(width, height, source.DpiX, source.DpiY, PixelFormats.Gray8, null);
            resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), outputPixels, stride, 0);

            return resultBitmap;
        }

        public BitmapSource MeanFilter(string mask)
        {
            WriteableBitmap writeableBitmap = new WriteableBitmap(GrayScaleImage);

            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;

            int[] originalPixels = new int[width * height];
            int[] filteredPixels = new int[width * height];
            writeableBitmap.CopyPixels(originalPixels, width * 4, 0);
            int kernelSize = GetValuesFromInput(mask)[0];
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

            WriteableBitmap result = new (width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), filteredPixels, width * 4, 0);

            return result;
        }

        public BitmapSource MedianFilter(string mask)
        {
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Gray8, null, 0);

            int filterSize = GetValuesFromInput(mask)[0], width = source.PixelWidth, height = source.PixelHeight, stride = width;
            
            byte[] pixels = new byte[width * height];

            source.CopyPixels(pixels, stride, 0);
            byte[] outputPixels = new byte[pixels.Length];

            int halfSize = filterSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    List<byte> neighbors = new List<byte>();

                    for (int j = -halfSize; j <= halfSize; j++)
                    {
                        for (int i = -halfSize; i <= halfSize; i++)
                        {
                            int neighborX = x + i;
                            int neighborY = y + j;

                            if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                            {
                                neighbors.Add(pixels[neighborY * stride + neighborX]);
                            }
                        }
                    }
                    neighbors.Sort();
                    outputPixels[y * stride + x] = neighbors[neighbors.Count / 2];
                }
            }

            WriteableBitmap resultBitmap = new(width, height, source.DpiX, source.DpiY, PixelFormats.Gray8, null);
            resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), outputPixels, stride, 0);

            return resultBitmap;
        }

        public BitmapSource ModeFilter(string mask)
        {
            int kernelSize = GetValuesFromInput(mask)[0];
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

            WriteableBitmap result = new (width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), modePixels, width * 4, 0);

            return result;
        }

        public BitmapSource MinFilter(string mask)
        {
            int kernelSize = GetValuesFromInput(mask)[0];
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

            WriteableBitmap result = new (width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), minPixels, width * 4, 0);

            return result;
        }

        public BitmapSource MaxFilter(string mask)
        {
            int kernelSize = GetValuesFromInput(mask)[0];
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

            WriteableBitmap result = new (width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), maxPixels, width * 4, 0);

            return result;
        }

        public BitmapSource ExpansionFilter(string values)
        {
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            int[] aAndBValues = GetValuesFromInput(values);

            int a = aAndBValues[0], b = aAndBValues[1];

            WriteableBitmap writableBitmap = new (GrayScaleImage);
            int width = writableBitmap.PixelWidth;
            int height = writableBitmap.PixelHeight;

            byte[] pixels = new byte[width * height];
            writableBitmap.CopyPixels(pixels, writableBitmap.BackBufferStride, 0);

            byte[] resultPixels = new byte[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                int expandedValue = (int)(a * pixels[i] + b);

                resultPixels[i] = (byte)Math.Clamp(expandedValue, 0, 255);
            }

            WriteableBitmap resultBitmap = new(width, height, writableBitmap.DpiX, writableBitmap.DpiY, PixelFormats.Gray8, null);
            resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width, 0);

            return resultBitmap;
        }

        public BitmapSource CompressionFilter(string values)
        {
            if (GrayScaleImage == null)  throw new InvalidOperationException("A imagem em níveis de cinza deve estar definida.");

            int[] aAndBValues = GetValuesFromInput(values);

            int a = aAndBValues[0], b = aAndBValues[1];

            WriteableBitmap writableBitmap = new (GrayScaleImage);
            int width = writableBitmap.PixelWidth;
            int height = writableBitmap.PixelHeight;

            byte[] pixels = new byte[width * height];
            writableBitmap.CopyPixels(pixels, writableBitmap.BackBufferStride, 0);

            byte[] resultPixels = new byte[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                int compressedValue = (int)(pixels[i] / a - b);

                resultPixels[i] = (byte)Math.Clamp(compressedValue, 0, 255);
            }

            WriteableBitmap resultBitmap = new(width, height, writableBitmap.DpiX, writableBitmap.DpiY, PixelFormats.Gray8, null);
            resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, width, 0);

            return resultBitmap;
        }

        public BitmapSource NearestNeighbor(string newSizes)
        {
            int sizes = GetValuesFromInput(newSizes)[0];

            if (BitmapImage == null) throw new Exception("Imagem não foi carregada.");

            WriteableBitmap resizedImage = new(sizes, sizes, BitmapImage.DpiX, BitmapImage.DpiY, PixelFormats.Pbgra32, null);

            resizedImage.Lock();

            double scaleX = (double)BitmapImage.PixelWidth / sizes;
            double scaleY = (double)BitmapImage.PixelHeight / sizes;

            int[] originalPixels = new int[BitmapImage.PixelWidth * BitmapImage.PixelHeight];
            BitmapImage.CopyPixels(Int32Rect.Empty, originalPixels,
                BitmapImage.PixelWidth * 4, 0);

            int[] resizedPixels = new int[sizes * sizes];

            for (int y = 0; y < sizes; y++)
            {
                for (int x = 0; x < sizes; x++)
                {
                    int originalX = (int)(x * scaleX);
                    int originalY = (int)(y * scaleY);

                    int originalIndex = originalY * BitmapImage.PixelWidth + originalX;
                    int resizedIndex = y * sizes + x;

                    resizedPixels[resizedIndex] = originalPixels[originalIndex];
                }
            }

            resizedImage.WritePixels(new Int32Rect(0, 0, sizes, sizes),
                resizedPixels, sizes * 4, 0);

            resizedImage.Unlock();

            return resizedImage;

        }

        public BitmapSource Bilinear(string newSizes)
        {
            int sizes = GetValuesFromInput(newSizes)[0];

            if (BitmapImage == null) throw new Exception("Imagem não foi carregada.");

            WriteableBitmap resizedImage = new WriteableBitmap(sizes, sizes, BitmapImage.DpiX, BitmapImage.DpiY, PixelFormats.Pbgra32, null);

            resizedImage.Lock();

            double scaleX = (double)BitmapImage.PixelWidth / sizes;
            double scaleY = (double)BitmapImage.PixelHeight / sizes;

            int[] originalPixels = new int[BitmapImage.PixelWidth * BitmapImage.PixelHeight];
            BitmapImage.CopyPixels(Int32Rect.Empty, originalPixels,
                BitmapImage.PixelWidth * 4, 0);

            int[] resizedPixels = new int[sizes * sizes];

            for (int y = 0; y < sizes; y++)
            {
                for (int x = 0; x < sizes; x++)
                {
                    double srcX = x * scaleX;
                    double srcY = y * scaleY;

                    int x0 = (int)srcX;
                    int y0 = (int)srcY;

                    int x1 = Math.Min(x0 + 1, BitmapImage.PixelWidth - 1);
                    int y1 = Math.Min(y0 + 1, BitmapImage.PixelHeight - 1);

                    double xDiff = srcX - x0;
                    double yDiff = srcY - y0;

                    int[] pixels = new int[4];
                    pixels[0] = originalPixels[y0 * BitmapImage.PixelWidth + x0];
                    pixels[1] = originalPixels[y0 * BitmapImage.PixelWidth + x1];
                    pixels[2] = originalPixels[y1 * BitmapImage.PixelWidth + x0];
                    pixels[3] = originalPixels[y1 * BitmapImage.PixelWidth + x1];

                    byte a = (byte)((((pixels[0] >> 24) & 0xFF) * (1 - xDiff) * (1 - yDiff)) +
                                    (((pixels[1] >> 24) & 0xFF) * xDiff * (1 - yDiff)) +
                                    (((pixels[2] >> 24) & 0xFF) * (1 - xDiff) * yDiff) +
                                    (((pixels[3] >> 24) & 0xFF) * xDiff * yDiff));

                    byte r = (byte)((((pixels[0] >> 16) & 0xFF) * (1 - xDiff) * (1 - yDiff)) +
                                    (((pixels[1] >> 16) & 0xFF) * xDiff * (1 - yDiff)) +
                                    (((pixels[2] >> 16) & 0xFF) * (1 - xDiff) * yDiff) +
                                    (((pixels[3] >> 16) & 0xFF) * xDiff * yDiff));

                    byte g = (byte)((((pixels[0] >> 8) & 0xFF) * (1 - xDiff) * (1 - yDiff)) +
                                    (((pixels[1] >> 8) & 0xFF) * xDiff * (1 - yDiff)) +
                                    (((pixels[2] >> 8) & 0xFF) * (1 - xDiff) * yDiff) +
                                    (((pixels[3] >> 8) & 0xFF) * xDiff * yDiff));

                    byte b = (byte)((((pixels[0] & 0xFF) * (1 - xDiff) * (1 - yDiff)) +
                                    (((pixels[1] & 0xFF) * xDiff * (1 - yDiff)) +
                                    (((pixels[2] & 0xFF) * (1 - xDiff) * yDiff) +
                                    (((pixels[3] & 0xFF) * xDiff * yDiff))))));

                    int resizedIndex = y * sizes + x;
                    resizedPixels[resizedIndex] = (a << 24) | (r << 16) | (g << 8) | b;
                }
            }

            resizedImage.WritePixels(new Int32Rect(0, 0, sizes, sizes),
                resizedPixels, sizes * 4, 0);

            resizedImage.Unlock();

            return resizedImage;
        }

        public string PointOfProve(string positions)
        {
            if (BitmapImage == null) throw new Exception("Imagem não carregada");
            int[] inputPositions = GetValuesFromInput(positions);
            int x = inputPositions[0];
            int y = inputPositions[1];

            if (x >= 0 && x < BitmapImage.PixelWidth && y >= 0 && y < BitmapImage.PixelHeight)
            {
                int[] pixels = new int[1];
                BitmapImage.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, BitmapImage.PixelWidth * 4, 0);

                byte a = (byte)((pixels[0] >> 24) & 0xFF);
                byte r = (byte)((pixels[0] >> 16) & 0xFF);
                byte g = (byte)((pixels[0] >> 8) & 0xFF);
                byte b = (byte)(pixels[0] & 0xFF);

                string colorInfo = $"NC: ({r}, {g}, {b}), Coord: ({x}, {y})";
                return colorInfo;
            }

            return "Posição invalida";
        }

        public List<TransformedBitmap> DegreesFilter(int degrees)
        {
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

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
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            TransformedBitmap transformedImage;

            if(mirroringProcess == EProcess.Vertical)
                transformedImage = new(GrayScaleImage, new ScaleTransform(1, -1));
            else 
                transformedImage = new(GrayScaleImage, new ScaleTransform(-1, 1));

            return transformedImage;
        }

        public BitmapSource PrewittOrSobelFilter(EProcess process)
        {
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            var masks = GetPrewittOrSobelMask(process);
            int[,] maskX = masks.Item1, maskY = masks.Item2;

            var writableBitmap = new WriteableBitmap(GrayScaleImage);
            int width = writableBitmap.PixelWidth;
            int height = writableBitmap.PixelHeight;

            byte[] newPixels = new byte[width * height];

            writableBitmap.Lock();
            unsafe
            {
                byte* pixels = (byte*)writableBitmap.BackBuffer;

                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int gx = 0;
                        int gy = 0;

                        for (int ky = -1; ky <= 1; ky++)
                        {
                            for (int kx = -1; kx <= 1; kx++)
                            {
                                int pixelValue = pixels[(y + ky) * width + (x + kx)];
                                gx += pixelValue * maskX[ky + 1, kx + 1];
                                gy += pixelValue * maskY[ky + 1, kx + 1];
                            }
                        }

                        int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);

                        newPixels[y * width + x] = (byte)Math.Clamp(magnitude, 0, 255);
                    }
                }
            }

            writableBitmap.Unlock();
            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, newPixels, width);
        }

        #region Métodos privados
        private static int[] GetValuesFromInput(string input)
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

        private static (int[,], int[,]) GetPrewittOrSobelMask(EProcess process)
        {
            int[,] x, y;

            switch (process)
            {
                case EProcess.Prewitt:
                    x = new int[,]
                    {
                        { -1, 0, 1 },
                        { -1, 0, 1 },
                        { -1, 0, 1 }
                    };

                    y = new int[,]
                    {
                        { -1, -1, -1 },
                        {  0,  0,  0 },
                        {  1,  1,  1 }
                    };
                    break;

                case EProcess.Sobel:
                    x = new int[,]
                    {
                        { -1, 0, 1 },
                        { -2, 0, 2 },
                        { -1, 0, 1 }
                    };

                    y = new int[,]
                    {
                        { -1, -2, -1 },
                        {  0,  0,  0 },
                        {  1,  2,  1 }
                    };
                    break;

                default:
                    throw new ArgumentException("Filtro inválido. Escolha entre SOBEL e PREWITT");
            }
            return (x, y);
        }

        private static BitmapSource ConvertToBitmapSource(Image<Rgba32> image)
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

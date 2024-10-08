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
using System.Windows.Media.Media3D;

namespace TP_PDI.Entities
{
    public class ImageProcessor : IImageProcessor
    {
        public BitmapImage? BitmapImage { get; set; }
        public BitmapImage? GrayScaleImage { get; set; }
        public BitmapImage? AuxiliarBitmapImage {  get; set; }
        public BitmapImage? AuxiliarGrayScaleImage {  get; set; }

        public BitmapSource NegativeFilter()
        {
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            
            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Bgra32, null, 0);
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4; 

            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte a = pixels[i + 3]; 
                byte r = pixels[i + 2]; 
                byte g = pixels[i + 1]; 
                byte b = pixels[i];     

                pixels[i + 2] = (byte)(255 - r); 
                pixels[i + 1] = (byte)(255 - g); 
                pixels[i] = (byte)(255 - b);     
            }

            
            WriteableBitmap negativeWriteableBitmap = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
            negativeWriteableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

            return negativeWriteableBitmap;
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

        public BitmapSource EqualizationFilter()
        {
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Gray8, null, 0);
            int width = source.PixelWidth, height = source.PixelHeight, stride = width;
            byte[] pixels = new byte[width * height];

            source.CopyPixels(pixels, stride, 0);

            int[] histogram = new int[256];
            for (int i = 0; i < pixels.Length; i++)
                histogram[pixels[i]]++;

            int totalPixels = width * height;
            int[] cdf = new int[256];
            cdf[0] = histogram[0];

            for (int i = 1; i < 256; i++)
                cdf[i] = cdf[i - 1] + histogram[i];

            byte[] outputPixels = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                outputPixels[i] = (byte)((cdf[pixels[i]] - cdf[0]) * 255 / (totalPixels - cdf[0]));

            WriteableBitmap resultBitmap = new(width, height, source.DpiX, source.DpiY, PixelFormats.Gray8, null);
            resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), outputPixels, stride, 0);

            return resultBitmap;
        }

        public BitmapSource TwoImagesSum(double percentage)
        {

            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Gray8, null, 0);
            BitmapSource auxiliarSource = new FormatConvertedBitmap(AuxiliarGrayScaleImage, PixelFormats.Gray8, null, 0);

            if (source.PixelWidth != auxiliarSource.PixelWidth || source.PixelHeight != auxiliarSource.PixelHeight)
            {
                throw new ArgumentException("As imagens devem ter as mesmas dimensões.");
            }

            int[] pixels1 = new int[source.PixelWidth * source.PixelHeight];
            int[] pixels2 = new int[auxiliarSource.PixelWidth * auxiliarSource.PixelHeight];
            int[] resultPixels = new int[source.PixelWidth * source.PixelHeight];

            source.CopyPixels(pixels1, source.PixelWidth * 4, 0);
            auxiliarSource.CopyPixels(pixels2, auxiliarSource.PixelWidth * 4, 0);
            percentage = percentage / 100;
            double percentage2 = 1.0 - percentage;
            for (int i = 0; i < pixels1.Length; i++)
            {
               
                byte a1 = (byte)((pixels1[i] >> 24) & 0xFF);
                byte r1 = (byte)((pixels1[i] >> 16) & 0xFF);
                byte g1 = (byte)((pixels1[i] >> 8) & 0xFF);
                byte b1 = (byte)(pixels1[i] & 0xFF);

                byte a2 = (byte)((pixels2[i] >> 24) & 0xFF);
                byte r2 = (byte)((pixels2[i] >> 16) & 0xFF);
                byte g2 = (byte)((pixels2[i] >> 8) & 0xFF);
                byte b2 = (byte)(pixels2[i] & 0xFF);


                byte aResult = (byte)Math.Min((a1 * percentage + a2 * percentage2), 255);
                byte rResult = (byte)Math.Min((r1 * percentage + r2 * percentage2), 255);
                byte gResult = (byte)Math.Min((g1 * percentage + g2 * percentage2), 255);
                byte bResult = (byte)Math.Min((b1 * percentage + b2 * percentage2), 255);


                resultPixels[i] = (aResult << 24) | (rResult << 16) | (gResult << 8) | bResult;
            }

            WriteableBitmap result = new(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, PixelFormats.Gray8, null);
            result.WritePixels(new Int32Rect(0, 0, result.PixelWidth, result.PixelHeight), resultPixels, result.PixelWidth * 4, 0);
            return result;
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

        public BitmapSource MeanFilter(string mask)
        {
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            
            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Bgra32, null, 0);
            int width = source.PixelWidth, height = source.PixelHeight;
            int stride = width * 4; 

            byte[] originalPixels = new byte[height * stride];
            byte[] filteredPixels = new byte[height * stride];
            source.CopyPixels(originalPixels, stride, 0);

            
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
                            int index = ((y + ky) * stride) + ((x + kx) * 4);
                            byte b = originalPixels[index];     
                            byte g = originalPixels[index + 1]; 
                            byte r = originalPixels[index + 2]; 

                            rSum += r;
                            gSum += g;
                            bSum += b;
                            pixelCount++;
                        }
                    }

                    
                    byte rAvg = (byte)(rSum / pixelCount);
                    byte gAvg = (byte)(gSum / pixelCount);
                    byte bAvg = (byte)(bSum / pixelCount);

                    
                    int pixelIndex = (y * stride) + (x * 4);
                    filteredPixels[pixelIndex] = bAvg;         
                    filteredPixels[pixelIndex + 1] = gAvg;     
                    filteredPixels[pixelIndex + 2] = rAvg;     
                    filteredPixels[pixelIndex + 3] = 255;      
                }
            }

            
            WriteableBitmap result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), filteredPixels, stride, 0);

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

            
            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Bgra32, null, 0);
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;

            byte[] originalPixels = new byte[height * stride];
            byte[] modePixels = new byte[height * stride];
            source.CopyPixels(originalPixels, stride, 0);

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
                            int index = ((y + ky) * stride) + ((x + kx) * 4);
                            byte r = originalPixels[index + 2]; // Red
                            byte g = originalPixels[index + 1]; // Green
                            byte b = originalPixels[index];     // Blue

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

                    int pixelIndex = (y * stride) + (x * 4);
                    modePixels[pixelIndex] = bMode;
                    modePixels[pixelIndex + 1] = gMode;
                    modePixels[pixelIndex + 2] = rMode;
                    modePixels[pixelIndex + 3] = 255; // Alpha
                }
            }

            WriteableBitmap result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), modePixels, stride, 0);

            return result;
        }

        public BitmapSource MinFilter(string mask)
        {
            int kernelSize = GetValuesFromInput(mask)[0];
            int radius = kernelSize / 2;

            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Bgra32, null, 0);
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;

            byte[] originalPixels = new byte[height * stride];
            byte[] minPixels = new byte[height * stride];
            source.CopyPixels(originalPixels, stride, 0);

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
                            int index = ((y + ky) * stride) + ((x + kx) * 4);
                            byte r = originalPixels[index + 2];
                            byte g = originalPixels[index + 1];
                            byte b = originalPixels[index];

                            rList.Add(r);
                            gList.Add(g);
                            bList.Add(b);
                        }
                    }

                    byte rMin = rList.Min();
                    byte gMin = gList.Min();
                    byte bMin = bList.Min();

                    int pixelIndex = (y * stride) + (x * 4);
                    minPixels[pixelIndex] = bMin;
                    minPixels[pixelIndex + 1] = gMin;
                    minPixels[pixelIndex + 2] = rMin;
                    minPixels[pixelIndex + 3] = 255; // Alpha
                }
            }

            WriteableBitmap result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), minPixels, stride, 0);

            return result;
        }

        public BitmapSource MaxFilter(string mask)
        {
            int kernelSize = GetValuesFromInput(mask)[0];
            int radius = kernelSize / 2;

            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Bgra32, null, 0);
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;

            byte[] originalPixels = new byte[height * stride];
            byte[] maxPixels = new byte[height * stride];
            source.CopyPixels(originalPixels, stride, 0);

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
                            int index = ((y + ky) * stride) + ((x + kx) * 4);
                            byte r = originalPixels[index + 2];
                            byte g = originalPixels[index + 1];
                            byte b = originalPixels[index];

                            rList.Add(r);
                            gList.Add(g);
                            bList.Add(b);
                        }
                    }

                    byte rMax = rList.Max();
                    byte gMax = gList.Max();
                    byte bMax = bList.Max();

                    int pixelIndex = (y * stride) + (x * 4);
                    maxPixels[pixelIndex] = bMax;
                    maxPixels[pixelIndex + 1] = gMax;
                    maxPixels[pixelIndex + 2] = rMax;
                    maxPixels[pixelIndex + 3] = 255; // Alpha
                }
            }

            WriteableBitmap result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), maxPixels, stride, 0);

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

        public List<BitmapSource> DegreesFilter(EProcess degreesProcess)
        {
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Gray8, null, 0);
            int width = source.PixelWidth, height = source.PixelHeight, stride = width;

            byte[] pixels = new byte[width * height];
            source.CopyPixels(pixels, stride, 0);

            List<BitmapSource> rotatedImages = [];

            if (degreesProcess == EProcess.NinetyDegrees)
            {
                byte[] rotatedClockwise = new byte[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                        rotatedClockwise[x * height + (height - 1 - y)] = pixels[y * stride + x];
                }

                byte[] rotatedCounterClockwise = new byte[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                        rotatedCounterClockwise[(width - 1 - x) * height + y] = pixels[y * stride + x];
                }

                rotatedImages.Add(CreateBitmap(rotatedClockwise, height, width));
                rotatedImages.Add(CreateBitmap(rotatedCounterClockwise, height, width));
            }
            else if (degreesProcess == EProcess.OneHundredEightyDegrees)
            {
                byte[] rotated = new byte[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        rotated[(height - 1 - y) * width + (width - 1 - x)] = pixels[y * stride + x];
                    }
                }

                rotatedImages.Add(CreateBitmap(rotated, width, height));
                rotatedImages.Add(CreateBitmap(rotated, width, height)); // 180 graus é o mesmo tanto horário quanto anti-horário
            }

            return rotatedImages;
        }

        public BitmapSource MirroringFilter(EProcess mirroringProcess)
        {
            if (GrayScaleImage == null) throw new Exception("Imagem não foi carregada corretamente.");

            BitmapSource source = new FormatConvertedBitmap(GrayScaleImage, PixelFormats.Gray8, null, 0);
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width;

            byte[] pixels = new byte[width * height];
            source.CopyPixels(pixels, stride, 0);

            byte[] outputPixels = new byte[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if(mirroringProcess == EProcess.Horizontal)
                        outputPixels[y * stride + (width - 1 - x)] = pixels[y * stride + x];
                    else if(mirroringProcess == EProcess.Vertical)
                        outputPixels[(height - 1 - y) * stride + x] = pixels[y * stride + x];
                }
            }

            WriteableBitmap resultBitmap = new(width, height, source.DpiX, source.DpiY, PixelFormats.Gray8, null);
            resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), outputPixels, stride, 0);

            return resultBitmap;
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

        private BitmapSource CreateBitmap(byte[] pixels, int width, int height)
        {
            WriteableBitmap resultBitmap = new(width, height, 96, 96, PixelFormats.Gray8, null);
            int stride = width;
            resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return resultBitmap;
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

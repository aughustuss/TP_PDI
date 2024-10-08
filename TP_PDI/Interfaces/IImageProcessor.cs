using Microsoft.Win32;
using System.Windows.Media.Imaging;
using TP_PDI.Enuns;

namespace TP_PDI.Interfaces
{
    public interface IImageProcessor
    {
        public BitmapSource NegativeFilter();
        public BitmapSource LogarithmicFilter();
        public BitmapSource InverseLogarithmFilter();
        public BitmapSource LaplacianFilter();
        public BitmapSource HighBoostFilter();
        public BitmapSource EqualizationFilter();
        public BitmapSource TwoImagesSum(double percentage);
        public BitmapSource PowerAndRootFilter(double gamma);
        public BitmapSource MeanFilter(string mask);
        public BitmapSource MedianFilter(string mask);
        public BitmapSource ModeFilter(string mask);
        public BitmapSource MinFilter(string mask);
        public BitmapSource MaxFilter(string mask);
        public BitmapSource ExpansionFilter(string values);
        public BitmapSource CompressionFilter(string values);
        public BitmapSource NearestNeighbor(string newSizes);
        public BitmapSource Bilinear(string newSizes);
        public string PointOfProve(string positions);
        public List<BitmapSource> DegreesFilter(EProcess process);
        public BitmapSource PrewittOrSobelFilter(EProcess process);
        public BitmapSource MirroringFilter(EProcess mirroringProcess);
    }
}

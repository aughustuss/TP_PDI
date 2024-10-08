using Microsoft.Win32;
using System.Windows.Media.Imaging;
using TP_PDI.Enuns;

namespace TP_PDI.Interfaces
{
    public interface IImageProcessor
    {
        public BitmapSource NegativeFilter();
        public BitmapSource LogarithmicFilter();
        public BitmapSource InverseLogaritmFilter();
        public BitmapSource MeanFilter(string mask);
        public BitmapSource MedianFilter(string mask);
        public BitmapSource ModeFilter(string mask);
        public BitmapSource MinFilter(string mask);
        public BitmapSource MaxFilter(string mask);
        public BitmapSource ExpansionFilter();
        public BitmapSource LaplacianFilter();
        public BitmapSource HighBoostFilter();
        public List<TransformedBitmap> DegreesFilter(int degrees);
        public TransformedBitmap MirroringFilter(EProcess mirroringProcess);
        public BitmapSource NearestNeighbor(string newSizes);
        public BitmapSource Bilinear(string newSizes);
        public string PointOfProve(string positions);
    }
}

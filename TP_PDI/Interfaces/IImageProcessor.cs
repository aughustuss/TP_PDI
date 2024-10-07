using Microsoft.Win32;
using System.Windows.Media.Imaging;
using TP_PDI.Enuns;

namespace TP_PDI.Interfaces
{
    public interface IImageProcessor
    {
        public void GetGrayScale(OpenFileDialog dialog);
        public BitmapSource ConvertToNegative();
        public BitmapSource MeanFilter();
        public BitmapSource LogarithmicFilter();
        public BitmapSource InverseLogaritmFilter();
        public BitmapSource MedianFilter();
        public BitmapSource ModeFilter();
        public BitmapSource MinFilter();
        public BitmapSource MaxFilter();
        public BitmapSource ExpansionFilter();
        public List<TransformedBitmap> DegreesFilter(int degrees);
        public TransformedBitmap MirroringFilter(EProcess mirroringProcess);
    }
}

using Microsoft.Win32;
using System.Drawing;
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
    }
}
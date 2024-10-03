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

        private void SubmetImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new();
            openDialog.Filter = "Image files|*.jpg;*.png";
            openDialog.FilterIndex = 1;
            if (openDialog.ShowDialog() == true)
                imagePicture.Source = new BitmapImage(new Uri(openDialog.FileName));

             //ConvertToGrayScale(imagePicture.Source);
        }

/*
        private void ConvertToGrayScale(BitmapImage image)
        {
            var grayImage = (BitmapImage)imagePicture.Source;
            Bitmap bitmap = (Bitmap)image.Clone(); 
        }*/
    }
}
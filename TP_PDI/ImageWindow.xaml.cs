using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TP_PDI
{
    /// <summary>
    /// Interaction logic for ImageWindow.xaml
    /// </summary>
    public partial class ImageWindow : Window
    {
        public ImageWindow(BitmapSource img)
        {
            InitializeComponent();

            this.Height = img.PixelHeight; 
            this.Width = img.PixelWidth;
            ResultingImage.Source = img;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}

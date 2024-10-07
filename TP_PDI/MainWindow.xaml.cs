using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TP_PDI.Enuns;
using TP_PDI.Entities;

namespace TP_PDI
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<EProcess, Func<BitmapSource>> _process;
        private readonly Dictionary<EProcess, Func<int, List<TransformedBitmap>>> _degreesProcesses;
        private readonly Dictionary<EProcess, Func<EProcess, TransformedBitmap>> _mirroringProcesses;
        private readonly ImageProcessor _image;
        private string
            _maskValue = "",
            _expansionOrCompressionValue = "";

        public MainWindow()
        {
            InitializeComponent();

            FilterOptions.ItemsSource = Enum.GetValues(typeof(EProcess)).Cast<EProcess>();
            DegreesOptions.ItemsSource = new List<int>()
            {
                90,
                180
            };

            _image = new ImageProcessor();
            _process = new Dictionary<EProcess, Func<BitmapSource>>
            {
                { EProcess.Negative, _image.ConvertToNegative },
                { EProcess.Expansion, _image.MeanFilter },
                { EProcess.Median, _image.MedianFilter },
                { EProcess.Maximun, _image.MaxFilter },
                { EProcess.Minimun, _image.MinFilter },
                { EProcess.Logarithm, _image.LogarithmicFilter },
                { EProcess.InverseLogarithm, _image.InverseLogaritmFilter }
            };
            _degreesProcesses = new Dictionary<EProcess, Func<int, List<TransformedBitmap>>>
            {
                { EProcess.NinetyDegrees, _image.DegreesFilter },
                { EProcess.OneHundredEightyDegrees, _image.DegreesFilter }
            };
            _mirroringProcesses = new Dictionary<EProcess, Func<EProcess, TransformedBitmap>>
            {
                { EProcess.Horizontal, _image.MirroringFilter },
                { EProcess.Vertical, _image.MirroringFilter },
            };
        }

        private void SubmitImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Filter = "Image files|*.jpg;*.png;*.bmp",
                FilterIndex = 1,
            };
            if (dialog.ShowDialog() == true)
            {
                _image.BitmapImage = new(new Uri(dialog.FileName));
                imagePicture.Source = _image.BitmapImage;
                _image.GetGrayScale(dialog);
            }
        }

        private void ProcessImage(object sender, RoutedEventArgs e)
        {
            if(FilterOptions.SelectedItem is EProcess selectedProcess)
            {
                if(_process.TryGetValue(selectedProcess, out var process))
                    resultImage.Source = process();
                else if(_degreesProcesses.TryGetValue(selectedProcess, out var degreesProcess))
                {
                    var transormedBitmaps = degreesProcess((int)DegreesOptions.SelectedItem);
                    resultImage.Source = transormedBitmaps[0];
                    rotationImageResult.Source = transormedBitmaps[1];
                    RotatedImage.Visibility = Visibility.Visible;
                } else if(_mirroringProcesses.TryGetValue(selectedProcess, out var mirroringProcess))
                    resultImage.Source = mirroringProcess(selectedProcess);
            }
        }

        private void HandleProcessChange(object sender, RoutedEventArgs e)
        {
            if(FilterOptions.SelectedItem is EProcess process)
            {
                switch (process)
                {
                    case EProcess.Median:
                    case EProcess.Minimun:
                    case EProcess.Maximun:
                    case EProcess.Mode:
                    case EProcess.Average:
                        MaskInput.Visibility = Visibility.Visible;
                        ExpansionOrCompressionInput.Visibility = Visibility.Hidden;
                        DegreesInput.Visibility = Visibility.Hidden;
                        break;
                    case EProcess.Expansion:
                    case EProcess.Compression:
                        MaskInput.Visibility = Visibility.Hidden;
                        ExpansionOrCompressionInput.Visibility = Visibility.Visible;
                        DegreesInput.Visibility = Visibility.Hidden;
                        break;
                    case EProcess.NinetyDegrees:
                    case EProcess.OneHundredEightyDegrees:
                        MaskInput.Visibility = Visibility.Hidden;
                        ExpansionOrCompressionInput.Visibility= Visibility.Hidden;
                        DegreesInput.Visibility = Visibility.Visible;
                        break;
                    default:
                        MaskInput.Visibility = Visibility.Hidden;
                        ExpansionOrCompressionInput.Visibility = Visibility.Hidden;
                        break;
                }
            }
        }
    }
}

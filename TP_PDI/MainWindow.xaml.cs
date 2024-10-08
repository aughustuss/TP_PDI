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
using System.Windows.Controls;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using TP_PDI.Utils;
using System.ComponentModel;

namespace TP_PDI
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<EProcess, Func<BitmapSource>> _process;
        private readonly Dictionary<EProcess, Func<int, List<TransformedBitmap>>> _degreesProcesses;
        private readonly Dictionary<EProcess, Func<EProcess, TransformedBitmap>> _mirroringProcesses;
        private readonly Dictionary<EProcess, Func<string, BitmapSource>> _processWithMask;
        private readonly Dictionary<EProcess, Func<EProcess, BitmapSource>> _prewittOrSobelProcess;
        private readonly Dictionary<EProcess, Func<double, BitmapSource>> _powerOrRootProcess;

        private readonly ImageProcessor _image;

        public MainWindow()
        {
            InitializeComponent();

            _image = new ImageProcessor();

            FilterOptions.ItemsSource = Enum
                .GetValues(typeof(EProcess)).Cast<EProcess>()
                .Select(ep => new { Value = ep, Description = HelpingMethods.GetEnumDescription(ep) });

            _process = new Dictionary<EProcess, Func<BitmapSource>>
            {
                { EProcess.Negative, _image.NegativeFilter },
                { EProcess.Logarithm, _image.LogarithmicFilter },
                { EProcess.InverseLogarithm, _image.InverseLogarithmFilter },
                { EProcess.Laplacian, _image.LaplacianFilter },
                { EProcess.HighBoost, _image.HighBoostFilter },
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

            _processWithMask = new Dictionary<EProcess, Func<string, BitmapSource>>
            {
                { EProcess.Mode, _image.ModeFilter },
                { EProcess.Average, _image.MeanFilter},
                { EProcess.Median, _image.MedianFilter },
                { EProcess.Maximun, _image.MaxFilter },
                { EProcess.Minimun, _image.MinFilter },
                { EProcess.EnlargementBilinear, _image.Bilinear },
                { EProcess.EnlargementNearestNeighbor, _image.NearestNeighbor },
                { EProcess.Expansion, _image.ExpansionFilter },
                { EProcess.Compression, _image.CompressionFilter }
            };

            _prewittOrSobelProcess = new Dictionary<EProcess, Func<EProcess, BitmapSource>>
            {
                { EProcess.Sobel, _image.PrewittOrSobelFilter },
                { EProcess.Prewitt, _image.PrewittOrSobelFilter },
            };

            _powerOrRootProcess = new Dictionary<EProcess, Func<double, BitmapSource>>
            {
                { EProcess.PowerAndRoot, _image.PowerAndRootFilter },
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
                _image.GrayScaleImage = HelpingMethods.GetGrayScale(dialog);
                int[] histogram = CalculateHistogram(_image.GrayScaleImage);
                DrawHistogram(histogram);
            }
        }

        private void ProcessImage(object sender, RoutedEventArgs e)
        {
            if(FilterOptions.SelectedValue is EProcess selectedProcess)
            {
                if (_process.TryGetValue(selectedProcess, out var process))
                    resultImage.Source = process();
                else if (_mirroringProcesses.TryGetValue(selectedProcess, out var mirroringProcess))
                    resultImage.Source = mirroringProcess(selectedProcess);
                else if(_prewittOrSobelProcess.TryGetValue(selectedProcess, out var prewittOrSobelProcess))
                    resultImage.Source = prewittOrSobelProcess(selectedProcess);
                else if(_powerOrRootProcess.TryGetValue(selectedProcess, out var powerOrRootProcess))
                {
                    if (double.TryParse(GammaValue.Text, out double gamma))
                        resultImage.Source = powerOrRootProcess(gamma);
                    else
                        MessageBox.Show("Insira apenas valores numéricos.");
                }
                else if (_processWithMask.TryGetValue(selectedProcess, out var processWithMask))
                {
                    BitmapSource img = processWithMask(MaskValues.Text);
                    resultImage.Source = img;
                    if (selectedProcess == EProcess.EnlargementBilinear || selectedProcess == EProcess.EnlargementNearestNeighbor)
                        OpenNewModal(img);
                }
                else if (_degreesProcesses.TryGetValue(selectedProcess, out var degreesProcess))
                {
                    int degrees = 0;
                    if (selectedProcess == EProcess.NinetyDegrees)
                        degrees = 90;
                    else if (selectedProcess == EProcess.OneHundredEightyDegrees)
                        degrees = 180;

                    var transormedBitmaps = degreesProcess(degrees);
                    resultImage.Source = transormedBitmaps[0];
                    rotationImageResult.Source = transormedBitmaps[1];
                    AuxiliarImage.Visibility = Visibility.Visible;
                }
                
            }
        }

        private void HandleProcessChange(object sender, RoutedEventArgs e)
        {
            if(FilterOptions.SelectedValue is EProcess process)
            {
                switch (process)
                {
                    case EProcess.Median:
                    case EProcess.Minimun:
                    case EProcess.Maximun:
                    case EProcess.Mode:
                    case EProcess.Average:
                    case EProcess.EnlargementNearestNeighbor:
                    case EProcess.EnlargementBilinear:
                    case EProcess.Expansion:
                    case EProcess.Compression:
                        MaskInput.Visibility = Visibility.Visible;
                        ExpansionOrCompressionInput.Visibility = Visibility.Hidden;
                        GammaInput.Visibility = Visibility.Hidden;
                        break;
                    case EProcess.PowerAndRoot:
                        MaskInput.Visibility = Visibility.Hidden;
                        ExpansionOrCompressionInput.Visibility = Visibility.Hidden;
                        GammaInput.Visibility = Visibility.Visible;
                        break;
                    default:
                        MaskInput.Visibility = Visibility.Hidden;
                        ExpansionOrCompressionInput.Visibility = Visibility.Hidden;
                        break;
                }
            }
        }

        private static void OpenNewModal(BitmapSource img)
        {
            ImageWindow imageModal = new(img);
            imageModal.ShowDialog();
        }

        private static int[] CalculateHistogram(BitmapSource bitmapSource)
        {
            int[] histogram = new int[256];

            WriteableBitmap writeableBitmap = new(bitmapSource);
            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;
            byte[] pixels = new byte[width * height * 4];

            writeableBitmap.CopyPixels(pixels, width * 4, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte gray = pixels[i]; 
                histogram[gray]++;
            }

            return histogram;
        }
        private void DrawHistogram(int[] histogram)
        {
            histogramCanvas.Children.Clear();

            int max = histogram.Max();

            double scale = 200.0 / max;

            for (int i = 0; i < histogram.Length; i++)
            {
                Rectangle bar = new Rectangle
                {
                    Width = 2,
                    Height = histogram[i] * scale,
                    Fill = System.Windows.Media.Brushes.Black
                };

                Canvas.SetLeft(bar, i * 3); 
                Canvas.SetBottom(bar, 0);

                histogramCanvas.Children.Add(bar);
            }
        }
        private void ImageDisplay_MouseMove(object sender, MouseButtonEventArgs e)
        {
            if (imagePicture.Source != null && _image.BitmapImage != null)
            {
                var position = e.GetPosition(imagePicture);
                int x = (int)position.X;
                int y = (int)position.Y;

                if (x >= 0 && x < _image.BitmapImage.PixelWidth && y >= 0 && y < _image.BitmapImage.PixelHeight)
                {
                    string positions = $"{x},{y}";
                    string colorInfo = _image.PointOfProve(positions);

                    MessageBox.Show(colorInfo, "Informações do Pixel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

        }

    }
}

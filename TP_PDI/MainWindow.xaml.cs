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
        private readonly Dictionary<EProcess, Func<EProcess, List<BitmapSource>>> _degreesProcesses;
        private readonly Dictionary<EProcess, Func<EProcess, BitmapSource>> _mirroringProcesses;
        private readonly Dictionary<EProcess, Func<string, BitmapSource>> _processWithMask;
        private readonly Dictionary<EProcess, Func<EProcess, BitmapSource>> _prewittOrSobelProcess;
        private readonly Dictionary<EProcess, Func<double, BitmapSource>> _powerOrRootProcess;
        private readonly Dictionary<EProcess, Func<double, BitmapSource>> _twoImagesSumProcess;

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
                { EProcess.Equalization, _image.EqualizationFilter },
            };

            _degreesProcesses = new Dictionary<EProcess, Func<EProcess, List<BitmapSource>>>
            {
                { EProcess.NinetyDegrees, _image.DegreesFilter },
                { EProcess.OneHundredEightyDegrees, _image.DegreesFilter }
            };

            _mirroringProcesses = new Dictionary<EProcess, Func<EProcess, BitmapSource>>
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

            _twoImagesSumProcess = new Dictionary<EProcess, Func<double, BitmapSource>>
            {
                { EProcess.TwoImagesSum, _image.TwoImagesSum },
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
                _image.GrayScaleImage = HelpingMethods.GetGrayScale(dialog);
                imagePicture.Source = _image.GrayScaleImage;
                DrawHistogram(_image.GrayScaleImage);
            }
        }

        private void SubmitAuxiliarImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Filter = "Image files|*.jpg;*.png;*.bmp",
                FilterIndex = 1,
            };
            if (dialog.ShowDialog() == true)
            {
                _image.AuxiliarBitmapImage = new(new Uri(dialog.FileName));
                _image.AuxiliarGrayScaleImage = HelpingMethods.GetGrayScale(dialog);
                auxiliarImageResult.Source = _image.AuxiliarGrayScaleImage;
            }
        }

        private void ProcessImage(object sender, RoutedEventArgs e)
        {
            if(FilterOptions.SelectedValue is EProcess selectedProcess)
            {
                if (_process.TryGetValue(selectedProcess, out var process))
                {
                    BitmapSource img = process();
                    resultImage.Source = img;
                    if (selectedProcess == EProcess.Equalization)
                        DrawHistogram(img);
                }
                else if (_mirroringProcesses.TryGetValue(selectedProcess, out var mirroringProcess))
                {
                    resultImage.Source = mirroringProcess(selectedProcess);
                }
                else if (_prewittOrSobelProcess.TryGetValue(selectedProcess, out var prewittOrSobelProcess))
                {
                    resultImage.Source = prewittOrSobelProcess(selectedProcess);
                }
                else if (_twoImagesSumProcess.TryGetValue(selectedProcess, out var twoImagesSumProcess))
                {
                    if (double.TryParse(AuxiliarImageValue.Text, out double percentage))
                        resultImage.Source = twoImagesSumProcess(percentage);
                    else
                        MessageBox.Show("Insira apenas valores numéricos.");
                }
                else if (_powerOrRootProcess.TryGetValue(selectedProcess, out var powerOrRootProcess))
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
                    var transormedBitmaps = degreesProcess(selectedProcess);
                    resultImage.Source = transormedBitmaps[0];
                    AuxiliarImage.Visibility = Visibility.Visible;
                    auxiliarImageResult.Source = transormedBitmaps[1];
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
                        GammaInput.Visibility = Visibility.Hidden;
                        SubmitProcessButton.IsEnabled = MaskValues.Text.Length >= 3;
                        AuxiliarImage.Visibility = Visibility.Hidden;
                        AuxiliarImageInput.Visibility = Visibility.Hidden;
                        SubmitAuxiliarImageButton.IsEnabled = false;
                        break;
                    case EProcess.PowerAndRoot:
                        MaskInput.Visibility = Visibility.Hidden;
                        GammaInput.Visibility = Visibility.Visible;
                        SubmitProcessButton.IsEnabled = GammaValue.Text.Length >= 3;
                        AuxiliarImage.Visibility = Visibility.Hidden;
                        AuxiliarImageInput.Visibility = Visibility.Hidden;
                        SubmitAuxiliarImageButton.IsEnabled = false;
                        break;
                    case EProcess.NinetyDegrees:
                    case EProcess.OneHundredEightyDegrees:
                        MaskInput.Visibility = Visibility.Hidden;
                        GammaInput.Visibility = Visibility.Hidden;
                        SubmitProcessButton.IsEnabled = true;
                        AuxiliarImage.Visibility = Visibility.Visible;
                        AuxiliarImageInput.Visibility = Visibility.Hidden;
                        SubmitAuxiliarImageButton.IsEnabled = false;
                        break;
                    case EProcess.TwoImagesSum:
                        MaskInput.Visibility = Visibility.Hidden;
                        GammaInput.Visibility = Visibility.Hidden;
                        SubmitProcessButton.IsEnabled = true;
                        AuxiliarImage.Visibility = Visibility.Visible;
                        AuxiliarImageInput.Visibility = Visibility.Visible;
                        SubmitAuxiliarImageButton.IsEnabled = true;
                        break;
                    default:
                        MaskInput.Visibility = Visibility.Hidden;
                        GammaInput.Visibility = Visibility.Hidden;
                        SubmitProcessButton.IsEnabled = true;
                        AuxiliarImage.Visibility = Visibility.Hidden;
                        AuxiliarImageInput.Visibility = Visibility.Hidden;
                        SubmitAuxiliarImageButton.IsEnabled = false;
                        break;
                }
            }
        }

        private void HandleMaskOrGammaChange(object sender, RoutedEventArgs e)
        {
            if (MaskValues.Text.Length > 2 || GammaValue.Text.Length > 2)
                SubmitProcessButton.IsEnabled = true;
            else
                SubmitProcessButton.IsEnabled = false;
        }

        private static void OpenNewModal(BitmapSource img)
        {
            ImageWindow imageModal = new(img);
            imageModal.ShowDialog();
        }

        private void DrawHistogram(BitmapSource grayScaleImage)
        {
            if (grayScaleImage == null) throw new InvalidOperationException("A imagem em níveis de cinza deve estar definida.");

            List<Rectangle> bars = GenerateHistogram(grayScaleImage, histogramCanvas.ActualWidth, histogramCanvas.ActualHeight);

            histogramCanvas.Children.Clear();
            double barWidth = histogramCanvas.ActualWidth / bars.Count;

            for (int i = 0; i <= 10; i++) 
            {
                double yValue = (histogramCanvas.ActualHeight / 10) * i;
                TextBlock yLabel = new TextBlock
                {
                    Text = (255 / 10 * i).ToString(),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(yLabel, 0);
                Canvas.SetBottom(yLabel, yValue); 
                histogramCanvas.Children.Add(yLabel);
            }

            for (int i = 0; i < bars.Count; i++)
            {
                Rectangle bar = bars[i];

                Canvas.SetLeft(bar, i * barWidth);
                Canvas.SetBottom(bar, 0); // Posicionando no eixo Y

                histogramCanvas.Children.Add(bar);
            }

            TextBlock xLabel = new()
            {
                Text = "NÍVEIS DE CINZA",
                FontSize = 12,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(xLabel, (histogramCanvas.ActualWidth / 2) - 40); 
            Canvas.SetBottom(xLabel, -25); 
        }

        private static List<Rectangle> GenerateHistogram(BitmapSource grayScaleImage, double canvasWidth, double canvasHeight)
        {
            int width = grayScaleImage.PixelWidth, height = grayScaleImage.PixelHeight;
            byte[] pixels = new byte[width * height];
            grayScaleImage.CopyPixels(pixels, width, 0);

            int[] histogram = new int[256];

            foreach (byte pixel in pixels)
                histogram[pixel]++;

            double maxFrequency = histogram.Max();
            double barWidth = canvasWidth / histogram.Length;

            List<Rectangle> bars = new();

            for (int i = 0; i < histogram.Length; i++)
            {
                double barHeight = (histogram[i] / maxFrequency) * canvasHeight;

                var bar = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = Brushes.Black,
                    Stroke = Brushes.Gray
                };

                bars.Add(bar);
            }

            return bars;
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

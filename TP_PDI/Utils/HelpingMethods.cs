using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
using TP_PDI.Enuns;

namespace TP_PDI.Utils
{
    public class HelpingMethods
    {

        public HelpingMethods() { }

        public static BitmapImage GetGrayScale(OpenFileDialog dialog)
        {
            using var image = Image.Load<Rgba32>(dialog.FileName);
            image.Mutate(x => x.Grayscale());

            using var ms = new MemoryStream();
            image.SaveAsBmp(ms);
            ms.Seek(0, SeekOrigin.Begin);

            BitmapImage grayScaleImage = new();
            grayScaleImage.BeginInit();
            grayScaleImage.StreamSource = ms;
            grayScaleImage.CacheOption = BitmapCacheOption.OnLoad;
            grayScaleImage.EndInit();

            return grayScaleImage;
        }

        public static string GetEnumDescription(EProcess value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fieldInfo!.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }
    }
}

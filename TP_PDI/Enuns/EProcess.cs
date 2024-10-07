using System.ComponentModel;

namespace TP_PDI.Enuns
{
    public enum EProcess
    {
        [Description("Negativo")]
        Negative,
        [Description("Logaritmo")]
        Logarithm,
        [Description("Logaritmo Inverso")]
        InverseLogarithm,
        [Description("Exponenciação")]
        Power,
        [Description("Raiz")]
        Root,
        [Description("Expansão")]
        Expansion,
        [Description("Compressão")]
        Compression,
        [Description("Média")]
        Average,
        [Description("Mediana")]
        Median,
        [Description("Moda")]
        Mode,
        [Description("Mínimo")]
        Minimun,
        [Description("Máximo")]
        Maximun,
        [Description("Replicação de Pixels")]
        PixelsReplication,
        [Description("Bilinear")]
        Bilinear,
        [Description("Laplaciano")]
        Laplacian,
        [Description("HighBoost")]
        HighBoost,
        [Description("Prewitt")]
        Prewitt,
        [Description("Sobel")]
        Sobel,
        [Description("Espelhamento Horizontal")]
        Horizontal,
        [Description("Espelhamento Vertical")]
        Vertical,
        [Description("Noventa Graus")]
        NinetyDegrees,
        [Description("Cento e Oitenta Graus")]
        OneHundredEightyDegrees
    }
}


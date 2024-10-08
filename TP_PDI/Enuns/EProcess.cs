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
        [Description("Potência e Raiz")]
        PowerAndRoot,
        [Description("Expansão")]
        Expansion,
        [Description("Compressão")]
        Compression,
        [Description("Filtro da Média")]
        Average,
        [Description("Filtro da Mediana")]
        Median,
        [Description("Filtro da Moda")]
        Mode,
        [Description("Filtro do Mínimo")]
        Minimun,
        [Description("Filtro do Máximo")]
        Maximun,
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
        OneHundredEightyDegrees,
        [Description("Ampliação da imagem com interpolação por replicação de pixels")]
        EnlargementNearestNeighbor,
        [Description("Ampliação da imagem com interpolação bilinear")]
        EnlargementBilinear

    }
}


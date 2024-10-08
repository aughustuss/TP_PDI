using System.ComponentModel;

namespace TP_PDI.Enuns
{
    public enum EProcess
    {
        [Description("Ampliação da imagem com interpolação bilinear")]
        EnlargementBilinear,
        [Description("Ampliação da imagem com interpolação por replicação de pixels")]
        EnlargementNearestNeighbor,
        [Description("Filtro de Compressão")]
        Compression,
        [Description("Equalização")]
        Equalization,
        [Description("Espelhamento Horizontal")]
        Horizontal,
        [Description("Espelhamento Vertical")]
        Vertical,
        [Description("Filtro de Expansão")]
        Expansion,
        [Description("Filtro de Logaritmo")]
        Logarithm,
        [Description("Filtro de Logaritmo Inverso")]
        InverseLogarithm,
        [Description("Filtro de Potência e Raiz")]
        PowerAndRoot,
        [Description("Filtro da Média")]
        Average,
        [Description("Filtro da Mediana")]
        Median,
        [Description("Filtro da Moda")]
        Mode,
        [Description("Filtro de Máximo")]
        Maximun,
        [Description("Filtro de Mínimo")]
        Minimun,
        [Description("Filtro Negativo")]
        Negative,
        [Description("Operador HighBoost")]
        HighBoost,
        [Description("Operador Laplaciano")]
        Laplacian,
        [Description("Operador Prewitt")]
        Prewitt,
        [Description("Operador Sobel")]
        Sobel,
        [Description("Soma de imagens")]
        TwoImagesSum,
        [Description("90 Graus")]
        NinetyDegrees,
        [Description("180 Graus")]
        OneHundredEightyDegrees,
    }
}


using SQLite;

namespace FinanzasProMaui.Modelo
{
    public class ActivoFinanciero
    {
        [PrimaryKey, AutoIncrement]
        public int IdActivo { get; set; }
        public string Simbolo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public double PrecioUsd { get; set; }
        public double Cantidad { get; set; }
        public double TotalUsd { get; set; }
        public double TotalBs { get; set; }
        public double Variacion24h { get; set; }
        public string Notas { get; set; } = string.Empty;
        public string FechaRegistro { get; set; } = string.Empty;
    }
}

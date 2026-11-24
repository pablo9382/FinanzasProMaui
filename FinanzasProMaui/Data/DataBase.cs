using SQLite;
using FinanzasProMaui.Modelo;

namespace FinanzasProMaui.Data
{
    public class DataBase
    {
        private readonly SQLiteAsyncConnection _conexion;

        public DataBase(string rutaBD)
        {
            _conexion = new SQLiteAsyncConnection(rutaBD);
            _conexion.CreateTableAsync<ActivoFinanciero>().Wait();
        }

        public async Task SembrarDatosInicialesAsync()
        {
            int count = await _conexion.Table<ActivoFinanciero>().CountAsync();
            if (count == 0)
            {
                var iniciales = new List<ActivoFinanciero>
                {
                    new ActivoFinanciero
                    {
                        Simbolo = "BTC",
                        Nombre = "Bitcoin (BTC)",
                        Categoria = "Criptoactivos",
                        PrecioUsd = 94850.00,
                        Cantidad = 0.05,
                        TotalUsd = 4742.50,
                        TotalBs = Math.Round(4742.50 * 6.96, 2),
                        Variacion24h = 3.85,
                        Notas = "Reserva estratégica a largo plazo",
                        FechaRegistro = DateTime.Now.AddHours(-3).ToString("dd/MM/yyyy HH:mm")
                    },
                    new ActivoFinanciero
                    {
                        Simbolo = "USDT",
                        Nombre = "Tether Dólar Digital (USDT)",
                        Categoria = "Divisas & Stablecoins",
                        PrecioUsd = 1.00,
                        Cantidad = 850.00,
                        TotalUsd = 850.00,
                        TotalBs = Math.Round(850.00 * 6.96, 2),
                        Variacion24h = 0.05,
                        Notas = "Fondo de ahorro en dólares digitales",
                        FechaRegistro = DateTime.Now.AddHours(-2).ToString("dd/MM/yyyy HH:mm")
                    },
                    new ActivoFinanciero
                    {
                        Simbolo = "XAU-GOLD",
                        Nombre = "Oro Físico / PAX Gold (Onza Troy)",
                        Categoria = "Metales & Índices",
                        PrecioUsd = 2645.50,
                        Cantidad = 0.50,
                        TotalUsd = 1322.75,
                        TotalBs = Math.Round(1322.75 * 6.96, 2),
                        Variacion24h = 1.85,
                        Notas = "Cobertura contra inflación en oro spot",
                        FechaRegistro = DateTime.Now.AddHours(-1).ToString("dd/MM/yyyy HH:mm")
                    }
                };

                foreach (var item in iniciales)
                {
                    await _conexion.InsertAsync(item);
                }
            }
        }

        public Task<List<ActivoFinanciero>> ListaActivos()
        {
            return _conexion.Table<ActivoFinanciero>()
                            .OrderByDescending(a => a.IdActivo)
                            .ToListAsync();
        }

        public Task<int> GuardarActivo(ActivoFinanciero activo)
        {
            return _conexion.InsertAsync(activo);
        }

        public Task<int> ModificarActivo(ActivoFinanciero activo)
        {
            return _conexion.UpdateAsync(activo);
        }

        public Task<int> EliminarActivo(ActivoFinanciero activo)
        {
            return _conexion.DeleteAsync(activo);
        }
    }
}

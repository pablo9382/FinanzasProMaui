using System.Globalization;
using FinanzasProMaui.Data;
using FinanzasProMaui.Modelo;

namespace FinanzasProMaui.Vista;

public partial class MainPage : ContentPage
{
    private readonly DataBase _db;
    private readonly ApiFinanzasServicio _apiServicio = new();
    private ActivoFinanciero? _activoSeleccionado;

    public MainPage()
    {
        InitializeComponent();
        string carpeta = FileSystem.AppDataDirectory;
        Directory.CreateDirectory(carpeta);
        string rutaBD = Path.Combine(carpeta, "BDFinanzas.db3");
        _db = new DataBase(rutaBD);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _db.SembrarDatosInicialesAsync();
        await MostrarTablaDgvAsync();
    }

    private void OnValoresCambiaron(object? sender, TextChangedEventArgs e)
    {
        double precio = ParsearNumero(txtPrecioUsd.Text);
        double cantidad = ParsearNumero(txtCantidad.Text);
        double totalUsd = Math.Round(precio * cantidad, 2);
        double totalBs = Math.Round(totalUsd * 6.96, 2);
        lblCalculoVivo.Text = $"💵 Subtotal Calculado: $us {totalUsd:N2}  |  🇧🇴 Equivalente en Bolivianos: Bs. {totalBs:N2}";
    }

    private static double ParsearNumero(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return 0;
        string limpio = texto.Trim().Replace(",", ".");
        double.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        return val;
    }

    private async Task MostrarTablaDgvAsync()
    {
        var lista = await _db.ListaActivos();

        // Actualizar los 3 KPIs superiores en tiempo real
        lblKpiTotalActivos.Text = $"{lista.Count} Activos";
        double sumaUsd = lista.Sum(a => a.TotalUsd);
        double sumaBs = lista.Sum(a => a.TotalBs);
        lblKpiValorUsd.Text = $"$us {sumaUsd:N2}";
        lblKpiValorBs.Text = $"Equivalente: Bs. {sumaBs:N2}";

        double promVar = lista.Count > 0 ? lista.Average(a => a.Variacion24h) : 0;
        string signo = promVar >= 0 ? "+" : "";
        lblKpiVariacion.Text = $"{signo}{promVar:F2}%";
        lblKpiVariacion.TextColor = promVar >= 0 ? Color.FromArgb("#059669") : Color.FromArgb("#DC2626");

        // Renderizar filas en el Grid Dgv
        Dgv.Children.Clear();
        Dgv.RowDefinitions.Clear();

        for (int i = 0; i < lista.Count; i++)
        {
            var item = lista[i];
            Dgv.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            bool esSeleccionado = _activoSeleccionado is not null && _activoSeleccionado.IdActivo == item.IdActivo;
            Color fondo = esSeleccionado
                ? Color.FromArgb("#FEF3C7")
                : (i % 2 == 0 ? Colors.White : Color.FromArgb("#F8FAFC"));

            Color colorVar = item.Variacion24h >= 0 ? Color.FromArgb("#059669") : Color.FromArgb("#DC2626");
            string textoVar = (item.Variacion24h >= 0 ? "+" : "") + $"{item.Variacion24h:F2}%";

            AgregarCeldaDgv(i, 0, $"#{item.IdActivo}", fondo, Color.FromArgb("#0F172A"), true, item);
            AgregarCeldaDgv(i, 1, item.Simbolo, fondo, Color.FromArgb("#1D4ED8"), true, item);
            AgregarCeldaDgv(i, 2, item.Nombre, fondo, Color.FromArgb("#1E293B"), esSeleccionado, item);
            AgregarCeldaDgv(i, 3, item.Categoria, fondo, Color.FromArgb("#475569"), false, item);
            AgregarCeldaDgv(i, 4, $"${item.PrecioUsd:N2}", fondo, Color.FromArgb("#334155"), false, item);
            AgregarCeldaDgv(i, 5, $"{item.Cantidad:N2}", fondo, Color.FromArgb("#0F172A"), true, item);
            AgregarCeldaDgv(i, 6, $"${item.TotalUsd:N2}", fondo, Color.FromArgb("#047857"), true, item);
            AgregarCeldaDgv(i, 7, $"Bs. {item.TotalBs:N2}", fondo, Color.FromArgb("#B45309"), true, item);
            AgregarCeldaDgv(i, 8, textoVar, fondo, colorVar, true, item);

            var contenedorBtn = new ContentView
            {
                BackgroundColor = fondo,
                Padding = new Thickness(6, 6)
            };
            var btnSel = new Button
            {
                Text = esSeleccionado ? "🎯 Activo" : "✏️ Seleccionar",
                BackgroundColor = esSeleccionado ? Color.FromArgb("#D97706") : Color.FromArgb("#1E3A8A"),
                TextColor = Colors.White,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 6,
                HeightRequest = 32,
                Padding = new Thickness(6, 0)
            };
            btnSel.Clicked += (s, e) => SeleccionarFilaDgv(item);
            contenedorBtn.Content = btnSel;
            Dgv.Add(contenedorBtn, 9, i);
        }
    }

    private void AgregarCeldaDgv(int fila, int col, string texto, Color fondo, Color colorTexto, bool negrita, ActivoFinanciero item)
    {
        var celda = new ContentView
        {
            BackgroundColor = fondo,
            Padding = new Thickness(8, 10)
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) => SeleccionarFilaDgv(item);
        celda.GestureRecognizers.Add(tap);

        celda.Content = new Label
        {
            Text = texto,
            FontSize = 11.5,
            FontAttributes = negrita ? FontAttributes.Bold : FontAttributes.None,
            TextColor = colorTexto,
            VerticalTextAlignment = TextAlignment.Center
        };

        Dgv.Add(celda, col, fila);
    }

    private async void SeleccionarFilaDgv(ActivoFinanciero item)
    {
        _activoSeleccionado = item;
        CargarEnFormulario(item);
        badgeSeleccionDgv.IsVisible = true;
        lblSeleccionDgv.Text = $"🎯 SELECCIONADO DEL DGV (#{item.IdActivo} - {item.Simbolo})";
        await MostrarTablaDgvAsync();
    }

    private void CargarEnFormulario(ActivoFinanciero a)
    {
        txtSimbolo.Text = a.Simbolo;
        txtNombre.Text = a.Nombre;
        txtCategoria.Text = a.Categoria;
        txtPrecioUsd.Text = a.PrecioUsd.ToString("F2", CultureInfo.InvariantCulture);
        txtCantidad.Text = a.Cantidad.ToString("F2", CultureInfo.InvariantCulture);
        txtVariacion.Text = a.Variacion24h.ToString("F2", CultureInfo.InvariantCulture);
        txtNotas.Text = a.Notas;
    }

    // === CONSULTAS A LA API EXTERNA ===
    private async void OnBuscarApiClicked(object? sender, EventArgs e)
    {
        string termino = txtBuscarApi.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(termino))
        {
            await DisplayAlert("Buscar en API", "Escribe un símbolo o nombre de activo (ej. BTC, ETH, USDT, SOL, XAU, EUR).", "OK");
            return;
        }

        indicadorApi.IsVisible = true;
        indicadorApi.IsRunning = true;
        var (activo, origen) = await _apiServicio.BuscarActivoAsync(termino);
        indicadorApi.IsRunning = false;
        indicadorApi.IsVisible = false;

        if (activo is null)
        {
            await DisplayAlert("Sin resultados", $"No se encontró '{termino}' en la API financiera. Prueba con BTC, ETH, USDT, SOL, BNB, XRP, ADA, EUR, XAU o SP500.", "OK");
            return;
        }

        _activoSeleccionado = null;
        badgeSeleccionDgv.IsVisible = false;
        CargarEnFormulario(activo);
        lblEstadoApi.Text = $"✅ Cotización importada desde {origen}: {activo.Nombre} a $us {activo.PrecioUsd:N2}";
    }

    private async void OnApiCriptoClicked(object? sender, EventArgs e) => await CargarCategoriaApiAsync("Criptoactivos");
    private async void OnApiDivisasClicked(object? sender, EventArgs e) => await CargarCategoriaApiAsync("Divisas");
    private async void OnApiMetalesClicked(object? sender, EventArgs e) => await CargarCategoriaApiAsync("Metales");

    private async void OnApiSiguienteClicked(object? sender, EventArgs e)
    {
        indicadorApi.IsVisible = true;
        indicadorApi.IsRunning = true;
        var (activo, origen) = await _apiServicio.ObtenerSiguienteAsync();
        indicadorApi.IsRunning = false;
        indicadorApi.IsVisible = false;

        _activoSeleccionado = null;
        badgeSeleccionDgv.IsVisible = false;
        CargarEnFormulario(activo);
        lblEstadoApi.Text = $"✅ Importado de {origen}: {activo.Nombre} ($us {activo.PrecioUsd:N2})";
    }

    private async Task CargarCategoriaApiAsync(string categoria)
    {
        indicadorApi.IsVisible = true;
        indicadorApi.IsRunning = true;
        var (activo, origen) = await _apiServicio.ObtenerPorCategoriaAsync(categoria);
        indicadorApi.IsRunning = false;
        indicadorApi.IsVisible = false;

        _activoSeleccionado = null;
        badgeSeleccionDgv.IsVisible = false;
        CargarEnFormulario(activo);
        lblEstadoApi.Text = $"✅ [{categoria}] Importado de {origen}: {activo.Nombre} ($us {activo.PrecioUsd:N2})";
    }

    // === OPERACIONES CRUD EN SQLITE ===
    private async void OnGuardarClicked(object? sender, EventArgs e)
    {
        string simbolo = txtSimbolo.Text?.Trim().ToUpperInvariant() ?? string.Empty;
        string nombre = txtNombre.Text?.Trim() ?? string.Empty;
        double precio = ParsearNumero(txtPrecioUsd.Text);
        double cantidad = ParsearNumero(txtCantidad.Text);

        if (string.IsNullOrEmpty(simbolo) || string.IsNullOrEmpty(nombre) || precio <= 0 || cantidad <= 0)
        {
            await DisplayAlert("Datos Incompletos", "Ingresa Símbolo, Nombre, Precio ($us > 0) y Cantidad (> 0) antes de guardar.", "OK");
            return;
        }

        // Validación Anti-Duplicados
        var existentes = await _db.ListaActivos();
        bool duplicado = existentes.Any(a =>
            a.Simbolo.Equals(simbolo, StringComparison.OrdinalIgnoreCase) ||
            a.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));

        if (duplicado)
        {
            await DisplayAlert("Validación Anti-Duplicados", $"El activo '{nombre} ({simbolo})' ya está registrado en el portafolio SQLite. Selecciónalo en la tabla Dgv inferior si deseas modificar su cantidad o precio.", "OK");
            return;
        }

        double totalUsd = Math.Round(precio * cantidad, 2);
        var nuevo = new ActivoFinanciero
        {
            Simbolo = simbolo,
            Nombre = nombre,
            Categoria = string.IsNullOrWhiteSpace(txtCategoria.Text) ? "Criptoactivos" : txtCategoria.Text.Trim(),
            PrecioUsd = precio,
            Cantidad = cantidad,
            TotalUsd = totalUsd,
            TotalBs = Math.Round(totalUsd * 6.96, 2),
            Variacion24h = ParsearNumero(txtVariacion.Text),
            Notas = string.IsNullOrWhiteSpace(txtNotas.Text) ? "Posición registrada en portafolio" : txtNotas.Text.Trim(),
            FechaRegistro = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
        };

        await _db.GuardarActivo(nuevo);
        LimpiarFormulario();
        await MostrarTablaDgvAsync();
        await DisplayAlert("Guardado Exitoso", $"El activo '{nuevo.Nombre}' fue guardado en BDFinanzas.db3.", "OK");
    }

    private async void OnModificarClicked(object? sender, EventArgs e)
    {
        if (_activoSeleccionado is null)
        {
            await DisplayAlert("Selección Requerida", "Primero selecciona una fila de la tabla Dgv inferior (botón '✏️ Seleccionar') para modificarla.", "OK");
            return;
        }

        string simbolo = txtSimbolo.Text?.Trim().ToUpperInvariant() ?? string.Empty;
        string nombre = txtNombre.Text?.Trim() ?? string.Empty;
        double precio = ParsearNumero(txtPrecioUsd.Text);
        double cantidad = ParsearNumero(txtCantidad.Text);

        if (string.IsNullOrEmpty(simbolo) || string.IsNullOrEmpty(nombre) || precio <= 0 || cantidad <= 0)
        {
            await DisplayAlert("Datos Inválidos", "Verifica que Símbolo, Nombre, Precio y Cantidad sean válidos.", "OK");
            return;
        }

        double totalUsd = Math.Round(precio * cantidad, 2);
        _activoSeleccionado.Simbolo = simbolo;
        _activoSeleccionado.Nombre = nombre;
        _activoSeleccionado.Categoria = string.IsNullOrWhiteSpace(txtCategoria.Text) ? _activoSeleccionado.Categoria : txtCategoria.Text.Trim();
        _activoSeleccionado.PrecioUsd = precio;
        _activoSeleccionado.Cantidad = cantidad;
        _activoSeleccionado.TotalUsd = totalUsd;
        _activoSeleccionado.TotalBs = Math.Round(totalUsd * 6.96, 2);
        _activoSeleccionado.Variacion24h = ParsearNumero(txtVariacion.Text);
        _activoSeleccionado.Notas = txtNotas.Text?.Trim() ?? string.Empty;
        _activoSeleccionado.FechaRegistro = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        await _db.ModificarActivo(_activoSeleccionado);
        LimpiarFormulario();
        await MostrarTablaDgvAsync();
        await DisplayAlert("Modificación Exitosa", "El registro seleccionado en el Dgv fue actualizado en SQLite.", "OK");
    }

    private async void OnEliminarClicked(object? sender, EventArgs e)
    {
        if (_activoSeleccionado is null)
        {
            await DisplayAlert("Selección Requerida", "Primero selecciona una fila de la tabla Dgv inferior para poder eliminarla.", "OK");
            return;
        }

        bool confirmar = await DisplayAlert("Confirmar Eliminación", $"¿Deseas eliminar '{_activoSeleccionado.Nombre}' del portafolio SQLite?", "Sí, Eliminar", "Cancelar");
        if (!confirmar)
            return;

        await _db.EliminarActivo(_activoSeleccionado);
        LimpiarFormulario();
        await MostrarTablaDgvAsync();
    }

    private async void OnLimpiarClicked(object? sender, EventArgs e)
    {
        LimpiarFormulario();
        await MostrarTablaDgvAsync();
    }

    private void LimpiarFormulario()
    {
        _activoSeleccionado = null;
        txtSimbolo.Text = string.Empty;
        txtNombre.Text = string.Empty;
        txtCategoria.Text = string.Empty;
        txtPrecioUsd.Text = string.Empty;
        txtCantidad.Text = string.Empty;
        txtVariacion.Text = string.Empty;
        txtNotas.Text = string.Empty;
        badgeSeleccionDgv.IsVisible = false;
        lblCalculoVivo.Text = "💵 Subtotal Calculado: $us 0.00  |  🇧🇴 Equivalente en Bolivianos: Bs. 0.00";
    }
}

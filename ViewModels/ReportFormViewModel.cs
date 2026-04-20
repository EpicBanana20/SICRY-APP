using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SICRY_APP.Models;
using SICRY_APP.Services;
using System.Collections.ObjectModel;

namespace SICRY_APP.ViewModels
{
    [QueryProperty(nameof(AsignacionSeleccionada), "Asignacion")]
    [QueryProperty(nameof(ReporteExistente), "Reporte")]
    public partial class ReportFormViewModel : ObservableObject
    {
        [ObservableProperty] private Asignacion asignacionSeleccionada;
        [ObservableProperty] private ReporteItem reporteExistente;
        [ObservableProperty] private string tipoReporte; // electricista | embobinado | mantenimiento
        [ObservableProperty] private string tituloPantalla;
        [ObservableProperty] private string descripcion;
        [ObservableProperty] private bool esConclusivo;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool modoEdicion;
        [ObservableProperty] private bool modoCreacion = true;
        [ObservableProperty] private string ubicacionTexto;

        // Catálogos
        [ObservableProperty] private ObservableCollection<CategoriaFallo> categoriasFallos;
        [ObservableProperty] private ObservableCollection<Refaccion> refacciones;
        [ObservableProperty] private ObservableCollection<Motor> motores;

        // Seleccionados
        [ObservableProperty] private ObservableCollection<CategoriaFallo> fallosSeleccionados;
        [ObservableProperty] private ObservableCollection<RefaccionSeleccionada> refaccionesSeleccionadas;
        [ObservableProperty] private ObservableCollection<FileResult> fotosSeleccionadas;

        // Pickers
        [ObservableProperty] private CategoriaFallo falloParaAgregar;
        [ObservableProperty] private Refaccion refaccionParaAgregar;
        [ObservableProperty] private int cantidadRefaccion = 1;
        [ObservableProperty] private Motor motorSeleccionado;
        [ObservableProperty] private bool mostrarMotor; // true si es embobinado o mantenimiento

        public ReportFormViewModel()
        {
            CategoriasFallos = new();
            Refacciones = new();
            Motores = new();
            FallosSeleccionados = new();
            RefaccionesSeleccionadas = new();
            FotosSeleccionadas = new();
        }

        partial void OnAsignacionSeleccionadaChanged(Asignacion value)
        {
            if (value == null) return;
            UbicacionTexto = value.Pozo;
            _ = InicializarAsync();
        }

        partial void OnReporteExistenteChanged(ReporteItem value)
        {
            if (value == null) return;
            ModoEdicion = true;
            ModoCreacion = false;
            TipoReporte = value.Tipo;
            Descripcion = value.Descripcion;
            EsConclusivo = value.EsConclusivo;
            UbicacionTexto = value.Ubicacion;
            TituloPantalla = value.Tipo switch
            {
                "electricista" => "Editar Reporte Eléctrico",
                "embobinado" => "Editar Reporte de Embobinado",
                "mantenimiento" => "Editar Reporte de Mantenimiento",
                _ => "Editar Reporte"
            };
            MostrarMotor = false; // motor no editable aquí
        }

        private async Task InicializarAsync()
        {
            // Determinar tipo de reporte según rol
            var perfil = await ApiService.Instance.GetPerfilDesdeTokenAsync();
            TipoReporte = perfil?.IdRol switch
            {
                1 => "electricista",  // Electricista
                5 => "electricista",  // Supervisor → electricista
                6 => "embobinado",    // Embobinador
                4 => "mantenimiento", // Mecánico
                _ => "electricista"
            };

            TituloPantalla = TipoReporte switch
            {
                "electricista" => "Reporte de Electricista",
                "embobinado" => "Reporte de Embobinado",
                "mantenimiento" => "Reporte de Mantenimiento",
                _ => "Nuevo Reporte"
            };

            MostrarMotor = TipoReporte is "embobinado" or "mantenimiento";

            // Cargar catálogos
            foreach (var c in await ApiService.Instance.GetCategoriasFallosAsync())
                CategoriasFallos.Add(c);
            foreach (var r in await ApiService.Instance.GetRefaccionesAsync())
                Refacciones.Add(r);
            if (MostrarMotor)
            {
                foreach (var m in await ApiService.Instance.GetMotoresAsync())
                    Motores.Add(m);
            }
        }

        [RelayCommand]
        private void AgregarFallo()
        {
            if (FalloParaAgregar == null) return;
            if (!FallosSeleccionados.Any(f => f.IdCategoriaFallo == FalloParaAgregar.IdCategoriaFallo))
                FallosSeleccionados.Add(FalloParaAgregar);
            FalloParaAgregar = null;
        }

        [RelayCommand]
        private void QuitarFallo(CategoriaFallo fallo)
        {
            if (fallo != null) FallosSeleccionados.Remove(fallo);
        }

        [RelayCommand]
        private void AgregarRefaccion()
        {
            if (RefaccionParaAgregar == null || CantidadRefaccion <= 0) return;
            RefaccionesSeleccionadas.Add(new RefaccionSeleccionada
            {
                Refaccion = RefaccionParaAgregar,
                Cantidad = CantidadRefaccion
            });
            RefaccionParaAgregar = null;
            CantidadRefaccion = 1;
        }

        [RelayCommand]
        private void QuitarRefaccion(RefaccionSeleccionada item)
        {
            if (item != null) RefaccionesSeleccionadas.Remove(item);
        }

        [RelayCommand]
        private async Task TomarFotoAsync()
        {
            try
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "La cámara no está disponible.", "OK");
                    return;
                }
                var foto = await MediaPicker.Default.CapturePhotoAsync();
                if (foto != null) FotosSeleccionadas.Add(foto);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task ElegirFotoAsync()
        {
            try
            {
                var foto = await MediaPicker.Default.PickPhotoAsync();
                if (foto != null) FotosSeleccionadas.Add(foto);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private void QuitarFoto(FileResult foto)
        {
            if (foto != null) FotosSeleccionadas.Remove(foto);
        }

        [RelayCommand]
        private async Task GuardarReporteAsync()
        {
            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                await Application.Current.MainPage.DisplayAlert("Aviso", "La descripción es obligatoria.", "OK");
                return;
            }

            // Rama: modo edición
            if (ModoEdicion && ReporteExistente != null)
            {
                try
                {
                    IsBusy = true;
                    ReporteExistente.Descripcion = Descripcion;
                    ReporteExistente.EsConclusivo = EsConclusivo;
                    var ok = await ApiService.Instance.ActualizarReporteAsync(ReporteExistente);
                    if (!ok)
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "No se pudo actualizar el reporte.", "OK");
                        return;
                    }
                    await Application.Current.MainPage.DisplayAlert("Éxito", "Reporte actualizado.", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                finally { IsBusy = false; }
                return;
            }

            if (MostrarMotor && MotorSeleccionado == null)
            {
                await Application.Current.MainPage.DisplayAlert("Aviso", "Selecciona un motor.", "OK");
                return;
            }

            try
            {
                IsBusy = true;
                int idReporte = 0;

                // 1. Crear reporte según tipo
                switch (TipoReporte)
                {
                    case "electricista":
                        int idPozo = AsignacionSeleccionada.IdPozo ?? 0;
                        System.Diagnostics.Debug.WriteLine($"===== POST electricista: idAsig={AsignacionSeleccionada.IdAsignacion}, idPozo={idPozo}, conclusivo={EsConclusivo} =====");
                        idReporte = await ApiService.Instance.CrearReporteElectricistaAsync(
                            AsignacionSeleccionada.IdAsignacion, idPozo, EsConclusivo, Descripcion);
                        System.Diagnostics.Debug.WriteLine($"===== RESPUESTA idReporte={idReporte} =====");
                        break;
                    case "embobinado":
                        idReporte = await ApiService.Instance.CrearReporteEmbobinadoAsync(
                            AsignacionSeleccionada.IdAsignacion, MotorSeleccionado.IdMotor, EsConclusivo, Descripcion);
                        break;
                    case "mantenimiento":
                        idReporte = await ApiService.Instance.CrearReporteMantenimientoAsync(
                            AsignacionSeleccionada.IdAsignacion, MotorSeleccionado.IdMotor, EsConclusivo, Descripcion);
                        break;
                }

                if (idReporte == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No se pudo crear el reporte.", "OK");
                    return;
                }

                // 2. Agregar fallos
                foreach (var f in FallosSeleccionados)
                    await ApiService.Instance.AgregarFalloReportadoAsync(f.IdCategoriaFallo, TipoReporte, idReporte);

                // 3. Agregar refacciones usadas
                foreach (var r in RefaccionesSeleccionadas)
                    await ApiService.Instance.AgregarRefaccionUsadaAsync(r.Refaccion.IdRefaccion, r.Cantidad, TipoReporte, idReporte);

                // 4. Subir evidencias (fotos)
                foreach (var foto in FotosSeleccionadas)
                {
                    using var stream = await foto.OpenReadAsync();
                    var ext = System.IO.Path.GetExtension(foto.FileName);
                    if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                    await ApiService.Instance.SubirEvidenciaAsync(
                        stream, ext, AsignacionSeleccionada.IdAsignacion, TipoReporte, idReporte);
                }

                // 5. Si es conclusivo → marcar asignación como Completada
                if (EsConclusivo)
                    await ApiService.Instance.CambiarEstadoAsignacionAsync(AsignacionSeleccionada.IdAsignacion, "Completada");

                await Application.Current.MainPage.DisplayAlert("Éxito", "Reporte guardado correctamente.", "OK");
                await Shell.Current.GoToAsync("..");
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task CancelarAsync() => await Shell.Current.GoToAsync("..");
    }
}
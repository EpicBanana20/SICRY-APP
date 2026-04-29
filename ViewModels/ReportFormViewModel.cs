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
        private bool _esConclusivoPrevio;

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

        [ObservableProperty] private bool tieneHorasExtras;
        [ObservableProperty] private int horasExtras = 1;

        // Datos existentes (modo edición)
        [ObservableProperty] private ObservableCollection<FalloDeReporte> fallosExistentes = new();
        [ObservableProperty] private ObservableCollection<RefaccionDeReporte> refaccionesExistentes = new();
        [ObservableProperty] private ObservableCollection<EvidenciaDeReporte> evidenciasExistentes = new();

        // Overlay de imagen expandida
        [ObservableProperty] private string imagenExpandida;
        [ObservableProperty] private bool mostrarImagenExpandida;

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
            _esConclusivoPrevio = value.EsConclusivo;
            UbicacionTexto = value.Ubicacion;
            TieneHorasExtras = value.TieneHorasExtras;
            HorasExtras = value.HorasExtras > 0 ? value.HorasExtras : 1;
            TituloPantalla = value.Tipo switch
            {
                "electricista" => "Editar Reporte Eléctrico",
                "embobinado" => "Editar Reporte de Embobinado",
                "mantenimiento" => "Editar Reporte de Mantenimiento",
                _ => "Editar Reporte"
            };
            MostrarMotor = false; // motor no editable aquí
            _ = CargarDetallesReporteAsync(value);
        }

        private async Task CargarDetallesReporteAsync(ReporteItem reporte)
        {
            var fallos = await ApiService.Instance.GetFallosDeReporteAsync(reporte.Tipo, reporte.Id);
            FallosExistentes.Clear();
            foreach (var f in fallos) FallosExistentes.Add(f);

            var refacciones = await ApiService.Instance.GetRefaccionesDeReporteAsync(reporte.Tipo, reporte.Id);
            RefaccionesExistentes.Clear();
            foreach (var r in refacciones) RefaccionesExistentes.Add(r);

            var evidencias = await ApiService.Instance.GetEvidenciasDeReporteAsync(reporte.Tipo, reporte.Id);
            EvidenciasExistentes.Clear();
            foreach (var e in evidencias) EvidenciasExistentes.Add(e);

            // Cargar catálogos para los pickers de edición
            if (CategoriasFallos.Count == 0)
                foreach (var c in await ApiService.Instance.GetCategoriasFallosAsync())
                    CategoriasFallos.Add(c);

            if (Refacciones.Count == 0)
                foreach (var r in await ApiService.Instance.GetRefaccionesAsync())
                    Refacciones.Add(r);
        }

        partial void OnTieneHorasExtrasChanged(bool value)
        {
            if (!value) HorasExtras = 1;
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
        private async Task EliminarFalloExistenteAsync(FalloDeReporte fallo)
        {
            if (fallo == null) return;
            var ok = await ApiService.Instance.EliminarFalloReportadoAsync(fallo.IdFalloReportado);
            if (ok)
                FallosExistentes.Remove(fallo);
            else
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo eliminar el fallo.", "OK");
        }

        [RelayCommand]
        private async Task AgregarFalloEnEdicionAsync()
        {
            if (FalloParaAgregar == null || ReporteExistente == null) return;
            var ok = await ApiService.Instance.AgregarFalloReportadoAsync(
                FalloParaAgregar.IdCategoriaFallo, ReporteExistente.Tipo, ReporteExistente.Id);
            if (ok)
            {
                // Refrescar la lista para obtener el ID real del nuevo fallo
                var actualizados = await ApiService.Instance.GetFallosDeReporteAsync(ReporteExistente.Tipo, ReporteExistente.Id);
                FallosExistentes.Clear();
                foreach (var f in actualizados) FallosExistentes.Add(f);
                FalloParaAgregar = null;
            }
            else
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo agregar el fallo.", "OK");
        }

        [RelayCommand]
        private async Task EliminarRefaccionExistenteAsync(RefaccionDeReporte refaccion)
        {
            if (refaccion == null) return;
            var ok = await ApiService.Instance.EliminarRefaccionUsadaAsync(refaccion.IdRefaccionesUsadas);
            if (ok)
                RefaccionesExistentes.Remove(refaccion);
            else
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo eliminar la refacción.", "OK");
        }

        [RelayCommand]
        private async Task AgregarRefaccionEnEdicionAsync()
        {
            if (RefaccionParaAgregar == null || ReporteExistente == null || CantidadRefaccion <= 0) return;
            var ok = await ApiService.Instance.AgregarRefaccionUsadaAsync(
                RefaccionParaAgregar.IdRefaccion, CantidadRefaccion, ReporteExistente.Tipo, ReporteExistente.Id);
            if (ok)
            {
                var actualizadas = await ApiService.Instance.GetRefaccionesDeReporteAsync(ReporteExistente.Tipo, ReporteExistente.Id);
                RefaccionesExistentes.Clear();
                foreach (var r in actualizadas) RefaccionesExistentes.Add(r);
                RefaccionParaAgregar = null;
                CantidadRefaccion = 1;
            }
            else
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo agregar la refacción.", "OK");
        }

        [RelayCommand]
        private async Task EliminarEvidenciaExistenteAsync(EvidenciaDeReporte evidencia)
        {
            if (evidencia == null) return;
            var ok = await ApiService.Instance.EliminarEvidenciaAsync(evidencia.IdEvidencias);
            if (ok)
                EvidenciasExistentes.Remove(evidencia);
            else
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo eliminar la evidencia.", "OK");
        }

        [RelayCommand]
        private void IncrementarHoras()
        {
            HorasExtras++;
        }

        [RelayCommand]
        private void DecrementarHoras()
        {
            if (HorasExtras > 1) HorasExtras--;
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
                    ReporteExistente.TieneHorasExtras = TieneHorasExtras;
                    ReporteExistente.HorasExtras = HorasExtras;
                    var ok = await ApiService.Instance.ActualizarReporteAsync(ReporteExistente);
                    if (!ok)
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "No se pudo actualizar el reporte.", "OK");
                        return;
                    }

                    // Subir fotos nuevas agregadas en edición
                    bool huboErroresFotos = false;
                    foreach (var foto in FotosSeleccionadas)
                    {
                        using var stream = await foto.OpenReadAsync();
                        var ext = System.IO.Path.GetExtension(foto.FileName);
                        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                        var url = await ApiService.Instance.SubirEvidenciaAsync(
                            stream, ext, ReporteExistente.IdAsignacion, ReporteExistente.Tipo, ReporteExistente.Id);
                        if (url == null) huboErroresFotos = true;
                    }

                    // Actualizar estado de la asignación si EsConclusivo cambió
                    if (EsConclusivo && !_esConclusivoPrevio)
                        await ApiService.Instance.CambiarEstadoAsignacionAsync(ReporteExistente.IdAsignacion, "Completada");
                    else if (!EsConclusivo && _esConclusivoPrevio)
                        await ApiService.Instance.CambiarEstadoAsignacionAsync(ReporteExistente.IdAsignacion, "Inconclusa");

                    string msg = huboErroresFotos
                        ? "Reporte actualizado, pero algunas fotos no pudieron subirse."
                        : "Reporte actualizado correctamente.";
                    await Application.Current.MainPage.DisplayAlert("Éxito", msg, "OK");
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
                        System.Diagnostics.Debug.WriteLine($"===== POST electricista: idAsig={AsignacionSeleccionada.IdAsignacion}, idPozo={idPozo}, conclusivo={EsConclusivo}, HE={TieneHorasExtras}/{HorasExtras} =====");
                        idReporte = await ApiService.Instance.CrearReporteElectricistaAsync(
                            AsignacionSeleccionada.IdAsignacion, idPozo, EsConclusivo, Descripcion,
                            TieneHorasExtras, HorasExtras);
                        System.Diagnostics.Debug.WriteLine($"===== RESPUESTA idReporte={idReporte} =====");
                        break;
                    case "embobinado":
                        idReporte = await ApiService.Instance.CrearReporteEmbobinadoAsync(
                            AsignacionSeleccionada.IdAsignacion, MotorSeleccionado.IdMotor, EsConclusivo, Descripcion,
                            TieneHorasExtras, HorasExtras);
                        break;
                    case "mantenimiento":
                        idReporte = await ApiService.Instance.CrearReporteMantenimientoAsync(
                            AsignacionSeleccionada.IdAsignacion, MotorSeleccionado.IdMotor, EsConclusivo, Descripcion,
                            TieneHorasExtras, HorasExtras);
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
                bool huboErroresFotos = false;

                foreach (var foto in FotosSeleccionadas)
                {
                    using var stream = await foto.OpenReadAsync();
                    var ext = System.IO.Path.GetExtension(foto.FileName);
                    if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

                    var urlResult = await ApiService.Instance.SubirEvidenciaAsync(
                        stream, ext, AsignacionSeleccionada.IdAsignacion, TipoReporte, idReporte);

                    if (urlResult == null)
                    {
                        huboErroresFotos = true;
                    }
                }

                // 5. Actualizar estado de asignación según conclusividad
                string estadoAsignacion = EsConclusivo ? "Completada" : "Inconclusa";
                await ApiService.Instance.CambiarEstadoAsignacionAsync(AsignacionSeleccionada.IdAsignacion, estadoAsignacion);

                // 6. Alertas finales
                if (huboErroresFotos)
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "El reporte se guardó, pero algunas evidencias no pudieron subirse por problemas de red.", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Éxito", "Reporte y evidencias guardados correctamente.", "OK");
                }

                await Shell.Current.GoToAsync("..");
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void ExpandirImagen(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            ImagenExpandida = url;
            MostrarImagenExpandida = true;
        }

        [RelayCommand]
        private void CerrarImagen()
        {
            MostrarImagenExpandida = false;
            ImagenExpandida = null;
        }

        [RelayCommand]
        private async Task CancelarAsync() => await Shell.Current.GoToAsync("..");
    }
}
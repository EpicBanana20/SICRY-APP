namespace SICRY_APP.Models
{
    public class Reporte
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Estado { get; set; }
        public string Ubicacion { get; set; }
        public string Descripcion { get; set; }
        public string ColorEstadoFondo { get; set; }
        public string ColorEstadoTexto { get; set; }
    }
}
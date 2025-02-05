using PeliculasAPI.Utilidades;
using PeliculasAPI.Validaciones;
using System.ComponentModel.DataAnnotations;

namespace PeliculasAPI.Entidades
{
    public class Genero
    {
        public int Id { get; set; }
        [Required (ErrorMessage = MensajesErroresValidaciones.CampoRequerido)]
        [StringLength(50, ErrorMessage = MensajesErroresValidaciones.LongitudString)]
        [PrimeraLetraMayuscula]
        public required string Nombre { get; set; }
    }
}

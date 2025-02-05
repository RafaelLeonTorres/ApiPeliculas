using PeliculasAPI.Utilidades;
using PeliculasAPI.Validaciones;
using System.ComponentModel.DataAnnotations;

namespace PeliculasAPI.DTOs
{
    public class GeneroCreacionDTO
    {
        [Required(ErrorMessage = MensajesErroresValidaciones.CampoRequerido)]
        [StringLength(50, ErrorMessage = MensajesErroresValidaciones.LongitudString)]
        [PrimeraLetraMayuscula]
        public required string Nombre { get; set; }
    }
}

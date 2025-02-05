using PeliculasAPI.Utilidades;
using System.ComponentModel.DataAnnotations;

namespace PeliculasAPI.Validaciones
{
    public class PrimeraLetraMayusculaAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }
            else 
            {
                var primeraLetra = value.ToString()![0].ToString();
                if (primeraLetra == primeraLetra.ToLower()) 
                {
                    return new ValidationResult(MensajesErroresValidaciones.PrimeraLetraMayuscula);
                }
                return ValidationResult.Success;
            }
        }
    }
}

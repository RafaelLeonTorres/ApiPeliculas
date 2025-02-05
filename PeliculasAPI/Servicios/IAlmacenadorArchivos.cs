namespace PeliculasAPI.Servicios
{
    public interface IAlmacenadorArchivos
    {
        Task<string> Almacenar(string contenedor, IFormFile archivo);
        Task Borrar(string? ruta, string contenedor);
        async Task<string> Editar(string? ruta, string contenedor, IFormFile archivo) 
        {
            // Verifica que la ruta y contenedor sean válidos.
            if (string.IsNullOrEmpty(ruta) || string.IsNullOrEmpty(contenedor))
            {
                throw new ArgumentException("Ruta o contenedor no pueden ser nulos o vacíos.");
            }

            // Intenta borrar el archivo antes de almacenarlo.
            try
            {
                await Borrar(ruta, contenedor);
            }
            catch (Exception ex)
            {
                // Aquí podrías registrar el error si es necesario, o lanzar una excepción.
                throw new InvalidOperationException("Error al intentar borrar el archivo.", ex);
            }

            // Luego almacena el archivo en el contenedor.
            return await Almacenar(contenedor, archivo);
        }
    }
}

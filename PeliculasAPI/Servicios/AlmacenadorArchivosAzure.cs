
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PeliculasAPI.Servicios
{
    public class AlmacenadorArchivosAzure : IAlmacenadorArchivos
    {
        private readonly string connectionString;
        public ILogger Logger { get; }
        public AlmacenadorArchivosAzure(IConfiguration configuration, ILogger<AlmacenadorArchivosAzure> logger)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection")!;
            Logger = logger;
        }
        

        public async Task<string> Almacenar(string contenedor, IFormFile archivo)
        {
            try
            {
                // Crea el cliente del contenedor de Blob.
                var cliente = new BlobContainerClient(connectionString, contenedor);

                // Crea el contenedor si no existe.
                await cliente.CreateIfNotExistsAsync();
                await cliente.SetAccessPolicyAsync(PublicAccessType.Blob);

                // Obtener la extensión y crear un nombre único para el archivo.
                var extension = Path.GetExtension(archivo.FileName);
                var nombreArchivo = $"{Guid.NewGuid()}{extension}";

                // Obtener el BlobClient para el archivo.
                var blob = cliente.GetBlobClient(nombreArchivo);

                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = archivo.ContentType
                };

                // Subir el archivo al contenedor de Blob.
                await blob.UploadAsync(archivo.OpenReadStream(), blobHttpHeaders);

                // Devolver la URL del archivo almacenado.
                return blob.Uri.ToString();
            }
            catch (ArgumentNullException ex)
            {
                // Maneja excepciones de argumentos nulos, como la ruta o el archivo nulo.
                Logger.LogError(ex, "Los parámetros de entrada no pueden ser nulos.");
                throw new InvalidOperationException("Los parámetros de entrada no pueden ser nulos.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Maneja excepciones de acceso no autorizado.
                Logger.LogError(ex, "No se tiene permiso para acceder al contenedor o realizar la operación.");
                throw new InvalidOperationException("No se tiene permiso para acceder al contenedor o realizar la operación.", ex);
            }
            catch (Azure.RequestFailedException ex)
            {
                // Maneja excepciones específicas de almacenamiento en Blob.
                Logger.LogError(ex, "Hubo un problema al interactuar con el contenedor de Blob.");
               throw new InvalidOperationException("Hubo un problema al interactuar con el contenedor de Blob.", ex);
            }
            catch (Exception ex)
            {
                // Captura cualquier otra excepción general.
                Logger.LogError(ex, "Ocurrió un error inesperado al almacenar el archivo.");
                throw new InvalidOperationException("Ocurrió un error inesperado al almacenar el archivo.", ex);
            }
        }


        public async Task Borrar(string? ruta, string contenedor)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                Logger.LogInformation("La ruta está vacía o es nula. No se realizará ninguna acción de borrado.");
                return;
            }

            try
            {
                Logger.LogInformation("Iniciando el proceso de borrado del archivo en la ruta: {Ruta} del contenedor: {Contenedor}", ruta, contenedor);

                // Crea el cliente del contenedor de Blob.
                var cliente = new BlobContainerClient(connectionString, contenedor);

                // Crea el contenedor si no existe.
                await cliente.CreateIfNotExistsAsync();
                Logger.LogInformation("Contenedor {Contenedor} creado si no existía o ya estaba disponible.", contenedor);

                // Obtener el nombre del archivo desde la ruta.
                var nombreArchivo = Path.GetFileName(ruta);
                var blob = cliente.GetBlobClient(nombreArchivo);

                // Intentar borrar el blob si existe.
                var resultado = await blob.DeleteIfExistsAsync();

                if (resultado)
                {
                    Logger.LogInformation("Archivo {NombreArchivo} borrado exitosamente del contenedor {Contenedor}.", nombreArchivo, contenedor);
                }
                else
                {
                    Logger.LogWarning("El archivo {NombreArchivo} no existía en el contenedor {Contenedor} y no se pudo borrar.", nombreArchivo, contenedor);
                }
            }
            catch (ArgumentNullException ex)
            {
                // Maneja casos donde la ruta o contenedor son nulos.
                Logger.LogError(ex, "La ruta o el contenedor son nulos.");
                throw new InvalidOperationException("La ruta o el contenedor no pueden ser nulos.", ex);
            }
            catch (Azure.RequestFailedException ex)
            {
                // Maneja errores específicos de Azure Blob Storage.
                Logger.LogError(ex, "Hubo un error al interactuar con Azure Blob Storage.");
                throw new InvalidOperationException("Error al intentar borrar el archivo del almacenamiento.", ex);
            }
            catch (Exception ex)
            {
                // Captura cualquier otro error no anticipado.
                Logger.LogError(ex, "Ocurrió un error inesperado al intentar borrar el archivo.");
                throw new InvalidOperationException("Ocurrió un error inesperado al intentar borrar el archivo.", ex);
            }
        }
    }
}

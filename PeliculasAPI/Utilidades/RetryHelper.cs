namespace PeliculasAPI.Utilidades
{
    using Polly;
    using System;
    using System.Threading.Tasks;
    public static class RetryHelper
    {
        /// <summary>
        /// Ejecuta una acción async con política de reintentos.
        /// </summary>
        /// <typeparam name="T">Tipo de retorno</typeparam>
        /// <param name="operation">Función a ejecutar</param>
        /// <param name="retryCount">Número de intentos</param>
        /// <param name="waitDuration">Tiempo de espera entre reintentos</param>
        /// <returns>Resultado de la operación</returns>
        public static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int retryCount = 3, TimeSpan? waitDuration = null)
        {
            waitDuration ??= TimeSpan.FromSeconds(2);

            var retryPolicy = Policy
                .Handle<Exception>() // Puedes especificar excepciones específicas si quieres
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => waitDuration.Value,
                    (exception, timeSpan, retryAttempt, context) =>
                    {
                        // Aquí puedes hacer logging si quieres
                        Console.WriteLine($"Intento {retryAttempt} fallido. Esperando {timeSpan.TotalSeconds} segundos. Error: {exception.Message}");
                    });

            return await retryPolicy.ExecuteAsync(operation);
        }

        public static async Task ExecuteWithRetryAsync(Func<Task> operation, int retryCount = 3, TimeSpan? waitDuration = null)
        {
            waitDuration ??= TimeSpan.FromSeconds(2);

            await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(retryCount, _ => waitDuration.Value)
                .ExecuteAsync(operation);
        }
    }
}

namespace PeliculasAPI.DTOs
{
    public class PaginacionDTO
    {
        public int Pagina { get; set; } = 1;

        private int registrosPorPagina = 10;
        private readonly int cantidadMazimaRegistrosPorPagina = 50;

        public int RegistrosPorPagina 
        {
            get { return registrosPorPagina; }
            set 
            {
                registrosPorPagina = (value > cantidadMazimaRegistrosPorPagina) ? cantidadMazimaRegistrosPorPagina : value;
            }
        }
    }
}

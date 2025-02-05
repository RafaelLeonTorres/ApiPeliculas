using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeliculasAPI.Data;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Servicios;
using PeliculasAPI.Utilidades;

namespace PeliculasAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActoresController : ControllerBase
    {
        private readonly IOutputCacheStore OutputCacheStore;
        private readonly ApplicationDbContext context;
        private readonly ILogger<GenerosController> Logger;
        private readonly IMapper Mapper;
        private readonly IAlmacenadorArchivos AlmacenadorArchivos;

        public ActoresController(IOutputCacheStore outputCacheStore, ApplicationDbContext applicationDbContext, ILogger<GenerosController> logger,
            IMapper mapper, IAlmacenadorArchivos almacenadorArchivos)
        {
            OutputCacheStore = outputCacheStore;
            context = applicationDbContext;
            Logger = logger;
            Mapper = mapper;
            AlmacenadorArchivos = almacenadorArchivos;
        }


        [HttpGet]
        [OutputCache(Tags = new[] { ConstantesString.cacheTagActores })]
        public async Task<ActionResult<IEnumerable<ActorDTO>>> Get([FromQuery] PaginacionDTO paginacion)
        {
            try
            {
                var queryable = context.Actores.AsQueryable();
                await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);

                var actores = await queryable
                    .OrderBy(g => g.Nombre)
                    .Paginar(paginacion)
                    .ProjectTo<ActorDTO>(Mapper.ConfigurationProvider)
                    .OrderBy(x => x.Nombre)
                    .ToListAsync();

                Logger.LogInformation("Se obtuvieron {Count} actores exitosamente.", actores.Count);
                return Ok(actores);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al obtener los actores.");
                return StatusCode(500, "Ocurrió un error interno al procesar la solicitud.");
            }
        }

        [HttpGet("{id:int}", Name = "Se creo el actor")]
        [OutputCache(Tags = new[] { ConstantesString.cacheTagActores })]
        public async Task<ActionResult<ActorDTO>> GetById(int id)
        {
            try
            {
                var actor = await context.Actores
                    .ProjectTo<ActorDTO>(Mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (actor == null)
                {
                    Logger.LogWarning("No se encontró el actor con ID {Id}.", id);
                    return NotFound();
                }

                Logger.LogInformation("Se encontró el actor con ID {Id}.", id);
                return Ok(actor);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al obtener el actor con ID {Id}.", id);
                return StatusCode(500, "Ocurrió un error interno al procesar la solicitud.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromForm] ActorCreacionDTO actorCreacioDTO)
        {              
            var actor = Mapper.Map<Actor>(actorCreacioDTO);

            if (actorCreacioDTO.Foto is not null) 
            {
                var url = await AlmacenadorArchivos.Almacenar(ConstantesString.contenedorActores, actorCreacioDTO.Foto);
                actor.Foto = url;
            }

               
            context.Add(actor);
            await context.SaveChangesAsync();

            // Invalidar la caché
            await OutputCacheStore.EvictByTagAsync(ConstantesString.cacheTagActores, default);
            return NoContent();
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] GeneroCreacionDTO generoCreacionDTO)
        {
            try
            {
                if (generoCreacionDTO == null)
                {
                    Logger.LogWarning("El DTO de actualización de género es nulo.");
                    return BadRequest("El DTO de actualización de género no puede ser nulo.");
                }

                var generoExiste = await context.Generos.AnyAsync(g => g.Id == id);

                if (!generoExiste)
                {
                    Logger.LogWarning("No se encontró el género con ID {Id} para actualizar.", id);
                    return NotFound();
                }

                var genero = Mapper.Map<Genero>(generoCreacionDTO);
                genero.Id = id;

                context.Update(genero);
                await context.SaveChangesAsync();
                await OutputCacheStore.EvictByTagAsync(ConstantesString.cacheTagGeneros, default);

                Logger.LogInformation("Se actualizó el género con ID {Id}.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al actualizar el género con ID {Id}.", id);
                return StatusCode(500, "Ocurrió un error interno al procesar la solicitud.");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var registrosBorrados = await context.Generos
                    .Where(g => g.Id == id)
                    .ExecuteDeleteAsync();

                if (registrosBorrados == 0)
                {
                    Logger.LogWarning("No se encontró el género con ID {Id} para eliminar.", id);
                    return NotFound("No se encontró el género con ID {Id} para eliminar. " + id);
                }

                await OutputCacheStore.EvictByTagAsync(ConstantesString.cacheTagGeneros, default);

                Logger.LogInformation("Se eliminó el género con ID {Id}.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al eliminar el género con ID {Id}.", id);
                return StatusCode(500, "Ocurrió un error interno al procesar la solicitud.");
            }
        }
    }
}

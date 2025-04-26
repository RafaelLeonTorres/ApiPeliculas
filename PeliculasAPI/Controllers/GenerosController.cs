using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.Data;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Utilidades;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PeliculasAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenerosController : ControllerBase
    {
        private readonly IOutputCacheStore OutputCacheStore;
        private readonly ApplicationDbContext context;
        private readonly ILogger<GenerosController> Logger;
        private readonly IMapper Mapper;

        public GenerosController(IOutputCacheStore outputCacheStore, ApplicationDbContext applicationDbContext, ILogger<GenerosController> logger,
            IMapper mapper)
        {
            OutputCacheStore = outputCacheStore;
            context = applicationDbContext;
            Logger = logger;
            Mapper = mapper;
        }


        [HttpGet]
        [OutputCache(Tags = new[] { ConstantesString.cacheTagGeneros })]
        public async Task<ActionResult<IEnumerable<GeneroDTO>>> Get([FromQuery] PaginacionDTO paginacion)
        {
            try
            {
                var queryable = context.Generos.AsQueryable();
                await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);

                var generos = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    return await queryable
                                    .OrderBy(g => g.Nombre)
                                    .Paginar(paginacion)
                                    .ProjectTo<GeneroDTO>(Mapper.ConfigurationProvider)
                                    .OrderBy(x => x.Nombre)
                                    .ToListAsync();
                });

                Logger.LogInformation("Se obtuvieron {Count} géneros exitosamente.", generos.Count);
                return Ok(generos);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al obtener los géneros.");
                return StatusCode(500, ex);
            }
        }

        [HttpGet("{id:int}", Name = "Se creo el genero")]
        [OutputCache(Tags = new[] { ConstantesString.cacheTagGeneros })]
        public async Task<ActionResult<GeneroDTO>> GetById(int id)
        {
            try
            {
                var genero = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    return await context.Generos
                                    .ProjectTo<GeneroDTO>(Mapper.ConfigurationProvider)
                                    .FirstOrDefaultAsync(g => g.Id == id);
                });

                if (genero == null)
                {
                    Logger.LogWarning("No se encontró el género con ID {Id}.", id);
                    return NotFound();
                }

                Logger.LogInformation("Se encontró el género con ID {Id}.", id);
                return Ok(genero);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al obtener el género con ID {Id}.", id);
                return StatusCode(500, ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GeneroCreacionDTO generoCreacionDTO)
        {
            try
            {
                // Validar que el DTO no sea nulo
                if (generoCreacionDTO == null)
                {
                    Logger.LogWarning("El DTO de creación de género es nulo.");
                    return BadRequest("El DTO de creación de género no puede ser nulo.");
                }

                // Validar que el nombre del género no esté vacío
                if (string.IsNullOrWhiteSpace(generoCreacionDTO.Nombre))
                {
                    Logger.LogWarning("El nombre del género no puede estar vacío.");
                    return BadRequest("El nombre del género no puede estar vacío.");
                }

                // Verificar si ya existe un género con el mismo nombre
                var generoExistente = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    return await context.Generos
                                    .AnyAsync(g => g.Nombre.Equals(generoCreacionDTO.Nombre, StringComparison.CurrentCultureIgnoreCase));
                });

                if (generoExistente)
                {
                    Logger.LogWarning("Ya existe un género con el nombre {Nombre}.", generoCreacionDTO.Nombre);
                    return Conflict("Ya existe un género con ese nombre.");
                }

                // Mapear el DTO a la entidad Genero
                var genero = Mapper.Map<Genero>(generoCreacionDTO);

                // Agregar el nuevo género al contexto
                context.Add(genero);
                await RetryHelper.ExecuteWithRetryAsync(async () => await context.SaveChangesAsync());

                // Invalidar la caché
                await OutputCacheStore.EvictByTagAsync(ConstantesString.cacheTagGeneros, default);

                // Registrar la creación exitosa
                Logger.LogInformation("Se creó un nuevo género con ID {Id} y nombre {Nombre}.", genero.Id, genero.Nombre);

                // Retornar la respuesta con la ubicación del nuevo recurso
                return CreatedAtRoute("Se creo el genero", new { id = genero.Id }, genero);
            }
            catch (Exception ex)
            {
                // Registrar el error
                Logger.LogError(ex, "Error al crear un nuevo género.");
                return StatusCode(500, ex);
            }
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

                var generoExiste = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    return await context.Generos.AnyAsync(g => g.Id == id);
                });

                if (!generoExiste)
                {
                    Logger.LogWarning("No se encontró el género con ID {Id} para actualizar.", id);
                    return NotFound();
                }

                var genero = Mapper.Map<Genero>(generoCreacionDTO);
                genero.Id = id;

                context.Update(genero);
                await RetryHelper.ExecuteWithRetryAsync(async () => await context.SaveChangesAsync());
                await OutputCacheStore.EvictByTagAsync(ConstantesString.cacheTagGeneros, default);

                Logger.LogInformation("Se actualizó el género con ID {Id}.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al actualizar el género con ID {Id}.", id);
                return StatusCode(500, ex);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var registrosBorrados = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    return await context.Generos
                                    .Where(g => g.Id == id)
                                    .ExecuteDeleteAsync();
                });

                if (registrosBorrados == 0)
                {
                    Logger.LogWarning("No se encontró el género con ID {Id} para eliminar.", id);
                    return NotFound("No se encontró el género con ID {Id} para eliminar. "+ id);
                }

                await OutputCacheStore.EvictByTagAsync(ConstantesString.cacheTagGeneros, default);

                Logger.LogInformation("Se eliminó el género con ID {Id}.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al eliminar el género con ID {Id}.", id);
                return StatusCode(500, ex);
            }
        }
    }
}

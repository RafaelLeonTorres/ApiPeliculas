﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Data.SqlClient;
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
        private readonly ILogger<ActoresController> Logger;
        private readonly IMapper Mapper;
        private readonly IAlmacenadorArchivos AlmacenadorArchivos;

        public ActoresController(IOutputCacheStore outputCacheStore, ApplicationDbContext applicationDbContext, ILogger<ActoresController> logger,
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
                var actores = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    var queryable = context.Actores.AsQueryable();
                    await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);

                    return await queryable
                        .OrderBy(g => g.Nombre)
                        .Paginar(paginacion)
                        .ProjectTo<ActorDTO>(Mapper.ConfigurationProvider)
                        .OrderBy(x => x.Nombre)
                        .ToListAsync();
                });

                Logger.LogInformation("Se obtuvieron {Count} actores exitosamente.", actores.Count);
                return Ok(actores);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al obtener los actores.");
                return StatusCode(500, ex.Message);
            }
        }


        [HttpGet("{id:int}", Name = "Se creo el actor")]
        [OutputCache(Tags = new[] { ConstantesString.cacheTagActores })]
        public async Task<ActionResult<ActorDTO>> GetById(int id)
        {
            try
            {
                var actor = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    return await context.Actores
                   .ProjectTo<ActorDTO>(Mapper.ConfigurationProvider)
                   .FirstOrDefaultAsync(g => g.Id == id);                
                });   
               

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
                return StatusCode(500, ex);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromForm] ActorCreacionDTO actorCreacioDTO)
        {
            try
            {
                var actor = Mapper.Map<Actor>(actorCreacioDTO);

                if (actorCreacioDTO.Foto is not null)
                {
                    var url = await RetryHelper.ExecuteWithRetryAsync(async () => 
                    {
                        return await AlmacenadorArchivos.Almacenar(ConstantesString.contenedorActores, actorCreacioDTO.Foto); 
                    });
                    
                    actor.Foto = url;
                }


                context.Add(actor);
                await context.SaveChangesAsync();

                // Invalidar la caché
                await OutputCacheStore.EvictByTagAsync(ConstantesString.cacheTagActores, default);
                return NoContent();
            }
            catch (Exception ex) 
            {
                Logger.LogError(ex, string.Concat("Error al crear el actor: ", actorCreacioDTO.Nombre));
                return StatusCode(500, ex);
            }            
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromForm] ActorCreacionDTO actorCreacionDTO)
        {
            try
            {
                if (actorCreacionDTO == null)
                {
                    Logger.LogWarning("El DTO de actualización de actor es nulo.");
                    return BadRequest("El DTO de actualización de actor no puede ser nulo.");
                }

                var actor = await RetryHelper.ExecuteWithRetryAsync(async () => {
                    return await context.Actores.FirstOrDefaultAsync(g => g.Id == id);
                });

                if (actor is null)
                {
                    Logger.LogWarning("No se encontró el actor con ID {Id} para actualizar.", id);
                    return NotFound();
                }

                // se actualiza con el savechangesasync
                actor = Mapper.Map(actorCreacionDTO, actor);

                if (actorCreacionDTO.Foto is not null) 
                {
                    actor.Foto = await RetryHelper.ExecuteWithRetryAsync(async () =>
                    {
                        return await AlmacenadorArchivos.Editar(actor.Foto, ConstantesString.contenedorActores,
                        actorCreacionDTO.Foto);
                     });
                }

                await context.SaveChangesAsync();
                await OutputCacheStore.EvictByTagAsync(ConstantesString.cacheTagActores, default);

                Logger.LogInformation("Se actualizó el actor con ID {Id}.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al actualizar el actor con ID {Id}.", id);
                return StatusCode(500, ex);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var actoresABorrar = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    return await context.Actores.Where(g => g.Id == id).ToListAsync();

                });
                var actoresBorrados = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    return await context.Actores
                                    .Where(g => g.Id == id)
                                    .ExecuteDeleteAsync();
                });
                

                if (actoresBorrados == 0)
                {
                    Logger.LogWarning("No se encontró el actor con ID {Id} para eliminar.", id);
                    return NotFound("No se encontró el actor con ID {Id} para eliminar. " + id);
                }


                if (actoresABorrar is not null && actoresABorrar.Count > 0) 
                {
                    foreach (var item in actoresABorrar)
                    {
                        await RetryHelper.ExecuteWithRetryAsync(async () =>
                        {
                            await AlmacenadorArchivos.Borrar(item.Foto, ConstantesString.contenedorActores);
                        });
                    }
                }
                

                await OutputCacheStore.EvictByTagAsync(ConstantesString.cacheTagActores, default);

                Logger.LogInformation("Se eliminó el actor con ID {Id}.", id);
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

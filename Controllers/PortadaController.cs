using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflix.Data;

namespace clase_4.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortadaController : ControllerBase
    {
        private readonly DataContext _context;

        public PortadaController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Portada
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Portada>>> GetPortada()
        {
            return await _context.Portada.ToListAsync();
        }

        // GET: api/Portada/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Portada>> GetPortada(int id)
        {
            var portada = await _context.Portada.FindAsync(id);

            if (portada == null)
            {
                return NotFound();
            }

            return portada;
        }

        // PUT: api/Portada/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPortada(int id, Portada portada)
        {
            if (id != portada.Id)
            {
                return BadRequest();
            }

            _context.Entry(portada).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PortadaExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Portada
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<Portada>> PostPortada(Portada portada)
        {
            _context.Portada.Add(portada);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPortada", new { id = portada.Id }, portada);
        }

        // DELETE: api/Portada/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Portada>> DeletePortada(int id)
        {
            var portada = await _context.Portada.FindAsync(id);
            if (portada == null)
            {
                return NotFound();
            }

            _context.Portada.Remove(portada);
            await _context.SaveChangesAsync();

            return portada;
        }

        private bool PortadaExists(int id)
        {
            return _context.Portada.Any(e => e.Id == id);
        }
    }
}

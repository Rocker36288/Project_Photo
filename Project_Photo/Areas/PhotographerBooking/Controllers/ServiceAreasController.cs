using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Models;

namespace Project_Photo.Areas.PhotographerBooking.Controllers
{
    [Area("PhotographerBooking")]
    public class ServiceAreasController : Controller
    {
        private readonly AAContext _context;

        public ServiceAreasController(AAContext context)
        {
            _context = context;
        }

        // GET: PhotographerBooking/ServiceAreas
        public async Task<IActionResult> Index()
        {
            var aAContext = _context.ServiceAreas.Include(s => s.Photographer);
            return View(await aAContext.ToListAsync());
        }

        // GET: PhotographerBooking/ServiceAreas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceArea = await _context.ServiceAreas
                .Include(s => s.Photographer)
                .FirstOrDefaultAsync(m => m.ServiceAreaId == id);
            if (serviceArea == null)
            {
                return NotFound();
            }

            return View(serviceArea);
        }

        // GET: PhotographerBooking/ServiceAreas/Create
        public IActionResult Create()
        {
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName");
            return View();
        }

        // POST: PhotographerBooking/ServiceAreas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceAreaId,PhotographerId,City,AdditionalFee")] ServiceArea serviceArea)
        {
            if (ModelState.IsValid)
            {
                _context.Add(serviceArea);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", serviceArea.PhotographerId);
            return View(serviceArea);
        }

        // GET: PhotographerBooking/ServiceAreas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceArea = await _context.ServiceAreas.FindAsync(id);
            if (serviceArea == null)
            {
                return NotFound();
            }
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", serviceArea.PhotographerId);
            return View(serviceArea);
        }

        // POST: PhotographerBooking/ServiceAreas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceAreaId,PhotographerId,City,AdditionalFee")] ServiceArea serviceArea)
        {
            if (id != serviceArea.ServiceAreaId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(serviceArea);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceAreaExists(serviceArea.ServiceAreaId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", serviceArea.PhotographerId);
            return View(serviceArea);
        }

        // GET: PhotographerBooking/ServiceAreas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceArea = await _context.ServiceAreas
                .Include(s => s.Photographer)
                .FirstOrDefaultAsync(m => m.ServiceAreaId == id);
            if (serviceArea == null)
            {
                return NotFound();
            }

            return View(serviceArea);
        }

        // POST: PhotographerBooking/ServiceAreas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var serviceArea = await _context.ServiceAreas.FindAsync(id);
            if (serviceArea != null)
            {
                _context.ServiceAreas.Remove(serviceArea);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceAreaExists(int id)
        {
            return _context.ServiceAreas.Any(e => e.ServiceAreaId == id);
        }
    }
}

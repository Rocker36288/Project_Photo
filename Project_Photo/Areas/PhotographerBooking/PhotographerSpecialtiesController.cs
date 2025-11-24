using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Models;

namespace Project_Photo.Areas.PhotographerBooking
{
    [Area("PhotographerBooking")]
    public class PhotographerSpecialtiesController : Controller
    {
        private readonly AAContext _context;

        public PhotographerSpecialtiesController(AAContext context)
        {
            _context = context;
        }

        // GET: PhotographerBooking/PhotographerSpecialties
        public async Task<IActionResult> Index()
        {
            var aAContext = _context.PhotographerSpecialties.Include(p => p.Photographer).Include(p => p.SpecialtyTag);
            return View(await aAContext.ToListAsync());
        }

        // GET: PhotographerBooking/PhotographerSpecialties/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var photographerSpecialty = await _context.PhotographerSpecialties
                .Include(p => p.Photographer)
                .Include(p => p.SpecialtyTag)
                .FirstOrDefaultAsync(m => m.PhotographerSpecialtyId == id);
            if (photographerSpecialty == null)
            {
                return NotFound();
            }

            return View(photographerSpecialty);
        }

        // GET: PhotographerBooking/PhotographerSpecialties/Create
        public IActionResult Create()
        {
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName");
            ViewData["SpecialtyTagId"] = new SelectList(_context.SpecialtyTags, "SpecialtyTagId", "SpecialtyName");
            return View();
        }

        // POST: PhotographerBooking/PhotographerSpecialties/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PhotographerSpecialtyId,PhotographerId,SpecialtyTagId,CreatedAt")] PhotographerSpecialty photographerSpecialty)
        {
            if (ModelState.IsValid)
            {
                _context.Add(photographerSpecialty);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", photographerSpecialty.PhotographerId);
            ViewData["SpecialtyTagId"] = new SelectList(_context.SpecialtyTags, "SpecialtyTagId", "SpecialtyName", photographerSpecialty.SpecialtyTagId);
            return View(photographerSpecialty);
        }

        // GET: PhotographerBooking/PhotographerSpecialties/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var photographerSpecialty = await _context.PhotographerSpecialties.FindAsync(id);
            if (photographerSpecialty == null)
            {
                return NotFound();
            }
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", photographerSpecialty.PhotographerId);
            ViewData["SpecialtyTagId"] = new SelectList(_context.SpecialtyTags, "SpecialtyTagId", "SpecialtyName", photographerSpecialty.SpecialtyTagId);
            return View(photographerSpecialty);
        }

        // POST: PhotographerBooking/PhotographerSpecialties/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PhotographerSpecialtyId,PhotographerId,SpecialtyTagId,CreatedAt")] PhotographerSpecialty photographerSpecialty)
        {
            if (id != photographerSpecialty.PhotographerSpecialtyId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(photographerSpecialty);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhotographerSpecialtyExists(photographerSpecialty.PhotographerSpecialtyId))
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
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", photographerSpecialty.PhotographerId);
            ViewData["SpecialtyTagId"] = new SelectList(_context.SpecialtyTags, "SpecialtyTagId", "SpecialtyName", photographerSpecialty.SpecialtyTagId);
            return View(photographerSpecialty);
        }

        // GET: PhotographerBooking/PhotographerSpecialties/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var photographerSpecialty = await _context.PhotographerSpecialties
                .Include(p => p.Photographer)
                .Include(p => p.SpecialtyTag)
                .FirstOrDefaultAsync(m => m.PhotographerSpecialtyId == id);
            if (photographerSpecialty == null)
            {
                return NotFound();
            }

            return View(photographerSpecialty);
        }

        // POST: PhotographerBooking/PhotographerSpecialties/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var photographerSpecialty = await _context.PhotographerSpecialties.FindAsync(id);
            if (photographerSpecialty != null)
            {
                _context.PhotographerSpecialties.Remove(photographerSpecialty);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PhotographerSpecialtyExists(int id)
        {
            return _context.PhotographerSpecialties.Any(e => e.PhotographerSpecialtyId == id);
        }
    }
}

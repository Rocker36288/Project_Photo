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
    public class SpecialtyTagsController : Controller
    {
        private readonly AAContext _context;

        public SpecialtyTagsController(AAContext context)
        {
            _context = context;
        }

        // GET: PhotographerBooking/SpecialtyTags
        public async Task<IActionResult> Index()
        {
            return View(await _context.SpecialtyTags.ToListAsync());
        }

        // GET: PhotographerBooking/SpecialtyTags/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var specialtyTag = await _context.SpecialtyTags
                .FirstOrDefaultAsync(m => m.SpecialtyTagId == id);
            if (specialtyTag == null)
            {
                return NotFound();
            }

            return View(specialtyTag);
        }

        // GET: PhotographerBooking/SpecialtyTags/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PhotographerBooking/SpecialtyTags/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SpecialtyTagId,SpecialtyName,Category,Description,DisplayOrder,IsActive,CreatedAt")] SpecialtyTag specialtyTag)
        {
            if (ModelState.IsValid)
            {
                _context.Add(specialtyTag);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(specialtyTag);
        }

        // GET: PhotographerBooking/SpecialtyTags/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var specialtyTag = await _context.SpecialtyTags.FindAsync(id);
            if (specialtyTag == null)
            {
                return NotFound();
            }
            return View(specialtyTag);
        }

        // POST: PhotographerBooking/SpecialtyTags/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SpecialtyTagId,SpecialtyName,Category,Description,DisplayOrder,IsActive,CreatedAt")] SpecialtyTag specialtyTag)
        {
            if (id != specialtyTag.SpecialtyTagId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(specialtyTag);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SpecialtyTagExists(specialtyTag.SpecialtyTagId))
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
            return View(specialtyTag);
        }

        // GET: PhotographerBooking/SpecialtyTags/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var specialtyTag = await _context.SpecialtyTags
                .FirstOrDefaultAsync(m => m.SpecialtyTagId == id);
            if (specialtyTag == null)
            {
                return NotFound();
            }

            return View(specialtyTag);
        }

        // POST: PhotographerBooking/SpecialtyTags/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var specialtyTag = await _context.SpecialtyTags.FindAsync(id);
            if (specialtyTag != null)
            {
                _context.SpecialtyTags.Remove(specialtyTag);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SpecialtyTagExists(int id)
        {
            return _context.SpecialtyTags.Any(e => e.SpecialtyTagId == id);
        }
    }
}

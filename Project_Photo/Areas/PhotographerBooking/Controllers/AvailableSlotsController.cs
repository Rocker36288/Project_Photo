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
    public class AvailableSlotsController : Controller
    {
        private readonly AAContext _context;

        public AvailableSlotsController(AAContext context)
        {
            _context = context;
        }

        // GET: PhotographerBooking/AvailableSlots
        public async Task<IActionResult> Index()
        {
            var aAContext = _context.AvailableSlots.Include(a => a.Photographer);
            return View(await aAContext.ToListAsync());
        }

        // GET: PhotographerBooking/AvailableSlots/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var availableSlot = await _context.AvailableSlots
                .Include(a => a.Photographer)
                .FirstOrDefaultAsync(m => m.AvailableSlotId == id);
            if (availableSlot == null)
            {
                return NotFound();
            }

            return View(availableSlot);
        }

        // GET: PhotographerBooking/AvailableSlots/Create
        public IActionResult Create()
        {
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName");
            return View();
        }

        // POST: PhotographerBooking/AvailableSlots/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AvailableSlotId,PhotographerId,SlotDate,StartTime,EndTime,BookingId,CreatedAt")] AvailableSlot availableSlot)
        {
            if (ModelState.IsValid)
            {
                _context.Add(availableSlot);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", availableSlot.PhotographerId);
            return View(availableSlot);
        }

        // GET: PhotographerBooking/AvailableSlots/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var availableSlot = await _context.AvailableSlots.FindAsync(id);
            if (availableSlot == null)
            {
                return NotFound();
            }
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", availableSlot.PhotographerId);
            return View(availableSlot);
        }

        // POST: PhotographerBooking/AvailableSlots/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AvailableSlotId,PhotographerId,SlotDate,StartTime,EndTime,BookingId,CreatedAt")] AvailableSlot availableSlot)
        {
            if (id != availableSlot.AvailableSlotId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(availableSlot);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AvailableSlotExists(availableSlot.AvailableSlotId))
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
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", availableSlot.PhotographerId);
            return View(availableSlot);
        }

        // GET: PhotographerBooking/AvailableSlots/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var availableSlot = await _context.AvailableSlots
                .Include(a => a.Photographer)
                .FirstOrDefaultAsync(m => m.AvailableSlotId == id);
            if (availableSlot == null)
            {
                return NotFound();
            }

            return View(availableSlot);
        }

        // POST: PhotographerBooking/AvailableSlots/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var availableSlot = await _context.AvailableSlots.FindAsync(id);
            if (availableSlot != null)
            {
                _context.AvailableSlots.Remove(availableSlot);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AvailableSlotExists(int id)
        {
            return _context.AvailableSlots.Any(e => e.AvailableSlotId == id);
        }
    }
}

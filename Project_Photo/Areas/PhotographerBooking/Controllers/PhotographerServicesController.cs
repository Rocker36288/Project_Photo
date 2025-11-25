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
    public class PhotographerServicesController : Controller
    {
        private readonly AAContext _context;

        public PhotographerServicesController(AAContext context)
        {
            _context = context;
        }

        // GET: PhotographerBooking/PhotographerServices
        public async Task<IActionResult> Index()
        {
            var aAContext = _context.PhotographerServices.Include(p => p.Photographer).Include(p => p.ServiceType);
            return View(await aAContext.ToListAsync());
        }

        // GET: PhotographerBooking/PhotographerServices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var photographerService = await _context.PhotographerServices
                .Include(p => p.Photographer)
                .Include(p => p.ServiceType)
                .FirstOrDefaultAsync(m => m.PhotographerServiceId == id);
            if (photographerService == null)
            {
                return NotFound();
            }

            return View(photographerService);
        }

        // GET: PhotographerBooking/PhotographerServices/Create
        public IActionResult Create()
        {
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName");
            ViewData["ServiceTypeId"] = new SelectList(_context.ServiceTypes, "ServiceTypeId", "ServiceName");
            return View();
        }

        // POST: PhotographerBooking/PhotographerServices/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PhotographerServiceId,PhotographerId,ServiceTypeId,ServiceName,Description,BasePrice,Duration,MaxRevisions,DeliveryDays,IncludedPhotos,AdditionalServices,IsActive,CreatedAt")] PhotographerService photographerService)
        {
            if (ModelState.IsValid)
            {
                _context.Add(photographerService);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", photographerService.PhotographerId);
            ViewData["ServiceTypeId"] = new SelectList(_context.ServiceTypes, "ServiceTypeId", "ServiceName", photographerService.ServiceTypeId);
            return View(photographerService);
        }

        // GET: PhotographerBooking/PhotographerServices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var photographerService = await _context.PhotographerServices.FindAsync(id);
            if (photographerService == null)
            {
                return NotFound();
            }
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", photographerService.PhotographerId);
            ViewData["ServiceTypeId"] = new SelectList(_context.ServiceTypes, "ServiceTypeId", "ServiceName", photographerService.ServiceTypeId);
            return View(photographerService);
        }

        // POST: PhotographerBooking/PhotographerServices/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PhotographerServiceId,PhotographerId,ServiceTypeId,ServiceName,Description,BasePrice,Duration,MaxRevisions,DeliveryDays,IncludedPhotos,AdditionalServices,IsActive,CreatedAt")] PhotographerService photographerService)
        {
            if (id != photographerService.PhotographerServiceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(photographerService);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhotographerServiceExists(photographerService.PhotographerServiceId))
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
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", photographerService.PhotographerId);
            ViewData["ServiceTypeId"] = new SelectList(_context.ServiceTypes, "ServiceTypeId", "ServiceName", photographerService.ServiceTypeId);
            return View(photographerService);
        }

        // GET: PhotographerBooking/PhotographerServices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var photographerService = await _context.PhotographerServices
                .Include(p => p.Photographer)
                .Include(p => p.ServiceType)
                .FirstOrDefaultAsync(m => m.PhotographerServiceId == id);
            if (photographerService == null)
            {
                return NotFound();
            }

            return View(photographerService);
        }

        // POST: PhotographerBooking/PhotographerServices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var photographerService = await _context.PhotographerServices.FindAsync(id);
            if (photographerService != null)
            {
                _context.PhotographerServices.Remove(photographerService);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PhotographerServiceExists(int id)
        {
            return _context.PhotographerServices.Any(e => e.PhotographerServiceId == id);
        }
    }
}

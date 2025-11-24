using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.PhotographerBooking;

namespace Project_Photo.Areas.PhotographerBooking.Controllers
{
    [Area("PhotographerBooking")]
    public class ServiceTypesController : Controller
    {
        private readonly AAContext _context;

        public ServiceTypesController(AAContext context)
        {
            _context = context;
        }

        // GET: PhotographerBooking/ServiceTypes
        public async Task<IActionResult> Index()
        {
            return View(await _context.ServiceTypes.ToListAsync());
        }

        // GET: PhotographerBooking/ServiceTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceType = await _context.ServiceTypes
                .FirstOrDefaultAsync(m => m.ServiceTypeId == id);
            if (serviceType == null)
            {
                return NotFound();
            }

            return View(serviceType);
        }

        // GET: PhotographerBooking/ServiceTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PhotographerBooking/ServiceTypes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceTypeId,ServiceName,Description,IconUrl,DisplayOrder,IsActive")] ServiceType serviceType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(serviceType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(serviceType);
        }

        // GET: PhotographerBooking/ServiceTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceType = await _context.ServiceTypes.FindAsync(id);
            if (serviceType == null)
            {
                return NotFound();
            }
            return View(serviceType);
        }

        // POST: PhotographerBooking/ServiceTypes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceTypeId,ServiceName,Description,IconUrl,DisplayOrder,IsActive")] ServiceType serviceType)
        {
            if (id != serviceType.ServiceTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(serviceType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceTypeExists(serviceType.ServiceTypeId))
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
            return View(serviceType);
        }

        // GET: PhotographerBooking/ServiceTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceType = await _context.ServiceTypes
                .FirstOrDefaultAsync(m => m.ServiceTypeId == id);
            if (serviceType == null)
            {
                return NotFound();
            }

            return View(serviceType);
        }

        // POST: PhotographerBooking/ServiceTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var serviceType = await _context.ServiceTypes.FindAsync(id);
            if (serviceType != null)
            {
                _context.ServiceTypes.Remove(serviceType);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceTypeExists(int id)
        {
            return _context.ServiceTypes.Any(e => e.ServiceTypeId == id);
        }
    }
}

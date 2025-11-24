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
    public class BookingsController : Controller
    {
        private readonly AAContext _context;

        public BookingsController(AAContext context)
        {
            _context = context;
        }

        // GET: PhotographerBooking/Bookings
        public async Task<IActionResult> Index()
        {
            var aAContext = _context.Bookings.Include(b => b.AvailableSlot).Include(b => b.PaymentMethod).Include(b => b.Photographer).Include(b => b.User);
            return View(await aAContext.ToListAsync());
        }

        // GET: PhotographerBooking/Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.AvailableSlot)
                .Include(b => b.PaymentMethod)
                .Include(b => b.Photographer)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.BookingId == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: PhotographerBooking/Bookings/Create
        public IActionResult Create()
        {
            ViewData["AvailableSlotId"] = new SelectList(_context.AvailableSlots, "AvailableSlotId", "AvailableSlotId");
            ViewData["PaymentMethodId"] = new SelectList(_context.PaymentMethods, "PaymentMethodId", "PaymentMethodId");
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Account");
            return View();
        }

        // POST: PhotographerBooking/Bookings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookingId,BookingNumber,UserId,PaymentMethodId,AvailableSlotId,PhotographerId,PhotographerServiceId,BookingDate,BookingStartTime,BookingEndTime,Location,ServicePrice,AdditionalFees,DiscountAmount,BookingStatus,PaymentStatus,DepositAmount,DepositPaidAt,FullPaymentAt,CustomerNotes,PhotographerNotes,CancellationReason,CancelledBy,CancelledAt,CreatedAt,UpdatedAt,CompletedAt")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AvailableSlotId"] = new SelectList(_context.AvailableSlots, "AvailableSlotId", "AvailableSlotId", booking.AvailableSlotId);
            ViewData["PaymentMethodId"] = new SelectList(_context.PaymentMethods, "PaymentMethodId", "PaymentMethodId", booking.PaymentMethodId);
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", booking.PhotographerId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Account", booking.UserId);
            return View(booking);
        }

        // GET: PhotographerBooking/Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            ViewData["AvailableSlotId"] = new SelectList(_context.AvailableSlots, "AvailableSlotId", "AvailableSlotId", booking.AvailableSlotId);
            ViewData["PaymentMethodId"] = new SelectList(_context.PaymentMethods, "PaymentMethodId", "PaymentMethodId", booking.PaymentMethodId);
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", booking.PhotographerId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Account", booking.UserId);
            return View(booking);
        }

        // POST: PhotographerBooking/Bookings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,BookingNumber,UserId,PaymentMethodId,AvailableSlotId,PhotographerId,PhotographerServiceId,BookingDate,BookingStartTime,BookingEndTime,Location,ServicePrice,AdditionalFees,DiscountAmount,BookingStatus,PaymentStatus,DepositAmount,DepositPaidAt,FullPaymentAt,CustomerNotes,PhotographerNotes,CancellationReason,CancelledBy,CancelledAt,CreatedAt,UpdatedAt,CompletedAt")] Booking booking)
        {
            if (id != booking.BookingId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.BookingId))
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
            ViewData["AvailableSlotId"] = new SelectList(_context.AvailableSlots, "AvailableSlotId", "AvailableSlotId", booking.AvailableSlotId);
            ViewData["PaymentMethodId"] = new SelectList(_context.PaymentMethods, "PaymentMethodId", "PaymentMethodId", booking.PaymentMethodId);
            ViewData["PhotographerId"] = new SelectList(_context.Photographers, "PhotographerId", "StudioName", booking.PhotographerId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Account", booking.UserId);
            return View(booking);
        }

        // GET: PhotographerBooking/Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.AvailableSlot)
                .Include(b => b.PaymentMethod)
                .Include(b => b.Photographer)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.BookingId == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: PhotographerBooking/Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingId == id);
        }
    }
}

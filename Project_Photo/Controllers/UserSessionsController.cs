using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Models;

namespace Project_Photo.Controllers
{
    public class UserSessionsController : Controller
    {
        private readonly AaContext _context;

        public UserSessionsController(AaContext context)
        {
            _context = context;
        }

        // GET: UserSessions
        public async Task<IActionResult> Index()
        {
            var aaContext = _context.UserSessions.Include(u => u.User);
            return View(await aaContext.ToListAsync());
        }

        // GET: UserSessions/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userSession = await _context.UserSessions
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.SessionId == id);
            if (userSession == null)
            {
                return NotFound();
            }

            return View(userSession);
        }

        // GET: UserSessions/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: UserSessions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SessionId,UserId,UserAgent,IsActive,LastActivityAt,ExpiresAt,CreatedAt")] UserSession userSession)
        {
            if (ModelState.IsValid)
            {
                _context.Add(userSession);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", userSession.UserId);
            return View(userSession);
        }

        // GET: UserSessions/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userSession = await _context.UserSessions.FindAsync(id);
            if (userSession == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", userSession.UserId);
            return View(userSession);
        }

        // POST: UserSessions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("SessionId,UserId,UserAgent,IsActive,LastActivityAt,ExpiresAt,CreatedAt")] UserSession userSession)
        {
            if (id != userSession.SessionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userSession);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserSessionExists(userSession.SessionId))
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
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", userSession.UserId);
            return View(userSession);
        }

        // GET: UserSessions/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userSession = await _context.UserSessions
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.SessionId == id);
            if (userSession == null)
            {
                return NotFound();
            }

            return View(userSession);
        }

        // POST: UserSessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var userSession = await _context.UserSessions.FindAsync(id);
            if (userSession != null)
            {
                _context.UserSessions.Remove(userSession);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserSessionExists(long id)
        {
            return _context.UserSessions.Any(e => e.SessionId == id);
        }
    }
}

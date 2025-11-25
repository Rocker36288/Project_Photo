using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Data;
using Project_Photo.Models;
using System;

namespace YourProjectName.Areas.Social.Controllers
{
    [Area("Social")]
    public class SocialHomeController : Controller
    {
        private readonly AAContext _context;

        public SocialHomeController(AAContext context)
        {
            _context = context;
        }

        // GET: /Social/SocialHome/Index
        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        // POST: /Social/SocialHome/CreatePost
        [HttpPost]
        public async Task<IActionResult> CreatePost(string PostContent)
        {
            if (string.IsNullOrWhiteSpace(PostContent))
            {
                TempData["Error"] = "貼文內容不能為空";
                return RedirectToAction("Index");
            }

            var post = new Post
            {
                UserId = 1, // TODO: 改成登入使用者 ID
                PostContent = PostContent,
                PostType = "text",
                Status = "active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}

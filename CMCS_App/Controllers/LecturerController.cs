using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMCS_App.Models;
using CMCS_App.Data;

namespace CMCS_App.Controllers
{
    public class LecturerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public LecturerController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var lecturerId = GetCurrentLecturerId(); // In real app, get from authentication
            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.LecturerID == lecturerId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            return View(claims);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Claim claim, IFormFile? supportingDocument)
        {
            if (ModelState.IsValid)
            {
                claim.LecturerID = GetCurrentLecturerId(); // In real app, get from authentication
                claim.CalculateTotal();
                claim.SubmitForApproval();

                // Handle file upload
                if (supportingDocument != null && supportingDocument.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(supportingDocument.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await supportingDocument.CopyToAsync(stream);
                    }

                    claim.SupportingDocument = fileName;
                }

                _context.Add(claim);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Claim submitted successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(claim);
        }

        public async Task<IActionResult> Details(int id)
        {
            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(c => c.ClaimID == id);

            if (claim == null || claim.LecturerID != GetCurrentLecturerId())
            {
                return NotFound();
            }

            return View(claim);
        }

        private int GetCurrentLecturerId()
        {
            // For prototype, return first lecturer ID
            // In real application, get from authenticated user
            return 1;
        }
    }
}
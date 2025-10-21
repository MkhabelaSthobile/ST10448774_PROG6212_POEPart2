using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMCS_App.Models;
using CMCS_App.Data;

namespace CMCS_App.Controllers
{
    public class AcademicManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AcademicManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            return View(claims);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            claim.UpdateStatus("Approved by Manager");
            await _context.SaveChangesAsync();

            TempData["Success"] = "Claim approved successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            claim.UpdateStatus($"Rejected by Manager: {reason}");
            claim.RejectionReason = reason;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Claim rejected successfully!";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Reports()
        {
            var claims = _context.Claims.ToList();
            var manager = new AcademicManager();
            var report = manager.GenerateSummaryReport(claims);

            ViewBag.Report = report;
            ViewBag.TotalClaims = claims.Count;
            ViewBag.ApprovedClaims = claims.Count(c => c.Status.Contains("Approved"));
            ViewBag.RejectedClaims = claims.Count(c => c.Status.Contains("Rejected"));
            ViewBag.PendingClaims = claims.Count(c => c.Status == "Pending" || c.Status == "Submitted");

            return View(claims);
        }
    }
}
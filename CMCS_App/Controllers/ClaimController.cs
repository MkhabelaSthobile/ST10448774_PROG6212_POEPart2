using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMCS_App.Models;
using CMCS_App.Data;

namespace CMCS_App.Controllers
{
    public class ClaimController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ClaimController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Claim - View all claims (for managers/coordinators)
        public async Task<IActionResult> Index()
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var claimsQuery = _context.Claims.Include(c => c.Lecturer).AsQueryable();

                // Filter based on user role
                switch (userRole)
                {
                    case "Lecturer":
                        var lecturerId = GetCurrentLecturerId();
                        claimsQuery = claimsQuery.Where(c => c.LecturerID == lecturerId);
                        ViewBag.ViewType = "My Claims";
                        break;
                    case "Coordinator":
                        claimsQuery = claimsQuery.Where(c => c.Status == "Submitted" || c.Status == "Pending");
                        ViewBag.ViewType = "Pending Claims for Verification";
                        break;
                    case "Manager":
                        ViewBag.ViewType = "All Claims Overview";
                        break;
                    default:
                        return RedirectToAction("Index", "Home");
                }

                var claims = await claimsQuery.OrderByDescending(c => c.SubmissionDate).ToListAsync();
                return View(claims);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading claims.";
                return View(new List<Claim>());
            }
        }

        // GET: Claim/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.ClaimID == id);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Authorization check
                if (!CanAccessClaim(claim))
                {
                    TempData["Error"] = "You don't have permission to view this claim.";
                    return RedirectToAction(nameof(Index));
                }

                return View(claim);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading claim details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Claim/Create
        public IActionResult Create()
        {
            var userRole = GetCurrentUserRole();
            if (userRole != "Lecturer")
            {
                TempData["Error"] = "Only lecturers can submit claims.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Claim/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Claim claim, IFormFile? supportingDocument)
        {
            try
            {
                if (GetCurrentUserRole() != "Lecturer")
                {
                    TempData["Error"] = "Only lecturers can submit claims.";
                    return RedirectToAction("Index", "Home");
                }

                if (ModelState.IsValid)
                {
                    // Validate file upload
                    if (supportingDocument != null)
                    {
                        var validationResult = ValidateFileUpload(supportingDocument);
                        if (!validationResult.IsValid)
                        {
                            ModelState.AddModelError("supportingDocument", validationResult.ErrorMessage);
                            return View(claim);
                        }
                    }

                    claim.LecturerID = GetCurrentLecturerId();
                    claim.CalculateTotal();
                    claim.SubmitForApproval();
                    claim.SubmissionDate = DateTime.Now;

                    // Handle file upload
                    if (supportingDocument != null && supportingDocument.Length > 0)
                    {
                        var fileResult = await SaveUploadedFile(supportingDocument);
                        if (!fileResult.Success)
                        {
                            ModelState.AddModelError("supportingDocument", fileResult.ErrorMessage);
                            return View(claim);
                        }
                        claim.SupportingDocument = fileResult.FileName;
                    }

                    _context.Add(claim);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Claim submitted successfully! Your claim is now pending approval.";
                    return RedirectToAction(nameof(Index));
                }

                return View(claim);
            }
            catch (DbUpdateException dbEx)
            {
                TempData["Error"] = "Database error occurred while saving your claim. Please try again.";
                return View(claim);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An unexpected error occurred. Please try again.";
                return View(claim);
            }
        }

        // GET: Claim/Verify/5 - For coordinators
        public async Task<IActionResult> Verify(int id)
        {
            try
            {
                if (GetCurrentUserRole() != "Coordinator")
                {
                    TempData["Error"] = "Only programme coordinators can verify claims.";
                    return RedirectToAction("Index", "Home");
                }

                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.ClaimID == id);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(claim);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading the claim for verification.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Claim/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                if (userRole != "Coordinator" && userRole != "Manager")
                {
                    TempData["Error"] = "You don't have permission to approve claims.";
                    return RedirectToAction("Index", "Home");
                }

                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (userRole == "Coordinator")
                {
                    claim.UpdateStatus("Approved by Coordinator");
                    TempData["Success"] = "Claim approved successfully! Waiting for manager approval.";
                }
                else if (userRole == "Manager")
                {
                    claim.UpdateStatus("Approved by Manager");
                    TempData["Success"] = "Claim approved successfully! The claim process is complete.";
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while approving the claim.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Claim/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                if (userRole != "Coordinator" && userRole != "Manager")
                {
                    TempData["Error"] = "You don't have permission to reject claims.";
                    return RedirectToAction("Index", "Home");
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["Error"] = "Please provide a reason for rejection.";
                    return RedirectToAction(nameof(Verify), new { id });
                }

                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (userRole == "Coordinator")
                {
                    claim.UpdateStatus($"Rejected by Coordinator: {reason}");
                }
                else if (userRole == "Manager")
                {
                    claim.UpdateStatus($"Rejected by Manager: {reason}");
                }

                claim.RejectionReason = reason;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Claim rejected successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while rejecting the claim.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Claim/Track/5 - For lecturers to track claim status
        public async Task<IActionResult> Track(int id)
        {
            try
            {
                if (GetCurrentUserRole() != "Lecturer")
                {
                    TempData["Error"] = "Only lecturers can track their claims.";
                    return RedirectToAction("Index", "Home");
                }

                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.ClaimID == id);

                if (claim == null || claim.LecturerID != GetCurrentLecturerId())
                {
                    TempData["Error"] = "Claim not found or you don't have permission to view it.";
                    return RedirectToAction(nameof(Index));
                }

                return View(claim);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading claim tracking information.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Claim/DownloadDocument/5
        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null || string.IsNullOrEmpty(claim.SupportingDocument))
                {
                    TempData["Error"] = "Document not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Authorization check
                if (!CanAccessClaim(claim))
                {
                    TempData["Error"] = "You don't have permission to download this document.";
                    return RedirectToAction(nameof(Index));
                }

                var filePath = Path.Combine(_environment.WebRootPath, "uploads", claim.SupportingDocument);
                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "Document file not found.";
                    return RedirectToAction(nameof(Index));
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                var contentType = GetContentType(claim.SupportingDocument);
                var fileName = Path.GetFileName(claim.SupportingDocument);

                return File(memory, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while downloading the document.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Claim/Statistics - For managers
        public async Task<IActionResult> Statistics()
        {
            try
            {
                if (GetCurrentUserRole() != "Manager")
                {
                    TempData["Error"] = "Only academic managers can view statistics.";
                    return RedirectToAction("Index", "Home");
                }

                var claims = await _context.Claims.Include(c => c.Lecturer).ToListAsync();
                var lecturers = await _context.Lecturers.ToListAsync();

                ViewBag.TotalClaims = claims.Count;
                ViewBag.ApprovedClaims = claims.Count(c => c.Status.Contains("Approved"));
                ViewBag.RejectedClaims = claims.Count(c => c.Status.Contains("Rejected"));
                ViewBag.PendingClaims = claims.Count(c => c.Status == "Pending" || c.Status == "Submitted");
                ViewBag.TotalAmount = claims.Where(c => c.Status.Contains("Approved")).Sum(c => c.TotalAmount);
                ViewBag.TotalLecturers = lecturers.Count;

                // Monthly statistics
                var monthlyStats = claims
                    .GroupBy(c => c.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Count = g.Count(),
                        Amount = g.Where(c => c.Status.Contains("Approved")).Sum(c => c.TotalAmount)
                    })
                    .OrderBy(x => x.Month)
                    .ToList();

                ViewBag.MonthlyStats = monthlyStats;

                return View(claims);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading statistics.";
                return RedirectToAction(nameof(Index));
            }
        }

        #region Helper Methods

        private string GetCurrentUserRole()
        {
            // For prototype, determine role from current controller context
            // In real application, get from authentication
            if (HttpContext.Request.Path.StartsWithSegments("/Lecturer"))
                return "Lecturer";
            if (HttpContext.Request.Path.StartsWithSegments("/ProgrammeCoordinator"))
                return "Coordinator";
            if (HttpContext.Request.Path.StartsWithSegments("/AcademicManager"))
                return "Manager";

            return "Unknown";
        }

        private int GetCurrentLecturerId()
        {
            // For prototype, return first lecturer ID
            // In real application, get from authenticated user
            return 1;
        }

        private int GetCurrentCoordinatorId()
        {
            // For prototype, return first coordinator ID
            return 1;
        }

        private int GetCurrentManagerId()
        {
            // For prototype, return first manager ID
            return 1;
        }

        private bool CanAccessClaim(Claim claim)
        {
            var userRole = GetCurrentUserRole();

            return userRole switch
            {
                "Lecturer" => claim.LecturerID == GetCurrentLecturerId(),
                "Coordinator" or "Manager" => true,
                _ => false
            };
        }

        private (bool IsValid, string ErrorMessage) ValidateFileUpload(IFormFile file)
        {
            if (file.Length > 5 * 1024 * 1024) // 5MB limit
            {
                return (false, "File size must be less than 5MB.");
            }

            var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return (false, "Only PDF, DOCX, and XLSX files are allowed.");
            }

            return (true, string.Empty);
        }

        private async Task<(bool Success, string FileName, string ErrorMessage)> SaveUploadedFile(IFormFile file)
        {
            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return (true, fileName, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, "Error saving file. Please try again.");
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }

        #endregion
    }
}
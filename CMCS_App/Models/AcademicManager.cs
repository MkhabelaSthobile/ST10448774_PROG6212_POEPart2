using System.Security.Claims;

namespace CMCS_App.Models
{
    public class AcademicManager
    {
        public int ManagerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Methods (prototype only)
        public void VerifyClaim(Claim claim)
        {
            claim.UpdateStatus("Verified by Manager");
        }

        public void ApproveClaim(Claim claim)
        {
            claim.UpdateStatus("Approved by Manager");
        }

        public void RejectClaim(Claim claim, string reason)
        {
            claim.UpdateStatus($"Rejected by Manager: {reason}");
        }

        public string GenerateSummaryReport(List<Claim> claims)
        {
            return $"Total Claims: {claims.Count}, " +
                   $"Approved: {claims.Count(c => c.Status.Contains("Approved"))}, " +
                   $"Rejected: {claims.Count(c => c.Status.Contains("Rejected"))}";
        }
    }
}

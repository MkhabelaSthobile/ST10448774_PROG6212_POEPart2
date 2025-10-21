using System.Security.Claims;

namespace CMCS_App.Models
{
    public class Lecturer
    {
        public int LecturerID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } = string.Empty;
        public string ModuleName { get; set; }
        public decimal HourlyRate { get; set; }


        // Methods (prototype only)
        public void SubmitClaim(Claim claim)
        {
            claim.SubmitForApproval();
        }

        public void UploadSupportingDocument(Claim claim, string filePath)
        {
            claim.SupportingDocument = filePath;
        }

        public string TrackClaimStatus(Claim claim)
        {
            return claim.Status;
        }
    }
}

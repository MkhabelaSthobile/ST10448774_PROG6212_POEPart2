
namespace CMCS_App.Models
{
    public class ProgrammeCoordinator
    {
        public int CoordinatorID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Methods (prototype only)
        public void ApproveClaim(Claim claim)
        {
            claim.UpdateStatus("Approved by Coordinator");
        }

        public void RejectClaim(Claim claim, string reason)
        {
            claim.UpdateStatus($"Rejected by Coordinator: {reason}");
        }
    }
}

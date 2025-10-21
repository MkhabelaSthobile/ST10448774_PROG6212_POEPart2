using System.ComponentModel.DataAnnotations;

namespace CMCS_App.Models
{
    public class Claim
    {
        public int ClaimID { get; set; }

        [Required]
        public int LecturerID { get; set; }

        [Required]
        public string Month { get; set; } = string.Empty;

        [Required]
        [Range(1, 200, ErrorMessage = "Hours worked must be between 1 and 200")]
        public int HoursWorked { get; set; }

        [Required]
        [Range(0.01, 1000, ErrorMessage = "Hourly rate must be between 0.01 and 1000")]
        public decimal HourlyRate { get; set; }

        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime SubmissionDate { get; set; } = DateTime.Now;
        public string SupportingDocument { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }

        // Navigation property
        public Lecturer? Lecturer { get; set; }

        public decimal CalculateTotal()
        {
            TotalAmount = HoursWorked * HourlyRate;
            return TotalAmount;
        }

        public void SubmitForApproval()
        {
            Status = "Submitted";
        }

        public void UpdateStatus(string newStatus)
        {
            Status = newStatus;
        }
    }
}
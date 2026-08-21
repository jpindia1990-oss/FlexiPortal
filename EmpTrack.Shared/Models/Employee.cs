using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmpTrack.Shared.Models
{
    [Table("Employees")]
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("EmployeeID")]
        public string? EmployeeId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public int DesignationId { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
     
        public double? AssignedLatitude { get; set; }
        public double? AssignedLongitude { get; set; }

        [ForeignKey("DesignationId")]
        public Designation? Designation { get; set; }

        public int CompanyId { get; set; }

        public string CompanyCode { get; set; }




    }
}
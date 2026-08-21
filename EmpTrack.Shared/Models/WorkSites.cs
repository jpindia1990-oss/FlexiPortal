using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EmpTrack.Shared.Models
{
    [Table("WorkSites")]
    public class WorkSite
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SiteID { get; set; }

        [Required]
        [MaxLength(100)]
        public string SiteName { get; set; } = string.Empty;

        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public int RadiusMeters { get; set; } = 50; // Default radius of 50m

        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

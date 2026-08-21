using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EmpTrack.Shared.Models.Dtos
{
    public class EmployeeDto
    {
        public int Id { get; set; }
        public string? EmployeeId { get; set; }
        public string? FirstName { get; set; } // Add?
        public string? LastName { get; set; } // Add?
        public string? FullName { get; set; }
        public string? Email { get; set; } // Add?
        public string? PhoneNumber { get; set; } // Add?
        public int DesignationId { get; set; }
        public string? DesignationName { get; set; }
        public bool IsActive { get; set; }
    }


    public class CreateEmployeeDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int DesignationId { get; set; }
    }

    public class LocationDto
    {
        public string MacAddress { get; set; } = string.Empty;
        public string? EmployeeCode { get; set; }
        [Required]
        public decimal Latitude { get; set; } // decimal, not double

        [Required]
        public decimal Longitude { get; set; } // decimal, not double
        public string? UnitName { get; set; }
        public string? AreaName { get; set; }
        public string? Address { get; set; }
        public int? BatteryLevel { get; set; } 
        public string? DeviceOS { get; set; } // 
        public int RadiusMeters { get; set; } = 300;
        public string CompanyCode { get; set; } = string.Empty;


    }

    public class AssignLocationDto
    {
        public double AssignedLatitude { get; set; }
        public double AssignedLongitude { get; set; }
    }
    public class AssignLocationRequest
    {

        public int EmployeeId { get; set; }
        public RequestDto Request { get; set; }
        public List<LocationDto> Locations { get; set; }
      
    }
    public class RequestDto 
    {
        public string Action { get; set; }
    }


    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginRequestDto
    {
        public string CompanyCode { get; set; }
        public string Username { get; set; } 
        public string Password { get; set; }
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}
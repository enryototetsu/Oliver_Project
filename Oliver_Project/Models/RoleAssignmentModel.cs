using System.ComponentModel.DataAnnotations;

namespace Oliver_Project.Models
{
    public class RoleAssignmentModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Role name is required.")]
        [RegularExpression("^(Admin|Member)$", ErrorMessage = "Invalid role name. Only 'Admin' or 'Member' are allowed.")]
        public string RoleName { get; set; }
    }
}

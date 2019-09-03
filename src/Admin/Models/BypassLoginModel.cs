using System.ComponentModel.DataAnnotations;

namespace Bit.Admin.Models
{
    public class BypassLoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        public string ReturnUrl { get; set; }

        public string Error { get; set; }
    }
}

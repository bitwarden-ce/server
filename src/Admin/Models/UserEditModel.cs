using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Bit.Core;
using Bit.Core.Models.Business;
using Bit.Core.Models.Table;
using Bit.Core.Utilities;

namespace Bit.Admin.Models
{
    public class UserEditModel : UserViewModel
    {
        public UserEditModel() { }

        public UserEditModel(User user, IEnumerable<Cipher> ciphers,
            GlobalSettings globalSettings)
            : base(user, ciphers)
        {
            Name = user.Name;
            Email = user.Email;
            EmailVerified = user.EmailVerified;
        }
        
        [Display(Name = "Name")]
        public string Name { get; set; }
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
        [Display(Name = "Email Verified")]
        public bool EmailVerified { get; set; }

        public User ToUser(User existingUser)
        {
            existingUser.Name = Name;
            existingUser.Email = Email;
            existingUser.EmailVerified = EmailVerified;
            return existingUser;
        }
    }
}

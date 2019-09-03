using Bit.Core.Models.Table;
using Bit.Core.Enums;
using Bit.Core.Models.Business;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Bit.Core.Utilities;

namespace Bit.Core.Models.Api
{
    public class OrganizationCreateRequestModel
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        [StringLength(50)]
        public string BusinessName { get; set; }
        [Required]
        public string Key { get; set; }
        public string CollectionName { get; set; }

        public virtual OrganizationSignup ToOrganizationSignup(User user)
        {
            return new OrganizationSignup
            {
                Owner = user,
                OwnerKey = Key,
                Name = Name,
                BusinessName = BusinessName,
                CollectionName = CollectionName
            };
        }
    }
}

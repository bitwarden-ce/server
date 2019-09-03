using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Bit.Core;
using Bit.Core.Enums;
using Bit.Core.Models.Business;
using Bit.Core.Models.Data;
using Bit.Core.Models.Table;
using Bit.Core.Utilities;

namespace Bit.Admin.Models
{
    public class OrganizationEditModel : OrganizationViewModel
    {
        public OrganizationEditModel() { }

        public OrganizationEditModel(Organization org, IEnumerable<OrganizationUserUserDetails> orgUsers,
            GlobalSettings globalSettings)
            : base(org, orgUsers)
        {
            Name = org.Name;
            BusinessName = org.BusinessName;
        }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }
        [Display(Name = "Business Name")]
        public string BusinessName { get; set; }

        public Organization ToOrganization(Organization existingOrganization)
        {
            existingOrganization.Name = Name;
            existingOrganization.BusinessName = BusinessName;
            return existingOrganization;
        }
    }
}

using System;
using System.Linq;
using Bit.Core.Models.Table;
using System.Collections.Generic;
using Bit.Core.Models.Business;

namespace Bit.Core.Models.Api
{
    public class OrganizationResponseModel : ResponseModel
    {
        public OrganizationResponseModel(Organization organization, string obj = "organization")
            : base(obj)
        {
            if(organization == null)
            {
                throw new ArgumentNullException(nameof(organization));
            }

            Id = organization.Id.ToString();
            Name = organization.Name;
            BusinessName = organization.BusinessName;
            BusinessAddress1 = organization.BusinessAddress1;
            BusinessAddress2 = organization.BusinessAddress2;
            BusinessAddress3 = organization.BusinessAddress3;
            BusinessCountry = organization.BusinessCountry;
            BusinessTaxNumber = organization.BusinessTaxNumber;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string BusinessName { get; set; }
        public string BusinessAddress1 { get; set; }
        public string BusinessAddress2 { get; set; }
        public string BusinessAddress3 { get; set; }
        public string BusinessCountry { get; set; }
        public string BusinessTaxNumber { get; set; }
    }
}

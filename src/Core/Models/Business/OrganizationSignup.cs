using Bit.Core.Enums;
using Bit.Core.Models.Table;

namespace Bit.Core.Models.Business
{
    public class OrganizationSignup : OrganizationUpgrade
    {
        public string Name { get; set; }
        public User Owner { get; set; }
        public string OwnerKey { get; set; }
        public string CollectionName { get; set; }
    }
}

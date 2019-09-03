using System.Threading.Tasks;
using Bit.Core.Models.Business;
using Bit.Core.Models.Table;
using System;
using System.Collections.Generic;
using Bit.Core.Enums;
using Bit.Core.Models.Data;

namespace Bit.Core.Services
{
    public interface IOrganizationService
    {
        Task<Tuple<Organization, OrganizationUser>> SignUpAsync(OrganizationSignup organizationSignup);
        Task DeleteAsync(Organization organization);
        Task UpdateAsync(Organization organization);
        Task UpdateTwoFactorProviderAsync(Organization organization, TwoFactorProviderType type);
        Task DisableTwoFactorProviderAsync(Organization organization, TwoFactorProviderType type);
        Task<OrganizationUser> InviteUserAsync(Guid organizationId, Guid? invitingUserId, string email,
            OrganizationUserType type, bool accessAll, string externalId, IEnumerable<SelectionReadOnly> collections);
        Task<List<OrganizationUser>> InviteUserAsync(Guid organizationId, Guid? invitingUserId, IEnumerable<string> emails,
            OrganizationUserType type, bool accessAll, string externalId, IEnumerable<SelectionReadOnly> collections);
        Task ResendInviteAsync(Guid organizationId, Guid invitingUserId, Guid organizationUserId);
        Task<OrganizationUser> AcceptUserAsync(Guid organizationUserId, User user, string token);
        Task<OrganizationUser> ConfirmUserAsync(Guid organizationId, Guid organizationUserId, string key, Guid confirmingUserId);
        Task SaveUserAsync(OrganizationUser user, Guid? savingUserId, IEnumerable<SelectionReadOnly> collections);
        Task DeleteUserAsync(Guid organizationId, Guid organizationUserId, Guid? deletingUserId);
        Task DeleteUserAsync(Guid organizationId, Guid userId);
        Task UpdateUserGroupsAsync(OrganizationUser organizationUser, IEnumerable<Guid> groupIds);
        Task ImportAsync(Guid organizationId, Guid importingUserId, IEnumerable<ImportedGroup> groups,
            IEnumerable<ImportedOrganizationUser> newUsers, IEnumerable<string> removeUserExternalIds,
            bool overwriteExisting);
        Task RotateApiKeyAsync(Organization organization);
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Repositories;
using Bit.Core.Models.Business;
using Bit.Core.Models.Table;
using Bit.Core.Utilities;
using Bit.Core.Exceptions;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection;
using Stripe;
using Bit.Core.Enums;
using Bit.Core.Models.Data;

namespace Bit.Core.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationUserRepository _organizationUserRepository;
        private readonly ICollectionRepository _collectionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IDataProtector _dataProtector;
        private readonly IMailService _mailService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IPushRegistrationService _pushRegistrationService;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IEventService _eventService;
        private readonly IInstallationRepository _installationRepository;
        private readonly GlobalSettings _globalSettings;

        public OrganizationService(
            IOrganizationRepository organizationRepository,
            IOrganizationUserRepository organizationUserRepository,
            ICollectionRepository collectionRepository,
            IUserRepository userRepository,
            IGroupRepository groupRepository,
            IDataProtectionProvider dataProtectionProvider,
            IMailService mailService,
            IPushNotificationService pushNotificationService,
            IPushRegistrationService pushRegistrationService,
            IDeviceRepository deviceRepository,
            IEventService eventService,
            IInstallationRepository installationRepository,
            GlobalSettings globalSettings)
        {
            _organizationRepository = organizationRepository;
            _organizationUserRepository = organizationUserRepository;
            _collectionRepository = collectionRepository;
            _userRepository = userRepository;
            _groupRepository = groupRepository;
            _dataProtector = dataProtectionProvider.CreateProtector("OrganizationServiceDataProtector");
            _mailService = mailService;
            _pushNotificationService = pushNotificationService;
            _pushRegistrationService = pushRegistrationService;
            _deviceRepository = deviceRepository;
            _eventService = eventService;
            _installationRepository = installationRepository;
            _globalSettings = globalSettings;
        }
        
        public async Task<Tuple<Organization, OrganizationUser>> SignUpAsync(OrganizationSignup signup)
        {
            var organization = new Organization
            {
                Id = CoreHelpers.GenerateComb(),
                Name = signup.Name,
                BusinessName = signup.BusinessName,
                ApiKey = CoreHelpers.SecureRandomString(30),
                CreationDate = DateTime.UtcNow,
                RevisionDate = DateTime.UtcNow
            };
            
            return await SignUpAsync(organization, signup.Owner.Id, signup.OwnerKey, signup.CollectionName, true);
        }

        private async Task<Tuple<Organization, OrganizationUser>> SignUpAsync(Organization organization,
            Guid ownerId, string ownerKey, string collectionName, bool withPayment)
        {
            try
            {
                await _organizationRepository.CreateAsync(organization);

                var orgUser = new OrganizationUser
                {
                    OrganizationId = organization.Id,
                    UserId = ownerId,
                    Key = ownerKey,
                    Type = OrganizationUserType.Owner,
                    Status = OrganizationUserStatusType.Confirmed,
                    AccessAll = true,
                    CreationDate = organization.CreationDate,
                    RevisionDate = organization.CreationDate
                };

                await _organizationUserRepository.CreateAsync(orgUser);

                if(!string.IsNullOrWhiteSpace(collectionName))
                {
                    var defaultCollection = new Collection
                    {
                        Name = collectionName,
                        OrganizationId = organization.Id,
                        CreationDate = organization.CreationDate,
                        RevisionDate = organization.CreationDate
                    };
                    await _collectionRepository.CreateAsync(defaultCollection);
                }

                // push
                var deviceIds = await GetUserDeviceIdsAsync(orgUser.UserId.Value);
                await _pushRegistrationService.AddUserRegistrationOrganizationAsync(deviceIds,
                    organization.Id.ToString());
                await _pushNotificationService.PushSyncOrgKeysAsync(ownerId);

                return new Tuple<Organization, OrganizationUser>(organization, orgUser);
            }
            catch
            {
                if(organization.Id != default(Guid))
                {
                    await _organizationRepository.DeleteAsync(organization);
                }

                throw;
            }
        }
        
        public async Task DeleteAsync(Organization organization)
        {
            await _organizationRepository.DeleteAsync(organization);
        }

        public async Task UpdateAsync(Organization organization)
        {
            if(organization.Id == default(Guid))
            {
                throw new ApplicationException("Cannot create org this way. Call SignUpAsync.");
            }

            await ReplaceAndUpdateCache(organization, EventType.Organization_Updated);
        }

        public async Task UpdateTwoFactorProviderAsync(Organization organization, TwoFactorProviderType type)
        {
            if(!type.ToString().Contains("Organization"))
            {
                throw new ArgumentException("Not an organization provider type.");
            }

            var providers = organization.GetTwoFactorProviders();
            if(!providers?.ContainsKey(type) ?? true)
            {
                return;
            }

            providers[type].Enabled = true;
            organization.SetTwoFactorProviders(providers);
            await UpdateAsync(organization);
        }

        public async Task DisableTwoFactorProviderAsync(Organization organization, TwoFactorProviderType type)
        {
            if(!type.ToString().Contains("Organization"))
            {
                throw new ArgumentException("Not an organization provider type.");
            }

            var providers = organization.GetTwoFactorProviders();
            if(!providers?.ContainsKey(type) ?? true)
            {
                return;
            }

            providers.Remove(type);
            organization.SetTwoFactorProviders(providers);
            await UpdateAsync(organization);
        }

        public async Task<OrganizationUser> InviteUserAsync(Guid organizationId, Guid? invitingUserId, string email,
            OrganizationUserType type, bool accessAll, string externalId, IEnumerable<SelectionReadOnly> collections)
        {
            var results = await InviteUserAsync(organizationId, invitingUserId, new List<string> { email },
                type, accessAll, externalId, collections);
            var result = results.FirstOrDefault();
            if(result == null)
            {
                throw new BadRequestException("This user has already been invited.");
            }
            return result;
        }

        public async Task<List<OrganizationUser>> InviteUserAsync(Guid organizationId, Guid? invitingUserId,
            IEnumerable<string> emails, OrganizationUserType type, bool accessAll, string externalId,
            IEnumerable<SelectionReadOnly> collections)
        {
            var organization = await GetOrgById(organizationId);
            if(organization == null)
            {
                throw new NotFoundException();
            }

            if(type == OrganizationUserType.Owner && invitingUserId.HasValue)
            {
                var invitingUserOrgs = await _organizationUserRepository.GetManyByUserAsync(invitingUserId.Value);
                var anyOwners = invitingUserOrgs.Any(
                    u => u.OrganizationId == organizationId && u.Type == OrganizationUserType.Owner);
                if(!anyOwners)
                {
                    throw new BadRequestException("Only owners can invite new owners.");
                }
            }

            var orgUsers = new List<OrganizationUser>();
            foreach(var email in emails)
            {
                // Make sure user is not already invited
                var existingOrgUserCount = await _organizationUserRepository.GetCountByOrganizationAsync(
                    organizationId, email, false);
                if(existingOrgUserCount > 0)
                {
                    continue;
                }

                var orgUser = new OrganizationUser
                {
                    OrganizationId = organizationId,
                    UserId = null,
                    Email = email.ToLowerInvariant(),
                    Key = null,
                    Type = type,
                    Status = OrganizationUserStatusType.Invited,
                    AccessAll = accessAll,
                    ExternalId = externalId,
                    CreationDate = DateTime.UtcNow,
                    RevisionDate = DateTime.UtcNow
                };

                if(!orgUser.AccessAll && collections.Any())
                {
                    await _organizationUserRepository.CreateAsync(orgUser, collections);
                }
                else
                {
                    await _organizationUserRepository.CreateAsync(orgUser);
                }

                await SendInviteAsync(orgUser);
                await _eventService.LogOrganizationUserEventAsync(orgUser, EventType.OrganizationUser_Invited);
                orgUsers.Add(orgUser);
            }

            return orgUsers;
        }

        public async Task ResendInviteAsync(Guid organizationId, Guid invitingUserId, Guid organizationUserId)
        {
            var orgUser = await _organizationUserRepository.GetByIdAsync(organizationUserId);
            if(orgUser == null || orgUser.OrganizationId != organizationId ||
                orgUser.Status != OrganizationUserStatusType.Invited)
            {
                throw new BadRequestException("User invalid.");
            }

            await SendInviteAsync(orgUser);
        }

        private async Task SendInviteAsync(OrganizationUser orgUser)
        {
            var org = await GetOrgById(orgUser.OrganizationId);
            var nowMillis = CoreHelpers.ToEpocMilliseconds(DateTime.UtcNow);
            var token = _dataProtector.Protect(
                $"OrganizationUserInvite {orgUser.Id} {orgUser.Email} {nowMillis}");
            await _mailService.SendOrganizationInviteEmailAsync(org.Name, orgUser, token);
        }

        public async Task<OrganizationUser> AcceptUserAsync(Guid organizationUserId, User user, string token)
        {
            var orgUser = await _organizationUserRepository.GetByIdAsync(organizationUserId);
            if(orgUser == null)
            {
                throw new BadRequestException("User invalid.");
            }

            if(orgUser.Status != OrganizationUserStatusType.Invited)
            {
                throw new BadRequestException("Already accepted.");
            }

            if(string.IsNullOrWhiteSpace(orgUser.Email) ||
                !orgUser.Email.Equals(user.Email, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new BadRequestException("User email does not match invite.");
            }

            var existingOrgUserCount = await _organizationUserRepository.GetCountByOrganizationAsync(
                orgUser.OrganizationId, user.Email, true);
            if(existingOrgUserCount > 0)
            {
                throw new BadRequestException("You are already part of this organization.");
            }

            if(!CoreHelpers.UserInviteTokenIsValid(_dataProtector, token, user.Email, orgUser.Id, _globalSettings))
            {
                throw new BadRequestException("Invalid token.");
            }

            orgUser.Status = OrganizationUserStatusType.Accepted;
            orgUser.UserId = user.Id;
            orgUser.Email = null;
            await _organizationUserRepository.ReplaceAsync(orgUser);

            // TODO: send notification emails to org admins and accepting user?

            return orgUser;
        }

        public async Task<OrganizationUser> ConfirmUserAsync(Guid organizationId, Guid organizationUserId, string key,
            Guid confirmingUserId)
        {
            var orgUser = await _organizationUserRepository.GetByIdAsync(organizationUserId);
            if(orgUser == null || orgUser.Status != OrganizationUserStatusType.Accepted ||
                orgUser.OrganizationId != organizationId)
            {
                throw new BadRequestException("User not valid.");
            }

            var org = await GetOrgById(organizationId);

            orgUser.Status = OrganizationUserStatusType.Confirmed;
            orgUser.Key = key;
            orgUser.Email = null;
            await _organizationUserRepository.ReplaceAsync(orgUser);
            await _eventService.LogOrganizationUserEventAsync(orgUser, EventType.OrganizationUser_Confirmed);

            var user = await _userRepository.GetByIdAsync(orgUser.UserId.Value);
            await _mailService.SendOrganizationConfirmedEmailAsync(org.Name, user.Email);

            // push
            var deviceIds = await GetUserDeviceIdsAsync(orgUser.UserId.Value);
            await _pushRegistrationService.AddUserRegistrationOrganizationAsync(deviceIds, organizationId.ToString());
            await _pushNotificationService.PushSyncOrgKeysAsync(orgUser.UserId.Value);

            return orgUser;
        }

        public async Task SaveUserAsync(OrganizationUser user, Guid? savingUserId,
            IEnumerable<SelectionReadOnly> collections)
        {
            if(user.Id.Equals(default(Guid)))
            {
                throw new BadRequestException("Invite the user first.");
            }

            if(savingUserId.HasValue)
            {
                var savingUserOrgs = await _organizationUserRepository.GetManyByUserAsync(savingUserId.Value);
                var savingUserIsOrgOwner = savingUserOrgs
                    .Any(u => u.OrganizationId == user.OrganizationId && u.Type == OrganizationUserType.Owner);
                if(!savingUserIsOrgOwner)
                {
                    var originalUser = await _organizationUserRepository.GetByIdAsync(user.Id);
                    var isOwner = originalUser.Type == OrganizationUserType.Owner;
                    var nowOwner = user.Type == OrganizationUserType.Owner;
                    if((isOwner && !nowOwner) || (!isOwner && nowOwner))
                    {
                        throw new BadRequestException("Only an owner can change the user type of another owner.");
                    }
                }
            }

            var confirmedOwners = (await GetConfirmedOwnersAsync(user.OrganizationId)).ToList();
            if(user.Type != OrganizationUserType.Owner &&
                confirmedOwners.Count == 1 && confirmedOwners[0].Id == user.Id)
            {
                throw new BadRequestException("Organization must have at least one confirmed owner.");
            }

            if(user.AccessAll)
            {
                // We don't need any collections if we're flagged to have all access.
                collections = new List<SelectionReadOnly>();
            }
            await _organizationUserRepository.ReplaceAsync(user, collections);
            await _eventService.LogOrganizationUserEventAsync(user, EventType.OrganizationUser_Updated);
        }

        public async Task DeleteUserAsync(Guid organizationId, Guid organizationUserId, Guid? deletingUserId)
        {
            var orgUser = await _organizationUserRepository.GetByIdAsync(organizationUserId);
            if(orgUser == null || orgUser.OrganizationId != organizationId)
            {
                throw new BadRequestException("User not valid.");
            }

            if(deletingUserId.HasValue && orgUser.UserId == deletingUserId.Value)
            {
                throw new BadRequestException("You cannot remove yourself.");
            }

            if(orgUser.Type == OrganizationUserType.Owner && deletingUserId.HasValue)
            {
                var deletingUserOrgs = await _organizationUserRepository.GetManyByUserAsync(deletingUserId.Value);
                var anyOwners = deletingUserOrgs.Any(
                    u => u.OrganizationId == organizationId && u.Type == OrganizationUserType.Owner);
                if(!anyOwners)
                {
                    throw new BadRequestException("Only owners can delete other owners.");
                }
            }

            var confirmedOwners = (await GetConfirmedOwnersAsync(organizationId)).ToList();
            if(confirmedOwners.Count == 1 && confirmedOwners[0].Id == organizationUserId)
            {
                throw new BadRequestException("Organization must have at least one confirmed owner.");
            }

            await _organizationUserRepository.DeleteAsync(orgUser);
            await _eventService.LogOrganizationUserEventAsync(orgUser, EventType.OrganizationUser_Removed);

            if(orgUser.UserId.HasValue)
            {
                // push
                var deviceIds = await GetUserDeviceIdsAsync(orgUser.UserId.Value);
                await _pushRegistrationService.DeleteUserRegistrationOrganizationAsync(deviceIds,
                    organizationId.ToString());
                await _pushNotificationService.PushSyncOrgKeysAsync(orgUser.UserId.Value);
            }
        }

        public async Task DeleteUserAsync(Guid organizationId, Guid userId)
        {
            var orgUser = await _organizationUserRepository.GetByOrganizationAsync(organizationId, userId);
            if(orgUser == null)
            {
                throw new NotFoundException();
            }

            var confirmedOwners = (await GetConfirmedOwnersAsync(organizationId)).ToList();
            if(confirmedOwners.Count == 1 && confirmedOwners[0].Id == orgUser.Id)
            {
                throw new BadRequestException("Organization must have at least one confirmed owner.");
            }

            await _organizationUserRepository.DeleteAsync(orgUser);
            await _eventService.LogOrganizationUserEventAsync(orgUser, EventType.OrganizationUser_Removed);

            if(orgUser.UserId.HasValue)
            {
                // push
                var deviceIds = await GetUserDeviceIdsAsync(orgUser.UserId.Value);
                await _pushRegistrationService.DeleteUserRegistrationOrganizationAsync(deviceIds,
                    organizationId.ToString());
                await _pushNotificationService.PushSyncOrgKeysAsync(orgUser.UserId.Value);
            }
        }

        public async Task UpdateUserGroupsAsync(OrganizationUser organizationUser, IEnumerable<Guid> groupIds)
        {
            await _organizationUserRepository.UpdateGroupsAsync(organizationUser.Id, groupIds);
            await _eventService.LogOrganizationUserEventAsync(organizationUser,
                EventType.OrganizationUser_UpdatedGroups);
        }

        public async Task ImportAsync(Guid organizationId,
            Guid importingUserId,
            IEnumerable<ImportedGroup> groups,
            IEnumerable<ImportedOrganizationUser> newUsers,
            IEnumerable<string> removeUserExternalIds,
            bool overwriteExisting)
        {
            var organization = await GetOrgById(organizationId);
            if(organization == null)
            {
                throw new NotFoundException();
            }

            var newUsersSet = new HashSet<string>(newUsers?.Select(u => u.ExternalId) ?? new List<string>());
            var existingUsers = await _organizationUserRepository.GetManyDetailsByOrganizationAsync(organizationId);
            var existingExternalUsers = existingUsers.Where(u => !string.IsNullOrWhiteSpace(u.ExternalId)).ToList();
            var existingExternalUsersIdDict = existingExternalUsers.ToDictionary(u => u.ExternalId, u => u.Id);

            // Users

            // Remove Users
            if(removeUserExternalIds?.Any() ?? false)
            {
                var removeUsersSet = new HashSet<string>(removeUserExternalIds);
                var existingUsersDict = existingExternalUsers.ToDictionary(u => u.ExternalId);

                var usersToRemove = removeUsersSet
                    .Except(newUsersSet)
                    .Where(ru => existingUsersDict.ContainsKey(ru))
                    .Select(ru => existingUsersDict[ru]);

                foreach(var user in usersToRemove)
                {
                    if(user.Type != OrganizationUserType.Owner)
                    {
                        await _organizationUserRepository.DeleteAsync(new OrganizationUser { Id = user.Id });
                        existingExternalUsersIdDict.Remove(user.ExternalId);
                    }
                }
            }

            if(overwriteExisting)
            {
                // Remove existing external users that are not in new user set
                foreach(var user in existingExternalUsers)
                {
                    if(user.Type != OrganizationUserType.Owner && !newUsersSet.Contains(user.ExternalId) &&
                        existingExternalUsersIdDict.ContainsKey(user.ExternalId))
                    {
                        await _organizationUserRepository.DeleteAsync(new OrganizationUser { Id = user.Id });
                        existingExternalUsersIdDict.Remove(user.ExternalId);
                    }
                }
            }

            if(newUsers?.Any() ?? false)
            {
                // Marry existing users
                var existingUsersEmailsDict = existingUsers
                    .Where(u => string.IsNullOrWhiteSpace(u.ExternalId))
                    .ToDictionary(u => u.Email);
                var newUsersEmailsDict = newUsers.ToDictionary(u => u.Email);
                var usersToAttach = existingUsersEmailsDict.Keys.Intersect(newUsersEmailsDict.Keys).ToList();
                foreach(var user in usersToAttach)
                {
                    var orgUserDetails = existingUsersEmailsDict[user];
                    var orgUser = await _organizationUserRepository.GetByIdAsync(orgUserDetails.Id);
                    if(orgUser != null)
                    {
                        orgUser.ExternalId = newUsersEmailsDict[user].ExternalId;
                        await _organizationUserRepository.UpsertAsync(orgUser);
                        existingExternalUsersIdDict.Add(orgUser.ExternalId, orgUser.Id);
                    }
                }

                // Add new users
                var existingUsersSet = new HashSet<string>(existingExternalUsersIdDict.Keys);
                var usersToAdd = newUsersSet.Except(existingUsersSet).ToList();

                foreach(var user in newUsers)
                {
                    if(!usersToAdd.Contains(user.ExternalId) || string.IsNullOrWhiteSpace(user.Email))
                    {
                        continue;
                    }

                    try
                    {
                        var newUser = await InviteUserAsync(organizationId, importingUserId, user.Email,
                            OrganizationUserType.User, false, user.ExternalId, new List<SelectionReadOnly>());
                        existingExternalUsersIdDict.Add(newUser.ExternalId, newUser.Id);
                    }
                    catch(BadRequestException)
                    {
                        continue;
                    }
                }
            }

            // Groups

            if(groups?.Any() ?? false)
            {
                var groupsDict = groups.ToDictionary(g => g.Group.ExternalId);
                var existingGroups = await _groupRepository.GetManyByOrganizationIdAsync(organizationId);
                var existingExternalGroups = existingGroups
                    .Where(u => !string.IsNullOrWhiteSpace(u.ExternalId)).ToList();
                var existingExternalGroupsDict = existingExternalGroups.ToDictionary(g => g.ExternalId);

                var newGroups = groups
                    .Where(g => !existingExternalGroupsDict.ContainsKey(g.Group.ExternalId))
                    .Select(g => g.Group);

                foreach(var group in newGroups)
                {
                    group.CreationDate = group.RevisionDate = DateTime.UtcNow;

                    await _groupRepository.CreateAsync(group);
                    await UpdateUsersAsync(group, groupsDict[group.ExternalId].ExternalUserIds,
                        existingExternalUsersIdDict);
                }

                var updateGroups = existingExternalGroups
                    .Where(g => groupsDict.ContainsKey(g.ExternalId))
                    .ToList();

                if(updateGroups.Any())
                {
                    var groupUsers = await _groupRepository.GetManyGroupUsersByOrganizationIdAsync(organizationId);
                    var existingGroupUsers = groupUsers
                        .GroupBy(gu => gu.GroupId)
                        .ToDictionary(g => g.Key, g => new HashSet<Guid>(g.Select(gr => gr.OrganizationUserId)));

                    foreach(var group in updateGroups)
                    {
                        var updatedGroup = groupsDict[group.ExternalId].Group;
                        if(group.Name != updatedGroup.Name)
                        {
                            group.RevisionDate = DateTime.UtcNow;
                            group.Name = updatedGroup.Name;

                            await _groupRepository.ReplaceAsync(group);
                        }

                        await UpdateUsersAsync(group, groupsDict[group.ExternalId].ExternalUserIds,
                            existingExternalUsersIdDict,
                            existingGroupUsers.ContainsKey(group.Id) ? existingGroupUsers[group.Id] : null);
                    }
                }
            }
        }

        public async Task RotateApiKeyAsync(Organization organization)
        {
            organization.ApiKey = CoreHelpers.SecureRandomString(30);
            organization.RevisionDate = DateTime.UtcNow;
            await ReplaceAndUpdateCache(organization);
        }

        private async Task UpdateUsersAsync(Group group, HashSet<string> groupUsers,
            Dictionary<string, Guid> existingUsersIdDict, HashSet<Guid> existingUsers = null)
        {
            var availableUsers = groupUsers.Intersect(existingUsersIdDict.Keys);
            var users = new HashSet<Guid>(availableUsers.Select(u => existingUsersIdDict[u]));
            if(existingUsers != null && existingUsers.Count == users.Count && users.SetEquals(existingUsers))
            {
                return;
            }

            await _groupRepository.UpdateUsersAsync(group.Id, users);
        }

        private async Task<IEnumerable<OrganizationUser>> GetConfirmedOwnersAsync(Guid organizationId)
        {
            var owners = await _organizationUserRepository.GetManyByOrganizationAsync(organizationId,
                OrganizationUserType.Owner);
            return owners.Where(o => o.Status == OrganizationUserStatusType.Confirmed);
        }

        private async Task<IEnumerable<string>> GetUserDeviceIdsAsync(Guid userId)
        {
            var devices = await _deviceRepository.GetManyByUserIdAsync(userId);
            return devices.Where(d => !string.IsNullOrWhiteSpace(d.PushToken)).Select(d => d.Id.ToString());
        }

        private async Task ReplaceAndUpdateCache(Organization org, EventType? orgEvent = null)
        {
            await _organizationRepository.ReplaceAsync(org);

            if(orgEvent.HasValue)
            {
                await _eventService.LogOrganizationEventAsync(org, orgEvent.Value);
            }
        }

        private async Task<Organization> GetOrgById(Guid id)
        {
            return await _organizationRepository.GetByIdAsync(id);
        }
    }
}

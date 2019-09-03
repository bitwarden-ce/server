using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Bit.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Bit.Core.Models.Api;
using Bit.Core.Exceptions;
using Bit.Core.Services;
using Bit.Core;
using Bit.Api.Utilities;
using Bit.Core.Models.Business;
using Bit.Core.Utilities;

namespace Bit.Api.Controllers
{
    [Route("organizations")]
    [Authorize("Application")]
    public class OrganizationsController : Controller
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationService _organizationService;
        private readonly IUserService _userService;
        private readonly CurrentContext _currentContext;
        private readonly GlobalSettings _globalSettings;

        public OrganizationsController(
            IOrganizationRepository organizationRepository,
            IOrganizationService organizationService,
            IUserService userService,
            CurrentContext currentContext,
            GlobalSettings globalSettings)
        {
            _organizationRepository = organizationRepository;
            _organizationService = organizationService;
            _userService = userService;
            _currentContext = currentContext;
            _globalSettings = globalSettings;
        }

        [HttpGet("{id}")]
        public async Task<OrganizationResponseModel> Get(string id)
        {
            var orgIdGuid = new Guid(id);
            if(!_currentContext.OrganizationOwner(orgIdGuid))
            {
                throw new NotFoundException();
            }

            var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
            if(organization == null)
            {
                throw new NotFoundException();
            }

            return new OrganizationResponseModel(organization);
        }

        [HttpGet("")]
        public async Task<ListResponseModel<OrganizationResponseModel>> GetUser()
        {
            var userId = _userService.GetProperUserId(User).Value;
            var organizations = await _organizationRepository.GetManyByUserIdAsync(userId);
            var responses = organizations.Select(o => new OrganizationResponseModel(o));
            return new ListResponseModel<OrganizationResponseModel>(responses);
        }

        [HttpPost("")]
        public async Task<OrganizationResponseModel> Post([FromBody]OrganizationCreateRequestModel model)
        {
            var user = await _userService.GetUserByPrincipalAsync(User);
            if(user == null)
            {
                throw new UnauthorizedAccessException();
            }

            var organizationSignup = model.ToOrganizationSignup(user);
            var result = await _organizationService.SignUpAsync(organizationSignup);
            return new OrganizationResponseModel(result.Item1);
        }
        
        [HttpPut("{id}")]
        [HttpPost("{id}")]
        public async Task<OrganizationResponseModel> Put(string id, [FromBody]OrganizationUpdateRequestModel model)
        {
            var orgIdGuid = new Guid(id);
            if(!_currentContext.OrganizationOwner(orgIdGuid))
            {
                throw new NotFoundException();
            }

            var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
            if(organization == null)
            {
                throw new NotFoundException();
            }

            await _organizationService.UpdateAsync(model.ToOrganization(organization));
            return new OrganizationResponseModel(organization);
        }
        
        [HttpPost("{id}/leave")]
        public async Task Leave(string id)
        {
            var orgGuidId = new Guid(id);
            if(!_currentContext.OrganizationUser(orgGuidId))
            {
                throw new NotFoundException();
            }

            var userId = _userService.GetProperUserId(User);
            await _organizationService.DeleteUserAsync(orgGuidId, userId.Value);
        }

        [HttpDelete("{id}")]
        [HttpPost("{id}/delete")]
        public async Task Delete(string id, [FromBody]OrganizationDeleteRequestModel model)
        {
            var orgIdGuid = new Guid(id);
            if(!_currentContext.OrganizationOwner(orgIdGuid))
            {
                throw new NotFoundException();
            }

            var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
            if(organization == null)
            {
                throw new NotFoundException();
            }

            var user = await _userService.GetUserByPrincipalAsync(User);
            if(user == null)
            {
                throw new UnauthorizedAccessException();
            }

            if(!await _userService.CheckPasswordAsync(user, model.MasterPasswordHash))
            {
                await Task.Delay(2000);
                throw new BadRequestException("MasterPasswordHash", "Invalid password.");
            }
            else
            {
                await _organizationService.DeleteAsync(organization);
            }
        }

        [HttpPost("{id}/import")]
        public async Task Import(string id, [FromBody]ImportOrganizationUsersRequestModel model)
        {
            if(model.Groups.Count() > 200 || model.Users.Count() > 1000)
            {
                throw new BadRequestException("You cannot import this much data at once.");
            }

            var orgIdGuid = new Guid(id);
            if(!_currentContext.OrganizationAdmin(orgIdGuid))
            {
                throw new NotFoundException();
            }

            var userId = _userService.GetProperUserId(User);
            await _organizationService.ImportAsync(
                orgIdGuid,
                userId.Value,
                model.Groups.Select(g => g.ToImportedGroup(orgIdGuid)),
                model.Users.Where(u => !u.Deleted).Select(u => u.ToImportedOrganizationUser()),
                model.Users.Where(u => u.Deleted).Select(u => u.ExternalId),
                model.OverwriteExisting);
        }

        [HttpPost("{id}/api-key")]
        public async Task<ApiKeyResponseModel> ApiKey(string id, [FromBody]ApiKeyRequestModel model)
        {
            var orgIdGuid = new Guid(id);
            if(!_currentContext.OrganizationOwner(orgIdGuid))
            {
                throw new NotFoundException();
            }

            var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
            if(organization == null)
            {
                throw new NotFoundException();
            }

            var user = await _userService.GetUserByPrincipalAsync(User);
            if(user == null)
            {
                throw new UnauthorizedAccessException();
            }

            if(!await _userService.CheckPasswordAsync(user, model.MasterPasswordHash))
            {
                await Task.Delay(2000);
                throw new BadRequestException("MasterPasswordHash", "Invalid password.");
            }
            else
            {
                var response = new ApiKeyResponseModel(organization);
                return response;
            }
        }

        [HttpPost("{id}/rotate-api-key")]
        public async Task<ApiKeyResponseModel> RotateApiKey(string id, [FromBody]ApiKeyRequestModel model)
        {
            var orgIdGuid = new Guid(id);
            if(!_currentContext.OrganizationOwner(orgIdGuid))
            {
                throw new NotFoundException();
            }

            var organization = await _organizationRepository.GetByIdAsync(orgIdGuid);
            if(organization == null)
            {
                throw new NotFoundException();
            }

            var user = await _userService.GetUserByPrincipalAsync(User);
            if(user == null)
            {
                throw new UnauthorizedAccessException();
            }

            if(!await _userService.CheckPasswordAsync(user, model.MasterPasswordHash))
            {
                await Task.Delay(2000);
                throw new BadRequestException("MasterPasswordHash", "Invalid password.");
            }
            else
            {
                await _organizationService.RotateApiKeyAsync(organization);
                var response = new ApiKeyResponseModel(organization);
                return response;
            }
        }
    }
}

using System.Threading.Tasks;
using Bit.Core.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bit.Admin.Identity
{
    public class BypassSigninManager: SignInManager<IdentityUser>
    {
        public BypassSigninManager(
            UserManager<IdentityUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<IdentityUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<IdentityUser>> logger,
            IAuthenticationSchemeProvider schemes)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes)
        {
        }

        public async Task<string> GenerateConfirmToken(string email)
        {
            var user = await UserManager.FindByEmailAsync(email);
            if(user == null)
            {
                return null;
            }

            return await GenerateConfirmToken(user);
        }

        public async Task<string> GenerateConfirmToken(IdentityUser user)
        {
            return await UserManager.GenerateUserTokenAsync(user, Options.Tokens.PasswordResetTokenProvider,
                PasswordlessSignInManager<IdentityUser>.PasswordlessSignInPurpose);
        }
    }
}

using System.Threading.Tasks;
using Bit.Core.Jobs;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Bit.Api.Jobs
{
    public class ValidateUsersJob : BaseJob
    {
        public ValidateUsersJob(
            ILogger<ValidateUsersJob> logger)
            : base(logger)
        {
        }

        protected async override Task ExecuteJobAsync(IJobExecutionContext context)
        {
        }
    }
}

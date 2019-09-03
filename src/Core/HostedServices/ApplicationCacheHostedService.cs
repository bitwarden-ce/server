using System;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Enums;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Utilities;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bit.Core.HostedServices
{
    public class ApplicationCacheHostedService : IHostedService, IDisposable
    {
        private readonly IOrganizationRepository _organizationRepository;
        protected readonly ILogger<ApplicationCacheHostedService> _logger;
        private readonly SubscriptionClient _subscriptionClient;
        private readonly ManagementClient _managementClient;
        private readonly string _subName;
        private readonly string _topicName;

        public ApplicationCacheHostedService(
            IOrganizationRepository organizationRepository,
            ILogger<ApplicationCacheHostedService> logger,
            GlobalSettings globalSettings)
        {
            _topicName = globalSettings.ServiceBus.ApplicationCacheTopicName;
            _subName = CoreHelpers.GetApplicationCacheServiceBusSubcriptionName(globalSettings);
            _organizationRepository = organizationRepository;
            _logger = logger;
            _managementClient = new ManagementClient(globalSettings.ServiceBus.ConnectionString);
            _subscriptionClient = new SubscriptionClient(globalSettings.ServiceBus.ConnectionString,
                _topicName, _subName);
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _managementClient.CreateSubscriptionAsync(new SubscriptionDescription(_topicName, _subName)
                {
                    DefaultMessageTimeToLive = TimeSpan.FromDays(14),
                    LockDuration = TimeSpan.FromSeconds(30),
                    EnableDeadLetteringOnFilterEvaluationExceptions = true,
                    EnableDeadLetteringOnMessageExpiration = true,
                }, new RuleDescription("default", new SqlFilter($"sys.Label != '{_subName}'")));
            }
            catch(MessagingEntityAlreadyExistsException) { }
            _subscriptionClient.RegisterMessageHandler(ProcessMessageAsync,
                new MessageHandlerOptions(ExceptionReceivedHandlerAsync)
                {
                    MaxConcurrentCalls = 2,
                    AutoComplete = false,
                });
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            await _subscriptionClient.CloseAsync();
            try
            {
                await _managementClient.DeleteSubscriptionAsync(_topicName, _subName, cancellationToken);
            }
            catch { }
        }

        public virtual void Dispose()
        { }

        private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
        {
            if(!cancellationToken.IsCancellationRequested)
            {
                await _subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
            }
        }

        private Task ExceptionReceivedHandlerAsync(ExceptionReceivedEventArgs args)
        {
            _logger.LogError(args.Exception, "Message handler encountered an exception.");
            return Task.FromResult(0);
        }
    }
}

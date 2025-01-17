﻿using System.Threading.Tasks;
using System;
using Bit.Core.Enums;
using Bit.Core.Repositories;
using Bit.Core.Models.Data;
using System.Linq;
using System.Collections.Generic;
using Bit.Core.Models.Table;

namespace Bit.Core.Services
{
    public class EventService : IEventService
    {
        private readonly IEventWriteService _eventWriteService;
        private readonly IOrganizationUserRepository _organizationUserRepository;
        private readonly CurrentContext _currentContext;
        private readonly GlobalSettings _globalSettings;

        public EventService(
            IEventWriteService eventWriteService,
            IOrganizationUserRepository organizationUserRepository,
            CurrentContext currentContext,
            GlobalSettings globalSettings)
        {
            _eventWriteService = eventWriteService;
            _organizationUserRepository = organizationUserRepository;
            _currentContext = currentContext;
            _globalSettings = globalSettings;
        }

        public async Task LogUserEventAsync(Guid userId, EventType type, DateTime? date = null)
        {
            var events = new List<IEvent>
            {
                new EventMessage(_currentContext)
                {
                    UserId = userId,
                    ActingUserId = userId,
                    Type = type,
                    Date = date.GetValueOrDefault(DateTime.UtcNow)
                }
            };

            var orgs = await _currentContext.OrganizationMembershipAsync(_organizationUserRepository, userId);
            var orgEvents = orgs
                .Select(o => new EventMessage(_currentContext)
                {
                    OrganizationId = o.Id,
                    UserId = userId,
                    ActingUserId = userId,
                    Type = type,
                    Date = DateTime.UtcNow
                });

            if(orgEvents.Any())
            {
                events.AddRange(orgEvents);
                await _eventWriteService.CreateManyAsync(events);
            }
            else
            {
                await _eventWriteService.CreateAsync(events.First());
            }
        }

        public async Task LogCipherEventAsync(Cipher cipher, EventType type, DateTime? date = null)
        {
            var e = BuildCipherEventMessageAsync(cipher, type, date);
            if(e != null)
            {
                await _eventWriteService.CreateAsync(e);
            }
        }

        public async Task LogCipherEventsAsync(IEnumerable<Tuple<Cipher, EventType, DateTime?>> events)
        {
            var cipherEvents = new List<IEvent>();
            foreach(var ev in events)
            {
                var e = BuildCipherEventMessageAsync(ev.Item1, ev.Item2, ev.Item3);
                if(e != null)
                {
                    cipherEvents.Add(e);
                }
            }
            await _eventWriteService.CreateManyAsync(cipherEvents);
        }

        private EventMessage BuildCipherEventMessageAsync(Cipher cipher, EventType type, DateTime? date = null)
        {
            // Only logging organization cipher events for now.
            if(!cipher.OrganizationId.HasValue || (!_currentContext?.UserId.HasValue ?? true))
            {
                return null;
            }

            return new EventMessage(_currentContext)
            {
                OrganizationId = cipher.OrganizationId,
                UserId = cipher.OrganizationId.HasValue ? null : cipher.UserId,
                CipherId = cipher.Id,
                Type = type,
                ActingUserId = _currentContext?.UserId,
                Date = date.GetValueOrDefault(DateTime.UtcNow)
            };
        }

        public async Task LogCollectionEventAsync(Collection collection, EventType type, DateTime? date = null)
        {
            var e = new EventMessage(_currentContext)
            {
                OrganizationId = collection.OrganizationId,
                CollectionId = collection.Id,
                Type = type,
                ActingUserId = _currentContext?.UserId,
                Date = date.GetValueOrDefault(DateTime.UtcNow)
            };
            await _eventWriteService.CreateAsync(e);
        }

        public async Task LogGroupEventAsync(Group group, EventType type, DateTime? date = null)
        {

            var e = new EventMessage(_currentContext)
            {
                OrganizationId = group.OrganizationId,
                GroupId = group.Id,
                Type = type,
                ActingUserId = _currentContext?.UserId,
                Date = date.GetValueOrDefault(DateTime.UtcNow)
            };
            await _eventWriteService.CreateAsync(e);
        }

        public async Task LogOrganizationUserEventAsync(OrganizationUser organizationUser, EventType type,
            DateTime? date = null)
        {

            var e = new EventMessage(_currentContext)
            {
                OrganizationId = organizationUser.OrganizationId,
                UserId = organizationUser.UserId,
                OrganizationUserId = organizationUser.Id,
                Type = type,
                ActingUserId = _currentContext?.UserId,
                Date = date.GetValueOrDefault(DateTime.UtcNow)
            };
            await _eventWriteService.CreateAsync(e);
        }

        public async Task LogOrganizationEventAsync(Organization organization, EventType type, DateTime? date = null)
        {
            var e = new EventMessage(_currentContext)
            {
                OrganizationId = organization.Id,
                Type = type,
                ActingUserId = _currentContext?.UserId,
                Date = date.GetValueOrDefault(DateTime.UtcNow)
            };
            await _eventWriteService.CreateAsync(e);
        }
    }
}

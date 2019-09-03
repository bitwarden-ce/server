using System;
using Bit.Core.Utilities;
using Bit.Core.Enums;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace Bit.Core.Models.Table
{
    public class Organization : ITableObject<Guid>, IRevisable
    {
        private Dictionary<TwoFactorProviderType, TwoFactorProvider> _twoFactorProviders;

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string BusinessName { get; set; }
        public string BusinessAddress1 { get; set; }
        public string BusinessAddress2 { get; set; }
        public string BusinessAddress3 { get; set; }
        public string BusinessCountry { get; set; }
        public string BusinessTaxNumber { get; set; }
        public string ApiKey { get; set; }
        public string TwoFactorProviders { get; set; }
        public DateTime CreationDate { get; internal set; } = DateTime.UtcNow;
        public DateTime RevisionDate { get; internal set; } = DateTime.UtcNow;

        public void SetNewId()
        {
            if(Id == default(Guid))
            {
                Id = CoreHelpers.GenerateComb();
            }
        }
        
        public bool IsUser()
        {
            return false;
        }

        public Dictionary<TwoFactorProviderType, TwoFactorProvider> GetTwoFactorProviders()
        {
            if(string.IsNullOrWhiteSpace(TwoFactorProviders))
            {
                return null;
            }

            try
            {
                if(_twoFactorProviders == null)
                {
                    _twoFactorProviders =
                        JsonConvert.DeserializeObject<Dictionary<TwoFactorProviderType, TwoFactorProvider>>(
                            TwoFactorProviders);
                }

                return _twoFactorProviders;
            }
            catch(JsonSerializationException)
            {
                return null;
            }
        }

        public void SetTwoFactorProviders(Dictionary<TwoFactorProviderType, TwoFactorProvider> providers)
        {
            if(!providers.Any())
            {
                TwoFactorProviders = null;
                _twoFactorProviders = null;
                return;
            }

            TwoFactorProviders = JsonConvert.SerializeObject(providers, new JsonSerializerSettings
            {
                ContractResolver = new EnumKeyResolver<byte>()
            });
            _twoFactorProviders = providers;
        }

        public bool TwoFactorProviderIsEnabled(TwoFactorProviderType provider)
        {
            var providers = GetTwoFactorProviders();
            if(providers == null || !providers.ContainsKey(provider))
            {
                return false;
            }

            return providers[provider].Enabled;
        }

        public bool TwoFactorIsEnabled()
        {
            var providers = GetTwoFactorProviders();
            if(providers == null)
            {
                return false;
            }

            return providers.Any(p => (p.Value?.Enabled ?? false));
        }

        public TwoFactorProvider GetTwoFactorProvider(TwoFactorProviderType provider)
        {
            var providers = GetTwoFactorProviders();
            if(providers == null || !providers.ContainsKey(provider))
            {
                return null;
            }

            return providers[provider];
        }
    }
}

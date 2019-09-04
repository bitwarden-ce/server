using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Azure.ServiceBus;
using YamlDotNet.Serialization;

namespace Bit.Setup
{
    public class Configuration
    {
        [Description("Note: After making changes to this file you need to run the `rebuild` or `update`\n" +
            "command for them to be applied.\n\n" +

            "Full Qualified Domain Name to access your vault. (Required)")]
        public string Domain { get; set; } = "localhost";

        [Description("General server configuration")]
        public ServerConfig Server = new ServerConfig();
        
        [Description("docker-compose file generation connfiguration.")]
        public ComposeConfig Compose = new ComposeConfig();

        [Description("nginx file generation configuration")]
        public NginxConfig Nginx = new NginxConfig();

        [Description("Configure SSL")]
        public SslConfig Ssl = new SslConfig();

        [Description("Configure database")]
        public DatabaseConfig Database = new DatabaseConfig();

        [Description("Configure SMTP")]
        public SmtpConfig Smtp = new SmtpConfig();

        [Description("Yubico config")]
        public YubicoConfig Yubico = new YubicoConfig();

        [Description("Config low-level instance attributes.")]
        public InstanceConfig Instance = new InstanceConfig();

        [YamlIgnore]
        public string Url
        {
            get
            {
                var protocol = SslEnabled ? "https" : "http";
                return $"{protocol}://{Domain}";
            }
            set
            {
                if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
                {
                    Domain = uri.Host;
                    SslEnabled = uri.Scheme == "https";
                }
            }
        }

        [YamlIgnore]
        public bool SslEnabled
        {
            get { return Ssl != null && Ssl.Enable; }
            set
            {
                if (Ssl == null)
                {
                    Ssl = new SslConfig();
                }

                Ssl.Enable = value;
            }
        }

        public class ServerConfig
        {
            [Description("Docker compose file port mapping for HTTP. Leave empty to remove the port mapping.\n" +
                         "Learn more: https://docs.docker.com/compose/compose-file/#ports")]
            public string HttpPort { get; set; } = "80";

            [Description("Docker compose file port mapping for HTTPS. Leave empty to remove the port mapping.\n" +
                         "Learn more: https://docs.docker.com/compose/compose-file/#ports")]
            public string HttpsPort { get; set; } = "443";
        }

        public class ComposeConfig
        {
            [Description("Auto-generate the `docker/docker-compose.yml` config file.\n" +
                         "WARNING: Disabling generated config files can break future updates. You will be\n" +
                         "responsible for maintaining this config file.")]
            public bool Enable { get; set; } = true;
            
            [Description("Docker compose file version. Leave empty for default.\n" +
                         "Learn more: https://docs.docker.com/compose/compose-file/compose-versioning/")]
            public string Version { get; set; }
            
            [Description("Use a docker volume (`mssql_data`) instead of a host-mapped volume for the persisted " +
                         "database.\n" +
                         "WARNING: Changing this value will cause you to lose access to the existing persisted database.\n" +
                         "Learn more: https://docs.docker.com/storage/volumes/")]
            public bool DatabaseDockerVolume { get; set; }
        }

        public class NginxConfig
        {
            [Description("Auto-generate the `./nginx/default.conf` file.\n" +
                         "WARNING: Disabling generated config files can break future updates. You will be\n" +
                         "responsible for maintaining this config file.")]
            public bool Enable { get; set; } = true;
            
            [Description("SSL versions used by Nginx (ssl_protocols). Leave empty for recommended default.\n" +
                         "Learn more: https://wiki.mozilla.org/Security/Server_Side_TLS")]
            public string SslVersions { get; set; }

            [Description("SSL ciphersuites used by Nginx (ssl_ciphers). Leave empty for recommended default.\n" +
                         "Learn more: https://wiki.mozilla.org/Security/Server_Side_TLS")]
            public string SslCiphersuites { get; set; }
            
            [Description("Defines \"real\" IPs in nginx.conf. Useful for defining proxy servers that forward the \n" +
                         "client IP address.\n" +
                         "Learn more: https://nginx.org/en/docs/http/ngx_http_realip_module.html")]
            public List<string> RealIps { get; set; }
        }

        public class SslConfig
        {
            [Description("Enable SSL.")]
            public bool Enable = true;

            [Description("Installation uses a managed Let's Encrypt certificate.")]
            public bool ManagedLetsEncrypt { get; set; }

            [Description("The actual certificate. (Required if using SSL without managed Let's Encrypt)\n" +
                         "Note: Path uses the container's ssl directory. The `./ssl` host directory is mapped to\n" +
                         "`/etc/ssl` within the container.")]
            public string CertificatePath { get; set; }

            [Description("The certificate's private key. (Required if using SSL without managed Let's Encrypt)\n" +
                         "Note: Path uses the container's ssl directory. The `./ssl` host directory is mapped to\n" +
                         "`/etc/ssl` within the container.")]
            public string KeyPath { get; set; }

            [Description("If the certificate is trusted by a CA, you should provide the CA's certificate.\n" +
                         "Note: Path uses the container's ssl directory. The `./ssl` host directory is mapped to\n" +
                         "`/etc/ssl` within the container.")]
            public string CaPath { get; set; }

            [Description("Diffie Hellman ephemeral parameters\n" +
                         "Learn more: https://security.stackexchange.com/q/94390/79072\n" +
                         "Note: Path uses the container's ssl directory. The `./ssl` host directory is mapped to\n" +
                         "`/etc/ssl` within the container.")]
            public string DiffieHellmanPath { get; set; }
        }

        public class DatabaseConfig
        {
            [Description("Database hostname, if needed to customize it.")]
            public string Hostname = "mssql";
        }

        public class SmtpConfig
        {
            [Description("SMTP hostname")]
            public string Hostname;

            [Description("SMTP port")]
            public int Port = 587;

            [Description("SMTP username")]
            public string Username;

            [Description("SMTP password")]
            public string Password;

            [Description("SMTP use SSL")]
            public bool Ssl = false;
        }

        public class YubicoConfig
        {
            [Description("Yubico API client ID")]
            public string ClientId;

            [Description("Yubico API key")]
            public string Key;
        }

        public class InstanceConfig
        {
            [Description("Enable user registration on the instance.")]
            public bool EnableUserRegistration { get; set; } = true;
        
            [Description("Instance admins email list.")]
            public List<string> Admins { get; set; } = new List<string>();
        }
    }
}

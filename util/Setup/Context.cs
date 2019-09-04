using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Bit.Setup
{
    public class Context
    {
        public string[] Args { get; set; }
        public bool Quiet { get; set; }
        public bool Stub { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
        public string DestDir { get; set; } = "/bitwarden";
        public string OutputDir { get; set; } = "/etc/bitwarden";
        public string HostOS { get; set; } = "win";
        public string CoreVersion { get; set; } = "latest";
        public string WebVersion { get; set; } = "latest";
        public Installation Install { get; set; } = new Installation();
        public Configuration Config { get; set; } = new Configuration();

        private string ConfigPath
        {
            get { return $"{DestDir}/config.yml"; }
        }

        public bool PrintToScreen()
        {
            return !Quiet || Parameters.ContainsKey("install");
        }

        public void LoadConfiguration()
        {
            if(!File.Exists(ConfigPath))
            {
                Helpers.WriteLine(this, "No existing `config.yml` detected. Let's generate one.");

                // Looks like updating from older version. Try to create config file.
                var url = Helpers.GetValueFromEnvFile(this, "global", "globalSettings__baseServiceUri__vault");
                if(!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    Helpers.WriteLine(this, "Unable to determine existing installation url.");
                    return;
                }
                Config.Url = url;

                var composeFile = $"{DestDir}/docker/docker-compose.yml";
                if(File.Exists(composeFile))
                {
                    var fileLines = File.ReadAllLines(composeFile);
                    foreach(var line in fileLines)
                    {
                        if(!line.StartsWith("# Parameter:"))
                        {
                            continue;
                        }

                        var paramParts = line.Split("=");
                        if(paramParts.Length < 2)
                        {
                            continue;
                        }

                        if(paramParts[0] == "# Parameter:MssqlDataDockerVolume" &&
                            bool.TryParse(paramParts[1], out var mssqlDataDockerVolume))
                        {
                            Config.Compose.DatabaseDockerVolume = mssqlDataDockerVolume;
                            continue;
                        }

                        if(paramParts[0] == "# Parameter:HttpPort" && int.TryParse(paramParts[1], out var httpPort))
                        {
                            Config.Server.HttpPort = httpPort == 0 ? null : httpPort.ToString();
                            continue;
                        }

                        if(paramParts[0] == "# Parameter:HttpsPort" && int.TryParse(paramParts[1], out var httpsPort))
                        {
                            Config.Server.HttpsPort = httpsPort == 0 ? null : httpsPort.ToString();
                            continue;
                        }
                    }
                }

                var nginxFile = $"{DestDir}/nginx/default.conf";
                if(File.Exists(nginxFile))
                {
                    var confContent = File.ReadAllText(nginxFile);
                    var selfSigned = confContent.Contains("/etc/ssl/self/");
                    Config.SslEnabled = confContent.Contains("ssl http2;");
                    Config.Ssl.ManagedLetsEncrypt = !selfSigned && confContent.Contains("/etc/letsencrypt/live/");
                    var diffieHellman = confContent.Contains("/dhparam.pem;");
                    var trusted = confContent.Contains("ssl_trusted_certificate ");
                    if(Config.Ssl.ManagedLetsEncrypt)
                    {
                        Config.SslEnabled = true;
                    }
                    else if(Config.SslEnabled)
                    {
                        var sslPath = selfSigned ? $"/etc/ssl/self/{Config.Domain}" : $"/etc/ssl/{Config.Domain}";
                        Config.Ssl.CertificatePath = string.Concat(sslPath, "/", "certificate.crt");
                        Config.Ssl.KeyPath = string.Concat(sslPath, "/", "private.key");
                        if(trusted)
                        {
                            Config.Ssl.CaPath = string.Concat(sslPath, "/", "ca.crt");
                        }
                        if(diffieHellman)
                        {
                            Config.Ssl.DiffieHellmanPath = string.Concat(sslPath, "/", "dhparam.pem");
                        }
                    }
                }

                SaveConfiguration();
            }

            var configText = File.ReadAllText(ConfigPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new UnderscoredNamingConvention())
                .Build();
            Config = deserializer.Deserialize<Configuration>(configText);
        }

        public void SaveConfiguration()
        {
            if(Config == null)
            {
                throw new Exception("Config is null.");
            }
            var serializer = new SerializerBuilder()
                .EmitDefaults()
                .WithNamingConvention(new UnderscoredNamingConvention())
                .WithTypeInspector(inner => new CommentGatheringTypeInspector(inner))
                .WithEmissionPhaseObjectGraphVisitor(args => new CommentsObjectGraphVisitor(args.InnerVisitor))
                .Build();
            var yaml = serializer.Serialize(Config);
            Directory.CreateDirectory($"{DestDir}");
            using(var sw = File.CreateText(ConfigPath))
            {
                sw.Write(yaml);
            }
        }

        public class Installation
        {
            public Guid InstallationId { get; set; }
            public string InstallationKey { get; set; }
            public bool DiffieHellman { get; set; }
            public bool Trusted { get; set; }
            public bool? Ssl { get; set; }
            public bool SelfSignedCert { get; set; }
            public string IdentityCertPassword { get; set; }
            public string Domain { get; set; }
        }
    }
}

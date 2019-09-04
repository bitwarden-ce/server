using System;
using System.IO;

namespace Bit.Setup
{
    public class DockerComposeBuilder
    {
        private readonly Context _context;

        public DockerComposeBuilder(Context context)
        {
            _context = context;
        }

        public void BuildForInstaller()
        {
            _context.Config.Compose.DatabaseDockerVolume = _context.HostOS == "mac";
            Build();
        }

        public void BuildForUpdater()
        {
            Build();
        }

        private void Build()
        {
            Directory.CreateDirectory($"{_context.DestDir}/docker/");
            Helpers.WriteLine(_context, "Building docker-compose.yml.");
            if(!_context.Config.Compose.Enable)
            {
                Helpers.WriteLine(_context, "...skipped");
                return;
            }

            var template = Helpers.ReadTemplate("DockerCompose");
            var model = new TemplateModel(_context);
            using(var sw = File.CreateText($"{_context.DestDir}/docker/docker-compose.yml"))
            {
                sw.Write(template(model));
            }
        }

        public class TemplateModel
        {
            public TemplateModel(Context context)
            {
                if(!string.IsNullOrWhiteSpace(context.Config.Compose.Version))
                {
                    ComposeVersion = context.Config.Compose.Version;
                }
                MssqlDataDockerVolume = context.Config.Compose.DatabaseDockerVolume;
                HttpPort = context.Config.Server.HttpPort;
                HttpsPort = context.Config.Server.HttpsPort;
                if(!string.IsNullOrWhiteSpace(context.CoreVersion))
                {
                    CoreVersion = context.CoreVersion;
                }
                if(!string.IsNullOrWhiteSpace(context.WebVersion))
                {
                    WebVersion = context.WebVersion;
                }
            }

            public string ComposeVersion { get; } = "3";
            public bool MssqlDataDockerVolume { get; }
            public string HttpPort { get; }
            public string HttpsPort { get; }
            public bool HasPort => !string.IsNullOrWhiteSpace(HttpPort) || !string.IsNullOrWhiteSpace(HttpsPort);
            public string CoreVersion { get; } = "latest";
            public string WebVersion { get; } = "latest";
        }
    }
}

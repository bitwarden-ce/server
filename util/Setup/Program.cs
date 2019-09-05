using Bit.Migrator;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using Bit.Core.Utilities;

namespace Bit.Setup
{
    public class Program
    {
        private static Context _context;

        public static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            _context = new Context
            {
                Args = args
            };
            ParseParameters();

            if(_context.Parameters.ContainsKey("q"))
            {
                _context.Quiet = _context.Parameters["q"] == "true" || _context.Parameters["q"] == "1";
            }
            if(_context.Parameters.ContainsKey("os"))
            {
                _context.HostOS = _context.Parameters["os"];
            }
            if(_context.Parameters.ContainsKey("corev"))
            {
                _context.CoreVersion = _context.Parameters["corev"];
            }
            if(_context.Parameters.ContainsKey("webv"))
            {
                _context.WebVersion = _context.Parameters["webv"];
            }
            if(_context.Parameters.ContainsKey("stub"))
            {
                _context.Stub = _context.Parameters["stub"] == "true" ||
                    _context.Parameters["stub"] == "1";
            }
            if(_context.Parameters.ContainsKey("destdir"))
            {
                _context.DestDir = _context.Parameters["destdir"];
            }
            if(_context.Parameters.ContainsKey("outputdir"))
            {
                _context.OutputDir = _context.Parameters["outputdir"];
            }

            Helpers.WriteLine(_context);

            if(_context.Parameters.ContainsKey("install"))
            {
                Install();
            }
            else if(_context.Parameters.ContainsKey("update"))
            {
                Update();
            }
            else if(_context.Parameters.ContainsKey("regenidentity"))
            {
                RegenerateIdentityCertificate();
            }
            else if(_context.Parameters.ContainsKey("printenv"))
            {
                PrintEnvironment();
            }
            else
            {
                Helpers.WriteLine(_context, "No top-level command detected. Exiting...");
            }
        }

        private static void Install()
        {
            if(_context.Parameters.ContainsKey("letsencrypt"))
            {
                _context.Config.Ssl.ManagedLetsEncrypt =
                    _context.Parameters["letsencrypt"].ToLowerInvariant() == "y";
            }
            if(_context.Parameters.ContainsKey("domain"))
            {
                _context.Install.Domain = _context.Parameters["domain"].ToLowerInvariant();
                _context.Config.Domain = _context.Install.Domain;
            }
            if(_context.Parameters.ContainsKey("ssl"))
            {
                _context.Config.Ssl.Enable = _context.Parameters["ssl"] == "true" ||
                                      _context.Parameters["ssl"] == "1";
                _context.Install.Ssl = _context.Config.Ssl.Enable;
            }
            if(_context.Parameters.ContainsKey("dbhost"))
            {
                _context.Config.Database.Hostname = _context.Parameters["dbhost"];
            }

            _context.Install.InstallationId = CoreHelpers.GenerateComb();
            _context.Install.InstallationKey = CoreHelpers.SecureRandomString(20);

            var certBuilder = new CertBuilder(_context);
            certBuilder.BuildForInstall();

            var nginxBuilder = new NginxConfigBuilder(_context);
            nginxBuilder.BuildForInstaller();

            var environmentFileBuilder = new EnvironmentFileBuilder(_context);
            environmentFileBuilder.BuildForInstaller();

            var appIdBuilder = new AppIdBuilder(_context);
            appIdBuilder.Build();

            var dockerComposeBuilder = new DockerComposeBuilder(_context);
            dockerComposeBuilder.BuildForInstaller();

            _context.SaveConfiguration();

            Console.WriteLine("\nInstallation complete");

            Console.WriteLine("\nIf you need to make additional configuration changes, you can modify\n" +
                "the settings in `{0}` and then run:\n{1}",
                _context.HostOS == "win" ? ".\\bwdata\\config.yml" : "./bwdata/config.yml",
                _context.HostOS == "win" ? "`.\\bytegarden.ps1 -rebuild` or `.\\bytegarden.ps1 -update`" :
                    "`./bytegarden.sh rebuild` or `./bytegarden.sh update`");

            Console.WriteLine("\nNext steps, run:");
            if(_context.HostOS == "win")
            {
                Console.WriteLine("`.\\bytegarden.ps1 -start`");
            }
            else
            {
                Console.WriteLine("`./bytegarden.sh start`");
            }
            Console.WriteLine(string.Empty);
        }

        private static void Update()
        {
            if(_context.Parameters.ContainsKey("db"))
            {
                MigrateDatabase();
            }
            else
            {
                RebuildConfigs();
            }
        }

        private static void RegenerateIdentityCertificate()
        {
            _context.LoadConfiguration();
            
            var certBuilder = new CertBuilder(_context);
            certBuilder.GenerateIdentityCertificate();

            var fixes = new Dictionary<String, String>
            {
                ["globalSettings__identityServer__certificatePassword"] = _context.Install?.IdentityCertPassword,
            };
            var environmentFileBuilder = new EnvironmentFileBuilder(_context);
            environmentFileBuilder.BuildForFixes(fixes);
        }

        private static void PrintEnvironment()
        {
            _context.LoadConfiguration();
            if(!_context.PrintToScreen())
            {
                return;
            }
            Console.WriteLine("\nByteGarden is up and running!");
            Console.WriteLine("===================================================");
            Console.WriteLine("\nvisit {0}", _context.Config.Url);
            Console.Write("to update, run ");
            if(_context.HostOS == "win")
            {
                Console.Write("`.\\bytegarden.ps1 -updateself` and then `.\\bytegarden.ps1 -update`");
            }
            else
            {
                Console.Write("`./bytegarden.sh updateself` and then `./bytegarden.sh update`");
            }
            Console.WriteLine("\n");
        }

        private static void MigrateDatabase(int attempt = 1)
        {
            try
            {
                Helpers.WriteLine(_context, "Migrating database.");
                var vaultConnectionString = Helpers.GetValueFromEnvFile(_context, "global",
                    "globalSettings__sqlServer__connectionString");
                var migrator = new DbMigrator(vaultConnectionString, null);
                var success = migrator.MigrateMsSqlDatabase(false);
                if(success)
                {
                    Helpers.WriteLine(_context, "Migration successful.");
                }
                else
                {
                    Helpers.WriteLine(_context, "Migration failed.");
                }
            }
            catch(SqlException e)
            {
                if(e.Message.Contains("Server is in script upgrade mode") && attempt < 10)
                {
                    var nextAttempt = attempt + 1;
                    Helpers.WriteLine(_context, "Database is in script upgrade mode. " +
                        "Trying again (attempt #{0})...", nextAttempt);
                    System.Threading.Thread.Sleep(20000);
                    MigrateDatabase(nextAttempt);
                    return;
                }
                throw e;
            }
        }

        private static void RebuildConfigs()
        {
            _context.LoadConfiguration();

            var environmentFileBuilder = new EnvironmentFileBuilder(_context);
            environmentFileBuilder.BuildForUpdater();

            var nginxBuilder = new NginxConfigBuilder(_context);
            nginxBuilder.BuildForUpdater();

            var appIdBuilder = new AppIdBuilder(_context);
            appIdBuilder.Build();

            var dockerComposeBuilder = new DockerComposeBuilder(_context);
            dockerComposeBuilder.BuildForUpdater();

            _context.SaveConfiguration();
            Console.WriteLine(string.Empty);
        }

        private static void ParseParameters()
        {
            _context.Parameters = new Dictionary<string, string>();
            for(var i = 0; i < _context.Args.Length; i = i + 2)
            {
                if(!_context.Args[i].StartsWith("-"))
                {
                    continue;
                }

                _context.Parameters.Add(_context.Args[i].Substring(1), _context.Args[i + 1]);
            }
        }
    }
}

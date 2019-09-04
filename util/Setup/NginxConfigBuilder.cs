using System.Collections.Generic;
using System.IO;
using Org.BouncyCastle.Asn1.Pkcs;

namespace Bit.Setup
{
    public class NginxConfigBuilder
    {
        private const string ContentSecurityPolicy =
            "default-src 'self'; style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https://haveibeenpwned.com https://www.gravatar.com; " +
            "child-src 'self' https://*.duosecurity.com; frame-src 'self' https://*.duosecurity.com; " +
            "connect-src 'self' wss://{0} https://api.pwnedpasswords.com " +
            "https://twofactorauth.org; " +
            "object-src 'self' blob:;";

        private readonly Context _context;

        private string ConfFile
        {
            get { return $"{_context.DestDir}/nginx/default.conf"; }
        }

        public NginxConfigBuilder(Context context)
        {
            _context = context;
        }

        public void BuildForInstaller()
        {
            var model = new TemplateModel(_context);
            if(model.Ssl && !_context.Config.Ssl.ManagedLetsEncrypt)
            {
                var sslPath = _context.Install.SelfSignedCert ?
                    $"/etc/ssl/self/{model.Domain}" : $"/etc/ssl/{model.Domain}";
                _context.Config.Ssl.CertificatePath = model.CertificatePath =
                    Path.Join(sslPath, "certificate.crt");
                _context.Config.Ssl.KeyPath = model.KeyPath =
                    Path.Join(sslPath, "private.key");
                if(_context.Install.Trusted)
                {
                    _context.Config.Ssl.CaPath = model.CaPath =
                        Path.Join(sslPath, "ca.crt");
                }
                if(_context.Install.DiffieHellman)
                {
                    _context.Config.Ssl.DiffieHellmanPath = model.DiffieHellmanPath =
                        Path.Join(sslPath, "dhparam.pem");
                }
            }
            Build(model);
        }

        public void BuildForUpdater()
        {
            var model = new TemplateModel(_context);
            Build(model);
        }

        private void Build(TemplateModel model)
        {
            Directory.CreateDirectory($"{_context.DestDir}/nginx");
            Helpers.WriteLine(_context, "Building nginx config.");
            if(!_context.Config.Nginx.Enable)
            {
                Helpers.WriteLine(_context, "...skipped");
                return;
            }

            var template = Helpers.ReadTemplate("NginxConfig");
            using(var sw = File.CreateText(ConfFile))
            {
                sw.WriteLine(template(model));
            }
        }

        public class TemplateModel
        {
            public TemplateModel() { }

            public TemplateModel(Context context)
            {
                Ssl = context.Config.Ssl.Enable;
                Domain = context.Config.Domain;
                Url = context.Config.Url;
                RealIps = context.Config.Nginx.RealIps;

                if(Ssl)
                {
                    if(context.Config.Ssl.ManagedLetsEncrypt)
                    {
                        var sslPath = $"/etc/letsencrypt/live/{Domain}";
                        CertificatePath = CaPath = Path.Join(sslPath, "fullchain.pem");
                        KeyPath = Path.Join(sslPath, "privkey.pem");
                        DiffieHellmanPath = Path.Join(sslPath, "dhparam.pem");
                    }
                    else
                    {
                        CertificatePath = context.Config.Ssl.CertificatePath;
                        KeyPath = context.Config.Ssl.KeyPath;
                        CaPath = context.Config.Ssl.CaPath;
                        DiffieHellmanPath = context.Config.Ssl.DiffieHellmanPath;
                    }
                }

                if(!string.IsNullOrWhiteSpace(context.Config.Nginx.SslCiphersuites))
                {
                    SslCiphers = context.Config.Nginx.SslCiphersuites;
                }
                else
                {
                    SslCiphers = "ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:" +
                        "ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:ECDHE-ECDSA-AES128-GCM-SHA256:" +
                        "ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-SHA384:ECDHE-RSA-AES256-SHA384:" +
                        "ECDHE-ECDSA-AES128-SHA256:ECDHE-RSA-AES128-SHA256";
                }

                if(!string.IsNullOrWhiteSpace(context.Config.Nginx.SslVersions))
                {
                    SslProtocols = context.Config.Nginx.SslVersions;
                }
                else
                {
                    SslProtocols = "TLSv1.2";
                }
            }

            public bool Ssl { get; set; }
            public string Domain { get; set; }
            public string Url { get; set; }
            public string CertificatePath { get; set; }
            public string KeyPath { get; set; }
            public string CaPath { get; set; }
            public string DiffieHellmanPath { get; set; }
            public string SslCiphers { get; set; }
            public string SslProtocols { get; set; }
            public List<string> RealIps { get; set; }
        }
    }
}

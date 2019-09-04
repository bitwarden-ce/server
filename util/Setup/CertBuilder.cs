using System;
using System.IO;
using Bit.Core.Utilities;

namespace Bit.Setup
{
    public class CertBuilder
    {
        private readonly Context _context;

        public CertBuilder(Context context)
        {
            _context = context;
        }

        public void BuildForInstall()
        {
            if(_context.Stub)
            {
                _context.Config.Ssl.Enable = true;
                _context.Install.Trusted = true;
                _context.Install.SelfSignedCert = false;
                _context.Install.DiffieHellman = false;
                _context.Install.IdentityCertPassword = "IDENTITY_CERT_PASSWORD";
                return;
            }

            _context.Config.Ssl.Enable = _context.Config.Ssl.ManagedLetsEncrypt;

            if(!_context.Config.Ssl.Enable && _context.Install.Ssl == null)
            {
                _context.Config.Ssl.Enable = Helpers.ReadQuestion("Do you have a SSL certificate to use?");
                if(_context.Config.Ssl.Enable)
                {
                    Directory.CreateDirectory($"{_context.DestDir}/ssl/{_context.Install.Domain}/");
                    var message = "Make sure 'certificate.crt' and 'private.key' are provided in the \n" +
                                  "appropriate directory before running 'start' (see docs for info).";
                    Helpers.ShowBanner(_context, "NOTE", message);
                }
                else if(Helpers.ReadQuestion("Do you want to generate a self-signed SSL certificate?"))
                {
                    Directory.CreateDirectory($"{_context.DestDir}/ssl/self/{_context.Install.Domain}/");
                    Helpers.WriteLine(_context, "Generating self signed SSL certificate.");
                    _context.Config.Ssl.Enable = true;
                    _context.Install.Trusted = false;
                    _context.Install.SelfSignedCert = true;
                    Helpers.Exec("openssl req -x509 -newkey rsa:4096 -sha256 -nodes -days 365 " +
                        $"-keyout {_context.DestDir}/ssl/self/{_context.Install.Domain}/private.key " +
                        $"-out {_context.DestDir}/ssl/self/{_context.Install.Domain}/certificate.crt " +
                        $"-reqexts SAN -extensions SAN " +
                        $"-config <(cat /usr/lib/ssl/openssl.cnf <(printf '[SAN]\nsubjectAltName=DNS:{_context.Install.Domain}\nbasicConstraints=CA:true')) " +
                        $"-subj \"/C=US/ST=Florida/L=Jacksonville/O=8bit Solutions LLC/OU=Bitwarden/CN={_context.Install.Domain}\"");
                }
            }

            if(_context.Config.Ssl?.ManagedLetsEncrypt ?? false)
            {
                _context.Install.Trusted = true;
                _context.Install.DiffieHellman = true;
                Directory.CreateDirectory($"{_context.DestDir}/letsencrypt/live/{_context.Install.Domain}/");
                Helpers.Exec($"openssl dhparam -out " +
                    $"{_context.DestDir}/letsencrypt/live/{_context.Install.Domain}/dhparam.pem 2048");
            }
            else if(_context.Config.Ssl.Enable && !_context.Install.SelfSignedCert)
            {
                _context.Install.Trusted = Helpers.ReadQuestion("Is this a trusted SSL certificate " +
                    "(requires ca.crt, see docs)?");
            }

            Helpers.WriteLine(_context, "Generating key for IdentityServer.");
            _context.Install.IdentityCertPassword = CoreHelpers.SecureRandomString(32);
            Directory.CreateDirectory($"{_context.DestDir}/identity");
            Helpers.Exec("openssl req -x509 -newkey rsa:4096 -sha256 -nodes -keyout /tmp/bitwarden-identity.key " +
                "-out /tmp/bitwarden-identity.crt -subj \"/CN=Bitwarden IdentityServer\" -days 10950");
            Helpers.Exec($"openssl pkcs12 -export -out {_context.DestDir}/identity/identity.pfx -inkey /tmp/bitwarden-identity.key " +
                $"-in /tmp/bitwarden-identity.crt -certfile /tmp/bitwarden-identity.crt -passout pass:{_context.Install.IdentityCertPassword}");
            File.Delete("/tmp/bitwarden-identity.key");
            File.Delete("/tmp/bitwarden-identity.crt");

            Helpers.WriteLine(_context);

            if(!_context.Config.Ssl.Enable)
            {
                var message = "You are not using a SSL certificate. Bitwarden requires HTTPS to operate. \n" +
                              "You must front your installation with a HTTPS proxy or the web vault (and \n" +
                              "other Bitwarden apps) will not work properly.";
                Helpers.ShowBanner(_context, "WARNING", message, ConsoleColor.Yellow);
            }
            else if(_context.Config.Ssl.Enable && !_context.Install.Trusted)
            {
                var message = "You are using an untrusted SSL certificate. This certificate will not be \n" +
                              "trusted by Bitwarden client applications. You must add this certificate to \n" +
                              "the trusted store on each device or else you will receive errors when trying \n" +
                              "to connect to your installation.";
                Helpers.ShowBanner(_context, "WARNING", message, ConsoleColor.Yellow);
            }
        }
    }
}

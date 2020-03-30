using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Novell.Directory.Ldap;
using System.Net;

namespace ALMS.App.Models
{
    public class LdapAuthenticator
    {
        private string host;
        private int port;
        private string basestr;
        private string id;
        private Func<LdapEntry, (string, string)> getEmailAndName;

        public LdapAuthenticator(string host, int port, string basestr, string id, Func<LdapEntry, (string, string)> getEmailAndName)
        {
            this.host = host;
            this.port = port;
            this.basestr = basestr;
            this.id = id;
            this.getEmailAndName = getEmailAndName;
        }


        public (bool, string, string) Authenticate(string account, string password)
        {
            var lc = new LdapConnection();
            lc.UserDefinedServerCertValidationDelegate += (sender, certificate, chain, sslPolicyErrors) => true;  // Ignore cert. error
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
                lc.SecureSocketLayer = true;
                lc.Connect(this.host, this.port);
                var dn = string.Format("{0}={1},{2}", this.id, account, this.basestr);
                lc.Bind(LdapConnection.Ldap_V3, dn, password);
                var (name, email) = getEmailAndName(lc.Read(dn));
                return (true, name, email);
            }
            catch
            {
                return (false, null, null);
            }
            finally
            {
                lc.Disconnect();
            }
        }
    }
}

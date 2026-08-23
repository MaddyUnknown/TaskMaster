using TaskMaster.Library.Common.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Library.Common.Auth.NoAuth;
using TaskMaster.Library.Common.Auth.Oidc;

namespace TaskMaster.Library.Common.Auth
{
    internal static class AuthDependencyFactory
    {
        public static IAuthDependency GetAuthDependency(string authMode)
        {
            switch(authMode)
            {
                case EnumConstants.AuthModeEnum.None: return new NoAuthDependency();
                case EnumConstants.AuthModeEnum.Oidc: return new OidcAuthDependency();
                default: throw new ArgumentException(ErrorMessage.InvalidAuthMode(authMode));
            }
        }
    }
}

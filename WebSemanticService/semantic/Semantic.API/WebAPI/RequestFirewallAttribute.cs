using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace ZeroLevel.WebAPI
{
    public sealed class RequestFirewallAttribute : AuthorizeAttribute
    {
        private static readonly HashSet<string> _accounts = new HashSet<string>();
        private static readonly bool _enabled;
        static RequestFirewallAttribute()
        {
            _enabled = Configuration.Default.FirstOrDefault<bool>("ntlmEnabled", false);
            if (Configuration.Default.Contains("ntlmAccounts"))
            {
                foreach (var acc in Configuration.Default.First("ntlmAccounts").Split(' ', ',', ';'))
                {
                    if (false == string.IsNullOrWhiteSpace(acc))
                    {
                        _accounts.Add(acc.Trim());
                    }
                }
            }
        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            if (false == _enabled)
            {
                return true;
            }
            if (false == base.IsAuthorized(actionContext)) return false;
            try
            {
                var principal = actionContext.RequestContext.Principal as ClaimsPrincipal;
                var method = actionContext.Request.Method.Method;
                var resource = actionContext.Request.RequestUri.LocalPath;
                if (null == principal || false == principal.Identity.IsAuthenticated)
                {
                    return false;
                }
                var allow_access = false;
                var claim = principal.FindFirst(ClaimTypes.Name);
                string userName = string.Empty, domain = "PRIME";
                if (claim != null)
                {
                    var userNameDomain = claim.Value;
                    if (false == string.IsNullOrWhiteSpace(userNameDomain))
                    {
                        var parts = userNameDomain.Split('\\');
                        if (parts.Length == 2)
                        {
                            userName = parts[1].Trim();
                            domain = parts[0].Trim();
                        }
                        else
                        {
                            userName = userNameDomain;
                        }
                        allow_access = _accounts.Contains(userNameDomain) || _accounts.Contains(userName);
                    }
                }
                if (false == allow_access)
                {
                    Log.Warning(string.Format("Пользователю {0}\\{1} отказано в доступе к ресурсу {2} методом {3}", domain, userName, resource, method));
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Сбой в модуле авторизации", ex.ToString());
                return false;
            }
            return true;
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException("actionContext");
            }
            if (!IsAuthorized(actionContext))
            {
                HandleUnauthorizedRequest(actionContext);
            }
        }
    }
}

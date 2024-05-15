using CactusFrontEnd.Cosmos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Cosmos;
using System.Linq.Dynamic.Core.Tokenizer;

namespace CactusFrontEnd.Security
{
    public abstract class AuthorizedPage: ComponentBase
    {
        private string tokenString;
        protected SignedToken signedToken;

        public async Task Initialize(ProtectedLocalStorage protectedLocalStore, Action action)
        {
            //Action gets called when the user is unauthorized
            var result = await protectedLocalStore.GetAsync<string>("AuthorizationToken");
            tokenString = result.Value;
            if (tokenString is null)
            {
                action.Invoke();
                return;
            }
            signedToken = TokenVerification.GetTokenFromString(tokenString);

            TokenVerification.AuthorizeUser(tokenString, action);
        }
    }
}

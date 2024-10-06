using CactusFrontEnd.Components;
using CactusFrontEnd.Cosmos;
using Messenger;
using MessengerInterfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Cosmos;

namespace CactusFrontEnd.Security
{
    public abstract class AuthorizedPage: ComponentBase
    {
		[Inject]
		protected NavigationManager navigationManager { get; set; }
		[Inject]
		private EventService eventService { get; set; }
        [Inject]
        private ProtectedLocalStorage protectedLocalStore { get; set; }
        [Inject]
        protected IMessengerService messengerService { get; set; }
		private string tokenString;
        protected SignedToken<AuthorizationToken> signedToken;
        protected Account user;
        protected string errorText;
        protected bool alertShown = false;

		public async Task Initialize(Action action)
        {
            //Action gets called when the user is unauthorized
            ProtectedBrowserStorageResult<string> result;
			try
            {
				result = await protectedLocalStore.GetAsync<string>("AuthorizationToken");
            }
            catch (TaskCanceledException)
            {
				action.Invoke();
				return;
			}
            tokenString = result.Value;
            if (tokenString is null)
            {
                action.Invoke();
                return;
            }
            try
            {
                signedToken = TokenVerification.GetTokenFromString<AuthorizationToken>(tokenString);
                user = await messengerService.GetAccount(signedToken.Token.UserId);
            }
            catch (Exception e)
            {
                errorText = e.Message;
                alertShown = true;
                action.Invoke();
                return;
            }
            if (user.Locked)
            {
				try
				{
					await protectedLocalStore.DeleteAsync("AuthorizationToken");
					eventService.TokenHasChanged();
				}
				finally
				{
					navigationManager.NavigateTo("accountLocked");
				}
			}

            TokenVerification.AuthorizeUser(tokenString, action);
        }
    }
}

﻿using CactusFrontEnd.Cosmos;
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
        private string tokenString;
        protected SignedToken signedToken;
        protected Account user;

        public async Task Initialize(ProtectedLocalStorage protectedLocalStore, Action action, IMessengerService messengerService)
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
            signedToken = TokenVerification.GetTokenFromString(tokenString);
            try
            {
                user = await messengerService.GetAccount(signedToken.UserId);
            }
            catch (KeyNotFoundException)
            {
				action.Invoke();
				return;
			}

            TokenVerification.AuthorizeUser(tokenString, action);
        }
    }
}
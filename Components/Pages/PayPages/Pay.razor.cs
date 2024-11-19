using CactusFrontEnd.Security;

namespace CactusFrontEnd.Components.Pages.PayPages;

public partial class Pay : AuthorizedPage
{
	// ReSharper disable once AsyncVoidMethod
	protected override async void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			await Initialize(() => navigationManager.NavigateTo("logout?redirectUrl=pay"));
			await InvokeAsync(StateHasChanged);
		}
	}
}
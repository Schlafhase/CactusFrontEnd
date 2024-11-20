using CactusFrontEnd.Security;
using CactusPay;

namespace CactusFrontEnd.Components.Pages.PayPages;

public partial class Pay : AuthorizedPage
{
	private string paylink = "";
	private float amount = 0;
	
	// ReSharper disable once AsyncVoidMethod
	protected override async void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			await Initialize(() => navigationManager.NavigateTo("logout?redirectUrl=pay"));
			await InvokeAsync(StateHasChanged);
		}
	}

	private async Task generatePayLink()
	{
		paylink = Payment.GeneratePaymentLink(user.Id, Guid.NewGuid(), DateTime.Now, TimeSpan.FromMinutes(5), amount);
		await InvokeAsync(StateHasChanged);
	}
}
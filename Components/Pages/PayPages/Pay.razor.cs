using CactusFrontEnd.Security;
using CactusPay;
using Microsoft.AspNetCore.Components;

namespace CactusFrontEnd.Components.Pages.PayPages;

public partial class Pay : AuthorizedPage
{
	[Inject]
	private Payment payment { get; set; }
	private string payLink = "";
	private float amount = 0;
	
	// ReSharper disable once AsyncVoidMethod
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await Initialize(() => navigationManager.NavigateTo("logout?redirectUrl=pay"));
			await InvokeAsync(StateHasChanged);
		}
	}

	private async Task generatePayLink()
	{
		payLink = payment.GeneratePaymentLink(user.Id, Guid.NewGuid(), DateTime.Now, TimeSpan.FromMinutes(5), amount, user.Balance, "Payment", []);
		await InvokeAsync(StateHasChanged);
	}
}
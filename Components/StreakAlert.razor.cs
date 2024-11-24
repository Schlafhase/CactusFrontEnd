using Majorsoft.Blazor.Components.Notifications;
using Microsoft.AspNetCore.Components;

namespace CactusFrontEnd.Components;

public partial class StreakAlert : ComponentBase
{
	private bool _visible = false;
	private string _text = "Something went wrong";
	private NotificationTypes _type = NotificationTypes.Success;

	public void StreakIncreaseAlert(int newStreak)
	{
		_type = NotificationTypes.Success;
		_text = $"Login streak increased to {newStreak} days 🎉";
		_visible = true;
	}

	public void StreakLostAlert(int prevStreak)
	{
		_type = NotificationTypes.Warning;
		_text = $"Login streak lost at {prevStreak} days 😔";
		_visible = true;
	}
}
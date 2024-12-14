using Majorsoft.Blazor.Components.Notifications;
using Microsoft.AspNetCore.Components;

namespace CactusFrontEnd.Components;

public partial class StreakAlert : ComponentBase
{
	private string _text = "Something went wrong";
	private NotificationTypes _type = NotificationTypes.Success;
	private bool _visible = false;

	public void ShowStreakIncreaseAlert(int newStreak)
	{
		_type = NotificationTypes.Success;
		_text = $"Login streak increased to {newStreak} days 🎉";
		_visible = true;
	}

	public void ShowStreakLostAlert(int prevStreak)
	{
		_type = NotificationTypes.Warning;
		_text = $"Login streak lost at {prevStreak} days 😔";
		_visible = true;
	}
}
namespace CactusFrontEnd.Components
{
	public class EventService
	{
		public event Action OnTokenChange;

		public void TokenHasChanged() => OnTokenChange.Invoke();
	}
}

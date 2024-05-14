namespace CactusFrontEnd.Security
{
	public class AuthorizationToken
	{
		public Guid UserId { get; private set; }
		public DateTime IssuingDate { get; private set; }

		public AuthorizationToken(Guid UserId, DateTime IssuingDate)
		{
			this.UserId = UserId;
			this.IssuingDate = IssuingDate;
		}
	}
}

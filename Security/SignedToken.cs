namespace CactusFrontEnd.Security
{
	public class SignedToken: AuthorizationToken
	{
		public byte[] Signature { get; }
		public SignedToken(Guid userId, DateTime issuingDate, byte[] signature) : base(userId, issuingDate)
		{
			this.Signature = signature;
		}
	}
}

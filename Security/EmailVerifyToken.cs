namespace CactusFrontEnd.Security
{
	public class EmailVerifyToken : IToken
	{
		public string Email { get; private set; }
		public Guid UserId { get; private set; }
		public DateTime IssuingDate { get; private set; }

        public EmailVerifyToken(string email, Guid userId, DateTime issuingDate)
        {
			this.Email = email;
			this.UserId = userId;
			this.IssuingDate = issuingDate;
        }
    }
}
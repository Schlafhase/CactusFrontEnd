namespace CactusFrontEnd.Security
{
	public class SignedToken<T> : ISignedToken where T : IToken
	{
		public byte[] Signature { get; }
		public T Token { get; }
		public SignedToken(T Token, byte[] signature)
		{
			this.Token = Token;
			this.Signature = signature;
		}
	}
}

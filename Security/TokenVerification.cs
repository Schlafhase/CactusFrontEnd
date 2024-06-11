using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace CactusFrontEnd.Security
{
    public static class TokenVerification
    {
        private static byte[] privateKey;
        public static byte[] PublicKey;

        public static void Initialize()
        {
            StreamReader sr = new("./privateKey.privkey");
            privateKey = Convert.FromBase64String(sr.ReadLine());
            sr.Close();
            
            StreamReader sr2 = new("./publicKey.pubkey");
            PublicKey = Convert.FromBase64String(sr2.ReadLine());
            sr2.Close();
        }

        public static (byte[], byte[]) CreateKeyPair()
        {

            RSA rsa = RSA.Create();
            RSAParameters rSAParameters = new RSAParameters();
            byte[] publicKey = rsa.ExportRSAPublicKey();
            byte[] privateKey = rsa.ExportRSAPrivateKey();
            return (publicKey, privateKey);
        }

        public static string GetTokenString(AuthorizationToken token)
        {
            string tokenAsString = JsonConvert.SerializeObject(token);
            byte[] signature = TokenVerification.signData(tokenAsString);
            SignedToken signedToken = new(token.UserId, token.IssuingDate, signature);
            string signedTokenAsString = JsonConvert.SerializeObject(signedToken);
            string signedTokenBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(signedTokenAsString));
            return signedTokenBase64;
        }

        public static bool ValidateToken(string signedTokenBase64)
        {
            string signedTokenAsString = Encoding.UTF8.GetString(Convert.FromBase64String(signedTokenBase64));
            SignedToken signedToken = JsonConvert.DeserializeObject<SignedToken>(signedTokenAsString);
            AuthorizationToken token = new(signedToken.UserId, signedToken.IssuingDate);
            string tokenAsString = JsonConvert.SerializeObject(token);
            return verifyData(tokenAsString, signedToken.Signature, PublicKey);
        }

        public static void AuthorizeUser(string tokenString, Action action)
        {
            //action will be invoked when the user is not authorized
            if(!ValidateToken(tokenString))
            {
                action.Invoke();
            }
        }

        public static SignedToken GetTokenFromString(string base64EncodedToken)
        {
            SignedToken signedToken = JsonConvert.DeserializeObject<SignedToken>(Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedToken)));
            return signedToken;
		}

        private static byte[] signData(string data)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportRSAPrivateKey(privateKey, out _);
				RSAParameters rsaParams = rsa.ExportParameters(true);
				byte[] bytes = rsa.SignData(Encoding.UTF8.GetBytes(data), SHA256.Create());
                return bytes;
            }
        }

        private static bool verifyData(string data, byte[] signature, byte[] publicKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportRSAPublicKey(publicKey, out _);
				RSAParameters rsaParams = rsa.ExportParameters(false);
                return rsa.VerifyData(Encoding.UTF8.GetBytes(data), SHA256.Create(), signature);
            }
        }
    }
}

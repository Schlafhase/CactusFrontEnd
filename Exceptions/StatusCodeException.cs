using System.Net;

namespace CactusFrontEnd.Exceptions
{
	public class StatusCodeException: Exception
	{
        public StatusCodeException(HttpStatusCode statusCode)
        {
            this.StatusCode = statusCode;
        }

		public HttpStatusCode StatusCode { get; }
	}
}

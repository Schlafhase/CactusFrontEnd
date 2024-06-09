namespace CactusFrontEnd.Utils
{
	public class AsyncLocker
	{
		private class Releaser : IDisposable
		{
			private readonly SemaphoreSlim semaphore;

			public Releaser(SemaphoreSlim sem)
            {
				this.semaphore = sem;
            }
            public void Dispose()
			{
				semaphore.Release();
			}
		}

		private readonly SemaphoreSlim semaphore;

		public AsyncLocker()
		{
			this.semaphore = new SemaphoreSlim(1);
		}

		public async Task<IDisposable> Enter()
		{
			await semaphore.WaitAsync();
			return new Releaser(semaphore);
		}
	}
}

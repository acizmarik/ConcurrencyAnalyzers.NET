using System;
using System.Collections.Generic;
using System.Threading;

namespace ConcurrencyAnalyzers.NET.Runtime
{
	internal class ThreadInfo
	{
		public bool Initialized { get; private set; }
		public WeakReference<Thread> Thread { get; private set; } = null!;
		public HashSet<LockInfo> Locks { get; } = [];
		public LockInfo? WaitingForLock { get; private set; }

		public void Initialize(Thread thread)
		{
			if (Initialized)
				throw new InvalidOperationException("Thread object has already been initialized.");
			Thread = new(thread);
			Initialized = true;
		}

		public void SetWaitingForLock(LockInfo lockInfo)
		{
			EnsureInitialized();
			if (WaitingForLock != null)
				throw new InvalidOperationException($"Thread already waiting for lock {WaitingForLock.LockObj}.");

			WaitingForLock = lockInfo;
		}

		public void ClearWaitingForLock()
		{
			EnsureInitialized();
			if (WaitingForLock == null)
				throw new InvalidOperationException($"Thread does not wait for any lock.");

			WaitingForLock = null;
		}

		private void EnsureInitialized()
		{
			if (!Initialized)
				throw new InvalidOperationException("Thread object has not been initialized.");
		}
	}
}

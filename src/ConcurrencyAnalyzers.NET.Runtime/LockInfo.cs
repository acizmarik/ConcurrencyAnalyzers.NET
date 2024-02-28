using System;
using System.Diagnostics;
using System.Threading;

namespace ConcurrencyAnalyzers.NET.Runtime
{
	internal class LockInfo
	{
		internal enum LockState { Unlocked, Locked, Unlocking }

		public int ObjectId { get; private set; }
		public LockState State { get; private set; }
		public bool Initialized { get; private set; }
		public WeakReference<object> LockObj { get; private set; } = null!;
		public int Reentrancy { get; private set; }
		public WeakReference<Thread>? Owner { get; private set; }
		private readonly object _syncObj = new();
		private static int _nextUnassignedObjectId = 0;

		public void Initialize(object obj)
		{
			if (Initialized)
				throw new InvalidOperationException("Lock object has already been initialized.");
			LockObj = new WeakReference<object>(obj);
			ObjectId = Interlocked.Increment(ref _nextUnassignedObjectId);
			Initialized = true;
			State = LockState.Unlocked;
		}

		public void Acquire(Thread thread)
		{
			EnsureInitialized();
			lock (_syncObj)
			{
				if (Owner == null)
				{
					// New thread takes lock
					Owner = new(thread);
					Reentrancy = 1;
					State = LockState.Locked;
				}
				else if (Owner.TryGetTarget(out var ownerThread) && thread.ManagedThreadId != ownerThread.ManagedThreadId)
				{
					if (State == LockState.Unlocking && Reentrancy == 1)
					{
						// Lock is being released, we just did not get the confirmation yet
						Owner = new(thread);
						State = LockState.Locked;
					}
					else
					{
						throw new InvalidOperationException($"Thread {thread} can not acquire {this} because it is locked by thread {Owner}");
					}
				}
				else
				{
					// Reentrant lock
					Reentrancy++;
				}
			}
		}

		public void Release(Thread thread)
		{
			EnsureInitialized();
			lock (_syncObj)
			{
				if (Owner == null || !Owner.TryGetTarget(out var ownerThread) || ownerThread.ManagedThreadId != thread.ManagedThreadId)
				{
					// It has hijacked before he received the confirmation
					return;
				}

				if (--Reentrancy == 0)
				{
					Owner = null;
					State = LockState.Unlocked;
				}
			}
		}

		private void EnsureInitialized()
		{
			if (!Initialized)
				throw new InvalidOperationException("Lock object has not been initialized.");
		}
	}
}

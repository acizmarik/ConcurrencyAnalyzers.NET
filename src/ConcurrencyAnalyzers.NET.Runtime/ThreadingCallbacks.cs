using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ConcurrencyAnalyzers.NET.Runtime
{
	public static class ThreadingCallbacks
	{
		private static readonly TimeSpan _delayBetweenCollections;
		private static ConditionalWeakTable<Thread, ThreadInfo> _threads;
		private static ConditionalWeakTable<object, LockInfo> _locks;
		private static Dictionary<int, ThreadInfo> _threadInfos;
		private static Dictionary<int, LockInfo> _locksInfos;
		private static readonly Thread _collectorThread;
		private static readonly object _syncObj;

		static ThreadingCallbacks()
		{
			_threads = new();
			_locks = new();
			_threadInfos = [];
			_locksInfos = [];
			_syncObj = new object();

			_delayBetweenCollections = TimeSpan.FromSeconds(value: 10);
			_collectorThread = new Thread(CollectorThreadLoop)
			{
				Name = "ConcurrencyAnalyzers.NET.Collector",
				IsBackground = true
			};

			_collectorThread.Start();
		}

		private static void CollectorThreadLoop()
		{
			var toDeleteThreads = new Queue<int>();
			var toDeleteLocks = new Queue<int>();

			for (;;)
			{
				Thread.Sleep(_delayBetweenCollections);

				lock (_syncObj)
				{
					foreach (var kv in _threadInfos.Where(kv => !_threadInfos.TryGetValue(kv.Key, out _)))
						toDeleteThreads.Enqueue(kv.Key);

					foreach (var kv in _locksInfos.Where(kv => !_locksInfos.TryGetValue(kv.Key, out _)))
						toDeleteLocks.Enqueue(kv.Key);

					while (toDeleteThreads.Count > 0)
						_threadInfos.Remove(toDeleteThreads.Dequeue());

					while (toDeleteLocks.Count > 0)
						_locksInfos.Remove(toDeleteLocks.Dequeue());
				}
			}
		}

		public static void Reset()
		{
			lock (_syncObj)
			{
				_threads = new();
				_locks = new();
				_threadInfos = [];
				_locksInfos = [];
			}
		}

		public static class Monitor
		{
			public static void PreEnter(object lockObj)
			{
				lock (_syncObj)
				{
					var thread = Thread.CurrentThread;
					var lockInfo = _locks.GetOrCreateValue(lockObj);
					var threadInfo = _threads.GetOrCreateValue(thread);

					if (!lockInfo.Initialized)
					{
						lockInfo.Initialize(lockObj);
						_locksInfos.Add(lockInfo.ObjectId, lockInfo);
					}

					if (!threadInfo.Initialized)
					{
						threadInfo.Initialize(Thread.CurrentThread);
						_threadInfos.Add(Environment.CurrentManagedThreadId, threadInfo);
					}

					threadInfo.SetWaitingForLock(lockInfo);
					EnsureNoDeadlocks(thread, lockInfo);
				}
			}

			public static void PostEnter(object lockObj)
			{
				lock (_syncObj)
				{
					var thread = Thread.CurrentThread;
					EnsureSuccessOrThrow(_locks.TryGetValue(lockObj, out var lockInfo));
					EnsureSuccessOrThrow(_threads.TryGetValue(thread, out var threadInfo));
					lockInfo.Acquire(thread);
					threadInfo.Locks.Add(lockInfo);
					threadInfo.ClearWaitingForLock();
				}
			}

			public static void PreEnter(object lockObj, ref bool _)
			{
				lock (_syncObj)
				{
					var thread = Thread.CurrentThread;
					var lockInfo = _locks.GetOrCreateValue(lockObj);
					var threadInfo = _threads.GetOrCreateValue(thread);

					if (!lockInfo.Initialized)
					{
						lockInfo.Initialize(lockObj);
						_locksInfos.Add(lockInfo.ObjectId, lockInfo);
					}

					if (!threadInfo.Initialized)
					{
						threadInfo.Initialize(Thread.CurrentThread);
						_threadInfos.Add(Environment.CurrentManagedThreadId, threadInfo);
					}

					threadInfo.SetWaitingForLock(lockInfo);
					EnsureNoDeadlocks(thread, lockInfo);
				}
			}

			public static void PostEnter(object lockObj, ref bool taken)
			{
				lock (_syncObj)
				{
					var thread = Thread.CurrentThread;
					EnsureSuccessOrThrow(_locks.TryGetValue(lockObj, out var lockInfo));
					EnsureSuccessOrThrow(_threads.TryGetValue(thread, out var threadInfo));
					if (taken)
					{
						lockInfo.Acquire(thread);
						threadInfo.Locks.Add(lockInfo);
					}

					threadInfo.ClearWaitingForLock();
				}
			}

			public static void PreExit(object lockObj)
			{
				lock (_syncObj)
				{
					var thread = Thread.CurrentThread;
					EnsureSuccessOrThrow(_locks.TryGetValue(lockObj, out var lockInfo));
					EnsureSuccessOrThrow(_threads.TryGetValue(thread, out var threadInfo));
					lockInfo.Release(thread);
					threadInfo.Locks.Remove(lockInfo);
				}
			}

			public static void PostExit(object lockObj)
			{
				lock (_syncObj)
				{
					var thread = Thread.CurrentThread;
					EnsureSuccessOrThrow(_locks.TryGetValue(lockObj, out var lockInfo));
					EnsureSuccessOrThrow(_threads.TryGetValue(thread, out var threadInfo));
					lockInfo.Release(thread);
					threadInfo.Locks.Remove(lockInfo);
				}
			}

			private static void EnsureNoDeadlocks(Thread thread, LockInfo lockObj)
			{
				var stack = new Stack<int>();
				var visited = new HashSet<int>() { thread.ManagedThreadId };
				stack.Push(thread.ManagedThreadId);
				while (stack.Count > 0)
				{
					var threadInfo = _threadInfos[stack.Peek()];
					if (threadInfo.WaitingForLock is not { } lockInfo ||
						lockInfo.Owner == null ||
						!lockInfo.Owner.TryGetTarget(out var blockedOn))
					{
						stack.Pop();
						continue;
					}

					if (stack.Contains(blockedOn.ManagedThreadId))
					{
						// Deadlock
						var threads = stack.Select(tid =>
						{
							_threadInfos.TryGetValue(tid, out var threadInfo);
							threadInfo.Thread.TryGetTarget(out var thread);
							return thread;
						});

						throw new DeadlockException(lockObj, threads.ToArray());
					}

					if (visited.Add(blockedOn.ManagedThreadId))
					{
						// Let's check that thread that the current thread is blocked on
						stack.Push(blockedOn.ManagedThreadId);
					}
					else
					{
						// The thread that we are blocked on has been already checked
						stack.Pop();
					}
				}
			}

			private static void EnsureSuccessOrThrow(
				bool expr,
				[CallerMemberName] string? memberName = null,
				[CallerArgumentExpression(nameof(expr))] string? memberExpression = null)
			{
				if (expr)
					return;

				throw new Exception($"Internal error. Expected \"{memberExpression}\" called by \"{memberName}\" to be evaluated to true.");
			}
		}
	}
}

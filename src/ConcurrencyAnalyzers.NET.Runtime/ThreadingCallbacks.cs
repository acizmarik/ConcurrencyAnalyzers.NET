using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ConcurrencyAnalyzers.NET.Runtime
{
	public static class ThreadingCallbacks
	{
		private static ConditionalWeakTable<Thread, ThreadInfo> _threads;
		private static ConditionalWeakTable<object, LockInfo> _locks;
		private static readonly object _syncObj;

		static ThreadingCallbacks()
		{
			_threads = new();
			_locks = new();
			_syncObj = new object();
		}

		public static void Reset()
		{
			lock (_syncObj)
			{
				_threads = new();
				_locks = new();
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
						lockInfo.Initialize(lockObj);

					if (!threadInfo.Initialized)
						threadInfo.Initialize(Thread.CurrentThread);

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
						lockInfo.Initialize(lockObj);

					if (!threadInfo.Initialized)
						threadInfo.Initialize(Thread.CurrentThread);

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
				var stack = new Stack<Thread>();
				var visited = new HashSet<Thread>() { thread };
				stack.Push(thread);

				while (stack.Count > 0)
				{
					thread = stack.Peek();
					EnsureSuccessOrThrow(_threads.TryGetValue(thread, out var threadInfo));
					if (threadInfo.WaitingForLock is not { } lockInfo ||
						lockInfo.Owner == null ||
						!lockInfo.Owner.TryGetTarget(out var blockedOn))
					{
						stack.Pop();
						continue;
					}

					if (stack.Contains(blockedOn))
					{
						// Deadlock
						var threads = stack.Select(t =>
						{
							EnsureSuccessOrThrow(_threads.TryGetValue(t, out var threadInfo));
							threadInfo.Thread.TryGetTarget(out var thread);
							return thread;
						});

						throw new DeadlockException(lockObj, threads.ToArray());
					}

					if (visited.Add(blockedOn))
					{
						// Let's check that thread that the current thread is blocked on
						stack.Push(blockedOn);
					}
					else
					{
						// The thread that we are blocked on has been already checked
						stack.Pop();
					}
				}

				stack.Clear();
				visited.Clear();
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

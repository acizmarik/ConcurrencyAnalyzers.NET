using System;
using System.Linq;
using System.Threading;

namespace ConcurrencyAnalyzers.NET.Runtime
{
	public class DeadlockException : Exception
	{
		public object LockObj { get; }
		public Thread[] Threads { get; }

		public DeadlockException(object lockObj, Thread[] threads)
			: base($"Detected deadlock on object: {lockObj}." +
				   $"Participating threads: {string.Join(", ", threads.Select(t => $"{{TID={t.ManagedThreadId},Name={t.Name}}}"))}.")
		{
			LockObj = lockObj;
			Threads = threads;
		}
	}
}

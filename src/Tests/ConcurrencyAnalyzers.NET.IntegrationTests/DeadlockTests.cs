using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ConcurrencyAnalyzers.NET.Runtime;
using Xunit;

namespace ConcurrencyAnalyzers.NET.IntegrationTests
{
	public class DeadlockTests
	{
		[Fact]
		public async Task SimpleDeadlock()
		{
			var ct = new CancellationTokenSource();
			var obj1 = new object();
			var obj2 = new object();

			var task1 = Task.Run(() =>
			{
				while (!ct.IsCancellationRequested)
				{
					lock (obj1)
					{
						lock (obj2)
						{

						}
					}
				}
			});

			var task2 = Task.Run(() =>
			{
				while (!ct.IsCancellationRequested)
				{
					lock (obj2)
					{
						lock (obj1)
						{

						}
					}
				}
			});

			var resultTask = await Task.WhenAny(task1, task2);
			ct.Cancel();
			await Assert.ThrowsAsync<DeadlockException>(() => resultTask);
		}
	}
}

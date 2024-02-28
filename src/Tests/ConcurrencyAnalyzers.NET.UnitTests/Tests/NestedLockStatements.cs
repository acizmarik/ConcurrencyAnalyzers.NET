using System;

namespace ConcurrencyAnalyzers.NET.UnitTests.Tests
{
	class NestedLockStatements
	{
		public void Test()
		{
			var obj1 = new object();
			var obj2 = new object();
			lock (obj1)
			{
				lock (obj2)
				{
					Console.WriteLine("Hello");
				}
			}
		}
	}
}

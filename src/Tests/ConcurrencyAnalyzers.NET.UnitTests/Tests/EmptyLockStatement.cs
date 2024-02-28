namespace ConcurrencyAnalyzers.NET.UnitTests.Tests
{
	class EmptyLockStatement
	{
		public static void Test()
		{
			var obj = new object();
			lock (obj)
			{

			}
		}
	}
}

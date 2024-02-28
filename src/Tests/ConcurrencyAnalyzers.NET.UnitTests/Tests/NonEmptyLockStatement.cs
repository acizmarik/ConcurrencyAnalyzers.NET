namespace ConcurrencyAnalyzers.NET.UnitTests.Tests
{
	class NonEmptyLockStatement
	{
		public static void Test()
		{
			var obj = new object();
			lock (obj)
			{
				System.Console.WriteLine("Hello");
			}
		}
	}
}

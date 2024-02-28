namespace ConcurrencyAnalyzers.NET.UnitTests.Tests
{
	class LockStatementWithoutBlockStatement
	{
		public void Test()
		{
			var obj = new object();
			lock (obj)
				System.Console.WriteLine("Hello");
		}
	}
}

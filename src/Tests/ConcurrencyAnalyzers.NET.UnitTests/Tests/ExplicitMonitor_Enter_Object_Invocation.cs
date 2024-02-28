namespace ConcurrencyAnalyzers.NET.UnitTests.Tests
{
	class ExplicitMonitor_Enter_Object_Invocation
	{
		public static void Test()
		{
			var obj = new object();
			System.Threading.Monitor.Enter(obj);
		}
	}
}

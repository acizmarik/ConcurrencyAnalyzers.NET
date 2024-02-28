namespace ConcurrencyAnalyzers.NET.UnitTests.Tests
{
	class ExplicitMonitor_Enter_Object_Boolean__Invocation
	{
		public static void Test()
		{
			var obj = new object();
			var taken = false;
			System.Threading.Monitor.Enter(obj, ref taken);
		}
	}
}

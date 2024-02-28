using System.IO;
using System.Threading.Tasks;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace ConcurrencyAnalyzers.NET.UnitTests
{
	public class TransformationTestDefinitions
	{
		[Theory]
		[InlineData("ExplicitMonitor_Enter_Object_Invocation.cs")]
		[InlineData("ExplicitMonitor_Enter_Object_Boolean&_Invocation.cs")]
		[InlineData("EmptyLockStatement.cs")]
		[InlineData("NonEmptyLockStatement.cs")]
		[InlineData("NestedLockStatements.cs")]
		[InlineData("LockStatementWithoutBlockStatement.cs")]
		public Task TransformationTest(string filename)
		{
			var path = Path.Combine("..", "..", "..", "obj", "Debug", "net8.0", "metalama", "Tests", filename);
			var transformedSource = File.ReadAllText(path);

			var settings = new VerifySettings();
			settings.UseDirectory(Path.Combine("..", "..", "..", "..", "Snapshots"));
			settings.UseFileName(filename);
			return Verifier.Verify(transformedSource, settings);
		}
	}
}

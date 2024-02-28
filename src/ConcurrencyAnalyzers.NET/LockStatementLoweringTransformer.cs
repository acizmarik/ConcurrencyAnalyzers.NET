using Metalama.Compiler;
using Microsoft.CodeAnalysis;
using ConcurrencyAnalyzers.NET.SyntaxRewriters;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ConcurrencyAnalyzers.NET
{
	[Transformer]
	public class LockStatementLoweringTransformer : ISourceTransformer
	{
		public void Execute(TransformerContext context)
		{
			foreach (var syntaxTree in context.Compilation.SyntaxTrees)
			{
				var newTree = LowerLockStatements(syntaxTree);
				context.ReplaceSyntaxTree(syntaxTree, newTree);
			}
		}

		private static SyntaxTree LowerLockStatements(SyntaxTree syntaxTree)
		{
			var rewriter = new LockStatementLoweringRewriter();
			var newRoot = rewriter.Visit(syntaxTree.GetRoot());
			return SyntaxTree(newRoot, syntaxTree.Options, syntaxTree.FilePath, syntaxTree.Encoding);
		}
	}
}

using Metalama.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ConcurrencyAnalyzers.NET.SyntaxRewriters;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ConcurrencyAnalyzers.NET
{
	[Transformer]
	public class MonitorInvocationsTransformer : ISourceTransformer
	{
		public void Execute(TransformerContext context)
		{
			foreach (var syntaxTree in context.Compilation.SyntaxTrees)
			{
				var newTree = FormatTree(RewriteMonitorInvocations(context, syntaxTree));
				context.ReplaceSyntaxTree(syntaxTree, newTree);
			}
		}

		private static SyntaxTree RewriteMonitorInvocations(TransformerContext context, SyntaxTree syntaxTree)
		{
			var rewriter = new MonitorInvocationsRewriter(context.Compilation.GetSemanticModel(syntaxTree), default);
			var newRoot = rewriter.Visit(syntaxTree.GetRoot());
			return SyntaxTree(newRoot, syntaxTree.Options, syntaxTree.FilePath, syntaxTree.Encoding);
		}

		private static SyntaxTree FormatTree(SyntaxTree syntaxTree)
		{
			return SyntaxTree(syntaxTree.GetRoot().NormalizeWhitespace(), syntaxTree.Options, syntaxTree.FilePath, syntaxTree.Encoding);
		}
	}
}

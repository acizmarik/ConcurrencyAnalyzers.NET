using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ConcurrencyAnalyzers.NET.SyntaxRewriters
{
	internal class MonitorInvocationsRewriter : CSharpSyntaxRewriter
	{
		private readonly SemanticModel _semanticModel;
		private readonly INamedTypeSymbol? _monitorSymbol;
		private readonly INamedTypeSymbol? _threadingCallbacksSymbol;
		private readonly CancellationToken _cancellationToken;

		public MonitorInvocationsRewriter(SemanticModel semanticModel, CancellationToken cancellationToken)
		{
			_semanticModel = semanticModel;
			_cancellationToken = cancellationToken;
			_monitorSymbol = _semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Monitor");
			_threadingCallbacksSymbol = _semanticModel.Compilation.GetTypeByMetadataName("ConcurrencyAnalyzers.NET.Runtime.ThreadingCallbacks");
		}

		public override SyntaxNode? VisitBlock(BlockSyntax node)
		{
			_cancellationToken.ThrowIfCancellationRequested();
			return node.WithStatements(List(ReplaceStatements(node.Statements)));
		}

		private IEnumerable<StatementSyntax> ReplaceStatements(SyntaxList<StatementSyntax> statements)
		{
			foreach (var statement in statements)
			{
				if (statement is ExpressionStatementSyntax expressionStatement &&
					expressionStatement.Expression is InvocationExpressionSyntax invocationExpression &&
					IsMonitorInvocation(invocationExpression, out var name))
				{
					foreach (var innerStatement in HandleExplicitMonitorInvocation(expressionStatement, invocationExpression, name!))
						yield return innerStatement;
				}

				else
				{
					yield return (Visit(statement) as StatementSyntax)!;
				}
			}
		}

		private static IEnumerable<StatementSyntax> HandleExplicitMonitorInvocation(
			ExpressionStatementSyntax statement,
			InvocationExpressionSyntax invocation,
			string methodName)
		{
			yield return ExpressionStatement(CreateMonitorEventCallback(IdentifierName($"Pre{methodName}"), invocation.ArgumentList));
			yield return statement;
			yield return ExpressionStatement(CreateMonitorEventCallback(IdentifierName($"Post{methodName}"), invocation.ArgumentList));
		}

		private bool IsMonitorInvocation(InvocationExpressionSyntax node, out string? name)
		{
			if (_monitorSymbol == null || _threadingCallbacksSymbol == null)
			{
				// Could not resolve symbols
				// It is probably not referenced by this compilation
				name = null;
				return false;
			}

			var symbol = _semanticModel.GetSymbolInfo(node).Symbol;
			if (symbol is not IMethodSymbol methodSymbol)
			{
				// Could not determine symbol for invocation expression
				// It is probably unfinished / contains compilation errors
				name = null;
				return false;
			}

			name = methodSymbol.Name;
			if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _monitorSymbol))
			{
				// Invocation is not performed on the System.Threading.Monitor
				return false;
			}

			return true;
		}

		private static InvocationExpressionSyntax CreateMonitorEventCallback(IdentifierNameSyntax methodName, ArgumentListSyntax argumentsListSyntax)
		{
			return InvocationExpression(
					CreateSimpleMemberAccessSyntax(
						CreateSimpleMemberAccessSyntax(
							CreateSimpleMemberAccessSyntax(
								CreateSimpleMemberAccessSyntax(
									CreateSimpleMemberAccessSyntax(
										IdentifierName("ConcurrencyAnalyzers"),
										IdentifierName("NET")),
									IdentifierName("Runtime")),
								IdentifierName("ThreadingCallbacks")),
							IdentifierName("Monitor")),
						methodName),
					argumentsListSyntax);
		}

		private static MemberAccessExpressionSyntax CreateSimpleMemberAccessSyntax(ExpressionSyntax expression, SimpleNameSyntax simpleName)
			=> MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, simpleName);
	}
}

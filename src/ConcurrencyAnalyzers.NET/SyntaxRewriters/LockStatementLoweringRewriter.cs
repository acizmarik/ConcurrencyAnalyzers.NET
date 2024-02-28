using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ConcurrencyAnalyzers.NET.SyntaxRewriters
{
	internal class LockStatementLoweringRewriter : CSharpSyntaxRewriter
	{
		private readonly CancellationToken _cancellationToken;
		private int _variableIndex;

		public override SyntaxNode? VisitBlock(BlockSyntax node)
		{
			_cancellationToken.ThrowIfCancellationRequested();
			return node.WithStatements(List(ReplaceStatements(node.Statements)));
		}

		private IEnumerable<StatementSyntax> ReplaceStatements(SyntaxList<StatementSyntax> statements)
		{
			foreach (var statement in statements)
			{
				if (statement is LockStatementSyntax lockStatement)
				{
					foreach (var innerStatement in HandleLockStatement(lockStatement))
						yield return innerStatement;
				}

				else
				{
					yield return (Visit(statement) as StatementSyntax)!;
				}
			}
		}

		private IEnumerable<StatementSyntax> HandleLockStatement(LockStatementSyntax lockStatement)
		{
			// First visit <STATEMENT>
			var innerStatement = Visit(lockStatement.Statement) as StatementSyntax;

			// var lockTaken_<GUID> = false;
			var lockTakenVariableName = $"__lockTaken__{nameof(MonitorInvocationsRewriter)}__{Interlocked.Increment(ref _variableIndex)}";
			yield return LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)))
				.WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(lockTakenVariableName))
					.WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.FalseLiteralExpression))))));

			// try { Monitor.Enter(<EXPR>, ref lockTaken_<GUID>); <STATEMENT>; } finally { if (lockTaken_<GUID>) Monitor.Exit(<EXPR>); }
			yield return
				TryStatement()
					.WithBlock(Block(
						// Monitor.Enter(<EXPR>, ref lockTaken_<GUID>);
						ExpressionStatement(
							InvocationExpression(
								MemberAccessExpression(
									SyntaxKind.SimpleMemberAccessExpression,
									MemberAccessExpression(
										SyntaxKind.SimpleMemberAccessExpression,
										MemberAccessExpression(
											SyntaxKind.SimpleMemberAccessExpression,
											IdentifierName("System"),
											IdentifierName("Threading")),
										IdentifierName("Monitor")),
									IdentifierName("Enter")))
							.WithArgumentList(ArgumentList(
									SeparatedList<ArgumentSyntax>(
										new SyntaxNodeOrToken[]
										{
											Argument(lockStatement.Expression),
											Token(SyntaxKind.CommaToken),
											Argument(IdentifierName(lockTakenVariableName))
												.WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword))
										})))),
						// <STATEMENT>
						innerStatement!))
				.WithFinally(FinallyClause(Block(
					SingletonList<StatementSyntax>(
						// if (lockTaken_<GUID>)
						IfStatement(
							IdentifierName(lockTakenVariableName),
							Block(
								SingletonList<StatementSyntax>(
									// Monitor.Exit(<EXPR>);
									ExpressionStatement(
										InvocationExpression(
											MemberAccessExpression(
												SyntaxKind.SimpleMemberAccessExpression,
												MemberAccessExpression(
													SyntaxKind.SimpleMemberAccessExpression,
													MemberAccessExpression(
														SyntaxKind.SimpleMemberAccessExpression,
														IdentifierName("System"),
														IdentifierName("Threading")),
													IdentifierName("Monitor")),
												IdentifierName("Exit")))
										.WithArgumentList(ArgumentList(
											SeparatedList<ArgumentSyntax>(
												new SyntaxNodeOrToken[] { Argument(lockStatement.Expression) })))))))))));
		}
	}
}

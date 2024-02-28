
using Metalama.Compiler;

[assembly: TransformerOrder("ConcurrencyAnalyzers.NET.LockStatementLoweringTransformer", "ConcurrencyAnalyzers.NET.MonitorInvocationsTransformer")]

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace WinUIEx.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class WinUIExAnalyzersAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "WinUIExAnalyzers";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private const string Category = "Interoperability";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "Support Guard", "API call must be guarded by IsSupported check", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "API call must be guarded by IsSupported check");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }
        private static ISymbol? GetOperationSymbol(IOperation operation)
            => operation switch
            {
                IInvocationOperation iOperation => iOperation.TargetMethod,
                IObjectCreationOperation cOperation => cOperation.Constructor,
                IFieldReferenceOperation fOperation => IsWithinConditionalOperation(fOperation) ? null : fOperation.Field,
                IMemberReferenceOperation mOperation => mOperation.Member,
                _ => null,
            };
        // Do not warn if platform specific enum/field value is used in conditional check, like: 'if (value == FooEnum.WindowsOnlyValue)'
        private static bool IsWithinConditionalOperation(IFieldReferenceOperation pOperation) =>
            pOperation.ConstantValue.HasValue &&
            pOperation.Parent is IBinaryOperation bo &&
            (bo.OperatorKind == BinaryOperatorKind.Equals ||
            bo.OperatorKind == BinaryOperatorKind.NotEquals ||
            bo.OperatorKind == BinaryOperatorKind.GreaterThan ||
            bo.OperatorKind == BinaryOperatorKind.LessThan ||
            bo.OperatorKind == BinaryOperatorKind.GreaterThanOrEqual ||
            bo.OperatorKind == BinaryOperatorKind.LessThanOrEqual);
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterOperationAction(context =>
            {
                AnalyzeOperation(context.Operation, context);
            },
            OperationKind.MethodReference,
            OperationKind.EventReference,
            OperationKind.FieldReference,
            OperationKind.Invocation,
            OperationKind.ObjectCreation,
            OperationKind.PropertyReference);

            /*context.RegisterSyntaxTreeAction(syntaxTreeContext =>
            {
                // Iterate through all statements in the tree
                var root = syntaxTreeContext.Tree.GetRoot(syntaxTreeContext.CancellationToken);
                foreach (var statement in root.DescendantNodes().OfType<StatementSyntax>())
                {
                    // Skip analyzing block statements 
                    if (statement is BlockSyntax)
                    {
                        continue;
                    }

                    // Report issues for all statements that are nested within a statement
                    // but not a block statement
                    if (statement.Parent is StatementSyntax && !(statement.Parent is BlockSyntax))
                    {
                        var diagnostic = Diagnostic.Create(Rule, statement.GetFirstToken().GetLocation());
                        syntaxTreeContext.ReportDiagnostic(diagnostic);
                    }
                }
            });*/
            //context.RegisterCodeFix(
            //    CodeAction.Create(
            //        title: Title,
            //        createChangedDocument: c => AddBracesAsync(context.Document, diagnostic, root),
            //        equivalenceKey: title),
            //    diagnostic);
        }
        private static void AnalyzeOperation(IOperation operation, OperationAnalysisContext context)
        {
            //if (operation.Parent is IArgumentOperation argumentOperation && UsedInCreatingNotSupportedException(argumentOperation, notSupportedExceptionType))
            //{
            //    return;
            //}

            var symbol = GetOperationSymbol(operation);
            var name = symbol.ToString();
            switch(name)
            {
                case "Microsoft.UI.Windowing.AppWindowTitleBar.ButtonBackgroundColor":
                    context.ReportDiagnostic(operation.CreateDiagnostic(Rule));
                    //context.ReportDiagnostic(operation.CreateDiagnostic(new DiagnosticDescriptor("WinUIEX1", "Not guarded", "fmt", "category", DiagnosticSeverity.Warning, true)));
                    return;
                default:
                    break;
            }
            if (symbol == null || symbol is ITypeSymbol type && type.SpecialType != SpecialType.None)
            {
                return;
            }
            if (symbol is IPropertySymbol property)
            {

            }
            else if (symbol is IMethodSymbol method)
            {
                //CheckTypeArguments(method.TypeArguments);
            }
        }

            private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Find just those named type symbols with names containing lowercase letters.
            if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}

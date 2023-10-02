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
    public class PlatformAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "WinUIEx";

        private const string Category = "Interoperability";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId + "2001",
            "Support Guard", "This call site is reachable on all Windows platforms. '{0}' must be guarded by '{1}'.", Category,
            DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "This API isn't available on all versions of Windows and should be guarded.", null);

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

        public class ApiType
        {
            public string MemberName { get; set; }
            public ISymbol Member { get; set; }
            public string GuardCheck { get; set; }
            public SymbolKind Kind { get; set; }
            public string HelpLink { get; set; }
            public DiagnosticDescriptor Rule { get; set; }
            public ISymbol Guard { get; set; }
        }
        private static readonly List<ApiType> ApiList = new List<ApiType> {
            new ApiType() {
                Kind = SymbolKind.NamedType, GuardCheck = "Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported",
                MemberName = "Microsoft.UI.Windowing.AppWindowTitleBar",
                Rule = new DiagnosticDescriptor(DiagnosticId + "2001", "Support Guard", "This call site is reachable on all Windows platforms. '{0}' must be guarded by '{1}'.", Category,
                    DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "This API isn't available on all versions of Windows and should be guarded.", "https://docs.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindowtitlebar#remarks")
                },
            /* These now have fallbacks and now sort of supported - consider doing a warning though
            new ApiType() {
                Kind = SymbolKind.Method, GuardCheck = "Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported",
                MemberName = "Microsoft.UI.Composition.SystemBackdrops.MicaController.AddSystemBackdropTarget(Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop)",
                Rule = new DiagnosticDescriptor(DiagnosticId + "2001", "Support Guard", "This call site is reachable on all Windows platforms. '{0}' must be guarded by 'MicaController.IsSupported()'.", Category,
                    DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "This API isn't available on all versions of Windows and should be guarded.", "https://docs.microsoft.com/en-us/windows/winui/api/microsoft.ui.composition.systembackdrops.micacontroller.issupported")
            },
            new ApiType() {
                Kind = SymbolKind.Method,
                GuardCheck = "Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported",
                MemberName = "Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.AddSystemBackdropTarget(Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop)",
                Rule = new DiagnosticDescriptor(DiagnosticId + "2001", "Support Guard", "This call site is reachable on all Windows platforms. '{0}' must be guarded by 'DesktopAcrylicController.IsSupported()'.", Category,
                    DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "This API isn't available on all versions of Windows and should be guarded.", "https://docs.microsoft.com/en-us/windows/winui/api/microsoft.ui.composition.systembackdrops.desktopacryliccontroller.issupported")
            }, */
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

            context.RegisterCompilationStartAction(context =>
            {
                Dictionary<string, ISymbol> guards = new Dictionary<string, ISymbol>();
                foreach (var type in ApiList)
                {
                    if (guards.ContainsKey(type.GuardCheck))
                        type.Guard = guards[type.GuardCheck];
                    else
                    {
                        var guardType = context.Compilation.GetTypeByMetadataName(type.GuardCheck.Substring(0, type.GuardCheck.LastIndexOf(".")));
                        if (guardType != null)
                        {
                            var member = guardType.GetMembers(type.GuardCheck.Substring(type.GuardCheck.LastIndexOf('.') + 1)).FirstOrDefault();
                            if (member != null)
                            {
                                type.Guard = member;
                                guards[type.GuardCheck] = member;
                            }
                        }
                    }
                    var method = type.MemberName;
                    if (method.Contains('('))
                        method = method.Substring(0, method.IndexOf('('));
                    var typeName = type.Kind == SymbolKind.NamedType ? method : method.Substring(0, method.LastIndexOf("."));
                    var methodType = context.Compilation.GetTypeByMetadataName(typeName);
                    if (methodType != null)
                    {
                        if (type.Kind == SymbolKind.NamedType)
                            type.Member = methodType;
                        else
                        {
                            var member = methodType.GetMembers().Where(t => t.ToString() == type.MemberName).FirstOrDefault();
                            if (member != null)
                            {
                                type.Member = member;
                            }
                        }
                    }


                    context.RegisterOperationBlockStartAction(context => AnalyzeOperationBlock(context));
                }
            });

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
        }

        private void AnalyzeOperationBlock(OperationBlockStartAnalysisContext context)
        {
            //https://github.com/dotnet/roslyn-analyzers/blob/e898a9d806d66adf687e2e3eb3d0180ca8a2167a/src/NetAnalyzers/Core/Microsoft.NetCore.Analyzers/InteropServices/PlatformCompatibilityAnalyzer.cs#L286
            context.RegisterOperationBlockEndAction(context =>
            {
                //context.GetControlFlowGraph()
            });
        }


        private static void AnalyzeOperation(IOperation operation, OperationAnalysisContext context)
        {
            var symbol = GetOperationSymbol(operation);
            if (ApiList.Where(a => SymbolEqualityComparer.Default.Equals(a.Member, symbol) ||
                a.Member.Kind == SymbolKind.NamedType && SymbolEqualityComparer.Default.Equals(symbol.ContainingType, a.Member) && !SymbolEqualityComparer.Default.Equals(symbol, a.Guard)).FirstOrDefault() is ApiType guardedApi)
            {
                if (!IsGuarded(context, operation, symbol, guardedApi))
                {
                    var diagnostic = Diagnostic.Create(guardedApi.Rule, operation.Syntax.GetLocation(), symbol.Name, guardedApi.Guard.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat));
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool IsGuarded(OperationAnalysisContext context, IOperation operation, ISymbol symbol, ApiType guardedType)
        {
            //var graph = context.GetControlFlowGraph();

            return false; //TODO
            // see
            // https://github.com/dotnet/roslyn-analyzers/blob/e898a9d806d66adf687e2e3eb3d0180ca8a2167a/src/NetAnalyzers/Core/Microsoft.NetCore.Analyzers/InteropServices/PlatformCompatibilityAnalyzer.cs#L419
            // https://github.com/dotnet/roslyn-analyzers/blob/e898a9d806d66adf687e2e3eb3d0180ca8a2167a/src/NetAnalyzers/Core/Microsoft.NetCore.Analyzers/InteropServices/PlatformCompatibilityAnalyzer.cs#L159

        }
    }
}
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace WinUIEx.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class WinUIExFrameNavigateAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId1003 = "WinUIEx1003";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.NavigateTypeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.NavigateTypeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.NavigateTypeDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor NavigateTypeRule = new DiagnosticDescriptor(
            DiagnosticId1003,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dotmorten.github.io/WinUIEx/rules/WinUIEx1003.html");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NavigateTypeRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterOperationAction(AnalyzeInvocationOperation, OperationKind.Invocation);
        }

        private void AnalyzeInvocationOperation(OperationAnalysisContext context)
        {
            var invocation = context.Operation as IInvocationOperation;
            if (invocation == null || !IsFrameNavigateWithTypeParameter(invocation.TargetMethod) || invocation.Arguments.Length == 0)
                return;

            var typeOfArgument = invocation.Arguments[0].Value as ITypeOfOperation;
            if (typeOfArgument == null || typeOfArgument.TypeOperand == null)
                return;

            var pageType = context.Compilation.GetTypeByMetadataName("Microsoft.UI.Xaml.Controls.Page");
            if (pageType == null || InheritsFrom(typeOfArgument.TypeOperand, pageType))
                return;

            var diagnostic = Diagnostic.Create(
                NavigateTypeRule,
                invocation.Arguments[0].Syntax.GetLocation(),
                typeOfArgument.TypeOperand.ToDisplayString(),
                pageType.ToDisplayString());
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsFrameNavigateWithTypeParameter(IMethodSymbol method)
        {
            return method != null &&
                   method.Name == "Navigate" &&
                   method.ContainingType?.ToDisplayString() == "Microsoft.UI.Xaml.Controls.Frame" &&
                   method.Parameters.Length > 0 &&
                   method.Parameters[0].Type.ToDisplayString() == "System.Type";
        }

        private static bool InheritsFrom(ITypeSymbol type, ITypeSymbol baseType)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                    return true;
            }

            return false;
        }
    }
}

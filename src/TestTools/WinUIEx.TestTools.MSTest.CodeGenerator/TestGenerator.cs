using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace WinUIEx.TestTools.MSTest
{
    [Generator]
    public class TestGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver 
            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
                return;

            // get the added attribute, and INotifyPropertyChanged
            INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName("WinUIEx.TestTools.WinUITestMethodAttribute");

            // group the fields by class, and generate the source
            foreach (IGrouping<INamedTypeSymbol, IMethodSymbol> group in receiver.Methods.GroupBy(f => f.ContainingType))
            {
                string classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol, context);
                System.IO.File.WriteAllText($"e:\\{group.Key.Name}_generatedUITests.cs", classSource);
                context.AddSource($"{group.Key.Name}_generatedUITests.cs", SourceText.From(classSource, Encoding.UTF8));
            }
        }
        private string ProcessClass(INamedTypeSymbol classSymbol, List<IMethodSymbol> methods, ISymbol attributeSymbol, GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            StringBuilder source = new StringBuilder($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name}
    {{
");
            var prop = classSymbol.GetMembers("WindowContext").FirstOrDefault();
            if (prop is null)
                source.AppendLine("        public global::Microsoft.UI.Xaml.Window WindowContext { get; set; }");

            prop = classSymbol.GetMembers("DispatcherQueue").FirstOrDefault();
            if (prop is null)
                source.AppendLine("        public global::Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue => WindowContext.DispatcherQueue;");

            // create properties for each field 
            foreach (IMethodSymbol methodSymbol in methods)
            {
                ProcessMethod(source, methodSymbol, attributeSymbol);
            }

            source.Append("    }\n}");
            return source.ToString();
        }
        private void ProcessMethod(StringBuilder source, IMethodSymbol methodSymbol, ISymbol attributeSymbol)
        {

            bool isAsync = methodSymbol.ReturnType.ToString() == "System.Threading.Tasks.Task";
            source.AppendLine();
            bool isDataRows = false; // methodSymbol.GetAttributes().Any(a => a.AttributeClass.ToString() == "Microsoft.VisualStudio.TestTools.UnitTesting.DataRowAttribute");
            source.AppendLine($@"        [Microsoft.VisualStudio.TestTools.UnitTesting.{(isDataRows ? "Data" : "")}TestMethod(""{methodSymbol.Name}"")]");
            //source.AppendLine($@"        [WinUIUnitTests.CustomTestMethod(""{methodSymbol.Name}"")]");
            
            foreach (var attribute in methodSymbol.GetAttributes())
            {
                if (attribute.AttributeClass.ToString() == "WinUIEx.TestTools.MSTest.WinUITestMethodAttribute")
                    continue;
                source.Append($"        [{attribute.AttributeClass}({string.Join(", ", attribute.ConstructorArguments.Select(s => s.ToCSharpString()))}");
                foreach(var arg in attribute.NamedArguments)
                {
                    source.Append($", {arg.Key} = {arg.Value.ToCSharpString()}");
                }
                if (attribute.AttributeClass.ToString() == "Microsoft.VisualStudio.TestTools.UnitTesting.DataRowAttribute" &&
                    !attribute.NamedArguments.Any(n=>n.Key == "DisplayName"))
                {
                    source.Append($", DisplayName = \"{methodSymbol.Name}({string.Join(", ", attribute.ConstructorArguments.Select(s => s.ToCSharpString()))})\"");
                }
                source.AppendLine(")]");
            }

            source.Append($@"        public async System.Threading.Tasks.Task {methodSymbol.Name}_generated(");
            source.Append(string.Join(", ", methodSymbol.Parameters.Select(s => $"{s.Type} {s.Name}")));

            source.AppendLine(")");
            source.AppendLine($@"        {{
            System.Threading.Tasks.TaskCompletionSource<object> tcs = new System.Threading.Tasks.TaskCompletionSource<object>();
            WindowContext = WinUIEx.TestTools.TestHost.Window;
            bool ok = WinUIEx.TestTools.TestHost.Window.DispatcherQueue.TryEnqueue({(isAsync ? "async " : "")}() =>
            {{
                var content = WinUIEx.TestTools.TestHost.Window.Content;
                try
                {{
                    {(isAsync ? "await " : "")}{methodSymbol.Name}({string.Join(", ", methodSymbol.Parameters.Select(s => s.Name))});
                    tcs.SetResult(null);
                }}
                catch (System.Exception ex)
                {{
                    tcs.SetException(ex);
                }}
                WinUIEx.TestTools.TestHost.Window.Content = content;
            }});
            await tcs.Task;
        }}
");
        }


        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<IMethodSymbol> Methods { get; } = new List<IMethodSymbol>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                // any field with at least one attribute is a candidate for property generation
                if (context.Node is MethodDeclarationSyntax methodDeclarationSyntax
                    && methodDeclarationSyntax.AttributeLists.Count > 0)
                {
                    IMethodSymbol methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax) as IMethodSymbol;
                    if (methodSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "WinUIEx.TestTools.MSTest.WinUITestMethodAttribute"))
                    {
                        Methods.Add(methodSymbol);
                    }
                }
            }
        }
    }
}

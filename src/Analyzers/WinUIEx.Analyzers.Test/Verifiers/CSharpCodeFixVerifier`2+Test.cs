using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Immutable;
using System.Linq;

namespace WinUIEx.Analyzers.Test
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, MSTestVerifier>
        {
            public Test()
            {
                SolutionTransforms.Add((solution, projectId) =>
                {
                    var project = solution.GetProject(projectId);
                    project = project.AddMetadataReference(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Microsoft.UI.Xaml.Window).Assembly.Location));
                    project = project.AddMetadataReference(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Microsoft.UI.Windowing.AppWindow).Assembly.Location));
                    var compilationOptions = project.CompilationOptions;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions).WithProjectMetadataReferences(projectId,
                        System.AppDomain.CurrentDomain.GetAssemblies().Where(a=>!a.IsDynamic ).Select(a => Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(a.Location)))
                    
                    //{
                    //    Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Microsoft.UI.Windowing.AppWindow).Assembly.Location),
                    //    Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Microsoft.UI.Xaml.Window).Assembly.Location)
                    //})*/
                    ;
                    return solution;
                });
                var a = System.Reflection.Assembly.Load("Microsoft.Windows.SDK.NET, Version=10.0.18362.18, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                    TestState.AdditionalReferences.Add(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(a.Location));
                //foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                //{
                //    if (assembly.IsDynamic) continue;
                //    TestState.AdditionalReferences.Add(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(assembly.Location));
                //}
                //TestState.AdditionalReferences.Add(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Microsoft.UI.Windowing.AppWindow).Assembly.Location));
                //TestState.AdditionalReferences.Add(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Microsoft.UI.Xaml.Window).Assembly.Location));
            }
            private class Resolver : Microsoft.CodeAnalysis.MetadataReferenceResolver
            {
                public override bool Equals(object other)
                {
                    return this == other;
                }

                public override int GetHashCode()
                {
                    return 0;
                }

                public override ImmutableArray<Microsoft.CodeAnalysis.PortableExecutableReference> ResolveReference(string reference, string baseFilePath, Microsoft.CodeAnalysis.MetadataReferenceProperties properties)
                {
                    return new ImmutableArray<Microsoft.CodeAnalysis.PortableExecutableReference>();
                }
            }
        }
    }
}

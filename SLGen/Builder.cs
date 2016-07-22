using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE80;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices;
using MyCodeGenerator;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace MyCoreBuilder
{
    public class Builder : IDisposable
    {

        private DTE2 _DTE2;
        private Log _Log;
        private IVsStatusbar _IVsStatusbar;
        private VisualStudioWorkspace _VisualStudioWorkspace;
        private CachedCompiler _CachedCompiler;

        public Builder(VisualStudioWorkspace worksp, DTE2 dte, IVsStatusbar stab)
        {
            _DTE2 = dte;
            _IVsStatusbar = stab;
            _Log = new Log(dte);
            _VisualStudioWorkspace = worksp;
            _CachedCompiler = new CachedCompiler(new RoslynCompiler());
            if (_DTE2 == null || _Log == null || _IVsStatusbar == null)
                ErrorHandler.ThrowOnFailure(1);
            _VisualStudioWorkspace.WorkspaceChanged += _VisualStudioWorkspace_WorkspaceChanged;
        }
        public Task Init()
        {
            return ScanForEntryPoint();
        }

        private async void _VisualStudioWorkspace_WorkspaceChanged(object sender, Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentAdded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentChanged:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentReloaded:
                    FileChanged(e);
                    await ScanForEntryPoint();
                    break;
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionAdded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionChanged:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionReloaded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectAdded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectChanged:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectReloaded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectRemoved:
                    await ScanForEntryPoint();
                    break;
                default:
                    break;
            }
        }
        void FileChanged(Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
        {
            //var doc = _VisualStudioWorkspace.GetFileCodeModel(e.DocumentId);
        }
        async Task ScanForEntryPoint()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                var startuprojectlist = (_DTE2.Solution?.SolutionBuild?.StartupProjects as object[])?.Select(a => a?.ToString()?.ToLower()).Where(a => !string.IsNullOrWhiteSpace(a));
                var mainproject = _VisualStudioWorkspace?.CurrentSolution?.Projects?.FirstOrDefault(a => startuprojectlist.Any(b => a.FilePath.ToLower().EndsWith(b)));
                var te = await mainproject.GetCompilationAsync();

                foreach (var tree in te.SyntaxTrees)
                {
                    var root = tree.GetRoot();
                    var founditem = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(b => b.BaseList?.Types.FirstOrDefault()?.ToString() == "ITestInterface");
                    if (founditem != null)
                    {
                        Debug.WriteLine("FOUND IT!");
                        var references = new MetadataReference[]
                            {
                                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
                            };
                       var assembly = _CachedCompiler.Compile(tree, references);
                        break;
                    }
                }
                stopwatch.Stop();

                Debug.WriteLine($"Time taken to find {stopwatch.ElapsedMilliseconds} ms ");

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

        }
        public void Dispose()
        {
            _VisualStudioWorkspace.WorkspaceChanged -= _VisualStudioWorkspace_WorkspaceChanged;
        }
    }

}

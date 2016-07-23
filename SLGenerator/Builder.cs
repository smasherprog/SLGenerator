using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE80;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Timers;

namespace SLGenerator
{
    public class Builder : IDisposable
    {

        private DTE2 _DTE2;
        private Log _Log;
        private IVsStatusbar _IVsStatusbar;
        private VisualStudioWorkspace _VisualStudioWorkspace;
        private CachedCompiler _CachedCompiler;
        private Project _MainProject;
        private string _LastPathToEntryPoint;
        private Timer _Timer;
        private bool _Building = false;
        private object _BuildingLock = new object();
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
            _Timer = new Timer(3000);
            _Timer.AutoReset = false;
            _Timer.Elapsed += _Timer_Elapsed;
            _Timer.Start();

        }

        private void _Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_Building) return;
            else _Building = true;
            SetMainEntryPoint();
            ScanForEntryPoint();
            _Building = false;
        }
        void TryRestartTimer()
        {
            Debug.WriteLine("Restarting TImer");
            if (_Building) return;
            _Timer.Stop();
            _Timer.Start();
        }

        private void _VisualStudioWorkspace_WorkspaceChanged(object sender, Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentAdded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentChanged:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentReloaded:
                    TryRestartTimer();
                    break;
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionAdded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionChanged:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionReloaded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectAdded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectChanged:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectReloaded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectRemoved:
                    TryRestartTimer();
                    break;
                default:
                    break;
            }
        }
        void SetMainEntryPoint()
        {
            var startuprojectlist = (_DTE2.Solution?.SolutionBuild?.StartupProjects as object[])?.Select(a => a?.ToString()?.ToLower()).Where(a => !string.IsNullOrWhiteSpace(a));
            _MainProject = _VisualStudioWorkspace?.CurrentSolution?.Projects?.FirstOrDefault(a => startuprojectlist.Any(b => a.FilePath.ToLower().EndsWith(b)));
        }
        bool FillCompilerInstructions(SyntaxTree tree, CompilerInstructions compileins)
        {
            try
            {
                var root = tree.GetRoot();
                var interfacename = nameof(ISLGeneratorScript);
                var founditem = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(b => b.BaseList?.Types.FirstOrDefault()?.ToString() == interfacename);
                if (founditem != null)
                {
                    Debug.WriteLine("FOUND IT!");
                    compileins.ClassName = founditem.Identifier.Text;
                    compileins.Code = tree;
                    _LastPathToEntryPoint = tree.FilePath;
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return false;
        }
        void ScanForEntryPoint()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                var compileins = new CompilerInstructions();
                var te = _MainProject.GetCompilationAsync().Result;

                if (string.IsNullOrWhiteSpace(_LastPathToEntryPoint))
                {

                    foreach (var tree in te.SyntaxTrees)
                    {
                        if (FillCompilerInstructions(tree, compileins))
                        {
                            Debug.WriteLine("Found using thourough search");
                            break;
                        }
                    }
                }
                else
                {
                    if (!FillCompilerInstructions(te.SyntaxTrees.FirstOrDefault(a => a.FilePath == _LastPathToEntryPoint), compileins))
                    {//if the search failed in the last location, try doing a full search
                        foreach (var tree in te.SyntaxTrees)
                        {
                            if (FillCompilerInstructions(tree, compileins))
                            {
                                Debug.WriteLine("Found using thourough search");
                                break;
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Found using fast search");
                    }
                }
                if (compileins.Code != null)
                {
                    var references = new MetadataReference[] {
                        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
                    };
                    compileins.Assemblies = references;
                    _CachedCompiler.CompileAndRun(compileins);
                }

            }
            catch (RoslynCompilationException e)
            {
                foreach (var item in e.Result.Diagnostics)
                {
                    Log.Error(item.Descriptor.Description.ToString());
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            stopwatch.Stop();
            Debug.WriteLine($"Time taken to find {stopwatch.ElapsedMilliseconds} ms ");
        }
        public void Dispose()
        {
            _Timer.Stop();
            _Timer.Dispose();
            _VisualStudioWorkspace.WorkspaceChanged -= _VisualStudioWorkspace_WorkspaceChanged;
        }
    }

}

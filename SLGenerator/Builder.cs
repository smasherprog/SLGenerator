using System;
using System.Linq;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace SLGenerator
{
    public class Builder : IDisposable
    {

        private DTE2 _DTE2;
        private Log _Log;
        private IVsStatusbar _IVsStatusbar;
        private VisualStudioWorkspace _VisualStudioWorkspace;
        // private CachedCompiler _CachedCompiler;
        private SLGeneratorScriptProxy _SLGeneratorScriptProxy = null;

        private object _BuildingLock = new object();
        public Builder(VisualStudioWorkspace worksp, DTE2 dte, IVsStatusbar stab)
        {
            _DTE2 = dte;
            _IVsStatusbar = stab;
            _Log = new Log(dte);
            _VisualStudioWorkspace = worksp;
            // _CachedCompiler = new CachedCompiler(new RoslynCompiler());
            if (_DTE2 == null || _Log == null || _IVsStatusbar == null)
                ErrorHandler.ThrowOnFailure(1);

            _VisualStudioWorkspace.WorkspaceChanged += _VisualStudioWorkspace_WorkspaceChanged;
        }

        public void BuildMain(string code, string filepath, System.IO.StringWriter log)
        {
            var docId = _VisualStudioWorkspace?.CurrentSolution?.GetDocumentIdsWithFilePath(filepath).FirstOrDefault();

            var startuprojectlist = (_DTE2.Solution?.SolutionBuild?.StartupProjects as object[])?.Select(a => a?.ToString()?.ToLower()).Where(a => !string.IsNullOrWhiteSpace(a));
            var mainproject = _VisualStudioWorkspace?.CurrentSolution?.Projects?.FirstOrDefault(a => startuprojectlist.Any(b => a.FilePath.ToLower().EndsWith(b)));
            //_VisualStudioWorkspace?.CurrentSolution?.document
            var project = _VisualStudioWorkspace?.CurrentSolution?.GetProject(docId.ProjectId);
            if (project == null) return;
        
            var compilation = project.GetCompilationAsync().Result;

            //Document doc = project.GetDocument(docId);
            //SyntaxTree docSyntaxTree = doc.GetSyntaxTreeAsync().Result;

            using (var ms = new MemoryStream())
            {
                var compilationResult = compilation.Emit(ms);
                if (compilationResult.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    var ass = Assembly.Load(ms.ToArray());
                    var asstype = ass.DefinedTypes.FirstOrDefault(a => a.GetInterfaces().Any(b => b.Name == "ISLGeneratorScript"));
                    var type = ass.GetType(asstype.FullName);
                    _SLGeneratorScriptProxy = new SLGeneratorScriptProxy(Activator.CreateInstance(type));
                    var projs = _SLGeneratorScriptProxy.IncludeProjects(_VisualStudioWorkspace?.CurrentSolution?.Projects).ToList();
                    if (_SLGeneratorScriptProxy.IncludeMainProject() && mainproject != null)
                    {
                        if (!projs.Any(a => a.Id == mainproject.Id)) projs.Add(mainproject);
                    }
                    foreach (var item in projs)
                    {
                        var comp = item.GetCompilationAsync().Result;
                        foreach (var d in item.Documents)
                        {
                            var docSyntaxTree = d.GetSyntaxTreeAsync().Result;
                            var semanticModel = d.GetSemanticModelAsync().Result;
                            var root = docSyntaxTree.GetRoot();
                            if (_SLGeneratorScriptProxy.IncludeDocument(d, root, semanticModel))
                            {
                                _SLGeneratorScriptProxy.Run(d, root, semanticModel);
                            }
                        }
                    }
                   
                }
            }
        }
        private void _VisualStudioWorkspace_WorkspaceChanged(object sender, Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentAdded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentChanged:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentReloaded:

                    break;
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionAdded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionChanged:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionReloaded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectAdded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectChanged:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectReloaded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectRemoved:

                    break;
                default:
                    break;
            }
        }


        public void Dispose()
        {

            _VisualStudioWorkspace.WorkspaceChanged -= _VisualStudioWorkspace_WorkspaceChanged;
        }
    }

}

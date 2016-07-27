using System;
using System.Linq;
using EnvDTE100;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using EnvDTE80;

namespace SLGenerator
{
    public class Builder : IDisposable
    {

        private DTE2 _DTE2;
        private VisualStudioWorkspace _VisualStudioWorkspace;
        private SLGeneratorScriptProxy _SLGeneratorScriptProxy = null;

        List<string> _DocumentsToWatch = new List<string>();

        public Builder(VisualStudioWorkspace worksp, DTE2 dte, IVsStatusbar stab)
        {
            _DTE2 = dte;
   
            _VisualStudioWorkspace = worksp;
 
            _VisualStudioWorkspace.WorkspaceChanged += _VisualStudioWorkspace_WorkspaceChanged;
        }

        public void BuildMain(string code, string filepath, System.IO.StringWriter log)
        {
            var docId = _VisualStudioWorkspace?.CurrentSolution?.GetDocumentIdsWithFilePath(filepath).FirstOrDefault();

            var startuprojectlist = (_DTE2.Solution?.SolutionBuild?.StartupProjects as object[])?.Select(a => a?.ToString()?.ToLower()).Where(a => !string.IsNullOrWhiteSpace(a));
            if (startuprojectlist == null) return;
            var projs = Utilities.GetProjects(_DTE2.Solution.Projects);

            var mainproject = projs.FirstOrDefault(a => startuprojectlist.Any(b => a.FullName.ToLower().EndsWith(b)));
         
            var project = _VisualStudioWorkspace?.CurrentSolution?.GetProject(docId.ProjectId);
            if (project == null) return;
        
            var compilation = project.GetCompilationAsync().Result;

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
                    //var projs = _SLGeneratorScriptProxy.IncludeProjects(_VisualStudioWorkspace?.CurrentSolution?.Projects).ToList();
                

                    if (_SLGeneratorScriptProxy.IncludeMainProject() && mainproject != null)
                    {
                        if (!projs.Any(a => a.UniqueName == mainproject.UniqueName)) projs.Add(mainproject);
                    }
                    var projlist = Utilities.GetJoinedProjs(_VisualStudioWorkspace?.CurrentSolution?.Projects, projs);
                    foreach (var item in projlist)
                    {
                        foreach (var doc in item.NProject.Documents)
                        {
                            RunOnce(doc, doc?.GetSyntaxTreeAsync()?.Result?.GetRoot(), doc?.GetSemanticModelAsync()?.Result);
                        }
                    }
                   
                }
            }
        }


        private void RunOnce(Document d, SyntaxNode root, SemanticModel sem)
        {
            if (_SLGeneratorScriptProxy == null || d==null || root ==null || sem == null) return;
            if (_SLGeneratorScriptProxy.IncludeDocument(d, root, sem))
            {
                _DocumentsToWatch.Add(d.FilePath);
                _SLGeneratorScriptProxy.Run(_DTE2 ,d, root, sem);
            }
        }
        void DocumentChanged(Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
        {
            var doc = e.NewSolution.GetDocument(e.DocumentId);
            RunOnce(doc, doc?.GetSyntaxTreeAsync()?.Result?.GetRoot(), doc?.GetSemanticModelAsync()?.Result);
       
        }
        void SolutionChanged(Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
        {
            int k = 8;
        }
        void ProjectChanged(Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
        {
            int k = 8;
        }
        private void _VisualStudioWorkspace_WorkspaceChanged(object sender, Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
        {

            switch (e.Kind)
            {
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentAdded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentChanged:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentReloaded:
                    DocumentChanged(e);
                    break;
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionAdded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionChanged:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionReloaded:
                    SolutionChanged(e);
                    break;
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectAdded:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectChanged:
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectReloaded:
                    ProjectChanged(e);
                    break;
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

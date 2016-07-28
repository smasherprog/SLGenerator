﻿using System;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using EnvDTE80;

namespace SLGenerator
{
    public class Builder : IDisposable
    {

        private DTE2 _DTE2;
        private VisualStudioWorkspace _VisualStudioWorkspace;
        private ISLGeneratorMain _ISLGeneratorMain = null;

        List<string> _DocumentsToWatch = new List<string>();

        public Builder(VisualStudioWorkspace worksp, DTE2 dte, IVsStatusbar stab)
        {
            _DTE2 = dte;

            _VisualStudioWorkspace = worksp;

            _VisualStudioWorkspace.WorkspaceChanged += _VisualStudioWorkspace_WorkspaceChanged;
     
        }
        string get_StartupProject()
        {
            return (_DTE2.Solution?.SolutionBuild?.StartupProjects as object[])?.Select(a => a?.ToString()?.ToLower()).Where(a => !string.IsNullOrWhiteSpace(a)).FirstOrDefault();
        }
        public void BuildMain(string code, string filepath, System.IO.StringWriter log)
        {
            var docId = _VisualStudioWorkspace?.CurrentSolution?.GetDocumentIdsWithFilePath(filepath).FirstOrDefault();
            if (docId == null) return;
            var compilation = _VisualStudioWorkspace?.CurrentSolution?.GetProject(docId.ProjectId)?.GetCompilationAsync()?.Result;
            if (compilation == null) return;
            using (var ms = new MemoryStream())
            {
                var compilationResult = compilation.Emit(ms);
                if (compilationResult.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    var ass = Assembly.Load(ms.ToArray());
                    var asstype = ass.DefinedTypes.FirstOrDefault(a => a.GetInterfaces().Any(b => b.Name == "ISLGeneratorMain"));
                    var type = ass.GetType(asstype.FullName);
                    _ISLGeneratorMain = (ISLGeneratorMain)new SLGeneratorMainProxy(Activator.CreateInstance(type));
                    OnProjectsChanged();
                }
            }
        }
        void OnProjectsChanged()
        {
           
            var mergedprojs = Utilities.JoinProjects(_VisualStudioWorkspace?.CurrentSolution?.Projects, Utilities.GetProjects(_DTE2?.Solution?.Projects));
            var startupprojname = get_StartupProject();
            if (string.IsNullOrWhiteSpace(startupprojname)) return;
            var startupproj = mergedprojs.FirstOrDefault(a => a.Item2.FullName.ToLower().EndsWith(startupprojname));

            _ISLGeneratorMain.OnProjectsChanged(mergedprojs.Where(a => a != startupproj).ToList(), startupproj);
            _DocumentsToWatch = new List<string>();
            foreach (var item in _ISLGeneratorMain.IncludeProjects())
            {
                foreach (var doc in item.Item1.Documents)
                {
                    OnDocumentChanged(doc, doc?.GetSyntaxTreeAsync()?.Result?.GetRoot(), doc?.GetSemanticModelAsync()?.Result);
                }
            }
        }
       
        void OnDocumentChanged(Document d, SyntaxNode root, SemanticModel sem)
        {
            if (_ISLGeneratorMain == null || d == null || root == null || sem == null) return;
            if (_ISLGeneratorMain.IncludeDocument(d, root, sem))
            {
                _DocumentsToWatch.Add(d.FilePath);
                _ISLGeneratorMain.OnDocumentChanged(d, root, sem);
            }
        }
        void DocumentChanged(Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
        {
            var doc = e.NewSolution.GetDocument(e.DocumentId);
            OnDocumentChanged(doc, doc?.GetSyntaxTreeAsync()?.Result?.GetRoot(), doc?.GetSemanticModelAsync()?.Result);
        }
        void SolutionChanged(Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
        {
            OnProjectsChanged();
        }
        void ProjectChanged(Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
        {
            OnProjectsChanged();
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
                case Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectRemoved:
                    ProjectChanged(e);
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

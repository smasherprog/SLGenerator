﻿using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SLGeneratorLib.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;

namespace SLGeneratorLib.Service
{
    public class SLDocumentWatcher : Interfaces.ISLDocumentWatcher
    {
        protected EnvDTE80.DTE2 _DTE2 = null;

        protected VisualStudioWorkspace _VisualStudioWorkspace = null;
        //current documents being watched by this class for changes. 
        protected HashSet<string> _DocumentsToWatch = new HashSet<string>();
        //Listing of all projects in the solution and just the projects to watch
        protected IEnumerable<MergedProject> _AllProjects, _ProjectsToWatch;
        //this is the project set to the startup project
        protected MergedProject _StartupProject = null;

        public SLDocumentWatcher()
        {

            var componentmodel = (IComponentModel)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SComponentModel));
            _VisualStudioWorkspace = componentmodel.GetService<VisualStudioWorkspace>();
            _DTE2 = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

            _VisualStudioWorkspace.WorkspaceChanged += _VisualStudioWorkspace_WorkspaceChanged;

        }

        string get_StartupProject()
        {
            return (_DTE2.Solution?.SolutionBuild?.StartupProjects as object[])?.Select(a => a?.ToString()?.ToLower()).Where(a => !string.IsNullOrWhiteSpace(a)).FirstOrDefault();
        }
        public virtual void Init()
        {
            ProjectsChanged();
        }
        void ProjectsChanged()
        {
            _AllProjects = Utilities.JoinProjects(_VisualStudioWorkspace?.CurrentSolution?.Projects, Utilities.GetProjects(_DTE2?.Solution?.Projects));
            var startupprojname = get_StartupProject();
            if (string.IsNullOrWhiteSpace(startupprojname)) return;
            _StartupProject = _AllProjects.FirstOrDefault(a => a.EnvDTE_Project.FullName.ToLower().EndsWith(startupprojname));
            OnProjectsChanged();

        }

        public virtual void OnProjectsChanged()
        {
            _ProjectsToWatch = IncludeProjects();
            if (_ProjectsToWatch == null) _ProjectsToWatch = new List<MergedProject>();

            _DocumentsToWatch = new HashSet<string>();

            foreach (var item in _ProjectsToWatch)
            {
                var docstoprocess = item?.CodeAnalysis_Project?.Documents;
                if (docstoprocess != null)
                {
                    foreach (var doc in docstoprocess)
                    {
                        OnDocumentChanged(item, doc);
                    }
                }
            }

        }
        public virtual IEnumerable<MergedProject> IncludeProjects()
        {
            return _AllProjects;
        }
        public class UsingCollector : CSharpSyntaxWalker
        {
            StreamWriter _StreamWriter;
            public UsingCollector(StreamWriter s)
            {
                _StreamWriter = s;
            }
            public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                _StreamWriter.WriteLine($"{node.Identifier}");
                //base.VisitPropertyDeclaration(node);
            }
      
        }
        public async virtual void OnDocumentChanged(MergedProject current_project, Document d)
        {
            var tree = await d?.GetSyntaxTreeAsync();
            var root = await tree.GetRootAsync();
            var nodes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(a => a.Identifier.ValueText.EndsWith("ViewModel"));
            if (nodes.Any() && _StartupProject != null)
            {

                _DocumentsToWatch.Add(d.FilePath);

                var outfile = System.IO.Path.GetDirectoryName(_StartupProject.CodeAnalysis_Project.FilePath) + "\\" + System.IO.Path.GetFileNameWithoutExtension(d.Name) + ".ts";

                using (var fs = new System.IO.FileStream(outfile, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write))
                using (var streamwriter = new System.IO.StreamWriter(fs))
                {
                    foreach (var item in nodes)
                    {
                       
                        streamwriter.WriteLine($"export class {item.Identifier.ValueText} " + "{");
                        var por = new UsingCollector(streamwriter);
                        por.Visit(item);
                      
                        streamwriter.WriteLine("}");
                    }
                }
                var pitem = FindProjectItem(outfile);
                if (pitem == null) pitem = _StartupProject.EnvDTE_Project.ProjectItems.AddFromFile(outfile);
                if (pitem != null) Debug.WriteLine(pitem.Name);
            }
        }
        private EnvDTE.ProjectItem FindProjectItem(string path)
        {
            foreach (EnvDTE.ProjectItem item in _StartupProject.EnvDTE_Project.ProjectItems)
            {
                try
                {
                    if (item.Document.FullName.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return item;
                    }
                }
                catch
                {
                    // Can't read properties from project item sometimes when deleting miltiple files
                }
            }

            return null;
        }

        void DocumentChanged(Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
        {
            var doc = e.NewSolution.GetDocument(e.DocumentId);
            if (_DocumentsToWatch.Contains(doc.FilePath))
            {
                var workingproj = _ProjectsToWatch.FirstOrDefault(a => doc.Project.Id == a.CodeAnalysis_Project.Id);
                OnDocumentChanged(workingproj, doc);
            }

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
            Debug.WriteLine("Dispose SLDocumentWatcher");
            // AppDomain.CurrentDomain.AssemblyResolve -= MyResolveEventHandler;
            _VisualStudioWorkspace.WorkspaceChanged -= _VisualStudioWorkspace_WorkspaceChanged;
        }

    }
}

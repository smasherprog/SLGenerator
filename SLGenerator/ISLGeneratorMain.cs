using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System;

namespace SLGenerator
{
    public interface ISLGeneratorMain
    {
        List<Tuple<Microsoft.CodeAnalysis.Project, EnvDTE.Project>> IncludeProjects();
        void OnProjectsChanged(List<Tuple<Microsoft.CodeAnalysis.Project, EnvDTE.Project>> projects, Tuple<Microsoft.CodeAnalysis.Project, EnvDTE.Project> startup_project);
        void OnDocumentChanged(Document doc, SyntaxNode root, SemanticModel sem);
        bool IncludeDocument(Document doc, SyntaxNode root, SemanticModel sem);
    }
}

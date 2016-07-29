using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SLGeneratorLib
{
    public interface ISLGeneratorMain
    {
        IEnumerable<Model.MergedProject> IncludeProjects();
        void OnProjectsChanged(IEnumerable<Model.MergedProject> projects, Model.MergedProject mainproject);
        void OnDocumentChanged(Document doc, SyntaxNode root, SemanticModel sem);
        bool IncludeDocument(Document doc, SyntaxNode root, SemanticModel sem);
    }
}

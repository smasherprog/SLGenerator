using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using SLGeneratorLib;
using SLGeneratorLib.Model;

namespace SLGenerator
{

    public class SLGeneratorMainProxy : ISLGeneratorMain
    {
        object _object;
      
        MethodInfo _IncludeProjects;
        MethodInfo _IncludeDocument;

        MethodInfo _OnProjectsChanged;
        MethodInfo _OnDocumentChanged;

        public SLGeneratorMainProxy(object obj)
        {
            _object = obj;

            _IncludeProjects = _object.GetType().GetMethod("IncludeProjects");
            _IncludeDocument = _object.GetType().GetMethod("IncludeDocument");

            _OnProjectsChanged = _object.GetType().GetMethod("OnProjectsChanged");
            _OnDocumentChanged = _object.GetType().GetMethod("OnDocumentChanged");

        }

        public IEnumerable<MergedProject> IncludeProjects()
        {
            return (IEnumerable<MergedProject>)_IncludeProjects.Invoke(_object, new object[0]);
        }

        public void OnProjectsChanged(IEnumerable<MergedProject> projects, MergedProject mainproject)
        {
            _OnProjectsChanged.Invoke(_object, new object[] { projects, mainproject });
        }

        public void OnDocumentChanged(Microsoft.CodeAnalysis.Document doc, SyntaxNode root, SemanticModel sem)
        {
            _OnDocumentChanged.Invoke(_object, new object[] {  doc, root, sem});
        }

        public bool IncludeDocument(Microsoft.CodeAnalysis.Document doc, SyntaxNode root, SemanticModel sem)
        {
            return (bool)_IncludeDocument.Invoke(_object, new object[] { doc, root, sem });
        }

    }

}

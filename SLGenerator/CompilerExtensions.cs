using EnvDTE80;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace SLGenerator
{
    public interface ISLGeneratorScript
    {
        bool IncludeMainProject();
        IEnumerable<Project> IncludeProjects(IEnumerable<Project> projects_in_solution);
        bool IncludeDocument(Document doc, SyntaxNode root, SemanticModel sem);
        void Run(DTE2 dte2, Document doc, SyntaxNode root, SemanticModel sem);
    }
    public class SLGeneratorScriptProxy : ISLGeneratorScript
    {
        object _object;
        MethodInfo _IncludeMainProject;
        MethodInfo _IncludeProjects;
        MethodInfo _Run;
        MethodInfo _IncludeDocument;

        public SLGeneratorScriptProxy(object obj)
        {
            _object = obj;
            _IncludeMainProject = _object.GetType().GetMethod("IncludeMainProject");
            _IncludeProjects = _object.GetType().GetMethod("IncludeProjects");
            _Run = _object.GetType().GetMethod("Run");
            _IncludeDocument = _object.GetType().GetMethod("IncludeDocument");
        }

        public bool IncludeMainProject()
        {
            return (bool)_IncludeMainProject.Invoke(_object, null);
        }
        public IEnumerable<Project> IncludeProjects(IEnumerable<Project> projects_in_solution)
        {
            return (IEnumerable<Project>)_IncludeProjects.Invoke(_object, new object[] { projects_in_solution });
        }
        public void Run(DTE2 dte2, Document doc, SyntaxNode root, SemanticModel sem)
        {
            _Run.Invoke(_object, new object[] { dte2,  doc, root, sem });
        }

        public bool IncludeDocument(Document doc, SyntaxNode root, SemanticModel sem)
        {
           return (bool) _IncludeDocument.Invoke(_object, new object[] { doc, root, sem });
        }
    }

    public interface ICompiler
    {
        Assembly Compile(string code, params string[] assemblyLocations);
        Assembly Compile(string code, params MetadataReference[] assemblyLocations);
        Assembly Compile(SyntaxTree code, params MetadataReference[] assemblyLocations);
    }
    public static class CompilerExtensions
    {
        public static ISLGeneratorScript Compile(this ICompiler compiler, string code, MetadataReference[] Assemblies, params object[] constructorParameters)
        {
            var assembly = compiler.Compile(code, Assemblies);
            var asstype = assembly.DefinedTypes.FirstOrDefault(a => a.GetInterfaces().Any(b => b.Name == "ISLGeneratorScript"));
            var type = assembly.GetType(asstype.FullName);
            return (ISLGeneratorScript)Activator.CreateInstance(type, constructorParameters);
        }
        public static ISLGeneratorScript Compile(this ICompiler compiler, SyntaxTree code, MetadataReference[] Assemblies, params object[] constructorParameters)
        {
            var assembly = compiler.Compile(code, Assemblies);
            var asstype = assembly.DefinedTypes.FirstOrDefault(a => a.GetInterfaces().Any(b => b.Name == "ISLGeneratorScript"));
            var type = assembly.GetType(asstype.FullName);
            return (ISLGeneratorScript)Activator.CreateInstance(type, constructorParameters);
        }

    }

}

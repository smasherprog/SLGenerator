using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Reflection;

//Copied from https://keestalkstech.com/2016/05/how-to-add-dynamic-compilation-to-your-projects/
namespace SLGenerator
{
    public interface ISLGeneratorScript
    {
        /// Runs the script.
        void Run();
    }

    public interface ICompiler
    {
        Assembly Compile(string code, params string[] assemblyLocations);
        Assembly Compile(string code, params MetadataReference[] assemblyLocations);
        Assembly Compile(SyntaxTree code, params MetadataReference[] assemblyLocations);
    }
    public class CompilerInstructions
    {
        public CompilerInstructions()
        {
            ClassName = null;
            Code = null;

        }
        public string ClassName { get; set; }
        public SyntaxTree Code { get; set; }
        public MetadataReference[] Assemblies { get; set; }
    }

    public static class CompilerExtensions
    {
        public static void CompileAndRun(this ICompiler compiler, CompilerInstructions instructions, params object[] constructorParameters)
        {
            var assembly = compiler.Compile(instructions.Code, instructions.Assemblies);
            var asstype = assembly.DefinedTypes.FirstOrDefault(a => a.Name == instructions.ClassName);
            var type = assembly.GetType(asstype.FullName);
            var obj= Activator.CreateInstance(type, constructorParameters);

            var objrun = type.GetMethod("Run");
            objrun.Invoke(obj, null);
        }
    }
}

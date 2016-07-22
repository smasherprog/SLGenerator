using Microsoft.CodeAnalysis;
using System.Reflection;

namespace MyCodeGenerator
{
    /// Indicates the object implements an object producer.
    public interface IProducer
    {
        /// Runs the producer.
        object Run();
    }

    /// Indicates the object implements a runnable script.
    public interface IScript
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
}

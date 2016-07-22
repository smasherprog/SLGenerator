using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
//Copied from https://keestalkstech.com/2016/05/how-to-add-dynamic-compilation-to-your-projects/
namespace MyCodeGenerator
{

    public class RoslynCompiler : ICompiler
    {
        public Assembly Compile(SyntaxTree code, params MetadataReference[] assemblyLocations)
        {
            Debug.WriteLine("Got here  IT!");
            var compilation = CSharpCompilation.Create(
                    "_" + Guid.NewGuid().ToString("D"),
                    new SyntaxTree[] { code },
                    assemblyLocations,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
             );

            using (var ms = new MemoryStream())
            {
                var compilationResult = compilation.Emit(ms);

                if (compilationResult.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    return Assembly.Load(ms.ToArray());
                }
                throw new RoslynCompilationException("Assembly could not be created.", compilationResult);
            }
        }

        public Assembly Compile(string code, params MetadataReference[] assemblyLocations)
        {
            return this.Compile(CSharpSyntaxTree.ParseText(code), assemblyLocations);
        }
        public Assembly Compile(string code, params string[] assemblyLocations)
        {
            return this.Compile(code, assemblyLocations.Select(l => MetadataReference.CreateFromFile(l)).ToArray());
        }
    }
    public class RoslynCompilationException : Exception
    {
        public EmitResult Result { get; private set; }
        public RoslynCompilationException(string message, EmitResult result) : base(message)
        {
            this.Result = result;
        }
    }
}

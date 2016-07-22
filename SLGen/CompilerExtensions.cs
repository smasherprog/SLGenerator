using System;

//Copied from https://keestalkstech.com/2016/05/how-to-add-dynamic-compilation-to-your-projects/
namespace MyCodeGenerator
{
    public static class CompilerExtensions
    {
        public static object CompileAndCreateObject(this ICompiler compiler, ICompilerInstructions instructions, params object[] constructorParameters)
        {
            var assembly = compiler.Compile(instructions.Code, instructions.AssemblyLocations);
            var type = assembly.GetType(instructions.ClassName);
            return Activator.CreateInstance(type, constructorParameters);
        }
        public static T CompileAndCreateObject<T>(this ICompiler compiler, ICompilerInstructions instructions, params object[] constructorParameters)
        {
            return (T)compiler.CompileAndCreateObject(instructions, constructorParameters);
        }
        public static object RunProducer(this ICompiler compiler, ICompilerInstructions instructions, params object[] constructorParameters)
        {
            var scriptObject = CompileAndCreateObject<IProducer>(compiler, instructions, constructorParameters);
            return scriptObject.Run();
        }
        public static void RunScript(this ICompiler compiler, ICompilerInstructions instructions, params object[] constructorParameters)
        {
            var scriptObject = CompileAndCreateObject<IScript>(compiler, instructions, constructorParameters);
            scriptObject.Run();
        }
    }
}

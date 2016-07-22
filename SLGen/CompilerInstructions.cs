using System.Collections.Generic;
//Copied from https://keestalkstech.com/2016/05/how-to-add-dynamic-compilation-to-your-projects/
namespace MyCodeGenerator
{
    public interface ICompilerInstructions
    {
        /// Gets the assembly locations.
        string[] AssemblyLocations { get; }

        /// Gets the code.
        string Code { get; }

        /// Gets the name of the class. It is used to get class out of th...
        string ClassName { get; }
    }

    /// Instructions that are used to compiler a piece of code. Used ...
    public class CompilerInstructions : ICompilerInstructions
    {
        /// Gets the assembly locations.
        public List<string> AssemblyLocations { get; } = new List<string>();

        /// Gets the name of the class. It is used to get class out of th...
        public string ClassName { get; set; }

        /// Gets the code.
        public string Code { get; set; }

        /// Gets the assembly locations.
        string[] ICompilerInstructions.AssemblyLocations
        {
            get { return AssemblyLocations.ToArray(); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SLGeneratorLib.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics;

namespace SLGeneratorLib.Service
{
    public class tsGenerator : SLDocumentWatcher
    {
        public tsGenerator()
        {

        }
        public override async void OnDocumentChanged(MergedProject current_project, Document d)
        {
            var sem = await d?.GetSemanticModelAsync();
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
                        var ptest1 = sem.GetDeclaredSymbol(item);
                        var genericnodes = item.DescendantNodes().OfType<GenericNameSyntax>());

                        streamwriter.WriteLine($"export class {ptest1.Name} " + "{");
                        foreach (PropertyDeclarationSyntax p in item.Members.Where(x => x.IsKind(SyntaxKind.PropertyDeclaration)))
                        {


                            var ptest = sem.GetDeclaredSymbol(p);

                            if (p.Modifiers.Any(a => a.ValueText == "public"))
                            {
                                streamwriter.WriteLine($"public {p.Identifier.ValueText}: {ToTypeScriptType(ptest)}");

                            }
                        }
                        streamwriter.WriteLine("}");
                    }
                }
                var pitem = _StartupProject.EnvDTE_Project.FindProjectItem(outfile);
                if (pitem == null) pitem = _StartupProject.EnvDTE_Project.ProjectItems.AddFromFile(outfile);
                if (pitem != null) Debug.WriteLine(pitem.Name);
            }
        }
        private string ToTypeScriptType(IPropertySymbol t)
        {
            var inamed = t as INamedTypeSymbol;


            switch (t.Type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return "boolean = false;";
                case SpecialType.System_String:
                case SpecialType.System_Char:
                    return "string = '';";
                case SpecialType.System_Byte:
                case SpecialType.System_Decimal:
                case SpecialType.System_Double:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_SByte:
                case SpecialType.System_Single:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_UIntPtr:
                    return "number = 0;";
                case SpecialType.System_DateTime:
                    return "date = new Date();";
                case SpecialType.System_Void:
                    return "void = void;";
            }
            if (t.Type.AllInterfaces.Any(a => a.Name == "IEnumerable"))
            {
                return "Array<any> = new Array<any>();";
            }
            return "any;";
        }

    }
}

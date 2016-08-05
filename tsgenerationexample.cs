using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLGeneratorLib.Model;
using Microsoft.CodeAnalysis;
using SLGeneratorLib;
using System.Diagnostics;

namespace ClassLibrary2
{
    public class SLGeneratorMain : ISLGeneratorMain
    {
        tsGenerator _tsGenerator;
        public SLGeneratorMain()
        {
            Debug.WriteLine("SLGeneratorMain()");
        }
        public void Init()
        {
            Debug.WriteLine("Init SLGeneratorMain");
            _tsGenerator = new tsGenerator();
            _tsGenerator.Init();

        }

        public void Dispose()
        {
            Debug.WriteLine("Disposing SLGeneratorMain");
            _tsGenerator?.Dispose();
        }
    }

    public class tsGenerator : SLGeneratorLib.Service.SLDocumentWatcher
    {
        public tsGenerator()
        {

        }

        public override IEnumerable<MergedProject> IncludeProjects()
        {
            return base.IncludeProjects().Where(a => a.CodeAnalysis_Project.Name != "ClassLibrary2");
        }

        public override async void OnDocumentChanged(MergedProject current_project, Microsoft.CodeAnalysis.Document d)
        {

            var tree = await d?.GetSyntaxTreeAsync();
            var root = await tree.GetRootAsync();
            var nodes = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().Where(a => a.Identifier.ValueText.EndsWith("ViewModel"));


            if (nodes.Any() && _StartupProject != null)
            {
                var sem = await d?.GetSemanticModelAsync();
                _DocumentsToWatch.Add(d.FilePath);

                var outfile = System.IO.Path.GetDirectoryName(_StartupProject.CodeAnalysis_Project.FilePath) + "\\" + System.IO.Path.GetFileNameWithoutExtension(d.Name) + ".ts";

                using (var fs = new System.IO.FileStream(outfile, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write))
                using (var streamwriter = new System.IO.StreamWriter(fs))
                {
                    fs.SetLength(0);
                    foreach (var item in nodes)
                    {
                        streamwriter.WriteLine($"export class {item.Identifier.ValueText} " + "{");
                        foreach (Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax p in item.Members.Where(x => x.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration)))
                        {


                            if (p.Modifiers.Any(a => a.ValueText == "public"))
                            {

                                var generictype = p.Type as Microsoft.CodeAnalysis.CSharp.Syntax.GenericNameSyntax;
                                if (generictype != null)
                                {
                                    var test = generictype.TypeArgumentList?.Arguments.FirstOrDefault();
                                    var type = sem.GetTypeInfo(test);
                                    var arrtype = $"Array<{GettsType(type)}>";
                                    streamwriter.WriteLine($"public {p.Identifier.ValueText}: {arrtype} = new {arrtype}();");
                                }
                                else
                                {
                                    var posgeneric1 = p.Type as Microsoft.CodeAnalysis.CSharp.Syntax.ArrayTypeSyntax;
                                    if (posgeneric1 != null)
                                    {
                                        var type = sem.GetTypeInfo(posgeneric1.ElementType);
                                        var arrtype = $"Array<{GettsType(type)}>";
                                        streamwriter.WriteLine($"public {p.Identifier.ValueText}: {arrtype} = new {arrtype}();");
                                    }
                                    else
                                    {
                                        var type = sem.GetTypeInfo(p.Type);
                                        streamwriter.WriteLine($"public {p.Identifier.ValueText}: {GettsType_w_Initializer(type)};");
                                    }
                                }
                            }
                        }
                        streamwriter.WriteLine("}");
                    }
                }
                var pitem = _StartupProject.EnvDTE_Project.FindProjectItem(outfile);
                if (pitem == null) pitem = _StartupProject.EnvDTE_Project.ProjectItems.AddFromFile(outfile);

            }
        }
        string GettsType(TypeInfo cstype)
        {
            switch (cstype.Type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return "boolean;";
                case SpecialType.System_String:
                case SpecialType.System_Char:
                    return "string";
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
                    return "number";
                case SpecialType.System_DateTime:
                    return "date";
                case SpecialType.System_Void:
                    return "void";
            }
            return "any";
        }
        string GettsType_w_Initializer(TypeInfo cstype)
        {

            switch (cstype.Type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return "boolean = false";
                case SpecialType.System_String:
                case SpecialType.System_Char:
                    return "string = ''";
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
                    return "number = 0";
                case SpecialType.System_DateTime:
                    return "date = new Date()";
                case SpecialType.System_Void:
                    return "void = void";
            }
            return "any";
        }
    }

}

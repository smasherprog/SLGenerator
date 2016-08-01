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
    public class tsGenerator: SLDocumentWatcher
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

                        streamwriter.WriteLine($"export class {ptest1.Name} " + "{");
                        foreach (PropertyDeclarationSyntax p in item.Members.Where(x => x.IsKind(SyntaxKind.PropertyDeclaration)))
                        {
                            var ptest = sem.GetDeclaredSymbol(p);

                            Debug.WriteLine(ptest.Type);
                            if(p.Modifiers.Any(a=> a.ValueText == "public"))
                            {
                                streamwriter.WriteLine($"public {p.Identifier} ");

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
    }
}

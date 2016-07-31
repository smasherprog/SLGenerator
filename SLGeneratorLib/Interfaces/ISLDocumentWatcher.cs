using Microsoft.CodeAnalysis;
using SLGeneratorLib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLGeneratorLib.Interfaces
{
    public interface ISLDocumentWatcher: IDisposable
    {
        void Init();
        IEnumerable<MergedProject> IncludeProjects();
        void OnDocumentChanged(MergedProject current_project, Document doc);
        void OnProjectsChanged();
    }
}

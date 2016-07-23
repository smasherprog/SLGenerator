//------------------------------------------------------------------------------
// <copyright file="SLGeneratorPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;

namespace SLGenerator
{
 
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(SLGeneratorPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class SLGeneratorPackage : Package
    {
        public const string PackageGuidString = "819e19b3-9fed-4ea1-bf37-08d6968ad863";

        SLGenerator.Builder _Builder;

        public SLGeneratorPackage()
        {
            Debug.WriteLine("SLGenPackage() Called");
        }
        protected override void Initialize()
        {
            Debug.WriteLine("Initialize Called");
            var componentmodel = (IComponentModel)this.GetService(typeof(SComponentModel));
            _Builder = new SLGenerator.Builder(componentmodel.GetService<VisualStudioWorkspace>(), Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2, GetService(typeof(SVsStatusbar)) as IVsStatusbar);

            base.Initialize();
        }
        protected override void Dispose(bool disposing)
        {
            _Builder.Dispose();
            base.Dispose(disposing);
        }
    }
}

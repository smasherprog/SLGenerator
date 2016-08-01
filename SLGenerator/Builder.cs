using System;
using System.Linq;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using SLGeneratorLib;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;

namespace SLGenerator
{
    public class Builder : IDisposable
    {
        private VisualStudioWorkspace _VisualStudioWorkspace = null;
        private ISLGeneratorMain _ISLGeneratorMain = null;

        private IVsGeneratorProgress _IVsGeneratorProgress;
        private string _FailedDllLoadMessage = "";

        public Builder(VisualStudioWorkspace worksp, IVsGeneratorProgress prop)
        {
            _VisualStudioWorkspace = worksp;
            AppDomain.CurrentDomain.AssemblyResolve += MyResolveEventHandler;
            _IVsGeneratorProgress = prop;
        }

        public void BuildMain(string filepath)
        {
            var docId = _VisualStudioWorkspace?.CurrentSolution?.GetDocumentIdsWithFilePath(filepath).FirstOrDefault();
            if (docId == null) return;
            var proj = _VisualStudioWorkspace?.CurrentSolution?.GetProject(docId.ProjectId);
            var compilation = proj?.GetCompilationAsync()?.Result;
           
            if (compilation == null) return;
            using (var ms = new MemoryStream())
            using (var ss = new MemoryStream())
            {
                var compilationResult = compilation.Emit(ms, ss);
                if (compilationResult.Success)
                {
                    try
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        ss.Seek(0, SeekOrigin.Begin);
                        var ass = Assembly.Load(ms.ToArray(), ss.ToArray());
                        var testtypes = ass.GetTypes();
                        var asstype = ass.DefinedTypes.FirstOrDefault(a => a.GetInterfaces().Any(b => b.Name == "ISLGeneratorMain"));

                        if (asstype != null)
                        {
                            var type = ass.GetType(asstype.FullName);

                            _ISLGeneratorMain?.Dispose();
                            _ISLGeneratorMain = null;
                            _ISLGeneratorMain = (ISLGeneratorMain)Activator.CreateInstance(type);
                            _ISLGeneratorMain?.Init();
                        } else
                        {
                            Log.Warn("ISLGeneratorMain not found in project.");
                        }

                    }
                    catch (Exception e)
                    {
                        if (!string.IsNullOrWhiteSpace(_FailedDllLoadMessage))
                        {
                            _IVsGeneratorProgress.GeneratorError(0, 0, _FailedDllLoadMessage, 0, 0);
                        }
                        else
                        {
                            _IVsGeneratorProgress.GeneratorError(0, 0, e.Message, 0, 0);
                        }
                    }
                }
            }
        }
        private Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                var osplits = ass.FullName.Split(',');
                var rsplits = args.Name.Split(',');
                if (osplits.Any() && rsplits.Any() && osplits[0] == rsplits[0])
                {//found a match possibly....
                    //split version name
                    var ovsplits = osplits[1].Split('=');
                    var rvsplits = rsplits[1].Split('=');
                    if (ovsplits.Count() >= 2 && rvsplits.Count() >= 2)
                    {
                        var oversion = ovsplits[1].Split('.');
                        var rversion = rvsplits[1].Split('.');

                        for (var i = 0; i < Math.Min(oversion.Length, rversion.Length); i++)
                        {
                            var l = oversion[i].FirstOrDefault();
                            var r = rversion[i].FirstOrDefault();

                            if (l > r)
                            {
                                _FailedDllLoadMessage = $"SLGenerator is using a newer version of the Nuget Package SLGeneratorLib. Please update your Nuget Package to {ovsplits[1]}";
                                SLGeneratorLib.Log.Error(_FailedDllLoadMessage);
                                return null;
                            }
                            else if (l < r)
                            {
                                _FailedDllLoadMessage = $"SLGenerator is using an older version of the Nuget Package SLGeneratorLib. Please update your SLGenerator Extension";
                                SLGeneratorLib.Log.Error(_FailedDllLoadMessage);
                                return null;
                            }
                        }
                        return ass;
                    }
                    else return null;
                }
            }
            return null;
        }

        public void Dispose()
        {
            _ISLGeneratorMain?.Dispose();
            _ISLGeneratorMain = null;
            AppDomain.CurrentDomain.AssemblyResolve -= MyResolveEventHandler;
        }
    }

}

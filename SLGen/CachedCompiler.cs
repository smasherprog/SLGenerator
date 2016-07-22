using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Diagnostics;

//Copied from https://keestalkstech.com/2016/05/how-to-add-dynamic-compilation-to-your-projects/
namespace MyCodeGenerator
{
    public class CachedCompiler : ICompiler
    {
        private readonly ConcurrentDictionary<string, Assembly> cache = new ConcurrentDictionary<string, Assembly>();
        private readonly ICompiler compiler;

        public CachedCompiler(ICompiler compiler)
        {
            if (compiler == null)
            {
                throw new ArgumentNullException(nameof(compiler));
            }

            this.compiler = compiler;
        }
        public Assembly Compile(SyntaxTree code, params MetadataReference[] assemblyLocations)
        {
            Debug.WriteLine("Got here  IT!22222");
            string key = GetCacheKey(code.ToString(), assemblyLocations.Select(a => a.Properties.ToString()).ToArray());
            return cache.GetOrAdd(key, (k) =>
            {
                Debug.WriteLine("Got here  IT!1111");
                return compiler.Compile(code, assemblyLocations);
            });
        }
        public Assembly Compile(string code, params MetadataReference[] assemblyLocations)
        {
            string key = GetCacheKey(code.ToString(), assemblyLocations.Select(a => a.Properties.ToString()).ToArray());
            return cache.GetOrAdd(key, (k) =>
            {
                return compiler.Compile(code, assemblyLocations);
            });
        }

        public Assembly Compile(string code, params string[] assemblyLocations)
        {
            string key = GetCacheKey(code, assemblyLocations);

            return cache.GetOrAdd(key, (k) =>
            {
                return compiler.Compile(code, assemblyLocations);
            });
        }
        private string GetCacheKey(string code, string[] assemblyLocations)
        {
            string key = String.Join("|", code, assemblyLocations);
            return key;
        }
    }
}

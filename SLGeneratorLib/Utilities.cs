using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLGeneratorLib
{
  
    static public class Utilities
    {
        public static List<Model.MergedProject> JoinProjects(IEnumerable<Microsoft.CodeAnalysis.Project> projs, List<EnvDTE.Project> oprojs)
        {
            var p = oprojs.ToList();//take a copy
            var np = new List<Model.MergedProject>();
            foreach (var item in projs)
            {
                var f = p.FirstOrDefault(a => item.FilePath.EndsWith(a.UniqueName));
                if (f != null)
                {
                    p.Remove(f);
                    np.Add(new Model.MergedProject {  CodeAnalysis_Project = item,  EnvDTE_Project = f });
                }
            }

            return np;
        }

        private static List<EnvDTE.Project> GetSolutionFolderProjects(EnvDTE.Project solutionFolder)
        {
            var list = new List<EnvDTE.Project>();

            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (subProject.Kind == EnvDTE80.ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    list.Add(subProject);
                }
            }

            return list;
        }

        public static List<EnvDTE.Project> GetProjects(EnvDTE.Projects projects)
        {
            var list = new List<EnvDTE.Project>();
            var item = projects.GetEnumerator();
            while (item.MoveNext())
            {
                var project = item.Current as EnvDTE.Project;
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == EnvDTE80.ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    list.Add(project);
                }
            }

            return list;
        }
        public static EnvDTE.ProjectItem FindProjectItem(this EnvDTE.Project pro, string path)
        {
            foreach (EnvDTE.ProjectItem item in pro.ProjectItems)
            {
                try
                {
                    if (item.Document.FullName.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return item;
                    }
                }
                catch
                {
                    // Can't read properties from project item sometimes when deleting miltiple files
                }
            }

            return null;
        }

    }
}

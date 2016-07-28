using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLGenerator
{
  
    static public class Utilities
    {
        public static List<Tuple<Microsoft.CodeAnalysis.Project, EnvDTE.Project>> JoinProjects(IEnumerable<Microsoft.CodeAnalysis.Project> projs, List<EnvDTE.Project> oprojs)
        {
            var p = oprojs.ToList();//take a copy
            var np = new List<Tuple<Microsoft.CodeAnalysis.Project, EnvDTE.Project>>();
            foreach (var item in projs)
            {
                var f = p.FirstOrDefault(a => item.FilePath.EndsWith(a.UniqueName));
                if (f != null)
                {
                    p.Remove(f);
                    np.Add(Tuple.Create( item, f));
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
    }
}

namespace SLGeneratorLib.Model
{
    public class MergedProject
    {
        public Microsoft.CodeAnalysis.Project CodeAnalysis_Project { get; set; }
        public EnvDTE.Project EnvDTE_Project { get; set; }
    }
}

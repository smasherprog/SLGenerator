using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SLGenerator
{
    [ComVisible(true)]
    [Guid("813FF3BC-A2CB-488B-9F23-12C873AA1456")]
    [CodeGeneratorRegistration(typeof(SLCodeGenerator), "C# SL Generator", "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}", GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(SLCodeGenerator), "VB SL Generator", "{164B10B9-B200-11D0-8C61-00A0C91E29D5}", GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(SLCodeGenerator), "J# SL Generator", "{E6FDF8B0-F3D1-11D4-8576-0002A516ECE8}", GeneratesDesignTimeSource = true)]
    [ProvideObject(typeof(SLCodeGenerator))]
    public class SLCodeGenerator : IVsSingleFileGenerator
    {
        SLGenerator.Builder _Builder;
        public SLCodeGenerator()
        {
            Debug.WriteLine("SLCodeGenerator() Called");
            var componentmodel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            _Builder = new SLGenerator.Builder(componentmodel.GetService<VisualStudioWorkspace>(), Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2, Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar);

        }

        public int DefaultExtension(out string pbstrDefaultExtension)
        {
            pbstrDefaultExtension = ".cs";
            return 0;//also S_OK
        }

        public int Generate(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace, IntPtr[] rgbOutputFileContents, out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            using (var writer = new StringWriter(new StringBuilder()))
            {
                Encoding enc = Encoding.GetEncoding(writer.Encoding.WindowsCodePage);
                _Builder.BuildMain(bstrInputFileContents, wszInputFilePath, writer);

                //Get the preamble (byte-order mark) for our encoding
                byte[] preamble = enc.GetPreamble();
                int preambleLength = preamble.Length;

                //Convert the writer contents to a byte array
                byte[] body = enc.GetBytes(writer.ToString());

                //Prepend the preamble to body (store result in resized preamble array)
                Array.Resize<byte>(ref preamble, preambleLength + body.Length);
                Array.Copy(body, 0, preamble, preambleLength, body.Length);

                //Return the combined byte array
                int outputLength = preamble.Length;
                rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(outputLength);
                Marshal.Copy(preamble, 0, rgbOutputFileContents[0], outputLength);

                pcbOutput = (uint)outputLength;
                return 0;

            }

           
        }
    }
}

using System;
using System.Linq;
using EnvDTE;

namespace SLGeneratorLib
{
    public class Log
    {
        private static readonly Lazy<Log> lazy = new Lazy<Log>(() => new Log());

        public static Log instance { get { return lazy.Value; } }

        private readonly EnvDTE80.DTE2 _DTE2;
        private OutputWindowPane outputWindowPane;

        Log()
        {
            _DTE2 = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
        }


        public static void Debug(string message, params object[] parameters)
        {
#if DEBUG
            instance?.Write("DEBUG", message, parameters);
#endif
        }

        public static void Info(string message, params object[] parameters)
        {
            instance?.Write("INFO", message, parameters);
        }

        public static void Warn(string message, params object[] parameters)
        {
            instance?.Write("WARNING", message, parameters);
        }

        public static void Error(string message, params object[] parameters)
        {
            instance?.Write("ERROR", message, parameters);
            instance?.OutputWindow?.Activate();
        }

        private void Write(string type, string message, object[] parameters)
        {
            message = $"{DateTime.Now:HH:mm:ss.fff} {type}: {message}";

            try
            {
                if (parameters.Any())
                    OutputWindow.OutputString(string.Format(message, parameters) + Environment.NewLine);
                else
                    OutputWindow.OutputString(message + Environment.NewLine);
            }
            catch { }
    
        }

        private OutputWindowPane OutputWindow
        {
            get
            {
                if (outputWindowPane != null) return outputWindowPane;
                var panes = _DTE2.ToolWindows.OutputWindow.OutputWindowPanes;

                try
                {
                    outputWindowPane = panes.Item("SLGenerator");
                }
                catch (ArgumentException)
                {
                    // Create a new pane and write to it.
                    outputWindowPane = panes.Add("SLGenerator");
                }
                return outputWindowPane;
            }
        }
    }
}
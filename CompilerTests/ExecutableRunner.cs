
namespace CompilerTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    internal static class ExecutableRunner
    {
        /// <summary>
        /// http://blogs.msdn.com/b/ericwhite/archive/2008/08/07/running-an-executable-and-collecting-the-output.aspx
        /// </summary>
        /// <returns></returns>
        public static RunResults RunExecutable(string executablePath, string arguments = "", string workingDirectory = "")
        {
            var runResults = new RunResults
            {
                Output = new StringBuilder(),
                Error = new StringBuilder(),
                RunException = null
            };

            try
            {
                if (File.Exists(executablePath))
                {
                    using (Process proc = new Process())
                    {
                        proc.StartInfo.FileName = executablePath;
                        proc.StartInfo.Arguments = arguments;
                        proc.StartInfo.WorkingDirectory = workingDirectory;
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.RedirectStandardOutput = true;
                        proc.StartInfo.RedirectStandardError = true;
                        proc.OutputDataReceived += (o, e) => runResults.Output.Append(e.Data).Append(Environment.NewLine);
                        proc.ErrorDataReceived += (o, e) => runResults.Error.Append(e.Data).Append(Environment.NewLine);
                        proc.Start();
                        proc.BeginOutputReadLine();
                        proc.BeginErrorReadLine();
                        proc.WaitForExit();
                        runResults.ExitCode = proc.ExitCode;
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid executable path.", "executablePath");
                }
            }
            catch (Exception e)
            {
                runResults.RunException = e;
            }

            return runResults;
        }
    }
}

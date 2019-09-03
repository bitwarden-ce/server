using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Bit.Setup
{
    public static class Helpers
    {
        public static string GetValueFromEnvFile(string envFile, string key)
        {
            if(!File.Exists($"/bitwarden/env/{envFile}.override.env"))
            {
                return null;
            }

            var lines = File.ReadAllLines($"/bitwarden/env/{envFile}.override.env");
            foreach(var line in lines)
            {
                if(line.StartsWith($"{key}="))
                {
                    return line.Split(new char[] { '=' }, 2)[1].Trim('"');
                }
            }

            return null;
        }

        public static string Exec(string cmd, bool returnStdout = false)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var escapedArgs = cmd.Replace("\"", "\\\"");
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{escapedArgs}\"";
            }
            else
            {
                process.StartInfo.FileName = "powershell";
                process.StartInfo.Arguments = cmd;
            }

            process.Start();
            var result = returnStdout ? process.StandardOutput.ReadToEnd() : null;
            process.WaitForExit();
            return result;
        }

        public static string ReadInput(string prompt)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("(!) ");
            Console.ResetColor();
            Console.Write(prompt);
            if(prompt.EndsWith("?"))
            {
                Console.Write(" (y/n)");
            }
            Console.Write(": ");
            var input = Console.ReadLine();
            Console.WriteLine();
            return input;
        }

        public static bool ReadQuestion(string prompt)
        {
            var input = ReadInput(prompt).ToLowerInvariant().Trim();
            return input == "y" || input == "yes";
        }

        public static void ShowBanner(Context context, string title, string message, ConsoleColor? color = null)
        {
            if(!context.PrintToScreen())
            {
                return;
            }
            if(color != null)
            {
                Console.ForegroundColor = color.Value;
            }
            Console.WriteLine($"!!!!!!!!!! {title} !!!!!!!!!!");
            Console.WriteLine(message);
            Console.WriteLine();
            Console.ResetColor();
        }

        public static Func<object, string> ReadTemplate(string templateName)
        {
            var assembly = typeof(Helpers).GetTypeInfo().Assembly;
            var fullTemplateName = $"Bit.Setup.Templates.{templateName}.hbs";
            if(!assembly.GetManifestResourceNames().Any(f => f == fullTemplateName))
            {
                return null;
            }
            using(var s = assembly.GetManifestResourceStream(fullTemplateName))
            using(var sr = new StreamReader(s))
            {
                var templateText = sr.ReadToEnd();
                return HandlebarsDotNet.Handlebars.Compile(templateText);
            }
        }

        public static void WriteLine(Context context, string format = null, object arg0 = null, object arg1 = null,
            object arg2 = null)
        {
            if(!context.PrintToScreen())
            {
                return;
            }
            if(format != null && arg0 != null && arg1 != null && arg2 != null)
            {
                Console.WriteLine(format, arg0, arg1, arg2);
            }
            else if(format != null && arg0 != null && arg1 != null)
            {
                Console.WriteLine(format, arg0, arg1);
            }
            else if(format != null && arg0 != null)
            {
                Console.WriteLine(format, arg0);
            }
            else if(format != null)
            {
                Console.WriteLine(format);
            }
            else
            {
                Console.WriteLine();
            }
        }
    }
}

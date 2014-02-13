/*--------------------------------------------------------------------------

The MIT License (MIT)

Copyright (c) 2014 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

---------------------------------------------------------------------------*/

using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Reactor.Scripting
{
    /// <summary>
    /// Simple scripting utility.
    /// </summary>
    class Program
    {
        static List<Assembly> Assemblies { get; set; }

        static void PrepareBinDirectory()
        {
            string bin_directory = Directory.GetCurrentDirectory() + "\\bin";

            if (!Directory.Exists(bin_directory))
            {
                Directory.CreateDirectory(bin_directory);
            }
        }

        static void CleanBinDirectory() 
        {
            string temp = Directory.GetCurrentDirectory() + "\\bin\\Temp.dll";

            if (File.Exists(temp))
            {
                File.Delete(temp);
            }        
        }

        static void LoadAssemblies()
        {
            Assemblies = new List<Assembly>();

            var directory = Directory.GetCurrentDirectory() + "\\bin\\";
            
            if (Directory.Exists(directory)) {

                foreach (var filename in  Directory.GetFiles(directory))  {

                    var fileinfo = new FileInfo(filename);

                    if (fileinfo.Extension == ".dll") {

                        var assembly = Assembly.LoadFrom(filename);

                        Assemblies.Add(assembly);
                    }
                }
            }
        }

        static Assembly Compile()
        {
            //-------------------------------------------------
            // configure the provider.
            //-------------------------------------------------

            Dictionary<string, string> providerOptions = new Dictionary<string, string>();

            providerOptions.Add("CompilerVersion", "v3.5");

            var provider = new CSharpCodeProvider(providerOptions);

            CompilerParameters compilerParameters = new CompilerParameters();

            compilerParameters.GenerateExecutable = false;

            compilerParameters.GenerateInMemory   = true;

            compilerParameters.OutputAssembly     = string.Format("bin/Temp.dll");

            //-------------------------------------------------
            // load in assemblies.
            //-------------------------------------------------

            compilerParameters.ReferencedAssemblies.Add("System.dll");

            var bin_directory = Directory.GetCurrentDirectory() + "\\bin\\";

            if (Directory.Exists(bin_directory)) {

                foreach (var filename in  Directory.GetFiles(bin_directory)) {

                    FileInfo fileinfo = new FileInfo(filename);

                    if (fileinfo.Extension == ".dll") {

                        compilerParameters.ReferencedAssemblies.Add(filename);
                    }
                }
            }

            //-------------------------------------------------
            // gather source files
            //-------------------------------------------------

            var src_directory = Directory.GetCurrentDirectory();

            var filenames = new List<string>();

            foreach (var filename in Directory.GetFiles(src_directory)) {

                FileInfo fileinfo = new FileInfo(filename);

                if (fileinfo.Extension == ".cs") {

                    filenames.Add(filename);
                }
            }

            //-------------------------------------------------
            // compile
            //-------------------------------------------------

            CompilerResults result = provider.CompileAssemblyFromFile(compilerParameters, filenames.ToArray());

            //CompilerResults result = provider.CompileAssemblyFromSource(compilerParameters, File.ReadAllText(Directory.GetCurrentDirectory() + "/app.cs"));

            if (result.Errors.HasErrors)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine("error: " + error);
                }

                return null;
            }

            if (result.Errors.HasWarnings)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine("warning: " + error);
                }
            }

            return result.CompiledAssembly;
        }

        static Type GetProgram(Assembly assembly)
        {
            if (assembly != null) {

                foreach (var type in assembly.GetTypes()) {

                    if (type.Name == "Program") {

                        return type;
                    }
                }
            }

            return null;                    
        }

        static MethodInfo GetMain(Type program) {

            //---------------------------------------------
            // look for private static
            //---------------------------------------------

            foreach (var method in program.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)) {
                
                if (method.Name == "Main") {

                    return method;
                }
            }
            //---------------------------------------------
            // look for public static
            //---------------------------------------------
            foreach (var method in program.GetMethods()) {

                if (method.Name == "Main") {

                    return method;
                }
            }

            return null;
        }

        static void Run(Assembly assembly, string [] arguments)
        {
            Type program = GetProgram(assembly);

            if(program != null) {

                MethodInfo method = GetMain(program);

                if(method != null) {

                    var parameters = method.GetParameters();

                    if(parameters.Length > 0) {

                        if(parameters[0].ParameterType.FullName == "System.String[]") 
                        {
                            try {

                                method.Invoke(program, new object[1] { arguments });
                            }
                            catch(Exception e) {

                                Console.WriteLine(e);
                            }
                        }
                        else 
                        {
                            Console.WriteLine("Program.Main() has a invalid signature");
                        }
                    }
                    else
                    {
                        try {

                            method.Invoke(program, new object[0]);
                        }
                        catch(Exception e) {

                            Console.WriteLine(e);
                        }
                    }
                }
            }
        }

 
        static void Main(string[] args)
        {
            //----------------------------------------------
            // ensure we have a bin directory.
            //----------------------------------------------

            PrepareBinDirectory();

            //----------------------------------------------
            // load assemblies
            //----------------------------------------------

            CleanBinDirectory();

            //----------------------------------------------
            // load assemblies
            //----------------------------------------------
          
            LoadAssemblies();

            //----------------------------------------------
            // setup assembly resolver
            //----------------------------------------------

            AppDomain.CurrentDomain.AssemblyResolve += (sender, argument) =>
            {

                foreach (var assembly in Assemblies)
                {
                    if (assembly.FullName == argument.Name)
                    {
                        return assembly;
                    }
                }

                return null;
            };

            //----------------------------------------------
            //  execute code
            //----------------------------------------------

            Run(  Compile(), args);
        }

    }
}

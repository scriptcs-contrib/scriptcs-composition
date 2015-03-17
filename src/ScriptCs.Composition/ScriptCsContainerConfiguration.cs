using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac.Core;
using ICSharpCode.NRefactory.CSharp;
using ScriptCs.Contracts;
using ScriptCs.Engine.Mono;
using ScriptCs.Engine.Roslyn;
using ScriptCs.Hosting;

namespace ScriptCs.Composition
{
    public class ScriptCsContainerConfiguration : ContainerConfiguration
    {
        private IFileSystem _fs = new FileSystem();
        private IList<string> _scripts = new List<string>();
        private Type _engine;

        public ScriptCsContainerConfiguration(IFileSystem fs)
        {
            _fs = fs;
        }

        public ScriptCsContainerConfiguration() : this(new FileSystem())
        {
            
        }

        public ScriptCsContainerConfiguration WithScriptEngine<T>() where T : IScriptEngine
        {
            _engine = typeof (T);
            return this;
        }

        public ScriptCsContainerConfiguration WithScript(string scriptPath)
        {
            if (!_fs.FileExists(scriptPath))
            {
               throw new FileNotFoundException(string.Format("Script:{0} does not exist", scriptPath), scriptPath); 
            }
            _scripts.Add(scriptPath);

            return this;
        }

        public ScriptCsContainerConfiguration Compile(string workingDirectory)
        {
            var engine = _engine;
            
            if (engine == null)
            {
                if (Type.GetType("Mono.Runtime") !=null)
                {
                    engine = typeof(MonoScriptEngine);
                }
                else
                {
                    engine = typeof(RoslynScriptEngine);
                }
            }
            var builder = CreateScriptServicesBuilder(engine, false);
            var services = builder.Build();
            var assemblies = services.AssemblyResolver.GetAssemblyPaths(workingDirectory,true);
            var loader = GetLoader();
            var packs = services.ScriptPackResolver.GetPacks();
            
            services.Executor.Initialize(assemblies, packs);
            services.Executor.AddReferences(typeof(System.Attribute), typeof(ExportAttribute));
            services.Executor.AddReferences("System.Runtime");
            services.Executor.ImportNamespaces("System.Composition");
            
            var result = services.Executor.ExecuteScript(loader);

            if (result.CompileExceptionInfo != null)
            {
                result.CompileExceptionInfo.Throw();
            }

            if (result.ExecuteExceptionInfo != null)
            {
                result.ExecuteExceptionInfo.Throw();
            }
            
            var marker = result.ReturnValue as Type;

            if (marker != null)
            {
                WithAssembly(marker.Assembly);
            }
            return this;

        }

        private string GetLoader()
        {
            var builder = new StringBuilder();
            foreach (var script in _scripts)
            {
                builder.AppendFormat("#load {0}{1}", script, Environment.NewLine);
            }

            if (builder.Length != 0)
            {
                builder.AppendLine("public class Marker {}");
                builder.AppendLine("typeof(Marker)");
            }

            return builder.ToString();
        }

        private ScriptServicesBuilder CreateScriptServicesBuilder(Type engine, bool useLogging)
        {
            var console = new ScriptConsole();
            var configurator = new LoggerConfigurator(useLogging ? LogLevel.Debug : LogLevel.Info);
            configurator.Configure(console);
            var logger = configurator.GetLogger();
            var scriptServicesBuilder = new ScriptServicesBuilder(console, logger);
            scriptServicesBuilder.Overrides[typeof (IScriptEngine)] = engine;

            return scriptServicesBuilder;
        }
    }
}

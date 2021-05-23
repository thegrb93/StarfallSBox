using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;

namespace Starfall
{
	public class Player { } public class Entity { } public class InstanceHook { },  public class UserHook { }

	public class StarfallCompileException : Exception
	{
		public CompilerErrorCollection errors;
		public StarfallCompileException(string message, CompilerErrorCollection errors) : base(message)
        {
			this.errors = errors;
		}
	}

	public class Instance
    {
        public static List<Instance> activeInstances = new List<Instance>();
        public static Dictionary<Player, List<Instance>> playerInstances = new Dictionary<Player, List<Instance>>();

		StarfallData data_;
		Player player_;
		Entity entity_;

		Dictionary<string, List<InstanceHook>> hooks = new Dictionary<string, List<InstanceHook>>();
		Dictionary<string, List<UserHook>> userhooks = new Dictionary<string, List<UserHook>>();


		public Instance(StarfallData data, Player player, Entity entity)
		{
			data_ = data;
			player_ = player;
			entity_ = entity;
		}

		public bool Compile()
		{
			CSharpCodeProvider codeProvider = new CSharpCodeProvider();
			CompilerParameters compileParam = new CompilerParameters();
            compileParam.GenerateExecutable = false;
            compileParam.OutputAssembly = "SFScript";

			string[] files = new string[data_.files.Count];
			data_.files.Values.CopyTo(files, 0);

			CompilerResults results = codeProvider.CompileAssemblyFromSource(compileParam, files);
			if (results.Errors.Count > 0)
			{
				throw new StarfallCompileException("Compilation failed", results.Errors);
			}

            Assembly asm = results.CompiledAssembly;
            Type t = asm.GetType("SF.Main");
            if (t == null)
            {
                throw new StarfallCompileException("Missing SF.Main class", results.Errors);
            }

            MethodInfo methodInfo = t.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
            if (methodInfo == null)
            {
                throw new StarfallCompileException("Failed to find Main method", results.Errors);
            }
            methodInfo.Invoke(null, null);

            return true;
		}

		int stackn = 0;
		public void runWithOps()
        {

        }
    }
}

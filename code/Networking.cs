using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Starfall
{
	public class StarfallData
	{
		public class PreprocessDirectives
		{
			static readonly Regex directivesRgx = new Regex("--@\\s*(\\w+)([^\\n]*)");
			static readonly var directiveActions = new Dictionary<string, Action<PreprocessDirectives, string>>(){
				{"include", (PreprocessDirectives directives, string value) => {
					directives.includes.Add(value);
				}},
				{"includedir", (PreprocessDirectives directives, string value) => {
					directives.includedirs.Add(value);
				}},
				{"name", (PreprocessDirectives directives, string value) => {
					directives.name = value;
				}},
				{"author", (PreprocessDirectives directives, string value) => {
					directives.author = value;
				}},
				{"model", (PreprocessDirectives directives, string value) => {
					directives.model = value;
				}},
				{"clientmain", (PreprocessDirectives directives, string value) => {
					directives.scriptclientmain = value;
				}},
				{"superuser", (PreprocessDirectives directives, string value) => {
					directives.superuser = true;
				}},
				{"server", (PreprocessDirectives directives, string value) => {
					directives.realm = Host.Server;
				}},
				{"shared", (PreprocessDirectives directives, string value) => {
					directives.realm = Host.Shared;
				}},
				{"client", (PreprocessDirectives directives, string value) => {
					directives.realm = Host.Client;
				}},
			};
			
			public List<string> includes = new List<string>();
			public List<string> includedirs = new List<string>();
			public string scriptname;
			public string scriptauthor;
			public string scriptmodel;
			public string scriptclientmain;
			public Host realm = Host.Shared;
			public bool superuser = false;

			public PreprocessDirectives(string code)
			{
				foreach(Match match in directivesRgx.matches(code))
				{
					string directive = match.Groups[1];
					string value = match.Groups[2].Trim();
					try
					{
						directiveActions[directive](this, value);
					}
					catch (KeyNotFoundException)
					{
						throw new Exception( "Invalid directive: " + directive );
					}
				}
			}
		}
		public class SFFile
		{
			string filename;
			string code;
			PreprocessDirectives directives;
			public SFFile(string filename)
			{
				SFFile(filename, FileSystem.Mounted.ReadAllBytes(filename));
			}
			public SFFile(string filename, string code)
			{
				this.filename = filename;
				this.code = code;
				this.directives = new PreprocessDirectives(code);
			}
		}
		
		public string mainfile;
		public Dictionary<string, SFFile> files;
		public StarfallData( Dictionary<string, SFFile> files, string mainfile )
		{
			if ( !files.ContainsKey( mainfile ) )
			{
				throw new Exception( "Mainfile is missing from files: " + files + " (" + mainfile + ")" );
			}
			this.files = files;
			this.mainfile = mainfile;
		}
	}

	class Networking
	{
		public static StarfallData CollectFiles(string mainfile)
		{
			Dictionary<string, SFFile> files = new Dictionary<string, SFFile>();
			HashSet<string> directoriesLoaded = new HashSet<string>();
			Stack<string> filesToLoad = new Stack(){mainfile};
			Stack<string> directoriesToLoad = new Stack();

			while(!filesToLoad.isEmpty())
			{
				string filename = filesToLoad.Pop();
				if(!files.Contains(filename))
				{
					try
					{
						SFFile file = new SFFile(filename);
						file.directives.includes.ForEach((string n) => filesToLoad.Push(n));
						file.directives.includedirs.ForEach((string n) => directoriesToLoad.Push(n));
						files[filename] = file;
					}
					catch(Exception e)
					{
						e.Message = "Failed to load file: "+filename+" ("+e.Message+")";
						throw e;
					}
				}

				while(!directoriesToLoad.isEmpty())
				{
					string dir = directoriesToLoad.Pop();
					if(directoriesLoaded.Add(dir))
					{
						// For files in dir, add to stack
					}
				}
				
			}
			return new StarfallData(files, mainfile);
		}
	}
}

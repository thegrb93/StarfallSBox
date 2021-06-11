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
		public class SFFile()
		{
			string code;
			PreprocessDirectives directives;
			public SFFile(string code)
			{
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
	}
}

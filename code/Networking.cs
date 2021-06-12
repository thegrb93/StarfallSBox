using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sandbox;

namespace Starfall
{
	public class PreprocessDirectives
	{
		static readonly Regex directivesRgx = new Regex( "--@\\s*(\\w+)([^\\n]*)" );
		static readonly Dictionary<string, Action<PreprocessDirectives, string>> directiveActions = new Dictionary<string, Action<PreprocessDirectives, string>>(){
			{"include", (PreprocessDirectives directives, string value) => {
				directives.includes.Add(value);
			}},
			{"includedir", (PreprocessDirectives directives, string value) => {
				directives.includedirs.Add(value);
			}},
			{"name", (PreprocessDirectives directives, string value) => {
				directives.scriptname = value;
			}},
			{"author", (PreprocessDirectives directives, string value) => {
				directives.scriptauthor = value;
			}},
			{"model", (PreprocessDirectives directives, string value) => {
				directives.scriptmodel = value;
			}},
			{"clientmain", (PreprocessDirectives directives, string value) => {
				directives.scriptclientmain = value;
			}},
			{"superuser", (PreprocessDirectives directives, string value) => {
				directives.superuser = true;
			}},
			{"server", (PreprocessDirectives directives, string value) => {
				directives.realm = "Server";
			}},
			{"shared", (PreprocessDirectives directives, string value) => {
				directives.realm = "Shared";
			}},
			{"client", (PreprocessDirectives directives, string value) => {
				directives.realm = "Client";
			}},
		};

		public List<string> includes = new List<string>();
		public List<string> includedirs = new List<string>();
		public string scriptname;
		public string scriptauthor;
		public string scriptmodel;
		public string scriptclientmain;
		public string realm;
		public bool superuser = false;

		public PreprocessDirectives( string code )
		{
			foreach ( Match match in directivesRgx.Matches( code ) )
			{
				string directive = match.Groups[1].Value;
				string value = match.Groups[2].Value.Trim();
				try
				{
					directiveActions[directive]( this, value );
				}
				catch ( KeyNotFoundException )
				{
					throw new Exception( "Invalid directive: " + directive );
				}
			}
		}
	}
	public class SFFile : NetworkComponent
	{
		[Net] public string filename;
		[Net] public string code;
		public PreprocessDirectives directives;
		public SFFile( string filename ) : this( filename, FileSystem.Mounted.ReadAllText( filename ) )
		{
		}
		public SFFile( string filename, string code )
		{
			this.filename = filename;
			this.code = code;
			this.directives = new PreprocessDirectives( code );
		}
	}

	class Networking
	{
		public static List<SFFile> CollectFiles( string mainfile )
		{
			List<SFFile> files = new List<SFFile>();
			HashSet<string> filesLoaded = new HashSet<string>();
			HashSet<string> directoriesLoaded = new HashSet<string>();
			Stack<string> filesToLoad = new Stack<string>();
			filesToLoad.Push( mainfile );
			Stack<string> directoriesToLoad = new Stack<string>();

			while ( filesToLoad.Count > 0 )
			{
				string filename = filesToLoad.Pop();
				if ( filesLoaded.Add( filename ) )
				{
					try
					{
						SFFile file = new SFFile( filename );
						file.directives.includes.ForEach( ( string n ) => filesToLoad.Push( n ) );
						file.directives.includedirs.ForEach( ( string n ) => directoriesToLoad.Push( n ) );
						files.Add( file );
					}
					catch ( Exception e )
					{
						throw new Exception( "Failed to load file: " + filename + " (" + e.Message + ")", e );
					}
				}

				while ( directoriesToLoad.Count > 0 )
				{
					string dir = directoriesToLoad.Pop();
					if ( directoriesLoaded.Add( dir ) )
					{
						// For files in dir, add to stack
					}
				}

			}
			return files;
		}
	}
}

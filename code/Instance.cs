using System;
using System.Collections.Generic;
using Sandbox;
using KopiLua;
using System.Diagnostics;

namespace Starfall
{
	[System.AttributeUsage( System.AttributeTargets.Method )]
	public class SFInitializeAttribute : System.Attribute
	{
		int realm;
		public SFInitializeAttribute( int realm )
		{
			this.realm = realm;
		}
	}

	public class InstanceHook { }
	public class UserHook { }

	public class StarfallException : Exception
	{
		public string luaStackTrace;
		public StarfallException( string message, string luaStackTrace ) : base( message ) { this.luaStackTrace = luaStackTrace; }
	}

	public partial class Instance
	{
		public double cpuQuota = 0.005;
		public double cpuQuotaRatio = 0.01;
		public bool cpuMonitor = true;
		public int ramMax = 500000; // 500MB

		[ServerCmd("sf_test")]
		static void test_sf(string code)
		{
			try
			{
				if ( code is null ) code = "";
				new Instance( null, null, new List<SFFile> { new SFFile( "test", code ) } ).Compile();
			}
			catch( StarfallException m)
			{
				Log.Warning( m.Message );
			}
		}

		public static List<Instance> activeInstances = new List<Instance>();
		public static Dictionary<Player, List<Instance>> playerInstances = new Dictionary<Player, List<Instance>>();

		List<SFFile> files;
		Player player;
		Entity entity;
		Lua.lua_State L;

		Dictionary<string, List<InstanceHook>> hooks = new Dictionary<string, List<InstanceHook>>();
		Dictionary<string, List<UserHook>> userhooks = new Dictionary<string, List<UserHook>>();

		public Instance( Player player, Entity entity, List<SFFile> files )
		{
			this.files = files;
			this.player = player;
			this.entity = entity;
		}

		public void Compile()
		{
			L = Lua.lua_open();
			Lua.lua_gc( L, Lua.LUA_GCSTOP, 0 );
			Lua.luaL_openlibs( L );
			Lua.lua_gc( L, Lua.LUA_GCRESTART, 0 );

			Lua.lua_sethook( L, ( Lua.lua_State L, Lua.lua_Debug ar ) => { this.cpuTimeCheck(); }, Lua.LUA_MASKCOUNT, 200 );

			// Compile all the files and store in _G.scripts
			Lua.lua_createtable( L, 0, files.Count );
			foreach ( SFFile file in files )
			{
				switch ( file.directives.realm )
				{
					case "Server":
						if ( !Host.IsServer ) continue;
						break;
					case "Client":
						if ( !Host.IsClient ) continue;
						break;
				}

				Lua.lua_pushstring( L, file.filename );
				if ( Lua.luaL_loadbuffer( L, file.code, "SF: " + file.filename ) != 0 )
				{
					string err = Lua.lua_tostring( L, -1 );
					if(err is null)
						err = "Error not a string";
					Lua.lua_settop( L, 0 ); // Clear the stack

					throw new StarfallException( err, "" );
				}
				Lua.lua_settable( L, 1 );
			}
			Lua.lua_setglobal( L, "_SCRIPTS" );

			Lua.lua_pushcfunction( L, ( Lua.lua_State L ) => { Lua.luaL_getmetatable( L, Lua.luaL_checkstring( L, 1 ) ); return 1; } );
			Lua.lua_setglobal( L, "getMetatable" );

			SFFile main = files[0];
			if ( Host.IsClient )
			{
				if ( !string.IsNullOrEmpty( main.directives.scriptclientmain ) )
				{
					main = files.Find( ( SFFile f ) => f.filename == main.directives.scriptclientmain );
					if ( main is null ) throw new StarfallException( "Couldn't load clientmain: " + main.directives.scriptclientmain, "" );
				}
			}
			// Call mainfile
			Lua.lua_pushcfunction( L, Lua.db_errorfb );
			Lua.lua_getglobal( L, "require" );
			Lua.lua_pushstring( L, main.filename );
			if ( Lua.lua_pcall( L, 1, 0, -3 ) != 0 )
			{
				string err = Lua.lua_tostring( L, -1 );
				if ( err is null )
					err = "Error not a string";
				Lua.lua_settop( L, 0 ); // Clear the stack
				throw new StarfallException( err, "" );
			}
			Lua.lua_settop( L, 0 ); // Clear the stack
		}

		public void Error( string message )
		{

		}

		Stopwatch cpuTimer = new Stopwatch();
		double cpuAverage = 0;

		double avgCpu()
		{
			return cpuAverage * (1 - cpuQuotaRatio) + cpuTimer.Elapsed.TotalSeconds * cpuQuotaRatio;
		}

		void cpuTimeCheck()
		{
			if ( avgCpu() > cpuQuota )
			{
				Lua.lua_pushstring( L, "CPU quota exceeded!" );
				Lua.lua_error( L );
			}
		}

		[Event( "tick" )]
		private void tick()
		{
			cpuAverage = avgCpu();
			cpuTimer.Restart();
			CallHook( "Think" );
		}

		// Registers library in _G
		public void RegisterLibrary( string name, Lua.luaL_Reg[] methods )
		{
			Lua.lua_createtable( L, 0, methods.Length - 1 );
			Lua.luaL_register( L, null, methods );
			Lua.lua_setglobal( L, name );
		}

		// Pushes the new metatable type on the stack
		public void RegisterType( string name )
		{
			Lua.luaL_newmetatable( L, name );
		}

		// Pushes the new metatable type on the stack and sets __index
		public void RegisterType( string name, Lua.luaL_Reg[] methods )
		{
			Lua.luaL_newmetatable( L, name );
			Lua.lua_pushvalue( L, -1 );
			Lua.lua_setfield( L, -2, "__index" );
			Lua.luaL_register( L, null, methods );
		}

		// Pushes new userdata on stack
		public void PushType<T>( string name, T obj )
		{
			Lua.lua_pushuserdata<T>( L, obj );
			Lua.luaL_getmetatable( L, name );
			Lua.lua_setmetatable( L, -2 );
		}
		public static void PushType<T>( Lua.lua_State L, string name, T obj )
		{
			Lua.lua_pushuserdata<T>( L, obj );
			Lua.luaL_getmetatable( L, name );
			Lua.lua_setmetatable( L, -2 );
		}

		// Checks that the type matches and returns it
		public T GetType<T>( string name, int index = 1 )
		{
			return (T)Lua.luaL_checkudata( L, index, name );
		}
		public static T GetType<T>( Lua.lua_State L, string name, int index = 1 )
		{
			return (T)Lua.luaL_checkudata( L, index, name );
		}

		public void CallHook( string name )
		{
			cpuTimer.Start();
			Lua.lua_pushcfunction( L, Lua.db_errorfb );
			Lua.lua_getglobal( L, "callhook" );
			Lua.lua_pushstring( L, name );
			if ( Lua.lua_pcall( L, 1, 0, -3 ) != 0 )
			{
				string err = Lua.lua_tostring( L, -1 );
				if ( err is null )
					err = "Error not a string";
				Error( err );
			}
			Lua.lua_pop( L, 1 );
			cpuTimer.Stop();
		}
	}
}

using System;
using System.Collections.Generic;
using Sandbox;
using KopiLua;

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
		public static List<Instance> activeInstances = new List<Instance>();
		public static Dictionary<Player, List<Instance>> playerInstances = new Dictionary<Player, List<Instance>>();

		StarfallData data;
		Player player;
		Entity entity;
		Lua.lua_State L;


		Dictionary<string, List<InstanceHook>> hooks = new Dictionary<string, List<InstanceHook>>();
		Dictionary<string, List<UserHook>> userhooks = new Dictionary<string, List<UserHook>>();


		public Instance( StarfallData data, Player player, Entity entity )
		{
			this.data = data;
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
			Lua.lua_createtable( L, 0, data.files.Count );
			foreach ( KeyValuePair<string, StarfallData.SFFile> file in data.files )
			{
				Lua.lua_pushlstring( L, file.Key, (uint)file.Key.Length );
				if ( Lua.luaL_loadbuffer( L, file.Value.code, (uint)file.Value.code.Length, "SF: " + file.Key ) != 0 )
				{
					string err = Lua.lua_tostring( L, -1 ).ToString();
					Lua.lua_settop( L, 0 ); // Clear the stack

					throw new StarfallException( err, "" );
				}
				Lua.lua_settable( L, 1 );
			}
			Lua.lua_setglobal( L, "_SCRIPTS" );

			Lua.lua_pushcfunction( L, ( Lua.lua_State L ) => { Lua.luaL_getmetatable( L, Lua.luaL_checkstring( L, 1 ) ); return 1; } );
			Lua.lua_setglobal( L, "getMetatable" );

			// Call mainfile
			Lua.lua_pushcfunction( L, Lua.db_errorfb );
			Lua.lua_getglobal( L, "require" );
			Lua.lua_pushlstring( L, data.mainfile, (uint)data.mainfile.Length );
			if ( Lua.lua_pcall( L, 1, 0, -3 ) != 0 )
			{
				string err = Lua.lua_tostring( L, -1 ).ToString();
				Lua.lua_settop( L, 0 ); // Clear the stack
				throw new StarfallException( err, "" );
			}
			Lua.lua_settop( L, 0 ); // Clear the stack
		}

		private void cpuTimeCheck()
		{

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
			Lua.lua_newuserdata<T>( L, obj );
			Lua.luaL_getmetatable( L, name );
			Lua.lua_setmetatable( L, -2 );
		}
		public static void PushType<T>( Lua.lua_State L, string name, T obj )
		{
			Lua.lua_newuserdata<T>( L, obj );
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
	}
}

using System;
using System.Collections.Generic;
using Sandbox;
using KopiLua;

namespace Starfall
{
    public class InstanceHook { }  public class UserHook { }

	public class StarfallException : Exception
	{
		public string luaStackTrace;
		public StarfallException( string message, string luaStackTrace ) : base( message ) { this.luaStackTrace = luaStackTrace; }
	}

    public class Instance
    {
        public static List<Instance> activeInstances = new List<Instance>();
        public static Dictionary<Player, List<Instance>> playerInstances = new Dictionary<Player, List<Instance>>();

        StarfallData data;
        Player player;
        Entity entity;
		Lua.lua_State L;


		Dictionary<string, List<InstanceHook>> hooks = new Dictionary<string, List<InstanceHook>>();
        Dictionary<string, List<UserHook>> userhooks = new Dictionary<string, List<UserHook>>();


        public Instance(StarfallData data, Player player, Entity entity)
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
			foreach ( KeyValuePair<string, string> file in data.files )
			{
				Lua.lua_pushlstring( L, file.Key, (uint)file.Key.Length );
				if ( Lua.luaL_loadbuffer( L, file.Value, (uint)file.Value.Length, "SF: " + file.Key ) != 0 )
				{
					string err = Lua.lua_tostring( L, -1 ).ToString();
					Lua.lua_settop( L, 0 ); // Clear the stack

					throw new StarfallException( err, "" );
				}
				Lua.lua_settable( L, 1 );
			}
			Lua.lua_setglobal( L, "_SCRIPTS" );

			// Call mainfile
			Lua.lua_pushcfunction( L, Lua.db_errorfb );
			Lua.lua_getglobal( L, "require" );
			Lua.lua_pushlstring( L, data.mainfile, (uint)data.mainfile.Length );
			if( Lua.lua_pcall( L, 1, 0, -3 ) != 0 )
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
    }
}

/*
 ** $Id: loadlib.c,v 1.52.1.3 2008/08/06 13:29:28 roberto Exp $
 ** Dynamic library loader for Lua
 ** See Copyright Notice in lua.h
 **
 ** This module contains an implementation of loadlib for Unix systems
 ** that have dlfcn, an implementation for Darwin (Mac OS X), an
 ** implementation for Windows, and a stub for other systems.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{
	public partial class Lua
	{

		/* prefix for open functions in C libraries */
		public const string LUA_POF = "luaopen_";

		/* separator for open functions in C libraries */
		public const string LUA_OFSEP = "_";


		public const string LIBPREFIX = "LOADLIB: ";

		public const string POF = LUA_POF;
		public const string LIB_FAIL = "open";


		/* error codes for ll_loadfunc */
		public const int ERRLIB = 1;
		public const int ERRFUNC = 2;

		//public static void setprogdir(lua_State L) { }

		public static void setprogdir( lua_State L )
		{
			string buff = Sandbox.FileSystem.Mounted.GetFullPath( "." );
			string str = lua_tostring( L, -1 );
			lua_pushstring( L, str.Replace( LUA_EXECDIR, buff ) );
			lua_remove( L, -2 );  /* remove original string */
		}

		/*
         ** {======================================================
         ** 'require' function
         ** =======================================================
         */

		public static object sentinel = new object();


		public static int ll_require( lua_State L )
		{
			string name = luaL_checkstring( L, 1 );
			int i;
			lua_settop( L, 1 );  /* _LOADED table will be at index 2 */
			lua_getfield( L, LUA_REGISTRYINDEX, "_LOADED" );
			lua_getfield( L, 2, name );
			if ( lua_toboolean( L, -1 ) != 0 )
			{  /* is it there? */
				if ( lua_touserdata( L, -1 ) == sentinel )  /* check loops */
					luaL_error( L, "loop or previous error loading module " + LUA_QS, name );
				return 1;  /* package is already loaded */
			}

			/* else must load it */
			lua_getglobal( L, "_SCRIPTS" );
			if ( !lua_istable( L, -1 ) )
				luaL_error( L, LUA_QL( "_SCRIPTS" ) + " must be a table" );

			lua_pushstring( L, name );
			lua_rawget( L, -2 );
			if ( !lua_isfunction( L, -1 ) )
				luaL_error( L, LUA_QL( "_SCRIPTS[\"" + name + "\"]" ) + " must be a function" );

			lua_pushlightuserdata( L, sentinel );
			lua_setfield( L, 2, name );  /* _LOADED[name] = sentinel */
			lua_pushstring( L, name );  /* pass name as argument to module */
			lua_call( L, 1, 1 );  /* run loaded module */
			if ( !lua_isnil( L, -1 ) )  /* non-nil return? */
				lua_setfield( L, 2, name );  /* _LOADED[name] = returned value */
			lua_getfield( L, 2, name );
			if ( lua_touserdata( L, -1 ) == sentinel )
			{   /* module did not set a value? */
				lua_pushboolean( L, 1 );  /* use true as result */
				lua_pushvalue( L, -1 );  /* extra copy to be returned */
				lua_setfield( L, 2, name );  /* _LOADED[name] = true */
			}
			return 1;
		}

		/* }====================================================== */



		/*
         ** {======================================================
         ** 'module' function
         ** =======================================================
         */


		private static void setfenv( lua_State L )
		{
			lua_Debug ar = new lua_Debug();
			if ( lua_getstack( L, 1, ar ) == 0 ||
				lua_getinfo( L, "f", ar ) == 0 ||  /* get calling function */
				lua_iscfunction( L, -1 ) )
				luaL_error( L, LUA_QL( "module" ) + " not called from a Lua function" );
			lua_pushvalue( L, -2 );
			lua_setfenv( L, -2 );
			lua_pop( L, 1 );
		}


		private static void dooptions( lua_State L, int n )
		{
			int i;
			for ( i = 2; i <= n; i++ )
			{
				lua_pushvalue( L, i );  /* get option (a function) */
				lua_pushvalue( L, -2 );  /* module */
				lua_call( L, 1, 0 );
			}
		}


		private static void modinit( lua_State L, string modname )
		{
			lua_pushvalue( L, -1 );
			lua_setfield( L, -2, "_M" );  /* module._M = module */
			lua_pushstring( L, modname );
			lua_setfield( L, -2, "_NAME" );
			int dot = modname.LastIndexOf( '.' );  /* look for last dot in module name */
			if ( dot != -1 ) modname = modname.Substring(dot + 1);
			/* set _PACKAGE as package name (full module name minus last part) */
			lua_pushstring( L, modname );
			lua_setfield( L, -2, "_PACKAGE" );
		}


		private static int ll_module( lua_State L )
		{
			string modname = luaL_checkstring( L, 1 );
			int loaded = lua_gettop( L ) + 1;  /* index of _LOADED table */
			lua_getfield( L, LUA_REGISTRYINDEX, "_LOADED" );
			lua_getfield( L, loaded, modname );  /* get _LOADED[modname] */
			if ( !lua_istable( L, -1 ) )
			{  /* not found? */
				lua_pop( L, 1 );  /* remove previous result */
				/* try global variable (and create one if it does not exist) */
				if ( luaL_findtable( L, LUA_GLOBALSINDEX, modname, 1 ) != null )
					return luaL_error( L, "name conflict for module " + LUA_QS, modname );
				lua_pushvalue( L, -1 );
				lua_setfield( L, loaded, modname );  /* _LOADED[modname] = new table */
			}
			/* check whether table already has a _NAME field */
			lua_getfield( L, -1, "_NAME" );
			if ( !lua_isnil( L, -1 ) )  /* is table an initialized module? */
				lua_pop( L, 1 );
			else
			{  /* no; initialize it */
				lua_pop( L, 1 );
				modinit( L, modname );
			}
			lua_pushvalue( L, -1 );
			setfenv( L );
			dooptions( L, loaded - 1 );
			return 0;
		}


		private static int ll_seeall( lua_State L )
		{
			luaL_checktype( L, 1, LUA_TTABLE );
			if ( lua_getmetatable( L, 1 ) == 0 )
			{
				lua_createtable( L, 0, 1 ); /* create new metatable */
				lua_pushvalue( L, -1 );
				lua_setmetatable( L, 1 );
			}
			lua_pushvalue( L, LUA_GLOBALSINDEX );
			lua_setfield( L, -2, "__index" );  /* mt.__index = _G */
			return 0;
		}


		/* }====================================================== */



		/* auxiliary mark (for internal use) */
		public readonly static string AUXMARK = String.Format( "{0}", (char)1 );

		private static void setpath( lua_State L, string fieldname, string envname, string def )
		{
			lua_pushstring( L, def );
			setprogdir( L );
			lua_setfield( L, -2, fieldname );
		}


		private readonly static luaL_Reg[] pk_funcs = {
			new luaL_Reg("seeall", ll_seeall)
		};


		private readonly static luaL_Reg[] ll_funcs = {
			new luaL_Reg("module", ll_module),
			new luaL_Reg("require", ll_require)
		};

		public static int luaopen_package( lua_State L )
		{
			int i;
			/* create new type _LOADLIB */
			luaL_newmetatable( L, "_LOADLIB" );
			/* create `package' table */
			luaL_register( L, LUA_LOADLIBNAME, pk_funcs );
#if LUA_COMPAT_LOADLIB
            lua_getfield(L, -1, "loadlib");
            lua_setfield(L, LUA_GLOBALSINDEX, "loadlib");
#endif
			lua_pushvalue( L, -1 );
			lua_replace( L, LUA_ENVIRONINDEX );
			setpath( L, "path", LUA_PATH, LUA_PATH_DEFAULT );  /* set field `path' */
			setpath( L, "cpath", LUA_CPATH, LUA_CPATH_DEFAULT ); /* set field `cpath' */
			/* store config information */
			lua_pushstring( L, LUA_DIRSEP + "\n" + LUA_PATHSEP + "\n" + LUA_PATH_MARK + "\n" +
							LUA_EXECDIR + "\n" + LUA_IGMARK );
			lua_setfield( L, -2, "config" );
			/* set field `loaded' */
			luaL_findtable( L, LUA_REGISTRYINDEX, "_LOADED", 2 );
			lua_setfield( L, -2, "loaded" );
			/* set field `preload' */
			lua_newtable( L );
			lua_setfield( L, -2, "preload" );
			lua_pushvalue( L, LUA_GLOBALSINDEX );
			luaL_register( L, null, ll_funcs );  /* open lib into global table */
			lua_pop( L, 1 );
			return 1;  /* return 'package' table */
		}

	}
}

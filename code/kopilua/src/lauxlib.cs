/*
** $Id: lauxlib.c,v 1.159.1.3 2008/01/21 13:20:51 roberto Exp $
** Auxiliary functions for building Lua libraries
** See Copyright Notice in lua.h
*/

#define lauxlib_c
#define LUA_LIB

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KopiLua
{
	using lua_Number = System.Double;
	using lua_Integer = System.Int32;

	public partial class Lua
	{
#if LUA_COMPAT_GETN
		public static int luaL_getn(lua_State L, int t);
		public static void luaL_setn(lua_State L, int t, int n);
#else
		public static int luaL_getn( lua_State L, int i ) { return (int)lua_objlen( L, i ); }
		public static void luaL_setn( lua_State L, int i, int j ) { } /* no op! */
#endif

#if LUA_COMPAT_OPENLIB
		//#define luaI_openlib	luaL_openlib
#endif


		/* extra error code for `luaL_load' */
		public const int LUA_ERRFILE = (LUA_ERRERR + 1);


		public class luaL_Reg
		{
			public luaL_Reg( string name, lua_CFunction func )
			{
				this.name = name;
				this.func = func;
			}

			public string name;
			public lua_CFunction func;
		};


		/*
		** ===============================================================
		** some useful macros
		** ===============================================================
		*/

		public static void luaL_argcheck( lua_State L, bool cond, int numarg, string extramsg )
		{
			if ( !cond )
				luaL_argerror( L, numarg, extramsg );
		}
		public static int luaL_checkint( lua_State L, int n ) { return (int)luaL_checkinteger( L, n ); }
		public static int luaL_optint( lua_State L, int n, lua_Integer d ) { return (int)luaL_optinteger( L, n, d ); }
		public static long luaL_checklong( lua_State L, int n ) { return luaL_checkinteger( L, n ); }
		public static long luaL_optlong( lua_State L, int n, lua_Integer d ) { return luaL_optinteger( L, n, d ); }

		public static string luaL_typename( lua_State L, int i ) { return lua_typename( L, lua_type( L, i ) ); }

		public static int luaL_dostring( lua_State L, string s ) { return luaL_loadstring( L, s ) != 0 || lua_pcall( L, 0, LUA_MULTRET, 0 ) != 0 ? 1 : 0; }

		public static void luaL_getmetatable( lua_State L, string n ) { lua_getfield( L, LUA_REGISTRYINDEX, n ); }

		public delegate lua_Number luaL_opt_delegate( lua_State L, int narg );
		public static lua_Number luaL_opt( lua_State L, luaL_opt_delegate f, int n, lua_Number d )
		{
			return lua_isnoneornil( L, (n != 0) ? d : f( L, n ) ) ? 1 : 0;
		}

		public delegate lua_Integer luaL_opt_delegate_integer( lua_State L, int narg );
		public static lua_Integer luaL_opt_integer( lua_State L, luaL_opt_delegate_integer f, int n, lua_Number d )
		{
			return (lua_Integer)(lua_isnoneornil( L, n ) ? d : f( L, (n) ));
		}

		/*
		** {======================================================
		** Generic Buffer manipulation
		** =======================================================
		*/



		public class luaL_Buffer
		{
			public int p;           /* current position in buffer */
			public int lvl;  /* number of strings in the stack (level) */
			public lua_State L;
			public CharPtr buffer = new char[LUAL_BUFFERSIZE];
		};

		public static void luaL_addchar( luaL_Buffer B, char c )
		{
			if ( B.p >= LUAL_BUFFERSIZE )
				luaL_prepbuffer( B );
			B.buffer[B.p++] = c;
		}

		///* compatibility only */
		public static void luaL_putchar( luaL_Buffer B, char c ) { luaL_addchar( B, c ); }

		public static void luaL_addsize( luaL_Buffer B, int n ) { B.p += n; }

		/* }====================================================== */


		/* compatibility with ref system */

		/* pre-defined references */
		public const int LUA_NOREF = (-2);
		public const int LUA_REFNIL = (-1);

		//#define lua_ref(L,lock) ((lock) ? luaL_ref(L, LUA_REGISTRYINDEX) : \
		//      (lua_pushstring(L, "unlocked references are obsolete"), lua_error(L), 0))

		//#define lua_unref(L,ref)        luaL_unref(L, LUA_REGISTRYINDEX, (ref))

		//#define lua_getref(L,ref)       lua_rawgeti(L, LUA_REGISTRYINDEX, (ref))


		//#define luaL_reg	luaL_Reg


		/* This file uses only the official API of Lua.
		** Any function declared here could be written as an application function.
		*/

		//#define lauxlib_c
		//#define LUA_LIB

		public const int FREELIST_REF = 0;  /* free list of references */


		/* convert a stack index to positive */
		public static int abs_index( lua_State L, int i )
		{
			return ((i) > 0 || (i) <= LUA_REGISTRYINDEX ? (i) : lua_gettop( L ) + (i) + 1);
		}


		/*
		** {======================================================
		** Error-report functions
		** =======================================================
		*/


		public static int luaL_argerror( lua_State L, int narg, string extramsg )
		{
			lua_Debug ar = new lua_Debug();
			if ( lua_getstack( L, 0, ar ) == 0 )  /* no stack frame? */
				return luaL_error( L, "bad argument #%d (%s)", narg, extramsg );
			lua_getinfo( L, "n", ar );
			if ( ar.namewhat == "method" )
			{
				narg--;  /* do not count `self' */
				if ( narg == 0 )  /* error is in the self argument itself? */
					return luaL_error( L, "calling " + LUA_QS + " on bad self ({1})",
										 ar.name, extramsg );
			}
			if ( ar.name == null )
				ar.name = "?";
			return luaL_error( L, "bad argument #%d to " + LUA_QS + " (%s)",
								  narg, ar.name, extramsg );
		}


		public static int luaL_typerror( lua_State L, int narg, string tname )
		{
			string msg = lua_pushfstring( L, "%s expected, got %s",
											  tname, luaL_typename( L, narg ) );
			return luaL_argerror( L, narg, msg );
		}


		private static void tag_error( lua_State L, int narg, int tag )
		{
			luaL_typerror( L, narg, lua_typename( L, tag ) );
		}


		public static void luaL_where( lua_State L, int level )
		{
			lua_Debug ar = new lua_Debug();
			if ( lua_getstack( L, level, ar ) != 0 )
			{  /* check function at level */
				lua_getinfo( L, "Sl", ar );  /* get info about it */
				if ( ar.currentline > 0 )
				{  /* is there info? */
					lua_pushfstring( L, "%s:%d: ", ar.short_src, ar.currentline );
					return;
				}
			}
			lua_pushstring( L, "" );  /* else, no information available... */
		}

		public static int luaL_error( lua_State L, string fmt, params object[] p )
		{
			luaL_where( L, 1 );
			lua_pushvfstring( L, fmt, p );
			lua_concat( L, 2 );
			return lua_error( L );
		}


		/* }====================================================== */


		public static int luaL_checkoption( lua_State L, int narg, string def, string[] lst )
		{
			string name = !string.IsNullOrEmpty( def ) ? luaL_optstring( L, narg, def ) : luaL_checkstring( L, narg );
			int i;
			for ( i = 0; i < lst.Length; i++ )
				if ( lst[i] == name )
					return i;
			return luaL_argerror( L, narg, lua_pushfstring( L, "invalid option " + LUA_QS, name ) );
		}


		public static int luaL_newmetatable( lua_State L, string tname )
		{
			lua_getfield( L, LUA_REGISTRYINDEX, tname );  /* get registry.name */
			if ( !lua_isnil( L, -1 ) )  /* name already in use? */
				return 0;  /* leave previous value on top, but return 0 */
			lua_pop( L, 1 );
			lua_newtable( L );  /* create metatable */
			lua_pushvalue( L, -1 );
			lua_setfield( L, LUA_REGISTRYINDEX, tname );  /* registry.name = metatable */
			return 1;
		}


		public static object luaL_checkudata( lua_State L, int ud, string tname )
		{
			object p = lua_touserdata( L, ud );
			if ( p != null )
			{  /* value is a userdata? */
				if ( lua_getmetatable( L, ud ) != 0 )
				{  /* does it have a metatable? */
					lua_getfield( L, LUA_REGISTRYINDEX, tname );  /* get correct metatable */
					if ( lua_rawequal( L, -1, -2 ) != 0 )
					{  /* does it have the correct mt? */
						lua_pop( L, 2 );  /* remove both metatables */
						return p;
					}
				}
			}
			luaL_typerror( L, ud, tname );  /* else error */
			return null;  /* to avoid warnings */
		}


		public static void luaL_checkstack( lua_State L, int space, string mes )
		{
			if ( lua_checkstack( L, space ) == 0 )
				luaL_error( L, "stack overflow (%s)", mes );
		}


		public static void luaL_checktype( lua_State L, int narg, int t )
		{
			if ( lua_type( L, narg ) != t )
				tag_error( L, narg, t );
		}


		public static void luaL_checkany( lua_State L, int narg )
		{
			if ( lua_type( L, narg ) == LUA_TNONE )
				luaL_argerror( L, narg, "value expected" );
		}


		public static string luaL_checkstring( lua_State L, int narg )
		{
			string str = lua_tostring( L, narg );
			if ( str is null ) tag_error( L, narg, LUA_TSTRING );
			return str;
		}


		public static string luaL_optstring( lua_State L, int narg, string def )
		{
			if ( lua_isnoneornil( L, narg ) )
			{
				return def;
			}
			else return luaL_checkstring( L, narg );
		}


		public static lua_Number luaL_checknumber( lua_State L, int narg )
		{
			lua_Number d = lua_tonumber( L, narg );
			if ( (d == 0) && (lua_isnumber( L, narg ) == 0) )  /* avoid extra test when d is not 0 */
				tag_error( L, narg, LUA_TNUMBER );
			return d;
		}


		public static lua_Number luaL_optnumber( lua_State L, int narg, lua_Number def )
		{
			return luaL_opt( L, luaL_checknumber, narg, def );
		}


		public static lua_Integer luaL_checkinteger( lua_State L, int narg )
		{
			lua_Integer d = lua_tointeger( L, narg );
			if ( d == 0 && lua_isnumber( L, narg ) == 0 )  /* avoid extra test when d is not 0 */
				tag_error( L, narg, LUA_TNUMBER );
			return d;
		}


		public static lua_Integer luaL_optinteger( lua_State L, int narg, lua_Integer def )
		{
			return luaL_opt_integer( L, luaL_checkinteger, narg, def );
		}


		public static int luaL_getmetafield( lua_State L, int obj, string event_ )
		{
			if ( lua_getmetatable( L, obj ) == 0 )  /* no metatable? */
				return 0;
			lua_pushstring( L, event_ );
			lua_rawget( L, -2 );
			if ( lua_isnil( L, -1 ) )
			{
				lua_pop( L, 2 );  /* remove metatable and metafield */
				return 0;
			}
			else
			{
				lua_remove( L, -2 );  /* remove only metatable */
				return 1;
			}
		}


		public static int luaL_callmeta( lua_State L, int obj, string event_ )
		{
			obj = abs_index( L, obj );
			if ( luaL_getmetafield( L, obj, event_ ) == 0 )  /* no metafield? */
				return 0;
			lua_pushvalue( L, obj );
			lua_call( L, 1, 1 );
			return 1;
		}


		public static void luaL_register( lua_State L, string libname, luaL_Reg[] l )
		{
			luaI_openlib( L, libname, l, 0 );
		}

		// we could just take the .Length member here, but let's try
		// to keep it as close to the C implementation as possible.
		private static int libsize( luaL_Reg[] l )
		{
			int size = 0;
			for ( ; l[size].name != null; size++ ) ;
			return size;
		}

		public static void luaI_openlib( lua_State L, string libname, luaL_Reg[] l, int nup )
		{
			if ( libname != null )
			{
				int size = libsize( l );
				/* check whether lib already exists */
				luaL_findtable( L, LUA_REGISTRYINDEX, "_LOADED", 1 );
				lua_getfield( L, -1, libname );  /* get _LOADED[libname] */
				if ( !lua_istable( L, -1 ) )
				{  /* not found? */
					lua_pop( L, 1 );  /* remove previous result */
					/* try global variable (and create one if it does not exist) */
					if ( luaL_findtable( L, LUA_GLOBALSINDEX, libname, size ) != null )
						luaL_error( L, "name conflict for module " + LUA_QS, libname );
					lua_pushvalue( L, -1 );
					lua_setfield( L, -3, libname );  /* _LOADED[libname] = new table */
				}
				lua_remove( L, -2 );  /* remove _LOADED table */
				lua_insert( L, -(nup + 1) );  /* move library table to below upvalues */
			}
			int reg_num = 0;
			for ( ; l[reg_num].name != null; reg_num++ )
			{
				int i;
				for ( i = 0; i < nup; i++ )  /* copy upvalues to the top */
					lua_pushvalue( L, -nup );
				lua_pushcclosure( L, l[reg_num].func, nup );
				lua_setfield( L, -(nup + 2), l[reg_num].name );
			}
			lua_pop( L, nup );  /* remove upvalues */
		}



		/*
		** {======================================================
		** getn-setn: size for arrays
		** =======================================================
		*/

#if LUA_COMPAT_GETN

		static int checkint (lua_State L, int topop) {
		  int n = (lua_type(L, -1) == LUA_TNUMBER) ? lua_tointeger(L, -1) : -1;
		  lua_pop(L, topop);
		  return n;
		}


		static void getsizes (lua_State L) {
		  lua_getfield(L, LUA_REGISTRYINDEX, "LUA_SIZES");
		  if (lua_isnil(L, -1)) {  /* no `size' table? */
			lua_pop(L, 1);  /* remove nil */
			lua_newtable(L);  /* create it */
			lua_pushvalue(L, -1);  /* `size' will be its own metatable */
			lua_setmetatable(L, -2);
			lua_pushstring(L, "kv");
			lua_setfield(L, -2, "__mode");  /* metatable(N).__mode = "kv" */
			lua_pushvalue(L, -1);
			lua_setfield(L, LUA_REGISTRYINDEX, "LUA_SIZES");  /* store in register */
		  }
		}


		public static void luaL_setn (lua_State L, int t, int n) {
		  t = abs_index(L, t);
		  lua_pushstring(L, "n");
		  lua_rawget(L, t);
		  if (checkint(L, 1) >= 0) {  /* is there a numeric field `n'? */
			lua_pushstring(L, "n");  /* use it */
			lua_pushinteger(L, n);
			lua_rawset(L, t);
		  }
		  else {  /* use `sizes' */
			getsizes(L);
			lua_pushvalue(L, t);
			lua_pushinteger(L, n);
			lua_rawset(L, -3);  /* sizes[t] = n */
			lua_pop(L, 1);  /* remove `sizes' */
		  }
		}


		public static int luaL_getn (lua_State L, int t) {
		  int n;
		  t = abs_index(L, t);
		  lua_pushstring(L, "n");  /* try t.n */
		  lua_rawget(L, t);
		  if ((n = checkint(L, 1)) >= 0) return n;
		  getsizes(L);  /* else try sizes[t] */
		  lua_pushvalue(L, t);
		  lua_rawget(L, -2);
		  if ((n = checkint(L, 2)) >= 0) return n;
		  return (int)lua_objlen(L, t);
		}

#endif

		/* }====================================================== */



		public static string luaL_gsub( lua_State L, string s, string p, string r )
		{
			int wild = 0;
			int sindex = 0;
			luaL_Buffer b = new luaL_Buffer();
			luaL_buffinit( L, b );
			while ( (wild = s.IndexOf( p, sindex )) > 0 )
			{
				luaL_addstring( b, s.Substring( sindex, wild - sindex ) );  /* push prefix */
				luaL_addstring( b, r );  /* push replacement in place of pattern */
				sindex = wild + p.Length;  /* continue after `p' */
			}
			luaL_addstring( b, s );  /* push last suffix */
			luaL_pushresult( b );
			return lua_tostring( L, -1 );
		}


		public static string luaL_findtable( lua_State L, int idx, string fname, int szhint )
		{
			int e = 0;
			lua_pushvalue( L, idx );
			while ( true )
			{
				e = fname.IndexOf( '.' );
				if ( e == -1 ) e = fname.Length;
				lua_pushstring( L, fname.Substring( 0, e ) );
				lua_rawget( L, -2 );
				if ( lua_isnil( L, -1 ) )
				{  /* no such field? */
					lua_pop( L, 1 );  /* remove this nil */
					lua_createtable( L, 0, (e == '.' ? 1 : szhint) ); /* new table for field */
					lua_pushstring( L, fname.Substring( 0, e ) );
					lua_pushvalue( L, -2 );
					lua_settable( L, -4 );  /* set new table into field */
				}
				else if ( !lua_istable( L, -1 ) )
				{  /* field has a non-table value? */
					lua_pop( L, 2 );  /* remove table and value */
					return fname;  /* return problematic part of the name */
				}
				lua_remove( L, -2 );  /* remove previous table */
				if ( e < fname.Length )
					fname = fname.Substring( e );
				else
					break;
			}
			return null;
		}



		/*
		** {======================================================
		** Generic Buffer manipulation
		** =======================================================
		*/


		private static int bufflen( luaL_Buffer B ) { return B.p; }
		private static int bufffree( luaL_Buffer B ) { return LUAL_BUFFERSIZE - bufflen( B ); }

		public const int LIMIT = LUA_MINSTACK / 2;


		private static int emptybuffer( luaL_Buffer B )
		{
			uint l = (uint)bufflen( B );
			if ( l == 0 ) return 0;  /* put nothing on stack */
			else
			{
				lua_pushstring( B.L, new string( B.buffer.chars, 0, (int)l ) );
				B.p = 0;
				B.lvl++;
				return 1;
			}
		}


		private static void adjuststack( luaL_Buffer B )
		{
			if ( B.lvl > 1 )
			{
				lua_State L = B.L;
				int toget = 1;  /* number of levels to concat */
				uint toplen = lua_strlen( L, -1 );
				do
				{
					uint l = lua_strlen( L, -(toget + 1) );
					if ( B.lvl - toget + 1 >= LIMIT || toplen > l )
					{
						toplen += l;
						toget++;
					}
					else break;
				} while ( toget < B.lvl );
				lua_concat( L, toget );
				B.lvl = B.lvl - toget + 1;
			}
		}


		public static CharPtr luaL_prepbuffer( luaL_Buffer B )
		{
			if ( emptybuffer( B ) != 0 )
				adjuststack( B );
			return new CharPtr( B.buffer, B.p );
		}


		public static void luaL_addstring( luaL_Buffer B, string s )
		{
			foreach ( char c in s )
			{
				luaL_addchar( B, c );
			}
		}


		public static void luaL_pushresult( luaL_Buffer B )
		{
			emptybuffer( B );
			lua_concat( B.L, B.lvl );
			B.lvl = 1;
		}


		public static void luaL_addvalue( luaL_Buffer B )
		{
			lua_State L = B.L;
			string str = lua_tostring( L, -1 );
			if ( str is null ) str = "";
			int vl = str.Length;
			if ( vl <= bufffree( B ) )
			{
				str.CopyTo( 0, B.buffer.chars, B.buffer.index + B.p, vl );
				B.p += vl;
				lua_pop( L, 1 );  /* remove from stack */
			}
			else
			{
				if ( emptybuffer( B ) != 0 )
					lua_insert( L, -2 );  /* put buffer before new value */
				B.lvl++;  /* add new value into B stack */
				adjuststack( B );
			}
		}


		public static void luaL_buffinit( lua_State L, luaL_Buffer B )
		{
			B.L = L;
			B.p = /*B.buffer*/ 0;
			B.lvl = 0;
		}

		/* }====================================================== */


		public static int luaL_ref( lua_State L, int t )
		{
			int ref_;
			t = abs_index( L, t );
			if ( lua_isnil( L, -1 ) )
			{
				lua_pop( L, 1 );  /* remove from stack */
				return LUA_REFNIL;  /* `nil' has a unique fixed reference */
			}
			lua_rawgeti( L, t, FREELIST_REF );  /* get first free element */
			ref_ = (int)lua_tointeger( L, -1 );  /* ref = t[FREELIST_REF] */
			lua_pop( L, 1 );  /* remove it from stack */
			if ( ref_ != 0 )
			{  /* any free element? */
				lua_rawgeti( L, t, ref_ );  /* remove it from list */
				lua_rawseti( L, t, FREELIST_REF );  /* (t[FREELIST_REF] = t[ref]) */
			}
			else
			{  /* no free elements */
				ref_ = (int)lua_objlen( L, t );
				ref_++;  /* create new reference */
			}
			lua_rawseti( L, t, ref_ );
			return ref_;
		}


		public static void luaL_unref( lua_State L, int t, int ref_ )
		{
			if ( ref_ >= 0 )
			{
				t = abs_index( L, t );
				lua_rawgeti( L, t, FREELIST_REF );
				lua_rawseti( L, t, ref_ );  /* t[ref] = t[FREELIST_REF] */
				lua_pushinteger( L, ref_ );
				lua_rawseti( L, t, FREELIST_REF );  /* t[FREELIST_REF] = ref */
			}
		}



		/*
		** {======================================================
		** Load functions
		** =======================================================
		*/

		private static int errfile( lua_State L, string what, int fnameindex )
		{
			string serr = strerror( errno() );
			string filename = lua_tostring( L, fnameindex );
			lua_pushfstring( L, "cannot %s %s: %s", what, filename, serr );
			lua_remove( L, fnameindex );
			return LUA_ERRFILE;
		}


		static string getS( lua_State L, object ud )
		{
			return (string)ud;
		}


		public static int luaL_loadbuffer( lua_State L, string buff, string name )
		{
			return lua_load( L, getS, buff, name );
		}


		public static int luaL_loadstring( lua_State L, string s )
		{
			return luaL_loadbuffer( L, s, s );
		}



		/* }====================================================== */


		private static object l_alloc<T>()
		{
			return System.Activator.CreateInstance<T>();
		}


		private static int panic( lua_State L )
		{
			string str = lua_tostring( L, -1 );
			Sandbox.Log.Error( "PANIC: unprotected error in call to Lua API (" + str + ")" );
			return 0;
		}


		public static lua_State luaL_newstate()
		{
			lua_State L = lua_newstate( null );
			if ( L != null ) lua_atpanic( L, panic );
			return L;
		}

	}
}

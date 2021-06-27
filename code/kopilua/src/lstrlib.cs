/*
 ** $Id: lstrlib.c,v 1.132.1.4 2008/07/11 17:27:21 roberto Exp $
 ** Standard library for string operations and pattern-matching
 ** See Copyright Notice in lua.h
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using ptrdiff_t = System.Int32;

	public partial class Lua
	{
		private static int str_len( lua_State L )
		{
			string s = luaL_checkstring( L, 1 );
			lua_pushinteger( L, s.Length );
			return 1;
		}


		private static ptrdiff_t posrelat( ptrdiff_t pos, int len )
		{
			/* relative string position: negative means back from end */
			if ( pos < 0 ) pos += (ptrdiff_t)len + 1;
			return (pos >= 0) ? pos : 0;
		}


		private static int str_sub( lua_State L )
		{
			string s = luaL_checkstring( L, 1 );
			ptrdiff_t start = Math.Max(posrelat( luaL_checkinteger( L, 2 ), s.Length ), 1);
			ptrdiff_t end = Math.Min(posrelat( luaL_optinteger( L, 3, -1 ), s.Length ), s.Length);
			if ( start <= end )
				lua_pushstring( L, s.Substring( start - 1, end - start + 1 ) );
			else lua_pushstring( L, "" );
			return 1;
		}


		private static int str_reverse( lua_State L )
		{
			string s = luaL_checkstring( L, 1 );
			char[] charArray = s.ToCharArray();
			Array.Reverse( charArray );
			lua_pushstring( L, new string( charArray ) );
			return 1;
		}


		private static int str_lower( lua_State L )
		{
			string s = luaL_checkstring( L, 1 );
			lua_pushstring( L, s.ToLower() );
			return 1;
		}


		private static int str_upper( lua_State L )
		{
			string s = luaL_checkstring( L, 1 );
			lua_pushstring( L, s.ToUpper() );
			return 1;
		}

		private static int str_rep( lua_State L )
		{
			string s = luaL_checkstring( L, 1 );
			int n = luaL_checkinteger( L, 2 );
			if ( s.Length * n > 1000000 )
				luaL_error( L, "String is too long!" );
			StringBuilder b = new StringBuilder();
			while ( n-- > 0 )
				b.Append( s );
			lua_pushstring( L, b.ToString() );
			return 1;
		}


		private static int str_byte( lua_State L )
		{
			string s = luaL_checkstring( L, 1 );
			ptrdiff_t posi = Math.Max(posrelat( luaL_optinteger( L, 2, 1 ), s.Length ), 1);
			ptrdiff_t pose = Math.Min( posrelat( luaL_optinteger( L, 3, posi ), s.Length ), s.Length );
			if ( posi > pose ) return 0;  /* empty interval; return no values */
			int n = pose - posi + 1;
			if ( posi + n <= pose )  /* overflow? */
				luaL_error( L, "string slice too long" );
			luaL_checkstack( L, n, "string slice too long" );
			for ( int i = 0; i < n; ++i )
				lua_pushinteger( L, (byte)(s[posi + i - 1]) );
			return n;
		}


		private static int str_char( lua_State L )
		{
			int n = lua_gettop( L );  /* number of arguments */
			int i;
			luaL_Buffer b = new luaL_Buffer();
			luaL_buffinit( L, b );
			for ( i = 1; i <= n; i++ )
			{
				int c = luaL_checkinteger( L, i );
				luaL_argcheck( L, (byte)(c) == c, i, "invalid value" );
				luaL_addchar( b, (char)(byte)c );
			}
			luaL_pushresult( b );
			return 1;
		}


		private readonly static luaL_Reg[] strlib = {
			new luaL_Reg("byte", str_byte),
			new luaL_Reg("char", str_char),
			new luaL_Reg("len", str_len),
			new luaL_Reg("lower", str_lower),
			new luaL_Reg("rep", str_rep),
			new luaL_Reg("reverse", str_reverse),
			new luaL_Reg("sub", str_sub),
			new luaL_Reg("upper", str_upper),
			new luaL_Reg(null, null)
		};


		private static void createmetatable( lua_State L )
		{
			lua_createtable( L, 0, 1 );  /* create metatable for strings */
			lua_pushstring( L, "" );  /* dummy string */
			lua_pushvalue( L, -2 );
			lua_setmetatable( L, -2 );  /* set string metatable */
			lua_pop( L, 1 );  /* pop dummy string */
			lua_pushvalue( L, -2 );  /* string library... */
			lua_setfield( L, -2, "__index" );  /* ...is the __index metamethod */
			lua_pop( L, 1 );  /* pop metatable */
		}


		/*
         ** Open string library
         */
		public static int luaopen_string( lua_State L )
		{
			luaL_register( L, LUA_STRLIBNAME, strlib );
#if LUA_COMPAT_GFIND
            lua_getfield(L, -1, "gmatch");
            lua_setfield(L, -2, "gfind");
#endif
			createmetatable( L );
			return 1;
		}

	}
}

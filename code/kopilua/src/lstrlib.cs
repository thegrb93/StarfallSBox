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
using System.Text.RegularExpressions;

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
			ptrdiff_t start = Math.Max( posrelat( luaL_checkinteger( L, 2 ), s.Length ), 1 );
			ptrdiff_t end = Math.Min( posrelat( luaL_optinteger( L, 3, -1 ), s.Length ), s.Length );
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
			ptrdiff_t posi = Math.Max( posrelat( luaL_optinteger( L, 2, 1 ), s.Length ), 1 );
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

		private static Match getMatch( lua_State L, string s, string p, int pos )
		{
			try
			{
				Regex rx = new Regex( p, RegexOptions.Compiled, TimeSpan.FromSeconds( 0.05 ) );
				return rx.Match( s, pos );
			}
			catch ( RegexMatchTimeoutException )
			{
				luaL_error( L, "Regex took too much cpu-time" );
			}
			catch
			{
				luaL_error( L, "Invalid regex" );
			}
			return null;
		}

		private static List<string> getCaptures( Match m )
		{
			List<string> captures = new List<string>();
			foreach ( Group g in m.Groups )
				foreach ( Capture c in g.Captures )
					captures.Add( c.Value );
			return captures;
		}

		private static int pushCaptures( lua_State L, Match m )
		{
			List<string> captures = getCaptures( m );
			luaL_checkstack( L, captures.Count, "too many captures" );
			foreach ( string str in captures )
				lua_pushstring( L, str );
			return captures.Count;
		}

		private static int str_find( lua_State L )
		{
			string s = luaL_checkstring( L, 1 );
			string p = luaL_checkstring( L, 2 );
			ptrdiff_t init = Math.Min( Math.Max( posrelat( luaL_optinteger( L, 3, 1 ), s.Length ) - 1, 0 ), s.Length );
			if ( lua_toboolean( L, 4 ) != 0 )
			{
				int pos = s.IndexOf( p, init );
				if ( pos != -1 )
				{
					lua_pushinteger( L, pos + 1 );
					lua_pushinteger( L, pos + p.Length );
					return 2;
				}
			}
			else
			{
				Match m = getMatch( L, s, p, init );
				if ( m.Success )
				{
					Capture cap = m.Groups[0].Captures[0];
					lua_pushinteger( L, cap.Index + 1 );
					lua_pushinteger( L, cap.Index + cap.Length );

					return pushCaptures( L, m ) + 2;
				}
			}
			lua_pushnil( L );
			return 1;
		}


		private static int str_match( lua_State L )
		{
			string s = luaL_checkstring( L, 1 );
			string p = luaL_checkstring( L, 2 );
			ptrdiff_t init = Math.Min( Math.Max( posrelat( luaL_optinteger( L, 3, 1 ), s.Length ) - 1, 0 ), s.Length );
			Match m = getMatch( L, s, p, init );
			if ( m.Success )
			{
				return pushCaptures( L, m );
			}
			lua_pushnil( L );
			return 1;
		}


		private static int gmatch_aux( lua_State L )
		{
			Match m = (Match)lua_touserdata( L, lua_upvalueindex( 1 ) );
			if ( m.Success )
			{
				int captures = pushCaptures( L, m );

				try
				{
					m = m.NextMatch();
				}
				catch ( RegexMatchTimeoutException )
				{
					luaL_error( L, "Regex took too much cpu-time" );
				}
				lua_pushuserdata( L, m );
				lua_replace( L, lua_upvalueindex( 1 ) );

				return captures;
			}
			lua_pushnil( L );
			return 1;
		}


		private static int gmatch( lua_State L )
		{
			string s = luaL_checkstring( L, 1 );
			string p = luaL_checkstring( L, 2 );
			ptrdiff_t init = Math.Min( Math.Max( posrelat( luaL_optinteger( L, 3, 1 ), s.Length ) - 1, 0 ), s.Length );


			lua_pushuserdata( L, getMatch( L, s, p, init ) );
			lua_pushcclosure( L, gmatch_aux, 1 );
			return 1;
		}

		private static Regex captureRegex = new Regex( "%([1-9])", RegexOptions.Compiled );
		private static string add_value_str( lua_State L, Match m, string p )
		{
			List<string> captures = getCaptures( m );
			return captureRegex.Replace( p, ( Match repl ) =>
			 {
				 int d = int.Parse( repl.Groups[0].Captures[0].Value );
				 if ( d >= captures.Count )
					 luaL_error( L, "invalid replacement capture (%d)", d );
				 return captures[d];
			 } );
		}

		private static string add_value_func( lua_State L, Match m, string p )
		{
			lua_pushvalue( L, 3 );
			int n = pushCaptures( L, m );
			lua_call( L, n, 1 );
			if ( lua_toboolean( L, -1 ) == 0 )
			{  /* nil or false? */
				lua_pop( L, 1 );
				return m.Value;
			}
			else if ( lua_isstring( L, -1 ) == 0 )
				luaL_error( L, "invalid replacement value (a %s)", luaL_typename( L, -1 ) );
			string ret = lua_tostring( L, -1 );
			lua_pop( L, 1 );
			return ret;
		}
		private static string add_value_table( lua_State L, Match m, string p )
		{
			lua_pushstring( L, m.Value );
			lua_gettable( L, 3 );
			if ( lua_toboolean( L, -1 ) == 0 )
			{  /* nil or false? */
				lua_pop( L, 1 );
				return m.Value;
			}
			else if ( lua_isstring( L, -1 ) == 0 )
				luaL_error( L, "invalid replacement value (a %s)", luaL_typename( L, -1 ) );
			string ret = lua_tostring( L, -1 );
			lua_pop( L, 1 );
			return ret;
		}

		private static int str_gsub( lua_State L )
		{
			string s = luaL_checkstring( L, 1 );
			string p = luaL_checkstring( L, 2 );
			int tr = lua_type( L, 3 );
			luaL_argcheck( L, tr == LUA_TNUMBER || tr == LUA_TSTRING || tr == LUA_TFUNCTION || tr == LUA_TTABLE, 3, "string/function/table expected" );
			int max_r = luaL_optinteger( L, 4, s.Length + 1 );

			int n = 0;
			try
			{
				Regex rx = new Regex( p, RegexOptions.Compiled, TimeSpan.FromSeconds( 0.05 ) );
				switch ( tr )
				{
					case LUA_TNUMBER:
					case LUA_TSTRING:
						{
							lua_pushstring( L, rx.Replace( s, ( Match m ) => { ++n; return add_value_str( L, m, p ); }, max_r ) );
							break;
						}
					case LUA_TFUNCTION:
						{
							lua_pushstring( L, rx.Replace( s, ( Match m ) => { ++n; return add_value_func( L, m, p ); }, max_r ) );
							break;
						}
					case LUA_TTABLE:
						{
							lua_pushstring( L, rx.Replace( s, ( Match m ) => { ++n; return add_value_table( L, m, p ); }, max_r ) );
							break;
						}
				}
			}
			catch ( ArgumentException )
			{
				luaL_error( L, "Invalid regex" );
			}
			catch ( RegexMatchTimeoutException )
			{
				luaL_error( L, "Regex took too much cpu-time" );
			}
			lua_pushinteger( L, n );  /* number of substitutions */
			return 2;
		}

		private readonly static luaL_Reg[] strlib = {
			new luaL_Reg("byte", str_byte),
			new luaL_Reg("char", str_char),
			new luaL_Reg("find", str_find),
			//new luaL_Reg("format", str_format),
			new luaL_Reg("gmatch", gmatch),
			new luaL_Reg("gsub", str_gsub),
			new luaL_Reg("len", str_len),
			new luaL_Reg("lower", str_lower),
			new luaL_Reg("match", str_match),
			new luaL_Reg("rep", str_rep),
			new luaL_Reg("reverse", str_reverse),
			new luaL_Reg("sub", str_sub),
			new luaL_Reg("upper", str_upper)
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

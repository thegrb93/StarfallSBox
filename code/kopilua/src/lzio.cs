/*
** $Id: lzio.c,v 1.31.1.1 2007/12/27 13:02:25 roberto Exp $
** a generic input stream interface
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using ZIO = Lua.Zio;

	public partial class Lua
	{
		public const int EOZ = -1;          /* end of stream */

		//public class ZIO : Zio { };

		public static int char2int( char c ) { return (int)c; }

		public static int zgetc( ZIO z )
		{
			if ( z.n-- > 0 )
			{
				int ch = char2int( z.p[0] );
				z.p.inc();
				return ch;
			}
			else
				return luaZ_fill( z );
		}

		public class Mbuffer
		{
			public CharPtr buffer = new CharPtr();
			public uint n;
			public uint buffsize;
		};

		public static void luaZ_initbuffer( lua_State L, Mbuffer buff )
		{
			buff.buffer = null;
		}

		public static CharPtr luaZ_buffer( Mbuffer buff ) { return buff.buffer; }
		public static uint luaZ_sizebuffer( Mbuffer buff ) { return buff.buffsize; }
		public static uint luaZ_bufflen( Mbuffer buff ) { return buff.n; }
		public static void luaZ_resetbuffer( Mbuffer buff ) { buff.n = 0; }


		public static void luaZ_resizebuffer( lua_State L, Mbuffer buff, int size )
		{
			if ( buff.buffer == null )
				buff.buffer = new CharPtr();
			luaM_reallocvector( L, ref buff.buffer.chars, (int)buff.buffsize, size );
			buff.buffsize = (uint)buff.buffer.chars.Length;
		}

		public static void luaZ_freebuffer( lua_State L, Mbuffer buff ) { luaZ_resizebuffer( L, buff, 0 ); }



		/* --------- Private Part ------------------ */

		public class Zio
		{
			public uint n;          /* bytes still unread */
			public CharPtr p;           /* current position in buffer */
			public lua_Reader reader;
			public object data;         /* additional data */
			public lua_State L;         /* Lua state (for reader) */
		};


		public static int luaZ_fill( ZIO z )
		{
			lua_State L = z.L;
			string buff = z.reader( L, z.data );
			if ( string.IsNullOrEmpty( buff ) ) return EOZ;
			z.n = (uint)buff.Length;
			z.p = new CharPtr( buff );
			int result = char2int( z.p[0] );
			z.p.inc();
			return result;
		}


		public static int luaZ_lookahead( ZIO z )
		{
			if ( z.n == 0 )
			{
				if ( luaZ_fill( z ) == EOZ )
					return EOZ;
				else
				{
					z.n++;  /* luaZ_fill removed first byte; put back it */
					z.p.dec();
				}
			}
			return char2int( z.p[0] );
		}


		public static void luaZ_init( lua_State L, ZIO z, lua_Reader reader, object data )
		{
			z.L = L;
			z.reader = reader;
			z.data = data;
			z.n = 0;
			z.p = null;
		}

		/* ------------------------------------------------------------------------ */
		public static CharPtr luaZ_openspace( lua_State L, Mbuffer buff, uint n )
		{
			if ( n > buff.buffsize )
			{
				if ( n < LUA_MINBUFFER ) n = LUA_MINBUFFER;
				luaZ_resizebuffer( L, buff, (int)n );
			}
			return buff.buffer;
		}


	}
}

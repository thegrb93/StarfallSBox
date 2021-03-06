//#define lua_assert

/*
** $Id: llimits.h,v 1.69.1.1 2007/12/27 13:02:25 roberto Exp $
** Limits, basic types, and some other `installation-dependent' definitions
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using lu_int32 = System.UInt32;
	using lu_mem = System.UInt32;
	using l_mem = System.Int32;
	using lu_byte = System.Byte;
	using l_uacNumber = System.Double;
	using lua_Number = System.Double;
	using Instruction = System.UInt32;

	public partial class Lua
	{

		//typedef LUAI_UINT32 lu_int32;

		//typedef LUAI_UMEM lu_mem;

		//typedef LUAI_MEM l_mem;



		/* chars used as small naturals (so that `char' is reserved for characters) */
		//typedef unsigned char lu_byte;


		public const uint MAX_SIZET = uint.MaxValue - 2;

		public const lu_mem MAX_LUMEM = lu_mem.MaxValue - 2;


		public const int MAX_INT = (Int32.MaxValue - 2);  /* maximum value of an int (-2 for safety) */
		
		internal static void lua_assert( bool c ) { }

		internal static void lua_assert( int c ) { }

		internal static object check_exp( bool c, object e ) { return e; }

		internal static void api_check( object o, bool e ) { }

		//#define UNUSED(x)	((void)(x))	/* to avoid warnings */


		internal static lu_byte cast_byte( int i ) { return (lu_byte)i; }
		internal static lu_byte cast_byte( long i ) { return (lu_byte)(int)i; }
		internal static lu_byte cast_byte( bool i ) { return i ? (lu_byte)1 : (lu_byte)0; }
		internal static lu_byte cast_byte( lua_Number i ) { return (lu_byte)i; }
		internal static lu_byte cast_byte( object i ) { return (lu_byte)(int)(i); }

		internal static int cast_int( int i ) { return (int)i; }
		internal static int cast_int( uint i ) { return (int)i; }
		internal static int cast_int( long i ) { return (int)(int)i; }
		internal static int cast_int( ulong i ) { return (int)(int)i; }
		internal static int cast_int( bool i ) { return i ? (int)1 : (int)0; }
		internal static int cast_int( lua_Number i ) { return (int)i; }
		internal static int cast_int( object i ) { Assert( false, "Can't convert int." ); return Convert.ToInt32( i ); }

		internal static lua_Number cast_num( int i ) { return (lua_Number)i; }
		internal static lua_Number cast_num( uint i ) { return (lua_Number)i; }
		internal static lua_Number cast_num( long i ) { return (lua_Number)i; }
		internal static lua_Number cast_num( ulong i ) { return (lua_Number)i; }
		internal static lua_Number cast_num( bool i ) { return i ? (lua_Number)1 : (lua_Number)0; }
		internal static lua_Number cast_num( object i ) { Assert( false, "Can't convert number." ); return Convert.ToSingle( i ); }

		/*
		** type for virtual-machine instructions
		** must be an unsigned with (at least) 4 bytes (see details in lopcodes.h)
		*/
		//typedef lu_int32 Instruction;



		/* maximum stack for a Lua function */
		public const int MAXSTACK = 250;



		/* minimum size for the string table (must be power of 2) */
		public const int MINSTRTABSIZE = 32;


		/* minimum size for string buffer */
		public const int LUA_MINBUFFER = 32;


		/*
		** macro to control inclusion of some hard tests on stack reallocation
		*/
		//#ifndef HARDSTACKTESTS
		//#define condhardstacktests(x)	((void)0)
		//#else
		//#define condhardstacktests(x)	x
		//#endif

	}
}

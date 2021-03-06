/*
** $Id: lmem.c,v 1.70.1.1 2007/12/27 13:02:25 roberto Exp $
** Interface to Memory Manager
** See Copyright Notice in lua.h
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{
	public partial class Lua
	{
		public const string MEMERRMSG = "not enough memory";

		public static T[] luaM_reallocv<T>( lua_State L, T[] block, int new_size )
		{
			return (T[])luaM_realloc_( L, block, new_size );
		}
		public static void luaM_freemem<T>( lua_State L, T b ) { luaM_realloc_<T>( L, new T[] { b }, 0 ); }
		public static void luaM_free<T>( lua_State L, T b ) { luaM_realloc_<T>( L, new T[] { b }, 0 ); }
		public static void luaM_freearray<T>( lua_State L, T[] b ) { luaM_reallocv( L, b, 0 ); }
		public static T luaM_new<T>( lua_State L ) { return (T)luaM_realloc_<T>( L ); }
		public static T[] luaM_newvector<T>( lua_State L, int n )
		{
			return luaM_reallocv<T>( L, null, n );
		}

		public static void luaM_growvector<T>( lua_State L, ref T[] v, int nelems, ref int size, int limit, string e )
		{
			if ( nelems + 1 > size )
				v = (T[])luaM_growaux_( L, ref v, ref size, limit, e );
		}

		public static T[] luaM_reallocvector<T>( lua_State L, ref T[] v, int oldn, int n )
		{
			Assert( (v == null && oldn == 0) || (v.Length == oldn) );
			v = luaM_reallocv<T>( L, v, n );
			return v;
		}


		/*
		** About the realloc function:
		** void * frealloc (void *ud, void *ptr, uint osize, uint nsize);
		** (`osize' is the old size, `nsize' is the new size)
		**
		** Lua ensures that (ptr == null) iff (osize == 0).
		**
		** * frealloc(ud, null, 0, x) creates a new block of size `x'
		**
		** * frealloc(ud, p, x, 0) frees the block `p'
		** (in this specific case, frealloc must return null).
		** particularly, frealloc(ud, null, 0, 0) does nothing
		** (which is equivalent to free(null) in ANSI C)
		**
		** frealloc returns null if it cannot create or reallocate the area
		** (any reallocation to an equal or smaller size cannot fail!)
		*/



		public const int MINSIZEARRAY = 4;


		public static T[] luaM_growaux_<T>( lua_State L, ref T[] block, ref int size,
							 int limit, string errormsg )
		{
			T[] newblock;
			int newsize;
			if ( size >= limit / 2 )
			{  /* cannot double it? */
				if ( size >= limit )  /* cannot grow even a little? */
					luaG_runerror( L, errormsg );
				newsize = limit;  /* still have at least one free place */
			}
			else
			{
				newsize = size * 2;
				if ( newsize < MINSIZEARRAY )
					newsize = MINSIZEARRAY;  /* minimum size */
			}
			newblock = luaM_reallocv<T>( L, block, newsize );
			size = newsize;  /* update only when everything else is OK */
			return newblock;
		}


		public static object luaM_toobig( lua_State L )
		{
			luaG_runerror( L, "memory allocation error: block too big" );
			return null;  /* to avoid warnings */
		}



		/*
		** generic allocation routine.
		*/

		public static object luaM_realloc_<T>( lua_State L )
		{
			AddTotalBytes( L, GetUnmanagedSize( typeof( T ) ) );
			return System.Activator.CreateInstance<T>();
		}

		public static object luaM_realloc_<T>( lua_State L, T[] old_block, int new_size )
		{
			int unmanaged_size = GetUnmanagedSize( typeof( T ) );
			int old_size = (old_block == null) ? 0 : old_block.Length;
			int osize = old_size * unmanaged_size;
			int nsize = new_size * unmanaged_size;
			T[] new_block = new T[new_size];
			for ( int i = 0; i < Math.Min( old_size, new_size ); i++ )
				new_block[i] = old_block[i];
			for ( int i = old_size; i < new_size; i++ )
				new_block[i] = System.Activator.CreateInstance<T>();
			if ( CanIndex( typeof( T ) ) )
				for ( int i = 0; i < new_size; i++ )
				{
					ArrayElement elem = new_block[i] as ArrayElement;
					Assert( elem != null, String.Format( "Need to derive type {0} from ArrayElement", typeof( T ).ToString() ) );
					elem.set_index( i );
					elem.set_array( new_block );
				}
			SubtractTotalBytes( L, osize );
			AddTotalBytes( L, nsize );
			return new_block;
		}

		public static bool CanIndex( Type t )
		{
			if ( t == typeof( char ) )
				return false;
			if ( t == typeof( byte ) )
				return false;
			if ( t == typeof( int ) )
				return false;
			if ( t == typeof( uint ) )
				return false;
			if ( t == typeof( LocVar ) )
				return false;
			return true;
		}

		static void AddTotalBytes( lua_State L, int num_bytes ) { G( L ).totalbytes += (uint)num_bytes; }
		static void SubtractTotalBytes( lua_State L, int num_bytes ) { G( L ).totalbytes -= (uint)num_bytes; }
	}
}

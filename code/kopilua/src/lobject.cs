/*
** $Id: lobject.c,v 2.22.1.1 2007/12/27 13:02:25 roberto Exp $
** Some generic functions over Lua objects
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lu_byte = System.Byte;
	using lua_Number = System.Double;
	using l_uacNumber = System.Double;
	using Instruction = System.UInt32;

	public partial class Lua
	{
		/* tags for values visible from Lua */
		public const int LAST_TAG = LUA_TTHREAD;

		public const int NUM_TAGS = (LAST_TAG + 1);


		/*
		** Extra tags for non-values
		*/
		public const int LUA_TPROTO = (LAST_TAG + 1);
		public const int LUA_TUPVAL = (LAST_TAG + 2);
		public const int LUA_TDEADKEY = (LAST_TAG + 3);

		public interface ArrayElement
		{
			void set_index( int index );
			void set_array( object array );
		}


		/*
		** Common Header for all collectable objects (in macro form, to be
		** included in other objects)
		*/
		public class CommonHeader
		{
			public GCObject next;
			public lu_byte tt;
			public lu_byte marked;
		}


		/*
		** Common header in struct form
		*/
		public class GCheader : CommonHeader
		{
		};




		/*
		** Union of all Lua values (in c# we use virtual data members and boxing)
		*/
		public class Value
		{

			// in the original code Value is a struct, so all assignments in the code
			// need to be replaced with a call to Copy. as it turns out, there are only
			// a couple. the vast majority of references to Value are the instance that
			// appears in the TValue class, so if you make that a virtual data member and
			// omit the set accessor then you'll get a compiler error if anything tries
			// to set it.
			public void Copy( Value copy )
			{
				this.p = copy.p;
			}

			public GCObject gc
			{
				get { return (GCObject)this.p; }
				set { this.p = value; }
			}
			public object p;
			public lua_Number n
			{
				get { return (lua_Number)this.p; }
				set { this.p = (object)value; }
			}
			public int b
			{
				get { return (int)this.p; }
				set { this.p = (object)value; }
			}
		};


		/*
		** Tagged Values
		*/

		//#define TValuefields	Value value; int tt

		public class lua_TValue : ArrayElement
		{
			private lua_TValue[] values = null;
			private int index = -1;

			public void set_index( int index )
			{
				this.index = index;
			}

			public void set_array( object array )
			{
				this.values = (lua_TValue[])array;
				Assert( this.values != null );
			}

			public lua_TValue this[int offset]
			{
				get { return this.values[this.index + offset]; }
			}

			public lua_TValue this[uint offset]
			{
				get { return this.values[this.index + (int)offset]; }
			}

			public static lua_TValue operator +( lua_TValue value, int offset )
			{
				return value.values[value.index + offset];
			}

			public static lua_TValue operator +( int offset, lua_TValue value )
			{
				return value.values[value.index + offset];
			}

			public static lua_TValue operator -( lua_TValue value, int offset )
			{
				return value.values[value.index - offset];
			}

			public static int operator -( lua_TValue value, lua_TValue[] array )
			{
				Assert( value.values == array );
				return value.index;
			}

			public static int operator -( lua_TValue a, lua_TValue b )
			{
				Assert( a.values == b.values );
				return a.index - b.index;
			}

			public static bool operator <( lua_TValue a, lua_TValue b )
			{
				Assert( a.values == b.values );
				return a.index < b.index;
			}

			public static bool operator <=( lua_TValue a, lua_TValue b )
			{
				Assert( a.values == b.values );
				return a.index <= b.index;
			}

			public static bool operator >( lua_TValue a, lua_TValue b )
			{
				Assert( a.values == b.values );
				return a.index > b.index;
			}

			public static bool operator >=( lua_TValue a, lua_TValue b )
			{
				Assert( a.values == b.values );
				return a.index >= b.index;
			}

			public static lua_TValue inc( ref lua_TValue value )
			{
				value = value[1];
				return value[-1];
			}

			public static lua_TValue dec( ref lua_TValue value )
			{
				value = value[-1];
				return value[1];
			}

			public static implicit operator int( lua_TValue value )
			{
				return value.index;
			}

			public lua_TValue()
			{
			}

			public lua_TValue( lua_TValue copy )
			{
				this.values = copy.values;
				this.index = copy.index;
				this.value.Copy( copy.value );
				this.tt = copy.tt;
			}

			public lua_TValue( Value value, int tt )
			{
				this.values = null;
				this.index = 0;
				this.value.Copy( value );
				this.tt = tt;
			}

			public Value value = new Value();
			public int tt;
		};


		/* Macros to test type */
		internal static bool ttisnil( TValue o ) { return (ttype( o ) == LUA_TNIL); }
		internal static bool ttisnumber( TValue o ) { return (ttype( o ) == LUA_TNUMBER); }
		internal static bool ttisstring( TValue o ) { return (ttype( o ) == LUA_TSTRING); }
		internal static bool ttistable( TValue o ) { return (ttype( o ) == LUA_TTABLE); }
		internal static bool ttisfunction( TValue o ) { return (ttype( o ) == LUA_TFUNCTION); }
		internal static bool ttisboolean( TValue o ) { return (ttype( o ) == LUA_TBOOLEAN); }
		internal static bool ttisuserdata( TValue o ) { return (ttype( o ) == LUA_TUSERDATA); }
		internal static bool ttisthread( TValue o ) { return (ttype( o ) == LUA_TTHREAD); }
		internal static bool ttislightuserdata( TValue o ) { return (ttype( o ) == LUA_TLIGHTUSERDATA); }

		/* Macros to access values */
#if DEBUG
		internal static int ttype( TValue o ) { return o.tt; }
		internal static int ttype( CommonHeader o ) { return o.tt; }
		internal static GCObject gcvalue( TValue o ) { return (GCObject)check_exp( iscollectable( o ), o.value.gc ); }
		internal static object pvalue( TValue o ) { return (object)check_exp( ttislightuserdata( o ), o.value.p ); }
		internal static lua_Number nvalue( TValue o ) { return (lua_Number)check_exp( ttisnumber( o ), o.value.n ); }
		internal static TString rawtsvalue( TValue o ) { return (TString)check_exp( ttisstring( o ), o.value.gc.ts ); }
		internal static TString_tsv tsvalue( TValue o ) { return rawtsvalue( o ).tsv; }
		internal static Udata rawuvalue( TValue o ) { return (Udata)check_exp( ttisuserdata( o ), o.value.gc.u ); }
		internal static Udata_uv uvalue( TValue o ) { return rawuvalue( o ).uv; }
		internal static Closure clvalue( TValue o ) { return (Closure)check_exp( ttisfunction( o ), o.value.gc.cl ); }
		internal static Table hvalue( TValue o ) { return (Table)check_exp( ttistable( o ), o.value.gc.h ); }
		internal static int bvalue( TValue o ) { return (int)check_exp( ttisboolean( o ), o.value.b ); }
		internal static lua_State thvalue( TValue o ) { return (lua_State)check_exp( ttisthread( o ), o.value.gc.th ); }
#else
		internal static int ttype(TValue o) { return o.tt; }
		internal static int ttype(CommonHeader o) { return o.tt; }
		internal static GCObject gcvalue(TValue o) { return o.value.gc; }
		internal static object pvalue(TValue o) { return o.value.p; }
		internal static lua_Number nvalue(TValue o) { return o.value.n; }
		internal static TString rawtsvalue(TValue o) { return o.value.gc.ts; }
		internal static TString_tsv tsvalue(TValue o) { return rawtsvalue(o).tsv; }
		internal static Udata rawuvalue(TValue o) { return o.value.gc.u; }
		internal static Udata_uv uvalue(TValue o) { return rawuvalue(o).uv; }
		internal static Closure clvalue(TValue o) { return o.value.gc.cl; }
		internal static Table hvalue(TValue o) { return o.value.gc.h; }
		internal static int bvalue(TValue o) { return o.value.b; }
		internal static lua_State thvalue(TValue o) { return (lua_State)check_exp(ttisthread(o), o.value.gc.th); }
#endif

		public static int l_isfalse( TValue o ) { return ((ttisnil( o ) || (ttisboolean( o ) && bvalue( o ) == 0))) ? 1 : 0; }

		/*
		** for internal debug only
		*/
		internal static void checkconsistency( TValue obj )
		{
		}

		internal static void checkliveness( global_State g, TValue obj )
		{
		}

		/* Macros to set values */
		internal static void setnilvalue( TValue obj )
		{
			obj.tt = LUA_TNIL;
		}

		internal static void setnvalue( TValue obj, lua_Number x )
		{
			obj.value.n = x;
			obj.tt = LUA_TNUMBER;
		}

		internal static void setpvalue( TValue obj, object x )
		{
			obj.value.p = x;
			obj.tt = LUA_TLIGHTUSERDATA;
		}

		internal static void setbvalue( TValue obj, int x )
		{
			obj.value.b = x;
			obj.tt = LUA_TBOOLEAN;
		}

		internal static void setsvalue( lua_State L, TValue obj, GCObject x )
		{
			obj.value.gc = x;
			obj.tt = LUA_TSTRING;
			checkliveness( G( L ), obj );
		}

		internal static void setuvalue( lua_State L, TValue obj, GCObject x )
		{
			obj.value.gc = x;
			obj.tt = LUA_TUSERDATA;
			checkliveness( G( L ), obj );
		}

		internal static void setthvalue( lua_State L, TValue obj, GCObject x )
		{
			obj.value.gc = x;
			obj.tt = LUA_TTHREAD;
			checkliveness( G( L ), obj );
		}

		internal static void setclvalue( lua_State L, TValue obj, Closure x )
		{
			obj.value.gc = x;
			obj.tt = LUA_TFUNCTION;
			checkliveness( G( L ), obj );
		}

		internal static void sethvalue( lua_State L, TValue obj, Table x )
		{
			obj.value.gc = x;
			obj.tt = LUA_TTABLE;
			checkliveness( G( L ), obj );
		}

		internal static void setptvalue( lua_State L, TValue obj, Proto x )
		{
			obj.value.gc = x;
			obj.tt = LUA_TPROTO;
			checkliveness( G( L ), obj );
		}

		internal static void setobj( lua_State L, TValue obj1, TValue obj2 )
		{
			obj1.value.Copy( obj2.value );
			obj1.tt = obj2.tt;
			checkliveness( G( L ), obj1 );
		}


		/*
		** different types of sets, according to destination
		*/

		/* from stack to (same) stack */
		//#define setobjs2s	setobj
		internal static void setobjs2s( lua_State L, TValue obj, TValue x ) { setobj( L, obj, x ); }
		///* to stack (not from same stack) */

		//#define setobj2s	setobj
		internal static void setobj2s( lua_State L, TValue obj, TValue x ) { setobj( L, obj, x ); }

		//#define setsvalue2s	setsvalue
		internal static void setsvalue2s( lua_State L, TValue obj, TString x ) { setsvalue( L, obj, x ); }

		//#define sethvalue2s	sethvalue
		internal static void sethvalue2s( lua_State L, TValue obj, Table x ) { sethvalue( L, obj, x ); }

		//#define setptvalue2s	setptvalue
		internal static void setptvalue2s( lua_State L, TValue obj, Proto x ) { setptvalue( L, obj, x ); }

		///* from table to same table */
		//#define setobjt2t	setobj
		internal static void setobjt2t( lua_State L, TValue obj, TValue x ) { setobj( L, obj, x ); }

		///* to table */
		//#define setobj2t	setobj
		internal static void setobj2t( lua_State L, TValue obj, TValue x ) { setobj( L, obj, x ); }

		///* to new object */
		//#define setobj2n	setobj
		internal static void setobj2n( lua_State L, TValue obj, TValue x ) { setobj( L, obj, x ); }

		//#define setsvalue2n	setsvalue
		internal static void setsvalue2n( lua_State L, TValue obj, TString x ) { setsvalue( L, obj, x ); }

		internal static void setttype( TValue obj, int tt ) { obj.tt = tt; }


		internal static bool iscollectable( TValue o ) { return (ttype( o ) >= LUA_TSTRING); }



		//typedef TValue *StkId;  /* index to stack elements */

		/*
		** String headers for string table
		*/
		public class TString_tsv : GCObject
		{
			public lu_byte reserved;
			public uint hash;
			public uint len;
		};
		public class TString : TString_tsv
		{
			//public L_Umaxalign dummy;  /* ensures maximum alignment for strings */			
			public TString_tsv tsv { get { return this; } }

			public TString()
			{
			}
			public TString( string str ) { this.str = str; }

			public string str;

			public override string ToString() { return str.ToString(); } // for debugging
		};

		public static string getstr( TString ts ) { return ts.str; }
		public static string svalue( StkId o ) { return getstr( rawtsvalue( o ) ); }

		public class Udata_uv : GCObject
		{
			public Table metatable;
			public Table env;
			public uint len;
		};

		public class Udata : Udata_uv
		{
			public Udata() { this.uv = this; }

			public new Udata_uv uv;

			//public L_Umaxalign dummy;  /* ensures maximum alignment for `local' udata */

			// in the original C code this was allocated alongside the structure memory. it would probably
			// be possible to still do that by allocating memory and pinning it down, but we can do the
			// same thing just as easily by allocating a seperate byte array for it instead.
			public object user_data;
		};




		/*
		** Function Prototypes
		*/
		public class Proto : GCObject
		{

			public Proto[] protos = null;
			public int index = 0;
			public Proto this[int offset] { get { return this.protos[this.index + offset]; } }

			public TValue[] k;  /* constants used by the function */
			public Instruction[] code;
			public new Proto[] p;  /* functions defined inside the function */
			public int[] lineinfo;  /* map from opcodes to source lines */
			public LocVar[] locvars;  /* information about local variables */
			public TString[] upvalues;  /* upvalue names */
			public TString source;
			public int sizeupvalues;
			public int sizek;  /* size of `k' */
			public int sizecode;
			public int sizelineinfo;
			public int sizep;  /* size of `p' */
			public int sizelocvars;
			public int linedefined;
			public int lastlinedefined;
			public GCObject gclist;
			public lu_byte nups;  /* number of upvalues */
			public lu_byte numparams;
			public lu_byte is_vararg;
			public lu_byte maxstacksize;
		};


		/* masks for new-style vararg */
		public const int VARARG_HASARG = 1;
		public const int VARARG_ISVARARG = 2;
		public const int VARARG_NEEDSARG = 4;

		public class LocVar
		{
			public TString varname;
			public int startpc;  /* first point where variable is active */
			public int endpc;    /* first point where variable is dead */
		};



		/*
		** Upvalues
		*/

		public class UpVal : GCObject
		{
			public TValue v;  /* points to stack or to its own value */

			public class _u
			{
				public TValue value = new TValue();  /* the value (when closed) */

				public class _l
				{  /* double linked list (when open) */
					public UpVal prev;
					public UpVal next;
				};

				public _l l = new _l();
			}
			public new _u u = new _u();
		};


		/*
		** Closures
		*/

		public class ClosureHeader : GCObject
		{
			public lu_byte isC;
			public lu_byte nupvalues;
			public GCObject gclist;
			public Table env;
		};

		public class ClosureType
		{

			ClosureHeader header;

			public static implicit operator ClosureHeader( ClosureType ctype ) { return ctype.header; }
			public ClosureType( ClosureHeader header ) { this.header = header; }

			public lu_byte isC { get { return header.isC; } set { header.isC = value; } }
			public lu_byte nupvalues { get { return header.nupvalues; } set { header.nupvalues = value; } }
			public GCObject gclist { get { return header.gclist; } set { header.gclist = value; } }
			public Table env { get { return header.env; } set { header.env = value; } }
		}

		public class CClosure : ClosureType
		{
			public CClosure( ClosureHeader header ) : base( header ) { }
			public lua_CFunction f;
			public TValue[] upvalue;
		};


		public class LClosure : ClosureType
		{
			public LClosure( ClosureHeader header ) : base( header ) { }
			public Proto p;
			public UpVal[] upvals;
		};

		public class Closure : ClosureHeader
		{
			public Closure()
			{
				c = new CClosure( this );
				l = new LClosure( this );
			}

			public CClosure c;
			public LClosure l;
		};


		public static bool iscfunction( TValue o ) { return ((ttype( o ) == LUA_TFUNCTION) && (clvalue( o ).c.isC != 0)); }
		public static bool isLfunction( TValue o ) { return ((ttype( o ) == LUA_TFUNCTION) && (clvalue( o ).c.isC == 0)); }


		/*
		** Tables
		*/

		public class TKey_nk : TValue
		{
			public TKey_nk() { }
			public TKey_nk( Value value, int tt, Node next ) : base( value, tt )
			{
				this.next = next;
			}
			public Node next;  /* for chaining */
		};

		public class TKey
		{
			public TKey()
			{
				this.nk = new TKey_nk();
			}
			public TKey( TKey copy )
			{
				this.nk = new TKey_nk( copy.nk.value, copy.nk.tt, copy.nk.next );
			}
			public TKey( Value value, int tt, Node next )
			{
				this.nk = new TKey_nk( value, tt, next );
			}

			public TKey_nk nk = new TKey_nk();
			public TValue tvk { get { return this.nk; } }
		};


		public class Node : ArrayElement
		{
			private Node[] values = null;
			private int index = -1;

			public void set_index( int index )
			{
				this.index = index;
			}

			public void set_array( object array )
			{
				this.values = (Node[])array;
				Assert( this.values != null );
			}

			public Node()
			{
				this.i_val = new TValue();
				this.i_key = new TKey();
			}

			public Node( Node copy )
			{
				this.values = copy.values;
				this.index = copy.index;
				this.i_val = new TValue( copy.i_val );
				this.i_key = new TKey( copy.i_key );
			}

			public Node( TValue i_val, TKey i_key )
			{
				this.values = new Node[] { this };
				this.index = 0;
				this.i_val = i_val;
				this.i_key = i_key;
			}

			public TValue i_val;
			public TKey i_key;

			public Node this[uint offset]
			{
				get { return this.values[this.index + (int)offset]; }
			}

			public Node this[int offset]
			{
				get { return this.values[this.index + offset]; }
			}

			public static int operator -( Node n1, Node n2 )
			{
				Assert( n1.values == n2.values );
				return n1.index - n2.index;
			}

			public static Node inc( ref Node node )
			{
				node = node[1];
				return node[-1];
			}

			public static Node dec( ref Node node )
			{
				node = node[-1];
				return node[1];
			}

			public static bool operator >( Node n1, Node n2 ) { Assert( n1.values == n2.values ); return n1.index > n2.index; }
			public static bool operator >=( Node n1, Node n2 ) { Assert( n1.values == n2.values ); return n1.index >= n2.index; }
			public static bool operator <( Node n1, Node n2 ) { Assert( n1.values == n2.values ); return n1.index < n2.index; }
			public static bool operator <=( Node n1, Node n2 ) { Assert( n1.values == n2.values ); return n1.index <= n2.index; }
			public static bool operator ==( Node n1, Node n2 )
			{
				object o1 = n1 as Node;
				object o2 = n2 as Node;
				if ( (o1 == null) && (o2 == null) ) return true;
				if ( o1 == null ) return false;
				if ( o2 == null ) return false;
				if ( n1.values != n2.values ) return false;
				return n1.index == n2.index;
			}
			public static bool operator !=( Node n1, Node n2 ) { return !(n1 == n2); }

			public override bool Equals( object o ) { return this == (Node)o; }
			public override int GetHashCode() { return 0; }
		};


		public class Table : GCObject
		{
			public lu_byte flags;  /* 1<<p means tagmethod(p) is not present */
			public lu_byte lsizenode;  /* log2 of size of `node' array */
			public Table metatable;
			public TValue[] array;  /* array part */
			public Node[] node;
			public int lastfree;  /* any free position is before this position */
			public GCObject gclist;
			public int sizearray;  /* size of `array' array */
		};



		/*
		** `module' operation for hashing (size is always a power of 2)
		*/
		//#define lmod(s,size) \
		//    (check_exp((size&(size-1))==0, (cast(int, (s) & ((size)-1)))))


		internal static int twoto( int x ) { return 1 << x; }
		internal static int sizenode( Table t ) { return twoto( t.lsizenode ); }

		public static TValue luaO_nilobject_ = new TValue( new Value(), LUA_TNIL );
		public static TValue luaO_nilobject = luaO_nilobject_;

		public static int ceillog2( int x ) { return luaO_log2( (uint)(x - 1) ) + 1; }



		/*
		** converts an integer to a "floating point byte", represented as
		** (eeeeexxx), where the real value is (1xxx) * 2^(eeeee - 1) if
		** eeeee != 0 and (xxx) otherwise.
		*/
		public static int luaO_int2fb( uint x )
		{
			int e = 0;  /* expoent */
			while ( x >= 16 )
			{
				x = (x + 1) >> 1;
				e++;
			}
			if ( x < 8 ) return (int)x;
			else return ((e + 1) << 3) | (cast_int( x ) - 8);
		}


		/* converts back */
		public static int luaO_fb2int( int x )
		{
			int e = (x >> 3) & 31;
			if ( e == 0 ) return x;
			else return ((x & 7) + 8) << (e - 1);
		}


		private readonly static lu_byte[] log_2 = {
			0,1,2,2,3,3,3,3,4,4,4,4,4,4,4,4,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
			6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
			7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
			7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
			8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
			8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
			8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
			8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8
		  };

		public static int luaO_log2( uint x )
		{
			int l = -1;
			while ( x >= 256 ) { l += 8; x >>= 8; }
			return l + log_2[x];

		}


		public static int luaO_rawequalObj( TValue t1, TValue t2 )
		{
			if ( ttype( t1 ) != ttype( t2 ) ) return 0;
			else switch ( ttype( t1 ) )
				{
					case LUA_TNIL:
						return 1;
					case LUA_TNUMBER:
						return luai_numeq( nvalue( t1 ), nvalue( t2 ) ) ? 1 : 0;
					case LUA_TBOOLEAN:
						return bvalue( t1 ) == bvalue( t2 ) ? 1 : 0;  /* boolean true must be 1....but not in C# !! */
					case LUA_TLIGHTUSERDATA:
						return pvalue( t1 ) == pvalue( t2 ) ? 1 : 0;
					default:
						lua_assert( iscollectable( t1 ) );
						return gcvalue( t1 ) == gcvalue( t2 ) ? 1 : 0;
				}
		}

		private const string number_chars = "0123456789+-eE.";
		public static int luaO_str2d( string s, out lua_Number result )
		{
			s = s.Trim();
			if ( s.Substring( 0, 2 ) == "0x" ) s = s.Substring( 2 );

			try
			{
				result = Convert.ToDouble( s );
				return 0;
			}
			catch ( System.OverflowException )
			{
				if ( s[0] == '-' )
					result = System.Double.NegativeInfinity;
				else
					result = System.Double.PositiveInfinity;
				return 1;
			}
			catch
			{
				result = 0;
				return 1;
			}
		}

		private static void pushstr( lua_State L, string str )
		{
			setsvalue2s( L, L.top, luaS_newstr( L, str ) );
			incr_top( L );
		}


		/* this function handles only `%d', `%c', %f, %p, and `%s' formats */
		public static string luaO_pushvfstring( lua_State L, string fmt, params object[] argp )
		{
			int parm_index = 0;
			int n = 1;
			int fmtindex = 0;
			pushstr( L, "" );
			for (; ; )
			{
				int e = fmt.IndexOf( '%', fmtindex );
				if ( e < 0 ) break;
				setsvalue2s( L, L.top, luaS_newstr( L, fmt.Substring( fmtindex, e - fmtindex) ) );
				incr_top( L );
				switch ( fmt[e + 1] )
				{
					case 's':
						{
							object o = argp[parm_index++];
							string s = o as string;
							if ( s == null ) s = "(null)";
							pushstr( L, s );
							break;
						}
					case 'c':
						{
							pushstr( L, ((char)(int)argp[parm_index++]).ToString() );
							break;
						}
					case 'd':
						{
							setnvalue( L.top, (int)argp[parm_index++] );
							incr_top( L );
							break;
						}
					case 'f':
						{
							setnvalue( L.top, (l_uacNumber)argp[parm_index++] );
							incr_top( L );
							break;
						}
					case 'p':
						{
							pushstr( L, String.Format( "0x{0,10:X}", argp[parm_index++].GetHashCode() ) );
							break;
						}
					case '%':
						{
							pushstr( L, "%" );
							break;
						}
					default:
						{
							pushstr( L, "%"+ fmt[e + 1] );
							break;
						}
				}
				n += 2;
				fmtindex = e + 2;
			}
			pushstr( L, fmt );
			luaV_concat( L, n + 1, cast_int( L.top - L.base_ ) - 1 );
			L.top -= n;
			return svalue( L.top - 1 );
		}

		public static string luaO_pushfstring( lua_State L, string fmt, params object[] args )
		{
			return luaO_pushvfstring( L, fmt, args );
		}


		public static string luaO_chunkid( string source, int bufflen )
		{
			if ( source[0] == '=' )
			{
				return source.Substring( 1 );
			}
			else
			{  /* out = "source", or "...source" */
				if ( source[0] == '@' )
				{
					string dots = " '...' ";
					int l = source.Length - 1;
					int sourceindex = 1;
					bufflen -= dots.Length + 1;
					StringBuilder sb = new StringBuilder();
					if ( l > bufflen )
					{
						sourceindex += (l - bufflen);  /* get last part of file name */
						sb.Append( "..." );
					}
					sb.Append( source.Substring( sourceindex ) );
					return sb.ToString();
				}
				else
				{  /* out = [string "string"] */
					int len = source.IndexOfAny( "\n\r".ToCharArray() );  /* stop at first newline */
					bufflen -= " [string \"...\"] ".Length + 1;
					if ( len > bufflen ) len = bufflen;
					StringBuilder sb = new StringBuilder();
					sb.Append( "[string \"" );
					if ( len > 0 )
					{  /* must truncate? */
						sb.Append( source.Substring( 0, len - 1 ) );
						sb.Append( "..." );
					}
					else
						sb.Append( source );
					sb.Append( "\"]" );
					return sb.ToString();
				}
			}
		}

	}
}

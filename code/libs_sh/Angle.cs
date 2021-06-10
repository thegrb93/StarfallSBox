using Sandbox;
using KopiLua;

namespace KopiLua
{
	public partial class Lua
	{
		public static void lua_pushangle( Lua.lua_State L, double p, double y, double r )
		{
			Lua.lua_createtable( L, 3, 0 );
			Lua.lua_pushnumber( L, p );
			Lua.lua_rawseti( L, -2, 1 );
			Lua.lua_pushnumber( L, y );
			Lua.lua_rawseti( L, -2, 2 );
			Lua.lua_pushnumber( L, r );
			Lua.lua_rawseti( L, -2, 3 );
			Lua.luaL_getmetatable( L, "Angle" );
			Lua.lua_setmetatable( L, -2 );
		}
		public static void lua_pushangle( Lua.lua_State L, Angles v )
		{
			lua_pushangle( L, (double)v.pitch, (double)v.yaw, (double)v.roll );
		}
		public static void lua_toangle( Lua.lua_State L, int index, out double x, out double y, out double z )
		{
			Lua.luaL_checktype( L, index, Lua.LUA_TTABLE );
			Lua.lua_rawgeti( L, index, 1 );
			x = Lua.lua_tonumber( L, -1 );
			Lua.lua_rawgeti( L, index, 2 );
			y = Lua.lua_tonumber( L, -1 );
			Lua.lua_rawgeti( L, index, 3 );
			z = Lua.lua_tonumber( L, -1 );
			Lua.lua_pop( L, 3 );
		}
		public static Angles lua_toangle( Lua.lua_State L, int index )
		{
			double p, y, r;
			lua_toangle( L, index, out p, out y, out r );
			return new Angles( (float)p, (float)y, (float)r );
		}
	}
}


namespace Starfall
{
	public partial class Instance
	{
		[SFInitialize( 1 )]
		public void AngleLib()
		{
			RegisterType( "Angle", new Lua.luaL_Reg[]{
				new Lua.luaL_Reg("getForward", (Lua.lua_State L) => {
					Lua.lua_pushvector(L, Lua.lua_toangle(L, 1).Direction);
					return 1;
				}),
				/*new Lua.luaL_Reg("getRight", (Lua.lua_State L) => {
					Lua.lua_pushvector(L, Lua.lua_toangle(L, 1).GetRight());
					return 1;
				}),
				new Lua.luaL_Reg("getUp", (Lua.lua_State L) => {
					Lua.lua_pushvector(L, Lua.lua_toangle(L, 1).GetUp());
					return 1;
				}),*/

				new Lua.luaL_Reg(null, null)
			} );
			Lua.lua_pop( L, 1 );

			Lua.luaL_dostring( L, @"
local ang_meta = getMetatable(""Angle"")

function Angle(x, y, z)
	return setmetatable({x, y, z}, ang_meta)
end
local Angle = Angle

function ang_meta.__add(a, b)
	return Angle(a[1]+b[2], a[2]+b[2], a[3]+b[3])
end

function ang_meta.__sub(a, b)
	return Angle(a[1]-b[2], a[2]-b[2], a[3]-b[3])
end

function ang_meta.__mul(a, b)
	local t1, t2 = type(a), type(b)
	if t1==""number"" then
		return Angle(a*b[1], a*b[2], a*b[3])
	elseif t2==""number"" then
		return Angle(a[1]*b, a[2]*b, a[3]*b)
	else
		error(""Expected multiplication with a number!"")
	end
end

function ang_meta.__div(a, b)
	b = 1/b
	if type(b)==""number"" then
		return Angle(a[1]*b, a[2]*b, a[3]*b)
	else
		error(""Expected division by a rhs number"")
	end
end

function ang_meta:clone()
	return Angle(self[1], self[2], self[3])
end
" );
		}
	}
}

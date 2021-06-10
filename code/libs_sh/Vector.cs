using Sandbox;
using KopiLua;

namespace KopiLua
{
	public partial class Lua
	{
		public static void lua_pushvector(Lua.lua_State L, double x, double y, double z)
		{
			Lua.lua_createtable()
			Lua.lua_pushnumber(L, x);
			Lua.lua_rawseti(L, -2, 1);
			Lua.lua_pushnumber(L, y);
			Lua.lua_rawseti(L, -2, 2);
			Lua.lua_pushnumber(L, z);
			Lua.lua_rawseti(L, -2, 3);
			Lua.luaL_getmetatable(L, "Vector");
			Lua.lua_setmetatable(L, -2);
		}
		public static void lua_pushvector(Lua.lua_State L, Vector3 v)
		{
			lua_pushvector(Lua.lua_State L, v.x, v.y, v.z);
		}
		public static void lua_tovector(Lua.lua_State L, int index, out double x, out double y, out double z)
		{
			Lua.luaL_checktable(L, index);
			Lua.lua_rawgeti(L, index, 1);
			x = Lua.lua_tonumber(L, -1);
			Lua.lua_rawgeti(L, index, 2);
			y = Lua.lua_tonumber(L, -1);
			Lua.lua_rawgeti(L, index, 3);
			z = Lua.lua_tonumber(L, -1);
			Lua.lua_pop(3);
		}
		public static Vector3 lua_tovector(Lua.lua_State L, int index)
		{
			Vector3 ret = new Vector3();
			lua_tovector(L, index, ret.x, ret.y, ret.z);
			return ret;
		}
	}
}


namespace Starfall
{
	public partial class Instance
	{
		[SFInitializeSh]
		public void VectorLib()
		{
			RegisterType("Vector", {
				new Lua.luaL_Reg("getAngle", (Lua.lua_State L) => {
					Vector3 vec = Lua.lua_tovector(L, 1);
					Lua.lua_pushangle(L, vec.GetAngle());
					return 1;
				}),

				new Lua.luaL_Reg(null, null)
			});
			Lua.lua_pop(L, 1);
			
			Lua.luaL_dostring(L, @"
local vec_meta = getMetatable(""Vector"")

function Vector(x, y, z)
	return setmetatable({x, y, z}, vec_meta)
end
local Vector = Vector

function vec_meta.__add(a, b)
	return Vector(a[1]+b[2], a[2]+b[2], a[3]+b[3])
end

function vec_meta.__sub(a, b)
	return Vector(a[1]-b[2], a[2]-b[2], a[3]-b[3])
end

function vec_meta.__mul(a, b)
	local t1, t2 = type(a), type(b)
	if t1==""number"" then
		return Vector(a*b[1], a*b[2], a*b[3])
	elseif t2==""number"" then
		return Vector(a[1]*b, a[2]*b, a[3]*b)
	elseif t1==""userdata"" then
	elseif t2==""userdata"" then
	else
		error(""Expected multiplication with a number or matrix!"")
	end
end

function vec_meta.__div(a, b)
	b = 1/b
	if type(b)==""number"" then
		return Vector(a[1]*b, a[2]*b, a[3]*b)
	else
		error(""Expected division by a rhs number"")
	end
end

function vec_meta:clone()
	return Vector(self[1], self[2], self[3])
end
");
		}
	}
}
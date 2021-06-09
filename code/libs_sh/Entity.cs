using Sandbox;
using KopiLua;

namespace KopiLua
{
    public partial class Lua
    {
        public static void lua_pushvector(Lua.lua_State L, Vector v)
        {
            Lua.lua_createtable()
            Lua.lua_pushnumber(L, v.x);
            Lua.lua_rawseti(L, -2, 1);
            Lua.lua_pushnumber(L, v.y);
            Lua.lua_rawseti(L, -2, 2);
            Lua.lua_pushnumber(L, v.z);
            Lua.lua_rawseti(L, -2, 3);
			Lua.luaL_getmetatable(L, "Vector");
			Lua.lua_setmetatable(L, -2);
        }
        public static Vector lua_tovector(Lua.lua_State L, int index)
        {
            Lua.luaL_checktable(L, index);
            Lua.lua_rawgeti(L, index, 1);
            Lua.lua_rawgeti(L, index, 2);
            Lua.lua_rawgeti(L, index, 3);
            Vector3 pos = new Vector3(Lua.lua_tonumber(L, -3), Lua.lua_tonumber(L, -2), Lua.lua_tonumber(L, -1));
            Lua.lua_pop(3);
        }
    }
}

namespace Starfall
{
    public partial class Instance
    {
        [SFInitializeSh]
        public void InitializeEntityLib()
        {
            RegisterType("Entity", {
                new Lua.luaL_Reg("getPos", (Lua.lua_State) => entity_getPos()),
                new Lua.luaL_Reg("setPos", (Lua.lua_State) => entity_setPos()),
                new Lua.luaL_Reg(null, null)
            });

            Lua.lua_pushcfunction(L, (Lua.lua_State) => entity_ctor());
            Lua.lua_setglobal(L, "Entity");
        }

        private int entity_ctor()
        {
            int index = Lua.luaL_checknumber(L, 1);
            Lua.lua_pushuserdata<Entity>(L, Entity.GetByIndex(index));
            return 1;
        }

        private int entity_getPos()
        {
            Entity ent = GetType("Entity");
            Lua.lua_pushvector(L, ent.Position);
            return 1;
        }

        private int entity_setPos()
        {
            Entity ent = GetType("Entity");
            ent.Position = Lua.lua_tovector(L, 2);
            return 0;
        }
    }
}

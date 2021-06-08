using Sandbox;
using KopiLua;

namespace Starfall
{
    public partial class Instance
    {
        [SFInitializeSh]
        public void InitializeEntityLib()
        {
            RegisterType("Entity", {
                new Lua.luaL_Reg("getPos", (Lua.lua_State) => entity_getPos()),
                new Lua.luaL_Reg(null, null)
            });

            Lua.lua_pushcfunction(L, (Lua.lua_State) => entity_ctor());
            Lua.lua_setglobal(L, "Entity");
        }

        private int entity_ctor()
        {
            
        }

        private int entity_getPos()
        {
            Entity ent = GetType("Entity");
            Vector3 pos = ent.Position;
            Lua.lua_pushnumber(L, pos.x);
            Lua.lua_pushnumber(L, pos.y);
            Lua.lua_pushnumber(L, pos.z);
            Lua.lua_getglobal(L, "Vector");
            Lua.lua_call(L, 3, 1);
            return 1;
        }
    }
}

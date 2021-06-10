using Sandbox;
using KopiLua;

namespace Starfall
{
	public partial class Instance
	{
		[SFInitializeSh]
		public void EntityLib()
		{
			RegisterType("Entity", {
				new Lua.luaL_Reg("getPos", (Lua.lua_State L) => {
					Entity ent = GetType(L, "Entity");
					Lua.lua_pushvector(L, ent.Position);
					return 1;
				}),
				new Lua.luaL_Reg("setPos", (Lua.lua_State L) => {
					Entity ent = GetType(L, "Entity");
					ent.Position = Lua.lua_tovector(L, 2);
					return 0;
				}),
				new Lua.luaL_Reg(null, null)
			});

			Lua.lua_pushcfunction(L, (Lua.lua_State L) => {
				int index = Lua.luaL_checkinteger(L, 1);
				PushType(L, "Entity", Entity.GetByIndex(index));
				return 1;
			});
			Lua.lua_setglobal(L, "Entity");
		}
	}
}

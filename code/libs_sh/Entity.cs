using Sandbox;
using KopiLua;

namespace Starfall
{
	public partial class Instance
	{
		[SFInitialize( 1 )]
		public void EntityLib()
		{
			RegisterType( "Entity", new Lua.luaL_Reg[]{
				new Lua.luaL_Reg("getPos", (Lua.lua_State L) => {
					Entity ent = GetType<Entity>(L, "Entity");
					Lua.lua_pushvector(L, ent.Position);
					return 1;
				}),
				new Lua.luaL_Reg("setPos", (Lua.lua_State L) => {
					Entity ent = GetType<Entity>(L, "Entity");
					ent.Position = Lua.lua_tovector(L, 2);
					return 0;
				})
			} );

			Lua.lua_pushcfunction( L, ( Lua.lua_State L ) =>
			{
				int index = Lua.luaL_checkinteger( L, 1 );
				PushType( L, "Entity", Entity.FindByIndex( index ) );
				return 1;
			} );
			Lua.lua_setglobal( L, "Entity" );
		}
	}
}

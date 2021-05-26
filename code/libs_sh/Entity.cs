using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starfall
{
    [MoonSharpUserData]
    class SFEntity
    {
        public static void Register()
        {
            UserData.RegisterProxyType<SFEntity, Entity>(r => new SFEntity(r));
        }

        Entity ent;
        public SFEntity(Entity ent)
        {
            this.ent = ent;
        }

        public List<double> getPos()
        {
            Vector v = ent.GetPos();
            return new List<double> {v.x, v.y, v.z};
        }
    }

}

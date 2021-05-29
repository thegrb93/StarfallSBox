using System.Collections.Generic;
using Sandbox;

namespace Starfall
{
    class SFEntity
    {
        Entity ent;
        public SFEntity(Entity ent)
        {
            this.ent = ent;
        }

        public List<double> getPos()
        {
            Vector3 v = ent.Position;
            return new List<double> {v.x, v.y, v.z};
        }
    }

}

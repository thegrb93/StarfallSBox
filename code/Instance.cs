using System;
using System.Collections.Generic;
using Sandbox;

namespace Starfall
{
    public class InstanceHook { }  public class UserHook { }

    public class StarfallCompileException : Exception
    {
        public StarfallCompileException(string message) : base(message)
        {
        }
    }

    public class Instance
    {
        public static List<Instance> activeInstances = new List<Instance>();
        public static Dictionary<Player, List<Instance>> playerInstances = new Dictionary<Player, List<Instance>>();

        StarfallData data;
        Player player;
        Entity entity;

        Dictionary<string, List<InstanceHook>> hooks = new Dictionary<string, List<InstanceHook>>();
        Dictionary<string, List<UserHook>> userhooks = new Dictionary<string, List<UserHook>>();


        public Instance(StarfallData data, Player player, Entity entity)
        {
            this.data = data;
            this.player = player;
            this.entity = entity;
        }

        public bool Compile()
        {
			Log.Info( System.Runtime.InteropServices.Marshal.SizeOf( this.player ).ToString() );
            return true;
        }

        int stackn = 0;
        public void runWithOps()
        {

        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Starfall
{
    public class StarfallData
    {
		public StarfallData( Dictionary<string, string> files, string mainfile )
		{
			if (!files.ContainsKey(mainfile))
			{
				throw new Exception( "Mainfile is missing from files: " + files + " (" + mainfile + ")" );
			}
			this.files = files;
			this.mainfile = mainfile;
		}
		public Dictionary<string, string> files;
        public string mainfile;
    }

    class Networking
    {
    }
}

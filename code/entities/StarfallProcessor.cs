using Sandbox;
using Sandbox.Joints;
using Starfall;
using System.Collections.Generic;

[Library( "starfall_processor" )]
public partial class StarfallProcessor : Prop
{
	Starfall.Instance instance;

	/*[ServerRpc]
	public void ReceivedCode()
	{
	}*/

	[ClientRpc]
	public void SendCode(Player owner, List<SFFile> files)
	{
	}
}

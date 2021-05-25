using Sandbox;
using Sandbox.Joints;

[Library( "starfall_processor" )]
public partial class StarfallProcessor : Prop
{
	public WeldJoint Joint;

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if ( Joint.IsValid() )
		{
			Joint.Remove();
		}
	}

	protected override void UpdatePropData( Model model )
	{
		base.UpdatePropData( model );

		Health = -1;
	}
}

using Sandbox;

[Library( "starfall_prop" )]
public partial class StarfallProp : Prop
{

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	protected override void UpdatePropData( Model model )
	{
		base.UpdatePropData( model );

		Health = -1;
	}
}

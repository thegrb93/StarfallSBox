using Sandbox;

[Library( "starfall_hologram" )]
public partial class StarfallHologram : Prop
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

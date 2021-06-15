namespace Sandbox.Tools
{
	[Library( "tool_starfall", Title = "Starfall", Description = "Starfall Processor", Group = "construction" )]
	public partial class Starfall : BaseTool
	{
		PreviewEntity previewModel;

		public Starfall()
		{
		}

		protected override bool IsPreviewTraceValid( TraceResult tr )
		{
			if ( !base.IsPreviewTraceValid( tr ) )
				return false;

			if ( tr.Entity is StarfallProcessor )
				return false;

			return true;
		}

		public override void CreatePreviews()
		{
			if ( TryCreatePreview( ref previewModel, "models/starfall_processor.vmdl" ) )
			{
				previewModel.RelativeToNormal = false;
			}
		}

		public override void Simulate()
		{
			if ( !Host.IsServer )
				return;

			using ( Prediction.Off() )
			{
				if ( !Input.Pressed( InputButton.Attack1 ) )
					return;

				var startPos = Owner.EyePos;
				var dir = Owner.EyeRot.Forward;

				var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
					.Ignore( Owner )
					.Run();

				if ( !tr.Hit || !tr.Entity.IsValid() )
					return;

				var attached = !tr.Entity.IsWorld && tr.Body.IsValid() && tr.Body.PhysicsGroup != null && tr.Body.Entity.IsValid();

				if ( attached && tr.Entity is not Prop )
					return;

				CreateHitEffects( tr.EndPos );

				if ( tr.Entity is StarfallProcessor )
				{
					return;
				}

				var ent = new StarfallProcessor
				{
					Position = tr.EndPos,
					Rotation = Rotation.LookAt( tr.Normal, dir ) * Rotation.From( new Angles( 90, 0, 0 ) ),
					PhysicsEnabled = !attached,
					EnableSolidCollisions = !attached,
				};

				if ( attached )
				{
					ent.SetParent( tr.Body.Entity, tr.Body.PhysicsGroup.GetBodyBoneName( tr.Body ) );
				}

				ent.SetModel( "models/starfall_processor.vmdl" );
			}
		}
	}
}

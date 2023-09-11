using Sandbox;

namespace TerrorTown;
[TraitorBuyable("Special", 1)]
public partial class DeadRinger : Carriable
{
	public override string ViewModelPath => "models/v_watch_pocket.vmdl";
	[Net, Predicted] bool Enabled { get; set; } = false;
	[Net, Predicted] bool Invisible { get; set; } = false;
	[Net, Predicted] bool SpeedUp { get; set; } = false;
	Entity FakeCorpseMade;
	TimeSince TimeSinceBecameInvisible;
	TimeSince TimeSinceSpeedup;
	public override void Simulate(IClient cl)
	{
		base.Simulate(cl);
		if (Input.Pressed("Attack1"))
		{
			PrimaryAttack();
		}
	}
	public void PrimaryAttack()
	{
		if (Invisible && TimeSinceBecameInvisible > 1)
		{
			Uncloak();
			return;
		}
		Enabled = !Enabled;
	}
	public void Uncloak()
	{
		PlaySound("spy_uncloak_feigndeath");
		Invisible = false;

		Owner.EnableDrawing = true;
		if (Owner is Player ply2 && ply2.Inventory.ActiveChild is ModelEntity mdl2)
		{
			mdl2.EnableDrawing = true;
		}
	}
	public void SpeedupOn()
	{
		Sound.FromEntity(To.Single(Owner.Client), "discipline_device_power_up", this);
		TimeSinceSpeedup = 0;
		SpeedUp = true;
		if (Owner is Player ply && ply.MovementController is WalkController wlk)
		{
			wlk.SpeedMultiplier = 1.5f;
		}
	}
	public void SpeedupOff()
	{
		Sound.FromEntity(To.Single(Owner.Client), "discipline_device_power_down", this);
		SpeedUp = false;

		if (Owner is Player ply && ply.MovementController is WalkController wlk)
		{
			wlk.SpeedMultiplier = 1;
		}
	}
	[Event.Tick.Server]
	void ServerTick()
	{

		if (Invisible && Owner is Player ply && ply.Inventory.ActiveChild is ModelEntity mdl)
		{
			mdl.EnableDrawing = false;
		}

		if (SpeedUp && TimeSinceSpeedup > 3 && Owner.LifeState == LifeState.Alive)
		{

			SpeedupOff();
		}
		if (Invisible && TimeSinceBecameInvisible > 10 && Owner.LifeState == LifeState.Alive)
		{

			Uncloak();
		}
		if (TimeSinceBecameInvisible > 16 && FakeCorpseMade != null)
		{
			FakeCorpseMade.Delete();
			FakeCorpseMade = null;
		}
	}
	[Event.Client.BuildInput]
	void input()
	{
		base.BuildInput();

		if ((Enabled || Invisible) && Owner is Player ply && ply.Inventory.ActiveChild != this)
		{
			Input.Clear("Attack1");
			Input.Clear("Attack2");


			Input.SetAction("Attack1", false);
			Input.SetAction("Attack2", false);
		}
	}
	[Event.Client.Frame]
	void Frame()
	{
		if (Owner.Client != Game.LocalClient) return;
		if (Enabled) DebugOverlay.ScreenText("Dead Ringer is primed for activation.");
	}

	[Event("Player.PostTakeDamage")]
	public void OnTakeDamage(DamageInfo info, Player ply)
	{
		if (Enabled && ply == Owner && ply.LifeState == LifeState.Alive)
		{
			ply.BecomeRagdoll(info);
			SpeedupOn();
			ply.EnableDrawing = false;
			ply.Health += info.Damage * 0.8f;
			TimeSinceBecameInvisible = 0;
			Invisible = true;
			FakeCorpseMade = ply.Corpse;
			Enabled = false;
			foreach (ModelEntity weapon in ply.Inventory.Items)
			{
				if (weapon is MagnetoStick || weapon is Holstered) continue;
				var b = new BasePhysics();
				b.Position = weapon.Position;
				b.Model = weapon.Model;
				b.SetupPhysicsFromModel(PhysicsMotionType.Dynamic);
				b.DeleteAsync(16f);
				if (b.PhysicsBody != null)
				{
					b.PhysicsBody.ApplyImpulse(info.Force * 24);
				}
			}
		}
	}
	public override void OnActiveEnd()
	{
		base.OnActiveEnd();
	}
	public override void SimulateAnimator(CitizenAnimationHelper anim)
	{
		base.SimulateAnimator(anim);
		anim.HoldType = CitizenAnimationHelper.HoldTypes.None;
	}
}

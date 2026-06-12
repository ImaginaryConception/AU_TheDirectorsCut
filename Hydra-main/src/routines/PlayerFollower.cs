namespace HydraMenu.routines
{
	public class PlayerFollowerRoutine : IRoutine
	{
		public PlayerFollowerRoutine() : base("PlayerFollower") { }

		public PlayerControl following;

		public override bool Enabled
		{
			get
			{
				return following != null;
			}
			set
			{
				if(!value) following = null;
			}
		}

		public override void Run()
		{
			if(PlayerControl.LocalPlayer == null) return;

			

			
			PlayerControl.LocalPlayer.transform.position = following.transform.position;
		}

		public override void OnDisconnect()
		{
			Hydra.notifications.Send("Player Follower", "Player Follower was disabled as you left the game.", 10);
			Enabled = false;
		}
	}
}
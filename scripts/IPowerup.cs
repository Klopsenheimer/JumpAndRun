namespace JumpAndRun.scripts
{    
	public interface IPowerup    
	{        
		bool IsCollected { get; }        
		bool CheckCollision(Player player);        
		void OnCollision(Player player);    
	}
}

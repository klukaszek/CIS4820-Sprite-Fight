using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateDefensive : State
{
	//Block, roll, back up
	public override void Execute(NPCController npc)
	{
		Fighter npcFighter = npc.GetNpcFighter();
		ActionManager actionManager = npcFighter.GetActionManager();
		
		npc.currentStateType = NPCController.StateType.defensive;

		//If stamina is low, then there is a 1/5 chance that the npc decides to not perform an action
		if(npcFighter.GetStamina() < npcFighter.GetMaxStamina() / 5)
		{
			int random = Random.Range(0, 6);

			if(random == 0) return;
		}

		//Make sure npc is not performing an action
		if(!npcFighter.performingAction && !npcFighter.isStunned)
		{
			int random = Random.Range(0, 7);
			
			//Block if random = [0, 3)
			//even though there is a performing action check, sometimes block decides to double up and drain stamina
			if(random <= 2 && !npcFighter.isBlocking)
			{
				IDictionary<string, float> animations = npcFighter.GetAnimationDurations();

				//Get a block time with a minimum duration of the animation time, and 1.5 seconds
				float blockTime = Random.Range(animations["Block"] * 2, 1f);
				actionManager.NpcBlock(blockTime);
			}
			//Roll if random = [3, 6) and not recovering from tired
			else if(random >= 3 && random < 6 && !npc.recoveringFromTired && npcFighter.GetStamina() > npcFighter.GetMaxStamina() / 10)
			{
				actionManager.Roll();

				//Ignore movement timer since a roll should go in a certain direction
				npc.RandomMove(2f);
			}
			//Back up if random = 6
			else
			{
				//Update movement if timer allows it
				if(npc.movementTimer < npc.movementDelay) return;
				npc.movementTimer = 0;

				npc.SetDestination(npc.transform.position - (npc.transform.forward/2.5f));
			}
		}
	}
}


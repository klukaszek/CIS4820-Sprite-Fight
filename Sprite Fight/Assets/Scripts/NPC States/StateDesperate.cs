using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateDesperate : State
{

	//Combat, roll, attempt parry, random move
	public override void Execute(NPCController npc)
	{
		Fighter npcFighter = npc.GetNpcFighter();
		Fighter playerFigher = npc.GetPlayerFighter();
		ActionManager actionManager = npcFighter.GetActionManager();
		
		npc.currentStateType = NPCController.StateType.desperate;

		if(!npcFighter.performingAction && !npcFighter.isStunned)
		{
			int random = Random.Range(0, 10);

			//Attack if random = [0, 5)
			if(random < 5)
			{
				npc.ChangeState(new StateCombat());
				npc.currentState.Execute(npc);
			}
			//Roll if random = [3, 7) and not recovering from tired
			else if(random >= 5 && random < 7 && !npc.recoveringFromTired)
			{
				actionManager.Roll();

				//Ignore movement timer since a roll should go in a certain direction
				npc.RandomMove(2f);
			}
			//Attempt to parry if random = [7, 9)
			//Parry an attack by doing a very quick block action
			else if(random >= 7 && random < 9 && !npcFighter.isBlocking)
			{
				IDictionary<string, float> animations = npcFighter.GetAnimationDurations();
				actionManager.NpcBlock(animations["Block"] * 2);
			}
			//Random move if random = 9
			else
			{
				npc.RandomMove(5f);
			}
		}
	}
}


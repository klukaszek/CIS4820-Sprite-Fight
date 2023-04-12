using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateCombat : State
{

	//Light Attack or Heavy Attack
	public override void Execute(NPCController npc)
	{
		Fighter npcFighter = npc.GetNpcFighter();
		Fighter playerFigher = npc.GetPlayerFighter();
		ActionManager actionManager = npcFighter.GetActionManager();

		npc.currentStateType = NPCController.StateType.combat;

		//If stamina is low, then there is a 1/5 chance that the npc decides to not perform an action
		if(npcFighter.GetStamina() < npcFighter.GetMaxStamina() / 5)
		{
			int random = Random.Range(0, 6);

			if(random == 0) return;
		}

		//in the event that the player is tired but the npc is not in range, the npc will try to move in first
		if(playerFigher.isTired && !npc.TargetInRange())
		{
			npc.SetDestination(npc.player.transform.position + (npc.player.transform.forward/2.5f));
			return;
		}

		//Make sure NPC is not recovering from tired
		if(!npc.recoveringFromTired)
		{
			//Make sure npc is not already attacking
			if(!npcFighter.performingAction && !npcFighter.isStunned)
			{
				int random = Random.Range(0, 2);

				//Perform light attack and heavy attack
				switch(random)
				{
					case 0:
						actionManager.LightAttack();
						break;
					case 1:
						actionManager.HeavyAttack();
						break;
				}
			}
		}
		//If the npc is recovering from tired, become defensive instead
		else
		{
			npc.ChangeState(new StateDefensive());
			npc.currentState.Execute(npc);
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StateNeutral : State
{
    //Approach Player or Random Move
	public override void Execute(NPCController npc)
	{
		npc.currentStateType = NPCController.StateType.neutral;

		//Update movement if timer allows it
		if(npc.movementTimer < npc.movementDelay) return;

		npc.movementTimer = 0;

		int random = Random.Range(0, 10);

		//Approach Player if random = [0, 4)
		if(random < 9) 
		{
			npc.SetDestination(npc.player.transform.position + (npc.player.transform.forward/2.5f));
		}
		//Random Move if random = 4
		else 
		{
			npc.RandomMove(1f);
		}
	}
}

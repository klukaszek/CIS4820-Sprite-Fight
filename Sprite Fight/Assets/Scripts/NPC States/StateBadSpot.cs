using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class StateBadSpot : State
{
	//Random move
	public override void Execute(NPCController npc)
	{
		npc.currentStateType = NPCController.StateType.bad_spot;

		//Update movement if timer allows it
		if(npc.movementTimer < npc.movementDelay) return;
		npc.movementTimer = 0;

		//Return to arena origin, or begin randomly moving if recovering from Tired
		if(!npc.recoveringFromTired) npc.SetDestination(Arena.Instance.transform.position);
		else npc.RandomMove(10);
	}
}

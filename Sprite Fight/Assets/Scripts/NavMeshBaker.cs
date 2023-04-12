using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshBaker : MonoBehaviour
{
    [SerializeField] private NavMeshSurface surface;
    private Arena arena;

    // Initialize arena navmesh for NPCs that require an arena navmesh for navigation
    void Start () 
    {
        arena = GetComponent<Arena>();

        //add 0.5 to the arena scale so that the npc can be knocked out of the arena
        arena.transform.localScale = new Vector3(arena.radius*2 + 0.5f, 0.01f, arena.radius*2 + 0.5f);

        //Clear existing navmeshes
        surface.RemoveData();

        //Build new navmesh using gameobject mesh
        surface.BuildNavMesh();
    }

}

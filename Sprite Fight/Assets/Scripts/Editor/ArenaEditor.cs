using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Arena))]
public class ArenaEditor:Editor
{
    private void OnSceneGUI() 
    {
        Arena arena =  (Arena) target;
        
        DrawArenaRadius(arena);
    }

    //Draw arena radius as a gizmo
    private void DrawArenaRadius(Arena arena)
    {
        Handles.color = Color.red;
        Handles.DrawWireArc(arena.transform.position, Vector3.up, Vector3.forward, 360, arena.radius);
    }
}

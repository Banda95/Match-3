using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExtension
{
    public static void OptimizedSetActive(this GameObject gameObject, bool active)
    {
        if(gameObject.activeSelf != active) 
            gameObject.SetActive(active);
    }
}

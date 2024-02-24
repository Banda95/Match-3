using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GemsDB", menuName = "ScriptableObjects/GemsDB", order = 1)]
public class GemsDB : ScriptableObject
{
    [Serializable]
    public struct Gem
    {
        public int id;
        public Sprite sprite;
    }

    [SerializeField]
    private Gem[] allGems;

    public Gem[] AllGems => allGems;

    private Dictionary<int, Gem> gemsById;

    public void Init()
    {
        gemsById= new Dictionary<int, Gem>();
        for(int i = 0; i < allGems.Length; i++)
        {
            gemsById.Add(allGems[i].id, allGems[i]);
        }
    }

    public int GetRandomGemId(int potentialMatches)
    {
        return UnityEngine.Random.Range(0, allGems.Length - potentialMatches);
    }

    public bool HasGemWithId(int id)
    {
        return gemsById.ContainsKey(id);
    }

    public Gem GetGemById(int id)
    {
        if(gemsById.ContainsKey(id)) 
            return gemsById[id];

        throw new InvalidOperationException("Gem with Id " + id + " not found in db");
    }
}

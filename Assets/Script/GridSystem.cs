using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class GridSystem : MonoBehaviour
{
    private enum Direction
    {
        Left, Right, Up, Down
    }

    private enum Sort
    {
        Horizontal,
        Vertical
    }

    [Serializable]
    public class GridCellData
    {
        public int2 Coords;
        public bool Empty => GemId == -1;
        public int GemId;
        public Sprite Sp;

        public GridCellData(int2 coords)
        {
            Coords = coords;
            GemId = -1;
        }

        public GridCellData(int x, int y)
        {
            Coords = new int2(x, y);
            GemId = -1;
        }

        public void SetGem(GemsDB.Gem gem)
        {
            GemId = gem.id;
            Sp = gem.sprite;
        }

        public void Clear()
        {
            GemId = -1;
            Sp = null;
        }
    }

    [SerializeField] private GemsDB gemsDb;
    [SerializeField] private int2 GridSize = new int2(8,8);
    [SerializeField] private UIGrid uiGrid;

    //Let's not use [,] for performance reason
    private GridCellData[][] grid;
    private Dictionary<Direction, int2> dirOffsets;

    public void Create()
    {
        do
        {

            GenerateEmptyGrid();
            FillGrid();
#if UNITY_EDITOR
            if (!AssertValid())
            {
                Debug.LogError("Generation not valid");
                //Plan b!
            }
#endif

        } while (!AssertValid());
    }

    private void Start()
    {
        gemsDb.Init();

        dirOffsets = new Dictionary<Direction, int2>
        {
            { Direction.Left, new int2(-1, 0) },
            { Direction.Right, new int2(1, 0) },
            { Direction.Up, new int2(0, 1) },
            { Direction.Down, new int2(0, -1) }
        };

        Create();
    }

    private void GenerateEmptyGrid()
    {
        if(grid == null)
            grid = new GridCellData[GridSize.x][];

        for(int x = 0; x < GridSize.x; x++)
        {
            if (grid[x] == null)
                grid[x] = new GridCellData[GridSize.y];

            for (int y = 0; y < GridSize.y; y++)
            {
                if (grid[x][y] == null)
                    grid[x][y] = new GridCellData(x, y);
                else
                    grid[x][y].Clear();
            }
        }
    }

    private void FillGrid()
    {
        //Start from a solvable solution and then shuffle until conditions are met.

        for (int x = 0; x < GridSize.x; x++)
        {
            for (int y = 0; y < GridSize.y; y++)
            {
                int potentialMatches = 0;
                GridCellData thisCell = grid[x][y];

                int minMatchId = -1;
                int secondMinMatchId = -1;

                int2 leftLeft = thisCell.Coords + dirOffsets[Direction.Left] + dirOffsets[Direction.Left];
                if(Valid(leftLeft))
                {
                    minMatchId = grid[leftLeft.x +1][leftLeft.y].GemId;
                    if(minMatchId == grid[leftLeft.x][leftLeft.y].GemId)
                    {
                        potentialMatches = 1;
                    }
                }

                int2 downDown = thisCell.Coords + dirOffsets[Direction.Down] + dirOffsets[Direction.Down];
                if (Valid(downDown))
                {
                    secondMinMatchId = grid[downDown.x][downDown.y+1].GemId;
                    if (secondMinMatchId == grid[downDown.x][downDown.y].GemId)
                    {
                        potentialMatches += 1;
                        if(potentialMatches == 1)
                        {
                            minMatchId = secondMinMatchId;
                        }
                        else if( secondMinMatchId < minMatchId)
                        {
                            (minMatchId, secondMinMatchId) = (secondMinMatchId, minMatchId);
                        }
                    }
                }

                //Random first iteration.
                int thisGemId = gemsDb.GetRandomGemId(potentialMatches);
                if(potentialMatches > 0 && thisGemId >= minMatchId)
                {
                    thisGemId += 1;
                }

                if(potentialMatches == 2 && thisGemId >= secondMinMatchId)
                {
                    thisGemId += 1;
                }


                thisCell.SetGem(gemsDb.GetGemById(thisGemId));
            }
        }

        //Now creates at least 3
        int matches = 0;
        HashSet<int2> skipCoords = new HashSet<int2>();
        do
        {
            int randomX = UnityEngine.Random.Range(0, GridSize.x);
            int randomY = UnityEngine.Random.Range(0, GridSize.y);

            if (skipCoords.Contains(new int2(randomX, randomY)))
                continue;

            GridCellData startingCell = grid[randomX][randomY];
            List<int2> createdMatch;

            if(CreateMatch3For(startingCell, out createdMatch, skipCoords))
            {
                for(int i = 0; i < createdMatch.Count; i++)
                {
                    skipCoords.Add(createdMatch[i]);
                }
                matches++;
            }

        } while (matches < 3);


        uiGrid.FromData(grid);
    }

    private bool Valid(int2 test)
    {
        return test.x >= 0 && test.y >= 0 && test.x < GridSize.x && test.y < GridSize.y;    
    }

    private bool CreateMatch3For(GridCellData forCell, out List<int2> createdMatch, HashSet<int2> skipCoords)
    {
        createdMatch = new List<int2>();
        
        //Pick a random direction and force it
        Sort randomSort = (Sort)UnityEngine.Random.Range(0, 2);
        if(ForceMatch(randomSort, forCell.Coords, forCell.GemId, ref createdMatch, skipCoords))
        {
            //Success!
#if UNITY_EDITOR
            Debug.Log("Match forced");
            for(int i = 0; i < createdMatch.Count; i++)
            {
                Debug.Log(createdMatch[i]);
            }
#endif
        }
        else
        {
            //Try other direction.
            randomSort = randomSort == Sort.Horizontal ? Sort.Vertical : Sort.Horizontal;
            if (ForceMatch(randomSort, forCell.Coords, forCell.GemId, ref createdMatch, skipCoords))
            {
#if UNITY_EDITOR
                Debug.Log("Match forced");
                for (int i = 0; i < createdMatch.Count; i++)
                {
                    Debug.Log(createdMatch[i]);
                }
#endif
            }
            else
            {
                //Nope, fail completely.
                return false;
            }
        }

        //Nice, switch a random one away.

        if (SwapWithRandomNeighbour(forCell.GemId, ref createdMatch))
            return true;


        return false;
    }



    private bool ForceMatch(Sort sort, int2 startingCoords, int id, ref List<int2> finalMatch, HashSet<int2> skipCoords)
    {
        //Random direction 0 full left, 1 center, 2 full right
        int whereToGo = UnityEngine.Random.Range(0, 3);
        int attempts = 0;

        do
        {
            int2 otherOther;
            int2 other;

            if (whereToGo == 0)
            {
                //Left or down.
                otherOther = startingCoords + dirOffsets[sort == Sort.Horizontal ? Direction.Left : Direction.Down] * 2;
                other = startingCoords + dirOffsets[sort == Sort.Horizontal ? Direction.Left : Direction.Down];
            }
            else if (whereToGo == 1)
            {
                //Both.
                otherOther = startingCoords + dirOffsets[sort == Sort.Horizontal ? Direction.Left : Direction.Down];
                other = startingCoords + dirOffsets[sort == Sort.Horizontal ? Direction.Right : Direction.Up];
            }
            else
            {
                //Right or up.
                otherOther = startingCoords + dirOffsets[sort == Sort.Horizontal ? Direction.Right : Direction.Up] * 2;
                other = startingCoords + dirOffsets[sort == Sort.Horizontal ? Direction.Right : Direction.Up];
            }

            if (!skipCoords.Contains(other) && !skipCoords.Contains(otherOther) && CheckCandidates(otherOther, other, ref finalMatch))
                return true;

            attempts++;
            whereToGo = (whereToGo + 1) % 3;            
        } while (attempts <= 3);

        return false;

        bool CheckCandidates(int2 a, int2 b, ref List<int2> finalMatch)
        {
            if (Valid(a) && IdValid(id, a) && Valid(b) && IdValid(id, b))
            {
                //Create it!
                SetGemToCoords(a, id);
                SetGemToCoords(b, id);

                finalMatch.Add(a);
                finalMatch.Add(b);
                finalMatch.Add(startingCoords);

                return true;
            }

            return false;
        }
    }

    private bool SwapWithRandomNeighbour(int id, ref List<int2> createdMatch)
    {
        int test = UnityEngine.Random.Range(0, 3);
        int attemptsTest = 0;

        do
        {
            int2 trySwap = createdMatch[test];
            Direction tryDir = (Direction)UnityEngine.Random.Range(0, 4);
            int dirAttempts = 0;

            do
            {
                int2 swapTarget = trySwap + dirOffsets[tryDir];
                if (Valid(swapTarget) && !CreatesMatch3(swapTarget, id))
                {
                    //swap the cells!
                    int original = grid[swapTarget.x][swapTarget.y].GemId;
                    if(!CreatesMatch3(trySwap, original))
                    {
                        SetGemToCoords(swapTarget, id);
                        SetGemToCoords(trySwap, original);
                        createdMatch.Add(swapTarget);
#if UNITY_EDITOR
                        Debug.Log("Swap " + swapTarget + " -> " + trySwap);
#endif
                        return true;
                    }
                }

                dirAttempts++;
                tryDir = (Direction)(((int)tryDir + 1) % 4);
            } while (dirAttempts <= 4);


            attemptsTest++;
            test = (test + 1) % 3;
        } while (attemptsTest <= 3);

        return false;
    }

    private bool IdValid(int wantedGemId, int2 inCoords)
    {
        //Is valid if matching neighbours in both direction is <= 1;
        int2 leftCoor = dirOffsets[Direction.Left] + inCoords;
        int2 rightCoor = dirOffsets[Direction.Right] + inCoords;
        int horizontalMatch = 0;
        if (ValidAndMatches(leftCoor, wantedGemId))
            horizontalMatch++;

        if(ValidAndMatches(rightCoor, wantedGemId))
            horizontalMatch++;

        if (horizontalMatch == 2)
            return false;

        int2 upCoor = dirOffsets[Direction.Up] + inCoords;
        int2 downCoor = dirOffsets[Direction.Down] + inCoords;
        int verticalMatch = 0;
        if (ValidAndMatches(upCoor, wantedGemId))
            verticalMatch++;

        if (ValidAndMatches(downCoor, wantedGemId))
            verticalMatch++;

        if (verticalMatch == 2)
            return false;

        return true;
    }

    private bool CreatesMatch3(int2 inCoords, int id)
    {
        //Check if a match3 exists in all directions from that point.
        bool crossMatch = !IdValid(id, inCoords);
        if (crossMatch)
            return true;


        //Full left:
        foreach(var kvp in dirOffsets)
        {
            int2 far = inCoords + dirOffsets[kvp.Key] * 2;
            int2 close = inCoords + dirOffsets[kvp.Key];

            if (ValidAndMatches(far, id) && ValidAndMatches(close, id))
                return true;
        }

        return false;
    }

    private bool ValidAndMatches(int2 coords, int withId)
    {
        return Valid(coords) && grid[coords.x][coords.y].GemId == withId;
    }

    private void SetGemToCoords(int2 coords, int id)
    {
        grid[coords.x][coords.y].SetGem(gemsDb.GetGemById(id));
    }

    private bool AssertValid()
    {
        for (int x = 0; x < GridSize.x; x++)
        {
            for (int y = 0; y < GridSize.y; y++)
            {
                GridCellData gridcell = grid[x][y];
                if (!IdValid(gridcell.GemId, gridcell.Coords))
                {
#if UNITY_EDITOR
                    Debug.Log("Not valid!" + gridcell.Coords);
#endif
                    return false;
                }
            }
        }

        return true;
    }
}

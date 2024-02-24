using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GridSystem;

public class UIGrid : MonoBehaviour
{
    [SerializeField] Sprite bgSpriteToUse;
    [SerializeField] UIGridCell cellPrefab;
    [SerializeField] Image cellBg;
    [SerializeField] GridLayoutGroup gemsGridLayout;
    [SerializeField] GridLayoutGroup bgGridLayout;
    [SerializeField] RectTransform gridRoot;
    [SerializeField] float enterAnimTime = 1f;

    private List<UIGridCell> allCells;
    private List<Image> allCellsBg;

    private Vector2 mySize;
    private bool initialized = false;

    private Coroutine animCoroutine;

    private void Start()
    {
        mySize = gridRoot.rect.size;
        allCells = new List<UIGridCell>();
        allCellsBg = new List<Image>();
    }

    public void FromData(GridCellData[][] grid)
    {
        if (!initialized)
        {
            initialized = true;
            int wantedPerRow = Mathf.FloorToInt((mySize.x - gemsGridLayout.padding.left - gemsGridLayout.padding.right
                - gemsGridLayout.spacing.x * (grid.Length - 1)) / grid.Length);

            float neededHeight = wantedPerRow * grid[0].Length + gemsGridLayout.padding.bottom + gemsGridLayout.padding.top
                + gemsGridLayout.spacing.y * (grid[0].Length - 1);

            gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, neededHeight);
            gridRoot.ForceUpdateRectTransforms();

            gemsGridLayout.cellSize = new Vector2(wantedPerRow, wantedPerRow);
            bgGridLayout.cellSize = new Vector2(wantedPerRow, wantedPerRow);

            for (int x = 0; x < grid.Length; x++)
            {
                for (int y = 0; y < grid[x].Length; y++)
                {
                    UIGridCell newCell = Instantiate(cellPrefab, gemsGridLayout.transform);
                    newCell.SetEmpty();
                    newCell.FromData(grid[x][y], bgSpriteToUse);
                    allCells.Add(newCell);

                    Image newCellBg = Instantiate(cellBg, bgGridLayout.transform);
                    newCellBg.sprite = bgSpriteToUse;
                    allCellsBg.Add(newCellBg);
                }
            }
        }
        else
        {
            //Just reset existing cells
            for(int i = 0; i < allCells.Count; i++)
            {
                allCells[i].Reset();
            }
        }
        if(animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(AnimateEntrance(grid));
    }

    private IEnumerator AnimateEntrance(GridCellData[][] grid)
    {
        yield return new WaitForSeconds(0.1f);

        int cellIndex = 0;
        for (int x = 0; x < grid.Length; x++)
        {
            for (int y = 0; y < grid[x].Length; y++)
            {
                yield return new WaitForSeconds(0.1f);
                allCells[cellIndex].AnimateGemEntrance(gridRoot.rect.height, enterAnimTime);
                allCells[cellIndex].SetGem(grid[x][y].Sp);
                cellIndex++;
            }
        }
        yield return new WaitForSeconds(enterAnimTime);

        //Start anim in sync
        for (cellIndex = 0; cellIndex < allCells.Count; cellIndex++)
        {
            allCells[cellIndex].AnimateIdle();
        }

        animCoroutine = null;
    }

    private void Update()
    {
    }
}

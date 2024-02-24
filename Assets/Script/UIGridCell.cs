using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GridSystem;

public class UIGridCell : MonoBehaviour
{
    [SerializeField] private Image gemImage;

    GridCellData myData;

    private Animation anim;
    private RectTransform gemRt;
    private RectTransform thisRt;

    private void Start()
    {
        anim = GetComponent<Animation>();
        gemRt = gemImage.GetComponent<RectTransform>();
        thisRt = GetComponent<RectTransform>();
    }

    public void FromData(GridCellData data, Sprite bgSprite)
    {
        myData = data;
    }

    public void SetEmpty()
    {
        gemImage.sprite = null;
        gemImage.gameObject.OptimizedSetActive(false);
    }

    public void SetGem(Sprite s)
    {
        gemImage.sprite = s;
        gemImage.gameObject.OptimizedSetActive(true);
    }

    public void AnimateIdle()
    {
        anim.Play();
    }

    public void Reset()
    {
        StopAllCoroutines();
        if(gemRt != null)
            gemRt.anchoredPosition = new Vector2(gemRt.anchoredPosition.x, 0);
        anim?.Stop();
        SetEmpty();
        myData = null;
    }

    public void AnimateGemEntrance(float fromHeight, float inSeconds)
    {
        float heightDelta = thisRt.anchoredPosition.y + fromHeight;
        gemRt.anchoredPosition = new Vector2(gemRt.anchoredPosition.x, fromHeight - heightDelta + gemRt.rect.height /2f);
        StartCoroutine(LerpToOriginalPosition(inSeconds));
    }

    private IEnumerator LerpToOriginalPosition(float inSeconds)
    {
        float t = 0;
        float posX = gemRt.anchoredPosition.x;
        float startH = gemRt.anchoredPosition.y;
        float newHeight;
        while (t < inSeconds)
        {
            newHeight = Mathf.Lerp(startH, 0, t / inSeconds);
            gemRt.anchoredPosition = new Vector2(posX, newHeight);
            yield return null;
            t += Time.deltaTime;
        }

        gemRt.anchoredPosition = new Vector2(posX, 0);
    }
}

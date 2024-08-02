using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleNormal : Bubble
{
    public override void Effect()
    {

    }

    public override void RemoveBubble()
    {
        SlotInfo.bubble = null;

        StartCoroutine(CoRemove());
    }

    IEnumerator CoRemove()
    {
        Color col = imgBubble.color;
        float scale = imgBubble.transform.localScale.x;

        //이펙트
        while (col.a >= 0)
        {
            col.a -= 6f * Time.deltaTime;
            imgBubble.color = col;
            scale = scale + Time.deltaTime;

            imgBubble.transform.localScale = new Vector2(scale, scale);

            yield return null;
        }

        //삭제로직        
        Destroy(gameObject);
    }
}

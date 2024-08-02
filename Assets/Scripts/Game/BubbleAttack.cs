using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BubbleAttack : Bubble
{
    [SerializeField] SpriteRenderer imgAttacker;
    [HideInInspector] public Boss boss;

    float _fSpeed = 15f;
    float _fRads = 0.25f;

    bool isHit = false;

    public override void Effect()
    {
        StartCoroutine(CoAttack());
    }

    IEnumerator CoAttack()
    {
        //imgAttacker.transform.SetParent(null);
        while (Vector2.Distance(imgAttacker.transform.position, boss.transform.position) > _fRads)
        {

            imgAttacker.transform.position = Vector2.MoveTowards(imgAttacker.transform.position, boss.transform.position, _fSpeed * Time.deltaTime);
            yield return null;
        }

        Color col = Color.white;
        float scale = imgAttacker.transform.localScale.x;

        while (col.a >= 0)
        {
            col.a -= 6f * Time.deltaTime;
            imgAttacker.color = col;
            scale = scale + Time.deltaTime;
            imgAttacker.transform.localScale = new Vector2(scale, scale);

            yield return null;
        }

        isHit = true;
        boss.Hit();
    }

    public override void RemoveBubble()
    {
        SlotInfo.bubble = null;

        Effect();

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

        yield return new WaitUntil(() => isHit == true);

        //삭제로직        
        Destroy(gameObject);
    }
}

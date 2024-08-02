using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public abstract class Bubble : MonoBehaviour
{
    public int TypeID;
    float _fMoveSpeed = 15f;
    float _fRadius = 0.2f;
    public Slot SlotInfo;

    public SpriteRenderer imgBubble;
    
    Action<bool> MatchedAction;

    List<Bubble> matchs = new List<Bubble>();

    public abstract void Effect();
    public abstract void RemoveBubble();

    public void Move(Vector2[] moveList, Slot slot, Action<bool> matchact)
    {
        //Debug.Log(moveList.Length);
        MatchedAction = matchact;
        SlotInfo = slot;
        slot.bubble = this;
        StartCoroutine(MoveBubble(moveList));
    }

    IEnumerator MoveBubble(Vector2[] moveList)
    {
        foreach (Vector2 move in moveList)
        {
            while (Vector2.Distance(transform.position, move) > _fRadius)
            {

                transform.position = Vector2.MoveTowards(transform.position, move, _fMoveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = move;
            yield return null;
        }

        GetComponent<Collider2D>().enabled = true;

        //transform.SetParent(SlotInfo.transform);

        bool match = FindMatchBubble();
        
        if (MatchedAction != null)
            MatchedAction.Invoke(match);
    }

    public void DropBubble()
    {
        SlotInfo.bubble = null;
        StartCoroutine(CoDrop());
    }

    IEnumerator CoDrop()
    {
        
        //float ranSpeed = UnityEngine.Random.Range(14f,18f);
        float ranSpeed = UnityEngine.Random.Range(1f,3f);
        float bottom = imgBubble.transform.position.y - 5f;
        //드롭 이펙트
        while (imgBubble.transform.position.y > bottom) 
        {
            ranSpeed *= 1.05f;

            imgBubble.transform.Translate(ranSpeed * Vector2.down * Time.deltaTime);

            yield return null;
        }

        //삭제로직        
        Destroy(gameObject);
    }


    public bool FindMatchBubble()
    {
        matchs.Add(this);
        CheckMatchBubble(SlotInfo);

        Debug.Log($"FindMatchBubble() : {matchs.Count}");
        bool ret = false;
        //매치가 3개 이상면 터뜨린다.
        if(matchs.Count > 2)
        {
            for (int i = 0; i < matchs.Count; i++)
            {
                matchs[i].RemoveBubble();
            }
            ret = true;
        }

        matchs.Clear();

        return ret;
    }
    
    void CheckMatchBubble(Slot checkSlot)
    {
        Debug.Log($"CheckMatchBubble({checkSlot.OffsetX},{checkSlot.OffsetY})");
        foreach(Slot link in checkSlot.LinkSlots)
        {
            if(link != null && link.bubble != null)
            {
                if(TypeID == link.bubble.TypeID)
                {
                    Debug.Log($"CheckMatchBubble() Match ({link.OffsetX},{link.OffsetY})");
                    if (matchs.Contains(link.bubble) == false)
                    {
                        matchs.Add(link.bubble);
                        CheckMatchBubble(link);
                    }

                }
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slot
{
    //[SerializeField] GameObject AimBubble;
    public Bubble bubble;
    //[HideInInspector] public GameObject bubbleObj;
    public int OffsetX;
    public int OffsetY;
    public Vector2 SlotPosition;

    public Slot[] LinkSlots = new Slot[6];

    public Slot()
    { }

    public Slot(int x, int y, Vector2 position)
    {
        OffsetX = x;
        OffsetY = y;
        SlotPosition = position;
    }

    public void SetSlot(int x, int y, Vector2 position)
    {
        OffsetX = x;
        OffsetY = y;
        SlotPosition = position;
    }

    
    public void SetBubble(GameObject bubble)
    {

        GameObject bubbleObj = bubble;//Instantiate(bubble, transform);
        this.bubble = bubbleObj.GetComponent<Bubble>();
        this.bubble.SlotInfo = this;
        bubbleObj.transform.position = SlotPosition;
        bubbleObj.SetActive(true);
    }
}

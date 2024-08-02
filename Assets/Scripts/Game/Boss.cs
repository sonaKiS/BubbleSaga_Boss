using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class Boss : MonoBehaviour
{
    [Header("* BOSS")]
    public Spawner Spawner_Left;
    public Spawner Spawner_Right;
    public bool isLive = true;

    [Header("* UI")]
    [SerializeField] Image HpGaauge;

    [Header("* FACE")]
    [SerializeField] SpriteRenderer imgBossFace;
    [SerializeField] Sprite spIdle;
    [SerializeField] Sprite spDamage;
    [SerializeField] Sprite spSpawn;

    // Private
    int _iBossHP;
    const int _iBossHPMax = 10;
        

    public void InitBoss(ref Slot[,] slots)
    {
        isLive = true;

        imgBossFace.sprite = spIdle;

        _iBossHP = _iBossHPMax;
        HpGaauge.fillAmount = _iBossHP / _iBossHPMax;

        Spawner_Left.ClearBubble();
        Spawner_Left.transform.position = slots[2, 4].SlotPosition;
        Spawner_Left.SetWaypointsLeft(ref slots);
        

        Spawner_Right.ClearBubble();
        Spawner_Right.transform.position = slots[6, 4].SlotPosition;
        Spawner_Right.SetWaypointsRight(ref slots);

        //Spawn(true);
    }

    public void Spawn(bool isInit)
    {
        StartCoroutine(CoSpawn(isInit));
    }

    IEnumerator CoSpawn(bool isInit)
    {
        imgBossFace.sprite = spSpawn;

        Spawner_Left.Spawn(isInit);
        Spawner_Right.Spawn(isInit);

        yield return new WaitUntil(() => Spawner_Left.isSpawning == false);
        yield return new WaitUntil(() => Spawner_Right.isSpawning == false);

        imgBossFace.sprite = spIdle;
    }

    public void Hit()
    {
        if (_iBossHP < 1)
            return;

        _iBossHP -= 1;
        if (_iBossHP <= 0)
        {
            isLive = false;
            imgBossFace.sprite = spDamage;
        }

        StartCoroutine(CoHpGauge());
    }

    IEnumerator CoHpGauge()
    {
        imgBossFace.sprite = spDamage;

        float setHp = (float)_iBossHP / _iBossHPMax;
        while (HpGaauge.fillAmount > setHp)
        {
            HpGaauge.fillAmount -= 0.2f * Time.deltaTime;

            yield return null;
        }

        HpGaauge.fillAmount = setHp;

        if(isLive)
            imgBossFace.sprite = spIdle;
        else
            imgBossFace.sprite = spDamage;
    }
}

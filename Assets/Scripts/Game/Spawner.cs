using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] Boss BossController;
    [SerializeField] GameObject[] PrefabBubbles;
    [SerializeField] GameObject[] PrefabAttackBubbles;
    [SerializeField] Transform BubbleRoot;
    Slot[] Waypoints;

    List<SpawnBubble> _spwanBubbles;

    float _fMoveSpeed = 5f;
    float _fRadius = 0.2f;

    int lastIndex = -1;

    public Slot fixedBubbleSlot;

    const int MaxBubbleCount = 16;
    int _lastLeftCount = 0;

    public bool isSpawning = false;

    void Awake()
    {
        _spwanBubbles = new List<SpawnBubble>();
    }

    public void Spawn(bool initCount = true)
    {
        isSpawning = true;

        Debug.Log($"{gameObject.name} SPAWN {_lastLeftCount}");
        if (initCount)
            StartCoroutine(SpawnProcess(MaxBubbleCount));
        else
        {
            int needCount = 0;
            int leftCount = 0;

            leftCount = BubbleRoot.childCount;
            if (_lastLeftCount == leftCount)
            {
                isSpawning = false;
                return;
            }

            needCount = MaxBubbleCount - leftCount;
            if (needCount > 1)
            {
                needCount -= Random.Range(0, (needCount > 4) ? 4 : needCount);
            }
            //int needCount = MaxBubbleCount - _spwanBubbles.Count;
            Debug.Log($"needCount:{leftCount}+{needCount}");

            _lastLeftCount = BubbleRoot.childCount;

            StartCoroutine(SpawnProcess(needCount));
        }
    }

    IEnumerator SpawnProcess(int spawnLenth)
    {
        int index = 0;
        for (int i = 0; i < spawnLenth; i++)
        {

            //기존 버블 이동
            if (_spwanBubbles != null && _spwanBubbles.Count > 0)
            {
                for (int b = 0; b < _spwanBubbles.Count; b++)
                {
                    //_spwanBubbles[b].Bubble.GetComponent<Bubble>().AirLink = false;
                    StartCoroutine(WaypointMove(_spwanBubbles[b]));
                }
            }

            //생성        
            GameObject bubble = null;
            if (Random.Range(0, 4) == 0)
            {
                bubble = Instantiate(PrefabAttackBubbles[Random.Range(0, PrefabAttackBubbles.Length)], BubbleRoot);
                bubble.GetComponent<BubbleAttack>().boss = BossController;
            }
            else
                bubble = Instantiate(PrefabBubbles[Random.Range(0, PrefabBubbles.Length)], BubbleRoot);

            bubble.transform.localPosition = Vector3.zero;
            //GameObject bubble = Instantiate(PrefabBubbles[Random.Range(0, PrefabBubbles.Length)], BubbleRoot);
            //GameObject bubble = Instantiate(PrefabAttackBubbles[Random.Range(0, PrefabAttackBubbles.Length)], BubbleRoot);
            SpawnBubble cBubble = new SpawnBubble(-1, bubble);
            _spwanBubbles.Add(cBubble);
            lastIndex = _spwanBubbles.Count - 1;

            yield return StartCoroutine(WaypointMove(cBubble));
            yield return null;
            index = cBubble.nowIndex;
        }

        yield return new WaitForSeconds(0.5f);
        Debug.Log($"isSpawning:{isSpawning}");
        isSpawning = false;
    }

    IEnumerator WaypointMove(SpawnBubble bubble)
    {
        if (bubble?.Bubble != null)
        {
            int moveIndex = bubble.nowIndex + 1;

            while (Vector2.Distance(bubble.Bubble.transform.position, Waypoints[moveIndex].SlotPosition) > _fRadius)
            {

                bubble.Bubble.transform.position = Vector2.MoveTowards(bubble.Bubble.transform.position, Waypoints[moveIndex].SlotPosition, _fMoveSpeed * Time.deltaTime);
                yield return null;
            }
            bubble.Bubble.transform.position = Waypoints[moveIndex].SlotPosition;
            bubble.Bubble.GetComponent<Bubble>().SlotInfo = Waypoints[moveIndex];
            Waypoints[moveIndex].bubble = bubble.Bubble.GetComponent<Bubble>();
            bubble.nowIndex = moveIndex;
        }
    }

    public void ClearBubble()
    {
        _spwanBubbles.Clear();
        for (int i = 0; i < BubbleRoot.childCount; i++)
        {
            Destroy(BubbleRoot.GetChild(i));
        }
    }
    

    public void SetWaypointsLeft(ref Slot[,] gameSlots)
    {
        Waypoints = new Slot[16];
        Waypoints[0] = gameSlots[1, 4];
        Waypoints[1] = gameSlots[0, 4];

        Waypoints[2] = gameSlots[0, 5];

        Waypoints[3] = gameSlots[0, 6];
        Waypoints[4] = gameSlots[1, 6];
        Waypoints[5] = gameSlots[2, 6];
        Waypoints[6] = gameSlots[3, 6];

        Waypoints[7] = gameSlots[4, 7];

        Waypoints[8] = gameSlots[3, 8];
        Waypoints[9] = gameSlots[2, 8];
        Waypoints[10] = gameSlots[1, 8];
        Waypoints[11] = gameSlots[0, 8];

        Waypoints[12] = gameSlots[0, 9];

        Waypoints[13] = gameSlots[0, 10];
        Waypoints[14] = gameSlots[1, 10];
        Waypoints[15] = gameSlots[2, 10];

        fixedBubbleSlot = Waypoints[0];
    }

    public void SetWaypointsRight(ref Slot[,] gameSlots)
    {
        Waypoints = new Slot[16];
        Waypoints[0] = gameSlots[7, 4];
        Waypoints[1] = gameSlots[8, 4];

        Waypoints[2] = gameSlots[9, 5];

        Waypoints[3] = gameSlots[8, 6];
        Waypoints[4] = gameSlots[7, 6];
        Waypoints[5] = gameSlots[6, 6];
        Waypoints[6] = gameSlots[5, 6];

        Waypoints[7] = gameSlots[5, 7];

        Waypoints[8] = gameSlots[5, 8];
        Waypoints[9] = gameSlots[6, 8];
        Waypoints[10] = gameSlots[7, 8];
        Waypoints[11] = gameSlots[8, 8];

        Waypoints[12] = gameSlots[9, 9];

        Waypoints[13] = gameSlots[8, 10];
        Waypoints[14] = gameSlots[7, 10];
        Waypoints[15] = gameSlots[6, 10];

        fixedBubbleSlot = Waypoints[0];
    }

    class SpawnBubble
    {
        public int nowIndex;
        public GameObject Bubble;

        public SpawnBubble(int nowIndex, GameObject Bubble)
        {
            this.nowIndex = nowIndex;
            this.Bubble = Bubble;
        }
    }
}

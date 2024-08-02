using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayController : MonoBehaviour//, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    

    [Header("* Ref Objects")]
    [SerializeField] Grid grid;         //�����ġ �����
    [SerializeField] GameObject Root;   //�����Ʈ
    [SerializeField] GameObject PrefabTopWall;  //��ܴ��̹���
    [SerializeField] GameObject[] PrefabBubbles;    //�⺻ ����

    [SerializeField] LineRenderer BubbleLine;   //�̵���� ǥ��

    [SerializeField] Camera CameraObject;
    [SerializeField] Transform ShotRootObject;

    [Header("* Shoot")]
    [SerializeField] Transform AimPoint;    //������ǥ
    [SerializeField] Transform AimTarget;   //��ǥ��ǥ  
    [SerializeField] Transform ShotPoint;   //���� �߻� ������ǥ
    [SerializeField] Bubble LoadedBubble;   //���õ� ����
    [SerializeField] Bubble SecondBubble;   //���� ����
    [SerializeField] Transform[] BubbleMagazine;    //������ ��ǥ���


    [Header("* UI")]
    [SerializeField] TextMesh txtBubbleCount;   //���� �����

    [Header("* BOSS")]
    [SerializeField] Boss BossController;   //���� ��Ʈ�ѷ�

    [Header("* PANEL")]
    [SerializeField] GameObject PanelReady; //��� �г�
    [SerializeField] GameObject PanelWin;   //�¸� �г�
    [SerializeField] GameObject PanelLost;  //�й� �г�
    [SerializeField] Image BlackScreen; //��潺ũ��


    // Private
    bool _bIsTouch = false; //��ġ����
    Vector2 _vShotPosition; //�߻���ǥ
    Vector2 _vPoint;    //��ġ��ǥ
    int _bubbleCount;   //���� ����� ī����

    int _iLayerMask;    //�浹 ��� ���̾� ����ũ
    const int _layerWall = 6;   //��,�� �� ���̾�
    const int _layerBubble = 7; //���� ���̾�
    int _iLineIndex = 0;    //�̵���� ���� ī����
        
    List<Vector2> _MovePostions;    //�̵���� ������
    Slot _AttachSlot;   // �߻�� ������ ���� ����

    Slot[,] _slots; //���� ���� ��
    List<Bubble> LinkedBubbles = new List<Bubble>(); //���԰��� ���� ���� ����
    const float _fCamMinY = 0.57f;
    const float _fShootRootMinY = -5f;    
    float _fBottomBubbleY = 10f;
    float _fBottomBubbleYLast = 10f;
    List<Slot> BottomCheckSlot = new List<Slot>();

    //GAME STATUS
    enum GAMESTATUS
    {
        INIT = 0,   //�ʱ�ȭ
        READY,      //�غ�
        PLAY,       //�÷���
        WAIT,       //���
        PAUSE,      //�Ͻ�����
        WIN,        //�¸�
        LOST,       //�й�
        RESULT      //���Ӱ��
    }
    GAMESTATUS _gamestatus = GAMESTATUS.INIT; //���ӻ���

    enum SHOTSTATUS
    {
        IDLE,
        AIM,
        SHOT,
        SHOTEND
    }
    SHOTSTATUS _shotstatus = SHOTSTATUS.IDLE; //�÷��̾� ����

    // Start is called before the first frame update
    IEnumerator Start()
    {
        _gamestatus = GAMESTATUS.INIT;
        PanelReady.SetActive(true);

        //���� ���� �ʱ�ȭ 
        InitStatus();

        //������ �ʱ�ȭ
        InitBubbleSlot();

        //���� ����
        SetBubble();

        _gamestatus = GAMESTATUS.READY;

        //��������
        BossController.InitBoss(ref _slots);
        yield return new WaitForSeconds(1f);
        BossController.Spawn(true);

        yield return new WaitUntil(() => BossController.Spawner_Left.isSpawning == false);
        yield return new WaitUntil(() => BossController.Spawner_Right.isSpawning == false);

        //��Ʈ�� ����
        StartCoroutine(ClickControl());

        _gamestatus = GAMESTATUS.PLAY;
        _shotstatus = SHOTSTATUS.IDLE;

        PanelReady.SetActive(false);
    }

    //���� ���� �ʱ�ȭ
    void InitStatus()
    {
        _gamestatus = GAMESTATUS.INIT;

        AimPoint.rotation = Quaternion.identity;
        _vShotPosition = new Vector2(ShotPoint.position.x, ShotPoint.position.y);

        _iLayerMask = 1 << _layerWall;
        _iLayerMask += 1 << _layerBubble;

        _MovePostions = new List<Vector2>();

        _bubbleCount = 22;
        txtBubbleCount.text = _bubbleCount.ToString();
    }

    //������ �ʱ�ȭ
    void InitBubbleSlot()
    {
        int yMin = 12;
        int yMax = -25;
        int xMin = 0;
        int xMax = 5;

        _slots = new Slot[10, (Math.Abs(yMin) + Math.Abs(yMax))];
        int incX = 0, incY = 0;

        //��� �������� ����
        xMin = -4;        
        for (int x = xMin; x < xMax; x++)
        {
            _slots[incX, incY] = new Slot();
            _slots[incX, incY].SetSlot(incX, incY, grid.CellToWorld(new Vector3Int(x, yMin, 0)));
            _slots[incX, incY].SetBubble(Instantiate(PrefabTopWall));
            incX++;
        }
        incY++;

        //����
        yMin--;
        for (int y = yMin; y > yMax; y--)
        {
            xMin = (y % 2 == 0) ? -4 : -5;
            incX = 0;
            for (int x = xMin; x < xMax; x++)
            {
                _slots[incX, incY] = new Slot();
                _slots[incX, incY].SetSlot(incX, incY, grid.CellToWorld(new Vector3Int(x, y, 0)));
                incX++;
            }
            incY++;
        }

        //���Ը�ũ ����        
        int matXmax = _slots.GetLength(0);
        int matYmax = _slots.GetLength(1);
        for (int y = 0; y < matYmax; y++)
        {
            for (int x = 0; x < matXmax; x++)
            {
                if (_slots[x, y] != null)
                {
                    _slots[x, y].LinkSlots = new Slot[6];

                    if (y % 2 == 0) //¦��
                    {
                        if (y > 0)
                        {
                            _slots[x, y].LinkSlots[0] = _slots[x, y - 1]; //��-��
                            _slots[x, y].LinkSlots[1] = _slots[x + 1, y - 1]; //��-��
                        }

                        if (x > 0)
                        {
                            _slots[x, y].LinkSlots[2] = _slots[x - 1, y]; //��-��
                        }

                        if (x < 8) //¦�� �ƽ� 8
                        {
                            _slots[x, y].LinkSlots[3] = _slots[x + 1, y]; //��-��
                        }

                        if (y < matYmax - 1)
                        {
                            _slots[x, y].LinkSlots[4] = _slots[x, y + 1]; //��-��
                            _slots[x, y].LinkSlots[5] = _slots[x + 1, y + 1]; //��-��
                        }
                    }
                    else //Ȧ��
                    {
                        if (y > 0)
                        {
                            if (x > 0)
                            {
                                _slots[x, y].LinkSlots[0] = _slots[x - 1, y - 1]; //��-��
                            }
                            _slots[x, y].LinkSlots[1] = _slots[x, y - 1]; //��-��
                        }

                        if(x > 0)
                        {
                            _slots[x, y].LinkSlots[2] = _slots[x - 1, y]; //��-��
                        }
                        
                        if (x < 9) //Ȧ�� �ƽ� 9
                        {
                            _slots[x, y].LinkSlots[3] = _slots[x + 1, y]; //��-��
                        }

                        if (y < matYmax - 1)
                        {
                            if (x > 0)
                            {
                                _slots[x, y].LinkSlots[4] = _slots[x - 1, y + 1]; //��-��
                            }                            
                            _slots[x, y].LinkSlots[5] = _slots[x, y + 1]; //��-��
                        }
                    }
                }

            }
        }
    }

    //���� ����
    void SetBubble()
    {
        LoadBubble(false);
    }

    //���� ����
    void LoadBubble(bool Count = true)
    {

        if (LoadedBubble == null)
        {
            if (SecondBubble != null)
            {
                LoadedBubble = SecondBubble;
                SecondBubble = null;
                LoadedBubble.transform.position = BubbleMagazine[0].position;
            }
            else if (_bubbleCount > 0)
            {
                int RandColor = UnityEngine.Random.Range(0, PrefabBubbles.Length);
                LoadedBubble = Instantiate(PrefabBubbles[RandColor]).GetComponent<Bubble>();
                LoadedBubble.gameObject.GetComponent<Collider2D>().enabled = false;
                LoadedBubble.transform.position = BubbleMagazine[0].position;
                _bubbleCount -= Count ? 1 : 0;
            }
        }

        if(SecondBubble == null && _bubbleCount > 0)
        {
            int RandColor = UnityEngine.Random.Range(0, PrefabBubbles.Length);
            SecondBubble = Instantiate(PrefabBubbles[RandColor]).GetComponent<Bubble>();
            SecondBubble.gameObject.GetComponent<Collider2D>().enabled = false;
            SecondBubble.transform.position = BubbleMagazine[1].position;
            _bubbleCount -= Count ? 1 : 0;
        }

        txtBubbleCount.text = _bubbleCount.ToString();
    }

    //����� ��Ʈ��
    IEnumerator ClickControl()
    {
        while (true)
        {
            if(_gamestatus == GAMESTATUS.PLAY)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _bIsTouch = true;
                }
                else if (Input.GetMouseButtonUp(0))
                    _bIsTouch = false;


                _vPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                if (_shotstatus == SHOTSTATUS.AIM)
                {
                    //Vector2 vHeading = (_vPoint - _vShotPosition).normalized;
                    _vShotPosition = new Vector2(ShotPoint.position.x, ShotPoint.position.y);
                    Vector2 vHeading = (_vPoint - _vShotPosition).normalized;

                    if (_vPoint.y > -3.2f)
                        CheckBubbleLine(_vShotPosition, vHeading);
                    else
                    {
                        _shotstatus = SHOTSTATUS.IDLE;
                        ClearLine();
                    }
                }


                if (_bIsTouch)
                {
                    if (_shotstatus == SHOTSTATUS.IDLE && _vPoint.y > -3.2f)
                        _shotstatus = SHOTSTATUS.AIM;
                }
                else
                {
                    if (_shotstatus == SHOTSTATUS.AIM)
                    {
                        _shotstatus = SHOTSTATUS.SHOT;
                        ShotBubble();
                    }
                }
                
                yield return null;
                ClearLine();
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
            
        }
    }

    //���� �̵���� ǥ��
    void CheckBubbleLine(Vector2 vShotPosition, Vector2 vHeading, bool isShot = false)//Vector2 vShotPosition)
    {
        RaycastHit2D hit = Physics2D.CircleCast(vShotPosition, 0.425f, vHeading, 20f, _iLayerMask);
        Debug.DrawRay(vShotPosition, vHeading * (hit.distance - 0.05f), Color.red);

        if (hit)
        {
            if (hit.collider.gameObject.layer == _layerWall)
            {
                Vector2 vAimPosition = vShotPosition + vHeading * (hit.distance - 0.05f);
                Vector2 vReflect = Vector2.Reflect(vHeading, hit.normal);

                if(isShot)
                    _MovePostions.Add(vAimPosition);

                CheckBubbleLine(vAimPosition, vReflect, isShot);
                DrawLine(vAimPosition);
            }
            else
            {
                AimTarget.gameObject.SetActive(true);

                if (isShot)
                {
                    _MovePostions.Add(vShotPosition + vHeading * (hit.distance - 0.05f));
                    _MovePostions.Add(ShowAimBubble(hit.collider.gameObject.GetComponent<Bubble>().SlotInfo, Vector2.SignedAngle(-hit.collider.transform.up, (hit.point - (Vector2)hit.collider.transform.position))));
                }
                else
                {
                    AimTarget.transform.position = ShowAimBubble(hit.collider.gameObject.GetComponent<Bubble>().SlotInfo, Vector2.SignedAngle(-hit.collider.transform.up, (hit.point - (Vector2)hit.collider.transform.position)));
                    DrawLine(AimTarget.transform.position);
                }
            }
        }
        else
        {
            _MovePostions.Clear();
            AimTarget.gameObject.SetActive(false);
        }
    }

    //���� �߻� ó��
    void ShotBubble()
    {
        Vector2 vPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vHeading = (vPoint - _vShotPosition).normalized;

        CheckBubbleLine(_vShotPosition, vHeading, true);

        LoadedBubble.Move(_MovePostions.ToArray(), _AttachSlot, AttatchEvent);
        LoadedBubble = null;

        AimTarget.gameObject.SetActive(false);
        _MovePostions.Clear();

        _shotstatus = SHOTSTATUS.SHOTEND;
    }

    //���� �̵���� ���� �׸���
    void DrawLine(Vector2 pos)
    {
        if (_iLineIndex < 5)
        {
            BubbleLine.SetPosition(_iLineIndex, pos);
            _iLineIndex++;
        }
    }

    void ClearLine()
    {
        _iLineIndex = 0;
        BubbleLine.SetPositions(new Vector3[] { _vShotPosition, _vShotPosition, _vShotPosition, _vShotPosition, _vShotPosition });
        AimTarget.gameObject.SetActive(false);
    }

    //���� ���� ��ó��
    void AttatchEvent(bool isMatch)
    {
        FindBottomBubble();

        StartCoroutine(CoAttachEvent(isMatch));
    }

    IEnumerator CoAttachEvent(bool isMatch)
    {
        if (isMatch)
        {
            yield return new WaitForSeconds(0.5f);

            //���� Ȯ���ؼ� ����߸���
            CheckBubbleLink(new Slot[] { BossController.Spawner_Left.fixedBubbleSlot, BossController.Spawner_Right.fixedBubbleSlot });

            yield return new WaitForSeconds(1f);

            //������ �ߵ�
            BossController.Spawn(false);

            yield return new WaitForSeconds(0.5f);
        }

        //�÷��̾� ��
        if (BossController.isLive)
        {
            LoadBubble();

            if(LoadedBubble == null && SecondBubble == null && _bubbleCount < 1)
            {
                _gamestatus = GAMESTATUS.LOST;
                StartCoroutine(ShowResult());
            }
            else
            {
                _shotstatus = SHOTSTATUS.IDLE;
            }
        }
        else
        {
            _gamestatus = GAMESTATUS.WIN;
            StartCoroutine(ShowResult());
        }
    }

    //���ǥ��
    IEnumerator ShowResult()
    {
        Color col = Color.black;
        col.a = 0;
        BlackScreen.gameObject.SetActive(true);

        while (col.a < 0.8f)
        {
            col.a += 0.8f * Time.deltaTime;
            BlackScreen.color = col;
            yield return null;
        }

        if(_gamestatus == GAMESTATUS.WIN)
        {
            PanelWin.SetActive(true);
        }
        else
        {
            PanelLost.SetActive(true);
        }
    }

    //������ ������ ���� ǥ��
    Vector2 ShowAimBubble(Slot info, float angle)
    {
        if (info == null)
            return Vector2.zero;

        //Debug.Log($"ShowAimBubble(({info.OffsetX},{info.OffsetY}) , {angle}) START");
        Slot RetSlot = info;
        bool even = info.OffsetY % 2 == 0 ? true : false;

        if (angle > 0 && angle < 60f) //��-��
        {
            if (info.LinkSlots[5] != null)
                RetSlot = info.LinkSlots[5];
            else
                RetSlot = info.LinkSlots[4];
        }
        else if (angle > 60f && angle < 120f) //��
        {
            if (info.LinkSlots[3] != null)
                RetSlot = info.LinkSlots[3];
            else
                RetSlot = even ? info.LinkSlots[5] : info.LinkSlots[4];
        }
        else if (angle > 120f && angle < 180f) //��-��
        {
            if (info.LinkSlots[1] != null)
                RetSlot = info.LinkSlots[1];
            else
                RetSlot = info.LinkSlots[0];
        }
        else if (angle < 0 && angle > -60f) //��-��
        {
            if (info.LinkSlots[4] != null)
                RetSlot = info.LinkSlots[4];
            else
                RetSlot = info.LinkSlots[5];
        }
        else if (angle < -60f && angle > -120f) //��
        {
            if (info.LinkSlots[2] != null)
                RetSlot = info.LinkSlots[2];
            else
                RetSlot = even ? info.LinkSlots[4] : info.LinkSlots[5];
        }
        else if (angle < -120f && angle > -180f) //��-��
        {
            if (info.LinkSlots[0] != null)
                RetSlot = info.LinkSlots[0];
            else
                RetSlot = info.LinkSlots[1];
        }

        _AttachSlot = RetSlot;
        return RetSlot.SlotPosition;
    }


    //��ũ�� ���� ���� üũ �� ���
    public void CheckBubbleLink(Slot[] fixBubbleSlots)
    {
        for (int i = 0; i < fixBubbleSlots.Length; i++)
        {
            FindBubbleLink(fixBubbleSlots[i]);
        }

        int matXmax = _slots.GetLength(0);
        int matYmax = _slots.GetLength(1);
        for (int y = 1; y < matYmax; y++)
        {
            for (int x = 0; x < matXmax; x++)
            {
                if (_slots[x, y] != null && _slots[x, y].bubble != null)
                {
                    if (LinkedBubbles.Contains(_slots[x, y].bubble) == false)
                        _slots[x, y].bubble.DropBubble();
                }
            }
        }

        LinkedBubbles.Clear();
    }

    void FindBubbleLink(Slot checkSlot)
    {
        foreach (Slot link in checkSlot.LinkSlots)
        {
            if (link != null && link.bubble != null)
            {
                if (LinkedBubbles.Contains(link.bubble) == false)
                {
                    LinkedBubbles.Add(link.bubble);
                    FindBubbleLink(link);
                }
            }
        }
    }

    void FindBottomBubble()
    {
        FindBottomBubbleLink(BossController.Spawner_Left.fixedBubbleSlot);
        FindBottomBubbleLink(BossController.Spawner_Right.fixedBubbleSlot);
        BottomCheckSlot.Clear();
        Debug.Log($"FindBottomBubble:{_fBottomBubbleY}");

        if(_fBottomBubbleY > -1.5)
        {
            if(CameraObject.transform.position.y < _fCamMinY)
                StartCoroutine(CoCameraMove(0));
        }
        else
        {
            StartCoroutine(CoCameraMove(Math.Abs(_fBottomBubbleY) - 1.5f ));
        }
    }

    void FindBottomBubbleLink(Slot find)
    {
        foreach (Slot link in find.LinkSlots)
        {
            if (link != null && link.bubble != null)
            {
                if (_fBottomBubbleY > link.SlotPosition.y)
                {
                    _fBottomBubbleY = link.SlotPosition.y;
                }
                if (BottomCheckSlot.Contains(link) == false)
                {
                    BottomCheckSlot.Add(link);
                    FindBottomBubbleLink(link);
                }
            }
        }
    }

    IEnumerator CoCameraMove(float moveValue)
    {
        float camMove = _fCamMinY - moveValue;
        float shotMove = _fShootRootMinY - moveValue;

        if (_fBottomBubbleYLast > _fBottomBubbleY)
        {
            while (CameraObject.transform.position.y >= camMove)
            {
                CameraObject.transform.Translate(Vector2.down * 2f * Time.deltaTime);
                ShotRootObject.transform.Translate(Vector2.down * 2f * Time.deltaTime);
                if (LoadedBubble != null)
                    LoadedBubble.transform.position = BubbleMagazine[0].position;
                if (SecondBubble != null)
                    SecondBubble.transform.position = BubbleMagazine[1].position;
                yield return null;
            }
        }
        else
        {
            {
                while (CameraObject.transform.position.y <= camMove)
                {
                    CameraObject.transform.Translate(Vector2.up * 2f * Time.deltaTime);
                    ShotRootObject.transform.Translate(Vector2.up * 2f * Time.deltaTime);
                    if (LoadedBubble != null)
                        LoadedBubble.transform.position = BubbleMagazine[0].position;
                    if (SecondBubble != null)
                        SecondBubble.transform.position = BubbleMagazine[1].position;
                    yield return null;
                }
            }
        }
        CameraObject.transform.position = new Vector3(CameraObject.transform.position.x, camMove, CameraObject.transform.position.z);
        ShotRootObject.transform.position = new Vector3(ShotRootObject.transform.position.x, shotMove, ShotRootObject.transform.position.z);
        if (LoadedBubble != null)
            LoadedBubble.transform.position = BubbleMagazine[0].position;
        if (SecondBubble != null)
            SecondBubble.transform.position = BubbleMagazine[1].position;

        _fBottomBubbleYLast = _fBottomBubbleY;
        _fBottomBubbleY = 10f;
    }

    //���� ���� ��ư �̺�Ʈ
    public void OnClick_Swap()
    {
        if (_shotstatus != SHOTSTATUS.IDLE)
            return;

        if (SecondBubble != null)
        {
            Bubble temp = SecondBubble;
            SecondBubble = LoadedBubble;
            LoadedBubble = temp;
            temp = null;

            LoadedBubble.transform.position = BubbleMagazine[0].position;
            SecondBubble.transform.position = BubbleMagazine[1].position;

        }
    }

    //���÷��� ��ư �̺�Ʈ
    public void OnClick_Replay()
    {
        SceneController.LoadScene("GamePlay");

    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayController : MonoBehaviour//, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    

    [Header("* Ref Objects")]
    [SerializeField] Grid grid;         //버블배치 참고용
    [SerializeField] GameObject Root;   //버블루트
    [SerializeField] GameObject PrefabTopWall;  //상단더미버블
    [SerializeField] GameObject[] PrefabBubbles;    //기본 버블

    [SerializeField] LineRenderer BubbleLine;   //이동경로 표시

    [SerializeField] Camera CameraObject;
    [SerializeField] Transform ShotRootObject;

    [Header("* Shoot")]
    [SerializeField] Transform AimPoint;    //조준좌표
    [SerializeField] Transform AimTarget;   //목표좌표  
    [SerializeField] Transform ShotPoint;   //버블 발사 시작좌표
    [SerializeField] Bubble LoadedBubble;   //선택된 버블
    [SerializeField] Bubble SecondBubble;   //다음 버블
    [SerializeField] Transform[] BubbleMagazine;    //버블대기 좌표목록


    [Header("* UI")]
    [SerializeField] TextMesh txtBubbleCount;   //남은 버블수

    [Header("* BOSS")]
    [SerializeField] Boss BossController;   //보스 컨트롤러

    [Header("* PANEL")]
    [SerializeField] GameObject PanelReady; //대기 패널
    [SerializeField] GameObject PanelWin;   //승리 패널
    [SerializeField] GameObject PanelLost;  //패배 패널
    [SerializeField] Image BlackScreen; //배경스크린


    // Private
    bool _bIsTouch = false; //터치여부
    Vector2 _vShotPosition; //발사좌표
    Vector2 _vPoint;    //터치좌표
    int _bubbleCount;   //남은 버블수 카운터

    int _iLayerMask;    //충돌 대상 레이어 마스크
    const int _layerWall = 6;   //좌,우 벽 레이어
    const int _layerBubble = 7; //버블 레이어
    int _iLineIndex = 0;    //이동경로 라인 카운터
        
    List<Vector2> _MovePostions;    //이동경로 포인터
    Slot _AttachSlot;   // 발사된 버블의 도달 슬롯

    Slot[,] _slots; //전제 슬롯 맵
    List<Bubble> LinkedBubbles = new List<Bubble>(); //슬롯간의 연결 정보 참고
    const float _fCamMinY = 0.57f;
    const float _fShootRootMinY = -5f;    
    float _fBottomBubbleY = 10f;
    float _fBottomBubbleYLast = 10f;
    List<Slot> BottomCheckSlot = new List<Slot>();

    //GAME STATUS
    enum GAMESTATUS
    {
        INIT = 0,   //초기화
        READY,      //준비
        PLAY,       //플레이
        WAIT,       //대기
        PAUSE,      //일시정지
        WIN,        //승리
        LOST,       //패배
        RESULT      //게임결과
    }
    GAMESTATUS _gamestatus = GAMESTATUS.INIT; //게임상태

    enum SHOTSTATUS
    {
        IDLE,
        AIM,
        SHOT,
        SHOTEND
    }
    SHOTSTATUS _shotstatus = SHOTSTATUS.IDLE; //플레이어 상태

    // Start is called before the first frame update
    IEnumerator Start()
    {
        _gamestatus = GAMESTATUS.INIT;
        PanelReady.SetActive(true);

        //게임 설정 초기화 
        InitStatus();

        //버블슬롯 초기화
        InitBubbleSlot();

        //버블 셋팅
        SetBubble();

        _gamestatus = GAMESTATUS.READY;

        //보스셋팅
        BossController.InitBoss(ref _slots);
        yield return new WaitForSeconds(1f);
        BossController.Spawn(true);

        yield return new WaitUntil(() => BossController.Spawner_Left.isSpawning == false);
        yield return new WaitUntil(() => BossController.Spawner_Right.isSpawning == false);

        //콘트롤 시작
        StartCoroutine(ClickControl());

        _gamestatus = GAMESTATUS.PLAY;
        _shotstatus = SHOTSTATUS.IDLE;

        PanelReady.SetActive(false);
    }

    //게임 설정 초기화
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

    //버블슬롯 초기화
    void InitBubbleSlot()
    {
        int yMin = 12;
        int yMax = -25;
        int xMin = 0;
        int xMax = 5;

        _slots = new Slot[10, (Math.Abs(yMin) + Math.Abs(yMax))];
        int incX = 0, incY = 0;

        //상단 벽역할의 버블
        xMin = -4;        
        for (int x = xMin; x < xMax; x++)
        {
            _slots[incX, incY] = new Slot();
            _slots[incX, incY].SetSlot(incX, incY, grid.CellToWorld(new Vector3Int(x, yMin, 0)));
            _slots[incX, incY].SetBubble(Instantiate(PrefabTopWall));
            incX++;
        }
        incY++;

        //슬롯
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

        //슬롯링크 설정        
        int matXmax = _slots.GetLength(0);
        int matYmax = _slots.GetLength(1);
        for (int y = 0; y < matYmax; y++)
        {
            for (int x = 0; x < matXmax; x++)
            {
                if (_slots[x, y] != null)
                {
                    _slots[x, y].LinkSlots = new Slot[6];

                    if (y % 2 == 0) //짝수
                    {
                        if (y > 0)
                        {
                            _slots[x, y].LinkSlots[0] = _slots[x, y - 1]; //좌-상
                            _slots[x, y].LinkSlots[1] = _slots[x + 1, y - 1]; //우-상
                        }

                        if (x > 0)
                        {
                            _slots[x, y].LinkSlots[2] = _slots[x - 1, y]; //좌-중
                        }

                        if (x < 8) //짝수 맥스 8
                        {
                            _slots[x, y].LinkSlots[3] = _slots[x + 1, y]; //우-중
                        }

                        if (y < matYmax - 1)
                        {
                            _slots[x, y].LinkSlots[4] = _slots[x, y + 1]; //좌-하
                            _slots[x, y].LinkSlots[5] = _slots[x + 1, y + 1]; //우-하
                        }
                    }
                    else //홀수
                    {
                        if (y > 0)
                        {
                            if (x > 0)
                            {
                                _slots[x, y].LinkSlots[0] = _slots[x - 1, y - 1]; //좌-상
                            }
                            _slots[x, y].LinkSlots[1] = _slots[x, y - 1]; //우-상
                        }

                        if(x > 0)
                        {
                            _slots[x, y].LinkSlots[2] = _slots[x - 1, y]; //좌-중
                        }
                        
                        if (x < 9) //홀수 맥스 9
                        {
                            _slots[x, y].LinkSlots[3] = _slots[x + 1, y]; //우-중
                        }

                        if (y < matYmax - 1)
                        {
                            if (x > 0)
                            {
                                _slots[x, y].LinkSlots[4] = _slots[x - 1, y + 1]; //좌-하
                            }                            
                            _slots[x, y].LinkSlots[5] = _slots[x, y + 1]; //우-하
                        }
                    }
                }

            }
        }
    }

    //버블 셋팅
    void SetBubble()
    {
        LoadBubble(false);
    }

    //버블 장전
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

    //사용자 컨트롤
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

    //버블 이동경로 표시
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

    //버블 발사 처리
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

    //버블 이동경로 라인 그리기
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

    //버블 도달 후처리
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

            //연결 확인해서 떨어뜨리기
            CheckBubbleLink(new Slot[] { BossController.Spawner_Left.fixedBubbleSlot, BossController.Spawner_Right.fixedBubbleSlot });

            yield return new WaitForSeconds(1f);

            //스포너 발동
            BossController.Spawn(false);

            yield return new WaitForSeconds(0.5f);
        }

        //플레이어 턴
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

    //결과표시
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

    //버블이 도착할 지점 표시
    Vector2 ShowAimBubble(Slot info, float angle)
    {
        if (info == null)
            return Vector2.zero;

        //Debug.Log($"ShowAimBubble(({info.OffsetX},{info.OffsetY}) , {angle}) START");
        Slot RetSlot = info;
        bool even = info.OffsetY % 2 == 0 ? true : false;

        if (angle > 0 && angle < 60f) //하-우
        {
            if (info.LinkSlots[5] != null)
                RetSlot = info.LinkSlots[5];
            else
                RetSlot = info.LinkSlots[4];
        }
        else if (angle > 60f && angle < 120f) //우
        {
            if (info.LinkSlots[3] != null)
                RetSlot = info.LinkSlots[3];
            else
                RetSlot = even ? info.LinkSlots[5] : info.LinkSlots[4];
        }
        else if (angle > 120f && angle < 180f) //상-우
        {
            if (info.LinkSlots[1] != null)
                RetSlot = info.LinkSlots[1];
            else
                RetSlot = info.LinkSlots[0];
        }
        else if (angle < 0 && angle > -60f) //하-좌
        {
            if (info.LinkSlots[4] != null)
                RetSlot = info.LinkSlots[4];
            else
                RetSlot = info.LinkSlots[5];
        }
        else if (angle < -60f && angle > -120f) //좌
        {
            if (info.LinkSlots[2] != null)
                RetSlot = info.LinkSlots[2];
            else
                RetSlot = even ? info.LinkSlots[4] : info.LinkSlots[5];
        }
        else if (angle < -120f && angle > -180f) //상-좌
        {
            if (info.LinkSlots[0] != null)
                RetSlot = info.LinkSlots[0];
            else
                RetSlot = info.LinkSlots[1];
        }

        _AttachSlot = RetSlot;
        return RetSlot.SlotPosition;
    }


    //링크를 잃은 버블 체크 및 드랍
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

    //버블 스왑 버튼 이벤트
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

    //리플레이 버튼 이벤트
    public void OnClick_Replay()
    {
        SceneController.LoadScene("GamePlay");

    }
}

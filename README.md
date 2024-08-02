# BubbleSaga_Boss
퍼즐 위치사가3 스테이지 10모작

기본플레이 방식을 익히고 임의로 기능을 구현

## 기본 분석
- 플레이 방식은 퍼블보블을 베이스로 진행
- 보블필드 헥사타일. 가로 9 or 10
- 보블의 타입
    - 플레이어 보블
        - 빨간색 : 일반
        - 노란색 : 일반
        - 파란색 : 일반
        - 필살기 : 7x7 영역의 보블을 제거
    - 필드 보블
        - 빨간색 : 일반
        - 노란색 : 일반
        - 파란색 : 일반
        - 공격 : 매칭시 보스에게 공격
        - 전기 : 타격시 3x3보블 제거
- 플레이 시나리오
    1. 보스의 등장과 체력바가 상단에 표시
    2. 보스의 소환 애니메이션과 함께 특정 라인을 따라 랜덤한 보블을 생성 좌우 각각 16개씩
    3. 플레이어는 22개의 랜덤한 일반 보블을 가지고 시작
    4. 2개가 주어지고 스왑가능
    5. 매칭시 아래에 순서를 따름
        1. 보블 매칭 판단에 따라 보블제거 및 링크가 없는 보블 추락
        2. 매칭보블이 공격이라면 보스에게 공격 및 체력바 조정
        3. 점수 카운팅 및 필살게이지 누적
        4. 보스의 체력이 남아있다면 좌우 각각 3-5개의 보블을 생성하되 16개가 넘지 않는다.
        5. 공격형 보블의 수는 각각 4개가 되어야 한다.
        6. 보스의 체력이 없다면 게임 승리
        7. 필살게이지가 가득찼다면 필살기보블 생성
- 플레이어가 22개의 보블을 다 사용하고 보스의 체력이 남아있다면 게임 패배
- 조작영역
    - 상단 드레그 터치한 포인트로 방향조정. 놓으면 발사
    - 하단 드레그 터치한 포인트의 반대 방향조정. 놓으면 발사
    - 드레그중 중앙으로 오면 취소
    - 중앙 터치시 대기 보블 전환
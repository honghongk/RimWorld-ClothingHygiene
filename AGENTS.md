# AGENTS.md — Clothing Hygiene 작업 규칙

> 뼈대: **Karpathy Guidelines** (Andrej Karpathy 의 LLM 코딩 실패 관찰 → 4원칙, MIT, `multica-ai/andrej-karpathy-skills`).
> 원칙은 그대로 두고, 이 프로젝트에 필요한 게이트만 덧붙였다.

---

## Part 1. Karpathy 4원칙 (원문 요약 + 이 프로젝트 적용)

### 1. Think Before Coding — 가정하지 말고 꺼내 놓는다

- 가정은 명시한다. 불확실하면 **묻는다**. 조용히 하나를 골라 진행하지 않는다.
- 해석이 갈리면 **양쪽을 제시**한다(`(a)`/`(b)` 라벨만 쓰지 말고 각각 무엇을 만드는지 한 줄로).
- 더 단순한 길이 있으면 말하고, 필요하면 **반대 의견을 낸다**.
- 헷갈리면 멈추고 **무엇이 헷갈리는지 이름 붙여** 질문한다.

> 이 프로젝트 적용: 밸런스 수치·게임 동작은 **실측 전 추측 금지**. `Version.txt`·DLL·def XML 로 먼저 확인한다.

### 2. Simplicity First — 문제를 푸는 최소 코드

- 요청에 없는 기능 금지. 단일 사용처를 위한 추상화 금지. 요청 안 한 "유연성/설정" 금지.
- 일어날 수 없는 상황의 예외 처리 금지.
- **200줄이 50줄이 될 수 있으면 다시 쓴다.** 선임 엔지니어가 "이거 과하네" 할 구조면 단순화.

> DDD 는 **경계를 지키는 만큼만** 도입한다. 계층을 늘리는 게 목적이 아니다 — Domain 에 게임 타입이 새어들지 않게 하는 게 목적이다.

### 3. Surgical Changes — 손대야 할 것만 손댄다

- 옆 코드·주석·포맷을 "개선"하지 않는다. 깨지지 않은 걸 리팩터링하지 않는다.
- 기존 스타일을 따른다(내 취향과 달라도).
- 관계없는 죽은 코드는 **지우지 말고 언급만** 한다.
- 내 변경 때문에 고아가 된 import/변수/함수만 정리한다.
- **테스트: 변경된 모든 줄이 요청과 직결돼야 한다.**

> 바닐라 시스템(Tainted·내구도·무드 계산)은 수정 대상이 아니다. 새 상태는 항상 별도로 둔다.

### 4. Goal-Driven Execution — 성공 기준을 정의하고 검증까지 루프

- 명령형 작업을 **검증 가능한 목표**로 바꾼다. ("동작하게 해줘" 금지)
- 다단계 작업은 계획을 먼저 적는다:

```
1. [단계] → verify: [확인 방법]
2. [단계] → verify: [확인 방법]
```

> 이 프로젝트에서 "동작"의 최소 기준은 **① `dotnet build` 0경고 0오류 → ② `Player.log` 에 모드 XML/패치 오류 0건 → ③ 인게임에서 상태 확인**. ①만으로 완료 보고하지 않는다.

---

## Part 2. 이 프로젝트 전용 규칙

### 2.1 구현 전 중복 확인 게이트 (필수)

새 클래스·메서드·def·번역키를 만들기 **전에** 이미 있는지 확인한다.

```bash
# 1) 바닐라 API 에 이미 있는가
AC="$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll"
ilspycmd -l c "$AC" | grep -i <이름>          # 타입
ilspycmd -t <Type> "$AC" | grep -i <멤버>      # 멤버

# 2) 바닐라 def/번역키에 이미 있는가
grep -rn "<이름>" "$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Data/Core" --include=*.xml

# 3) 이 저장소에 이미 있는가
grep -rn "이름" Source/
```

- 있으면 **재사용**, 없으면 만든다. 확인 없이 새로 만든 심볼은 컴파일이 통과해도 게임에서 `MissingMethodException` 으로 죽는다.
- 네임스페이스도 추측하지 않는다(`Apparel`=RimWorld, `ThingComp`·`Scribe_Values`=Verse).

### 2.2 성능 — 틱이 1순위 요구사항

이 모드는 **인게임 틱 속도 저하가 곧 실패**다. 로딩 시간은 목표가 아니다.

| 규칙 | 이유 |
|---|---|
| 매 틱 훅 금지. `GameComponentTick` 은 `TicksGame % 250` 가드 | 250틱 = rare 틱, 림월드 관례 |
| 순회는 `mapPawns.AllPawnsSpawned` (`IReadOnlyList`, 무할당) | `FreeColonistsSpawned` 는 호출마다 `List` 생성 |
| `foreach` + LINQ 금지(핫패스). `for` + 인덱스 | enumerator/클로저 할당 |
| 문자열 연결·`Translate` 를 틱 루프에서 금지 | inspect/툴팁에서만 |
| 새 캐시를 만들기 전에 **먼저 측정** | 측정 없는 최적화는 대개 비용만 늘린다 |
| 추가한 비용은 **숫자로 보고** (Dubs Performance Analyzer 또는 자체 Stopwatch) | "가볍습니다"는 근거가 아니다 |

- 예산 감각: 콜로니스트 30명 × 착용 5벌 = 150 comp / 250틱 → **틱당 0.6회**. 이 규모에서 순회 자체는 무시 가능하다. 실제 위험은 **폰마다 매 틱 도는 훅**과 **할당**이다.
- 성능 변경은 전후 수치를 같은 조건에서 비교한다(같은 세이브, 같은 배속).

### 2.3 아키텍처 — 도메인 주도, 경계는 컴파일 가능한 규칙으로

```
Source/
├── Domain/         게임 엔진 비의존. 순수 C#. 여기엔 `using Verse` 가 없어야 한다.
├── Application/    유스케이스. Domain 규칙 + 세계 상태를 조합. Verse 최소 접촉.
├── Infrastructure/ Verse 어댑터. comp/gamecomponent/패치/세이브.
└── Presentation/   표시. inspect 문자열·설정 UI·gizmo.
```

- **Domain 은 Verse 를 모른다.** 확인: `grep -rn "using Verse\|using RimWorld" Source/Domain/` → 0건이어야 한다.
- 경계를 넘는 값은 원시 타입이나 Domain 값 객체로만. `Pawn`·`Thing` 을 Domain 으로 넘기지 않는다.
- 게임 쪽 사정(틱 간격, 세이브 형식)은 Infrastructure 에서 흡수하고 Domain 은 "하루에 얼마나 더러워지는가"만 안다.
- 계층을 더 쪼개지 않는다. 이 모드 규모에서 4계층이 상한이다.

### 2.4 빌드·검증

```bash
export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
cd ~/Desktop/project/ClothingHygiene
dotnet build Source/ClothingHygiene.csproj -c Release
```

- 타깃은 **net472** (게임 1.6 = Unity Mono). `net6/8/10` 은 로드 불가.
- 래퍼 스크립트를 만들지 않는다. 명령을 직접 친다.
- 로그: `~/Library/Application Support/Ludeon Studios/RimWorld/Logs/Player.log`
  확인 문구: `Failed to find a node with the given xpath`, `Could not find class`, `MissingMethodException`, 모드 DLL 관련 오류.
- **빌드 성공과 인게임 동작을 구분해서 보고한다.**

### 2.5 불변식 (깨지면 설계 실패)

- **오염도는 개별 의복 Thing 단위다.** 슬롯·폰·콜로니 단위로 캐싱하거나 합산 저장하지 않는다.
  같은 종류 2벌은 별개 값이고, 옷을 벗어 바닥·재고·선반에 두거나 다른 림에게 넘겨도 값은 그 아이템을 따라간다.
- **무드 보너스는 중첩(최대 +5), 디버프는 비중첩**(가장 더러운 한 벌만).
- **DBH 는 소프트 의존.** `Dubwise.DubsBadHygiene` 를 컴파일 참조하지 않는다(활성 감지 + 리플렉션 어댑터).
  DBH 미설치 환경에서 모든 기능이 성립해야 한다.
- **자체 세탁기.** DBH 세탁기는 Tainted 전용이므로 재사용하지 않는다(역할이 다르다).

### 2.6 세이브·호환

- 새 상태는 전부 `Scribe_Values.Look(ref x, "키", 기본값)` 로 저장/복원. 키는 `ch_` 접두사.
- 모드 제거 시 세이브에 남는 것은 무해해야 한다(별도 def 를 추가하지 않고 comp 만 쓰는 이유).
- 바닐라 `Tainted`·`hitPoints` 를 읽기만 하고 쓰지 않는다.

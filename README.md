<div align="center">

# 🧺 Clothing Hygiene

**입고 있는 옷이 시간에 따라 더러워지고, 더러우면 기분이 나빠지고, 빨거나 갈아입어야 해결되는 림월드 모드**

![RimWorld](https://img.shields.io/badge/RimWorld-1.6.4871-4a7eba?style=flat-square)
![Target](https://img.shields.io/badge/.NET-net472-512BD4?style=flat-square)
![Platform](https://img.shields.io/badge/macOS-arm64-000000?style=flat-square)
![Status](https://img.shields.io/badge/status-WIP-yellow?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)

</div>

---

## 무엇을 하나

바닐라의 옷 페널티는 두 가지뿐입니다.

| 바닐라 | 조건 |
|---|---|
| **Tainted** | 죽은 사람이 입던 옷 |
| **내구도 50% 이하** | 옷이 낡음 |

**멀쩡하고 새 옷이어도, 며칠 입으면 갈아입어야 한다**는 축이 없습니다. 이 모드가 그 축을 추가합니다.

> 바닐라 Tainted·내구도 시스템은 **읽지도 쓰지도 않습니다.** 완전히 독립된 상태입니다.

## 코어 루프

```
     착용 ──▶ 오염 누적 ──▶ 단계 상승 ──▶ 무드 페널티
                              │
              ┌───────────────┴───────────────┐
        (a) 예비복으로 갈아입기          (b) 빨래해서 다시 입기
              │                               │
              └──────── 깨끗한 옷 확보 ◀──────┘
```

오염도만 만들고 끝내면 "무드 디버프 하나 추가한 모드"입니다.
**깨끗한 예비복 확보 → 스스로 갈아입기**까지 있어야 플레이가 성립합니다.

## 표시

- 의복 inspect 패널 · 호버 툴팁 · 정보 카드(i) 에 청결도 % (항상 표시)
- **아이콘 좌측 세로 막대** — `Widgets.ThingIcon` 후킹이라 바닐라 장비 탭·재고·상점은 물론, 같은 함수로 아이콘을 그리는 UI 모드에도 함께 표시됩니다. 단계별 색: 연두(사용감) → 노랑(냄새남) → 주황(찝찝함) → 빨강(매우 더러움). 깨끗한 옷은 막대를 그리지 않습니다.

## 의존성

| 모드 | 종류 | 이유 |
|---|---|---|
| **Harmony** (`brrainz.harmony`) | 필수 | 아이콘 오버레이 패치(`Widgets.ThingIcon` postfix) |
| Dubs Bad Hygiene | 선택 | 배관 물. 없으면 물 없이 세탁 |

## 오염도와 무드

의복 한 벌당 `0~100%`, 기본 **하루 65%** — 유휴 18.5시간 / 노동 12.3시간에 `냄새남`(−3) 도달. 거의 매일 갈아입게 됩니다.

| 오염도 | 단계 | 무드 | 중첩 |
|---|---|---|---|
| 0~20% | 깨끗함 | **+1** | **가능 (최대 +5)** |
| 20~50% | 사용감 | 0 | — |
| 50~70% | 냄새남 | −3 | 불가 |
| 70~90% | 찝찝함 | −6 | 불가 |
| 90~100% | 매우 더러움 | −10 | 불가 |

- **깨끗한 옷은 보너스가 중첩됩니다** (깨끗한 벌마다 +1, 상한 +5) — 옷을 잘 관리한 콜로니가 보상받습니다.
- **디버프는 중첩되지 않습니다** — 가장 더러운 한 벌만 계산합니다. 옷 5벌이 −50 이 되는 걸 막습니다.

오염은 상황에 따라 빨라집니다: 수면 ×0.4, 노동 ×1.5, 격렬한 작업(광산·화재·전투) ×2.5, 기온 30℃ 이상 ×1.3.
의복 종류도 반영됩니다: 속옷 ×0.7, 일반 ×1.0, 방한복·판금 ×1.4.

## 세탁 — 연구로 푸는 계단

**손세탁**: 더러운 옷을 바닥·재고에 두면 폰이 집어다 세탁대에서 빱니다(작업 우선순위 **청소**). **입고 있는 옷은 빨지 않습니다** — 갈아입기로 벗은 옷만 대상입니다.

**물가 세탁 자리**: 건물 없이 강·호수 옆에서 손빨래. 배치할 때 8방향 중 한 칸이라도 물이어야 설치되며, 물을 소비하지 않습니다(대신 느림).

**세탁기**: 옷을 **안에 넣어 두는** 방식입니다. 내부 수납 5칸, `운반` 작업으로 넣고 꺼냅니다. 투입 후 15초간은 추가로 넣을 수 있고, **가동이 시작되면 문이 잠깁니다**(더 넣기 불가). 안의 더러운 옷을 다 씻으면 다시 열립니다. inspect 에 `내부 3/5벌 · 가동 중` 표시.

기본 빨래는 **연구 없이** 됩니다. 편해지는 건 연구로 엽니다.

| 시대 | 수단 | 물 | 세제 | 연구 |
|---|---|---|---|---|
| 아무 시대 | **손빨래** (물가·세탁통) | 선택 | 불필요 | 없음 |
| 중세 | **세탁통** (전용 작업대) | 필요 | 잿물 | `ClothingHygiene_Basin` |
| 산업 | **비누** (세제 생산) | — | 생산 | `ClothingHygiene_Soap` |
| 산업 | **세탁기** (전력·자동 반복) | 필요 | 필요 | `ClothingHygiene_Washer` + `Electricity` |
| 우주 | **초음파 세탁기** (물·세제 절반) | 필요 | 필요 | `ClothingHygiene_UltraSonic` |

- 세탁하면 오염도 0. 대신 **내구도가 조금 닳습니다**(기본 −2%p, 설정으로 0까지).
- **Tainted 는 제거하지 않습니다.** 그건 별개 축입니다.

## 물 — Dubs Bad Hygiene 가 있으면 빌려 씁니다

| 환경 | 동작 |
|---|---|
| **Dubs Bad Hygiene 설치** | 배관망의 물을 끌어다 씁니다. 배관이 없으면 물통으로 대체 |
| **미설치** | 물 없이 세탁. 모드 자체 자원을 새로 만들지 않습니다 |

하드 의존이 아닙니다. DBH 는 **소프트 의존**으로만 감지하고, 없으면 조용히 무물 모드로 떨어집니다.

> DBH 는 자체 `Building_WashingMachine`(전력·배관·`innerContainer`) 을 이미 갖고 있습니다.
> 이 모드는 그 건물을 **오염도 시스템에 연결**하는 방향으로 갑니다 — 같은 세탁기를 하나 더 만들지 않습니다.

## 성능

이 모드는 **인게임 틱 속도가 1순위**입니다. 로딩 시간은 목표가 아닙니다.

| 항목 | 설계 |
|---|---|
| 틱 진입 | `TicksGame % 250` 가드 (rare 틱), 매 틱 훅 없음 |
| 순회 | `mapPawns.AllPawnsSpawned` (무할당 `IReadOnlyList`) |
| 핫패스 | `for` + 인덱스, LINQ·문자열·클로저 없음 |
| 예상 부하 | 콜로니스트 30명 × 착용 5벌 = 150 comp / 250틱 → **틱당 0.6회** |
| 측정 | Dubs Performance Analyzer + 자체 Stopwatch (일 1회 로그) |

최적화는 측정 후에만. "가볍습니다" 대신 전후 수치로 말합니다.

## 아키텍처

```
Source/
├── Domain/          게임 엔진 비의존 순수 C#  (using Verse 0건)
│   ├── HygieneValue       오염도 값 객체 (0~1 클램프)
│   ├── HygieneStage       단계 경계·라벨
│   ├── FilthRates         활동·환경·의복 계수
│   └── GarmentChangeRule  갈아입기 조건 판정
├── Application/     유스케이스 (누적·갈아입기 오케스트레이션)
├── Infrastructure/  Verse 어댑터 (ThingComp·GameComponent·Harmony·세이브)
└── Presentation/    inspect 문자열·설정 UI·gizmo
```

**경계는 grep 으로 검증되는 규칙입니다:** `Source/Domain/` 에 `using Verse`·`using RimWorld` 가 0건.

## 빌드

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"

dotnet build Source/ClothingHygiene.csproj -c Release
# → Assemblies/ClothingHygiene.dll   (.NETFramework,Version=v4.7.2)
```

- **타깃은 `net472`.** RimWorld 1.6 은 Unity Mono 이므로 `net6/8/10` 빌드는 게임이 로드하지 못합니다.
- 게임 DLL 경로는 `Source/ClothingHygiene.csproj` 의 `RimWorldContent` 프로퍼티에 있습니다.
- API 는 반드시 실제 DLL 에서 확인합니다:

```bash
AC="$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll"
ilspycmd -t RimWorld.Pawn_ApparelTracker "$AC"
ilspycmd -l c "$AC" | grep -i Apparel
```

게임에 물리기(로컬 모드):

```bash
ln -sfn "$PWD" "$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/ClothingHygiene"
```

## 저장소 구조

```
ClothingHygiene/
├── About/About.xml                     honghongk.clothinghygiene · 1.6
├── Assemblies/                         빌드 산출물 (git 제외)
├── Patches/Hygiene_ApparelComps.xml    코어 추상 부모에 오염 comp 부착
├── Languages/{English,Korean}/Keyed/
├── Source/                             csproj + Domain/Application/Infrastructure/Presentation
├── docs/PLAN.md                        시스템 명세·밸런스·검증 시나리오
├── docs/TODO.md                        마일스톤 (항목별 verify 기준)
└── AGENTS.md                           작업 규칙 (Karpathy 4원칙 + 중복확인 게이트)
```

## 로드맵

- [x] 오염도 저장 · 착용 중 누적 · inspect 표시 · 설정 슬라이더
- [ ] Domain 분리 · 밸런스 기본값 정렬
- [ ] 단계별 무드 (보너스 중첩 / 디버프 비중첩)
- [ ] 세탁 연구 트리 · 세제 생산 · 세탁기
- [ ] Dubs Bad Hygiene 물 연동 (소프트 의존)
- [ ] 자동 갈아입기 Job
- [ ] 성능 계측 · 100인 콜로니 검증

## 크레딧

- 작업 규칙: [Andrej Karpathy's guidelines](https://x.com/karpathy/status/2015883857489522876) (via [multica-ai/andrej-karpathy-skills](https://github.com/multica-ai/andrej-karpathy-skills), MIT)
- RimWorld © Ludeon Studios. 이 모드는 Ludeon 과 무관합니다.

## 라이선스

MIT

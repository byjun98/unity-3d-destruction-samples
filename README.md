<div align="center">

# 3D Destruction Sandbox

**Unity URP 기반 6종 파괴·붕괴·슬라이싱 시뮬레이션 — 게임에서 자주 보는 “부서지는 연출”을 게임 클라이언트 관점에서 직접 구현**

Unity 2022.3 LTS · URP 14 · C# · Unity Physics · Mesh API · Custom Simulation

[![Unity](https://img.shields.io/badge/Unity-2022.3.62f3_LTS-000000?style=for-the-badge&logo=unity&logoColor=white)](https://unity.com/)
[![URP](https://img.shields.io/badge/URP-14.0.12-1A1A1A?style=for-the-badge)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/)
[![C#](https://img.shields.io/badge/C%23-9.0-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Unity Physics](https://img.shields.io/badge/Unity-Physics%2FMesh_API-000000?style=for-the-badge&logo=unity&logoColor=white)](https://docs.unity3d.com/ScriptReference/)

</div>

---

## 목차

- [프로젝트 소개](#프로젝트-소개)
- [시연 영상 & 대표 스크린샷](#시연-영상--대표-스크린샷)
- [데모 한눈에 보기](#데모-한눈에-보기)
- [시스템 아키텍처](#시스템-아키텍처)
- [기술 스택 & 선정 이유](#기술-스택--선정-이유)
- [데모별 심층 분석](#데모별-심층-분석)
  - [1. 모델 교체형 파괴 (Impact Destruction)](#1-모델-교체형-파괴-impact-destruction)
  - [2. 그리드 기반 화재 전파 (Fire Spread)](#2-그리드-기반-화재-전파-fire-spread)
  - [3. 복셀 건물 파괴 (Voxel Destruction)](#3-복셀-건물-파괴-voxel-destruction)
  - [4. 구조 응력 붕괴 (Stress Collapse)](#4-구조-응력-붕괴-stress-collapse)
  - [5. 빔 소프트바디 차량 (Beam Soft-Body Vehicle)](#5-빔-소프트바디-차량-beam-soft-body-vehicle)
  - [6. 런타임 메시 슬라이싱 (Mesh Slicing)](#6-런타임-메시-슬라이싱-mesh-slicing)
- [공통 설계 패턴](#공통-설계-패턴)
- [트러블슈팅 & 배운 점](#트러블슈팅--배운-점)
- [활용 시나리오와 양산 시 주의점](#활용-시나리오와-양산-시-주의점)
- [프로젝트 구조](#프로젝트-구조)
- [실행 방법](#실행-방법)
- [핵심 요약](#핵심-요약)
- [알려진 한계와 에셋 정책](#알려진-한계와-에셋-정책)

---

## 프로젝트 소개

`3d_sample`은 상용 게임에서 자주 등장하는 **“파괴·붕괴·슬라이싱” 연출**의 내부 구현을 게임 클라이언트 관점에서 직접 만들어 본 샘플 프로젝트입니다.

게임에서 “벽이 부서지고, 건물이 무너지고, 차가 찌그러지고, 검에 두 동강 나는” 연출은 화면상으로는 비슷해 보여도 **내부 구현은 전혀 다른 시뮬레이션 패턴들의 조합**으로 만들어집니다. 본 프로젝트는 그 패턴들을 여섯 가지로 나눠 각각 독립된 데모 씬과 시스템으로 구현했고, 외부 destruction 라이브러리(예: Obi, RayFire, NVIDIA Blast) 에 의존하지 않고 Unity 표준 빌딩블록(Rigidbody/Collider/Joint/Mesh API)만으로 **내부 동작을 끝까지 통제 가능하도록** 만들었습니다.

> 핵심 의도: “상용 게임처럼 보이는 결과”를 만드는 가장 짧은 길이 항상 “시뮬레이션을 더 정밀하게”는 아니라는 사실을 검증.
> 데이터 모델 + 단계적 상태 전이 + 시각·사운드 피드백의 조합이, 풀-피직스 시뮬레이션보다 **체감 품질**에서 우위인 경우가 많다는 것을 6개 데모로 보였습니다.

본 저장소는 학습 및 기술 검증 목적이며, 일부 VFX·모델은 무료 / Asset Store 에셋을 사용했고 시뮬레이션 로직(`Assets/Scripts`, `Assets/Slicing`)은 직접 작성했습니다.

> 에셋 정책: Git 저장소에는 직접 작성한 코드, 데모 씬, 래퍼 프리팹, 샘플용 머티리얼만 포함합니다. Asset Store 원본 패키지와 대용량 모델·텍스처·사운드는 라이선스와 용량 문제로 제외되어 있어, 새로 클론한 환경에서는 일부 프리팹/씬 참조가 끊길 수 있습니다.

---

## 시연 영상 & 대표 스크린샷

공개 저장소에는 대용량 캡처 파일을 포함하지 않습니다. 로컬에서 데모를 실행해 캡처를 추가할 경우 아래 파일명을 기준으로 정리하면 README와 포트폴리오 자료를 연결하기 쉽습니다.

### 캡처 파일명 가이드

| 용도 | 권장 파일명 | 보여주면 좋은 장면 |
| --- | --- | --- |
| 대표 이미지 | `docs/hero.png` | 6개 데모를 한 장에 모은 콜라주 또는 가장 인상적인 파괴 순간 |
| Impact Destruction | `docs/01_impact.gif` | 타격 흔적 → HP 감소 → 파편 교체 흐름 |
| Fire Spread | `docs/02_fire.gif` | 나무/기름/물/돌 셀의 다른 전파 반응 |
| Voxel Destruction | `docs/03_voxel.gif` | 폭발 반경에 따른 구멍, 지지 끊긴 청크 분리 |
| Stress Collapse | `docs/04_stress.gif` | 기둥 파괴 후 지연 붕괴와 하중 시각화 |
| Beam Soft-Body | `docs/05_beam.gif` | 충돌 후 차체 변형, 빔 손상 HUD |
| Mesh Slicing | `docs/06_slice.gif` | 드래그 방향에 따른 절단 평면과 단면 색 |

---

## 데모 한눈에 보기

| # | 데모 | 스크린샷 자리 | 핵심 키워드 | 게임 내 활용 예시 |
| --- | --- | --- | --- | --- |
| 1 | **Impact Destruction** | `docs/01_impact.gif` | HP / 머티리얼 태그 / 사전분할 파편 | 항아리, 유리창, 박스, 약한 환경 오브젝트 |
| 2 | **Fire Spread** | `docs/02_fire.gif` | 셀 상태머신 / 온도·습도·HP / 활성셀 큐 | 창고 화재 이벤트, 산불 미션, 마법 화염 전파 |
| 3 | **Voxel Destruction** | `docs/03_voxel.gif` | 6면 인접 메시화 / BFS 무결성 / 청크 분리 | 정형 건물·벽, 멀티플레이 친화적 파괴 |
| 4 | **Stress Collapse** | `docs/04_stress.gif` | 하중 그래프 / 재분배 / 단계 붕괴 큐 | 보스전 스테이지 붕괴, 미션 트리거 연출 |
| 5 | **Beam Soft-Body Vehicle** | `docs/05_beam.gif` | 노드-빔 / yield/break strain / 메시 디포머 | 카크래시 연출, 차체 데미지 시각화 |
| 6 | **Mesh Slicing** | `docs/06_slice.gif` | 평면 분할 / cap 폴리곤 / 서브메시 보존 | 검·레이저 절단, 처형 연출, 도형 퍼즐 |

각 데모는 “입력 → 시뮬 → 표현”이라는 공통 흐름을 공유하지만, 시뮬레이션 코어는 폴더 단위로 완전히 분리되어 있어 한 데모를 수정해도 다른 데모에 영향을 주지 않습니다.

---

## 시스템 아키텍처

```text
┌─────────────────────────────────────────────────────────────┐
│                       Unity Client (URP)                    │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │ Input Layer  │  │ Camera Rig   │  │  HUD / Debug     │  │
│  │ Shooter,     │  │ Orbit,       │  │  Stress HUD,     │  │
│  │ Drive Input, │  │ Follow Cam,  │  │  SoftBody HUD,   │  │
│  │ Slice Input  │  │ Camera Shake │  │  Visualizers     │  │
│  └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘  │
│         │                 │                   │             │
│         ▼                 ▼                   ▼             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              Demo Simulation Layer                  │   │
│  │                                                     │   │
│  │ Impact │ Fire  │ Voxel  │ Stress │ Beam   │ Mesh    │   │
│  │ Break  │ Grid  │ Build  │Collapse│Vehicle │ Slicing │   │
│  └─────────────────────────────────────────────────────┘   │
│                          │                                  │
│                          ▼                                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │       Unity Physics + URP Render + Editor Tools     │   │
│  │  Rigidbody  Collider  Joint  Shader/Material  PostFX│   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

> 각 데모는 **입력·시뮬·표현 3계층**을 분리하고, `TakeDamage`, `ApplyExplosion`, `ApplyImpact`, `TrySlice`처럼 좁은 진입점으로 외부 입력을 받습니다. 덕분에 발사체·폭발·근접타격처럼 입력 출처가 달라도 시뮬레이션 코어와 표현 로직을 재사용하기 쉽습니다.

---

## 기술 스택 & 선정 이유

| 기술 | 선정 이유 (게임 개발 관점) |
| --- | --- |
| **Unity 2022.3 LTS (2022.3.62f3)** | 작성 시점 LTS. 장기 지원 버전이라 URP 14, PostProcessing, TextMeshPro 같은 패키지를 안정적으로 묶을 수 있습니다. |
| **URP 14.0.12** | 모바일까지 포함하는 멀티 플랫폼 일관 화질. Bloom/Vignette PostFX로 폭발·붕괴 임팩트를 강조하기 좋고, 머티리얼/셰이더를 URP 기준으로 통일해 외부 에셋을 한 씬 안에서 맞추기 좋습니다. |
| **순수 C# + Unity 표준 물리** | Obi/RayFire/Blast 같은 외부 destruction 라이브러리에 묶이지 않기 위해 의도적으로 표준 빌딩블록만 사용. 구현 흐름을 직접 추적하고, 각 단계의 비용과 한계를 코드 레벨에서 확인하기 위함. |
| **데이터 지향 구조 설계** | 화재는 `activeCells`만 틱 처리하고, 복셀은 3차원 셀 배열·BFS 큐·노출 면 메시화로 상태와 표현을 분리했습니다. Burst/Jobs는 패키지로 포함되어 있지만 현재 직접 작성 코드는 순수 C#이며, 이후 최적화 후보로 남겨두었습니다. |
| **커스텀 셰이더 / 머티리얼 제어** | 슬라이싱 단면은 `CapUnlit.shader`, 화재/응력/빔 데모는 머티리얼 색과 emissive 계열 값을 통해 상태를 시각화했습니다. 복잡한 렌더링보다 “데이터 상태가 화면에서 바로 읽히는 것”을 우선했습니다. |
| **PostProcessing 3.4** | Bloom/Vignette/Chromatic을 단계적 임팩트 큐로 활용. 카메라 셰이크 + Bloom 펄스가 “단계 변화”의 체감을 가장 크게 끌어올림. |
| **에디터 자동화 (`Assets/Editor`)** | 데모 씬 자동 재구성, URP 머티리얼 일괄 보정(`UrpMaterialFixer`, `StressUrpMaterialFixer`) 메뉴. 외부 에셋을 그대로 쓰면서도 파이프라인을 통일하기 위한 인프라성 코드. |

---

## 데모별 심층 분석

각 데모는 같은 포맷으로 정리했습니다.
**왜 만들었는가 → 어떻게 구현했는가 → 어디서 고생했는가 → 대안은 무엇인가 → 게임/기획 관점.**

---

### 1. 모델 교체형 파괴 (Impact Destruction)

<details>
<summary><b>기술 상세 펼치기</b></summary>

#### 왜 이 패턴인가

“상자·항아리·유리창처럼 한 번 부서지면 끝나는 환경 오브젝트”는 **런타임 메시 절단이 과잉**입니다. 게임에서는 미리 분할된 파편 프리팹을 만들어 두고, HP가 0이 되는 순간 정상 모델을 끄고 파편 프리팹을 같은 자리에 켜는 방식을 자주 씁니다. 이 데모는 그 실용적인 절차를 작은 규모로 재현합니다.

#### 데이터 흐름

```text
Player Input (Space / Mouse)
        │
        ▼
DemoImpactShooter ── ray / projectile ──▶ BreakableObject.TakeDamage(dmg, pt, normal, dir)
                                            │
                                            ├── currentHealth -= dmg × MaterialMultiplier
                                            ├── SpawnHitFeedback(decal, dust, sfx)
                                            └── if hp <= 0 → SwapToFragments()
                                                    │
                                                    ▼
                                            IntactVisual.SetActive(false)
                                            ownColliders.enabled = false
                                            Instantiate(fracturedPrefab)
                                            foreach Rigidbody:
                                                AddExplosionForce(hitPoint, ..., Impulse)
                                                AddForce(dir × force × 0.4)
```

핵심은 `BreakableObject.TakeDamage` 단일 진입점입니다. Raycast 데미지, 발사체 충돌 데미지, 폭발 범위 데미지 모두 **같은 메서드 시그니처**로 받습니다.

```csharp
public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitNormal, Vector3 forceDirection)
{
    if (isBroken) return;
    int scaled = Mathf.RoundToInt(damage * GetDamageMultiplier());
    currentHealth -= scaled;
    SpawnHitFeedback(hitPoint, hitNormal);
    if (currentHealth <= 0) Break(hitPoint, forceDirection);
}
```

#### 설계 포인트

- **3단계 시각 상태**: 온전 → 균열(데칼·먼지·사운드) → 파괴(프리팹 교체). 한 단계 더 둔 덕분에 “물체가 사라지고 다른 게 생기는” 느낌이 “부서지는 과정으로 인식되는” 느낌으로 바뀝니다.
- **`DestructibleMaterialTag`** 로 머티리얼 타입(`Wood`/`Glass`/`Concrete` 등)을 구분, 같은 파괴 로직 위에 머티리얼별 데미지 배율·파편 잔여 시간·이펙트를 다르게 적용.
- **파편은 미리 자식으로 배치**한 뒤 파괴 시점에 부모를 끄고 자식 Rigidbody를 활성화. 런타임 메시 분할 비용을 회피한 의도적 단순화.

#### 트러블슈팅

| 문제 | 원인 | 해결 |
| --- | --- | --- |
| 파편이 즉시 멈춰버려 “부서진 게 아니라 사라진 느낌” | `AddExplosionForce` 의 `upwardsModifier`가 0이라 위 방향 성분이 없어 파편이 바닥에 박힘 | upwardsModifier 살짝 + Z 방향 보조 `AddForce`로 단방향 폭발 + 발사 방향 합성 |
| 같은 위치에 반복 타격해도 변화가 없음 | 데미지가 들어가도 시각 피드백이 동일 | 누적 HP 비율에 따라 균열 데칼/사운드 강도 단계화 |
| 머티리얼 타입별로 “느낌이 비슷”함 | 배율만 다르고 이펙트가 같음 | `MaterialTag`에 SFX/VFX 슬롯을 추가해 데이터로 분리 |

#### 대안과 비교

- **런타임 메시 분할(Voronoi/Boolean)**: 결과는 화려하지만 **삼각형 폭증 + 매 프레임 충돌체 재생성 비용**이 큼. 모바일·멀티플레이에서는 비현실적.
- **Cell Fracture(Blender 등) 사전 분할 + 동일 프리팹 교체**: 현재 채택한 방식. 아트가 분할 결과를 미리 검수 가능 → **아트 디렉션 통제력**이 가장 큼.
- **NVIDIA Blast / RayFire**: 풍부한 시뮬이지만 라이선스/플랫폼 제약, 그리고 “내가 만든 게 아닌 게 너무 많아짐”.

#### 게임/기획 관점

- **활용 예**: RPG의 깨지는 항아리, FPS의 유리창, 어드벤처 게임의 환경 상호작용. 데미지 진입점만 맞추면 다른 무기/스킬에도 적용하기 쉽습니다.
- **기획 메모**: “부서지는 순간”의 임팩트는 **시뮬 정밀도가 아니라 단계 분할 + 카메라 셰이크 + 사운드 큐**가 결정. 자원이 한정된 환경에서는 미리 분할된 파편 + 데칼 + 파티클 조합이 가성비 1순위.

</details>

---

### 2. 그리드 기반 화재 전파 (Fire Spread)

<details>
<summary><b>기술 상세 펼치기</b></summary>

#### 왜 이 패턴인가

“불꽃 파티클이 옆 칸을 태운다”가 아니라, **보이지 않는 셀 데이터 위에서 상태가 전이되고, 그 결과를 파티클로 보여준다**가 게임에서 실제로 쓰는 방식입니다. 이 데모는 그 분리를 의도적으로 드러냅니다.

#### 셀 상태 머신

```text
        ┌───────────┐                  ┌───────────┐
        │  Normal   │── temp 임계 초과 ──▶│ Burning  │
        └─────▲─────┘                  └─────┬─────┘
              │                              │
              │                              ▼
              │                          hp <= 0
              │                              │
              │                              ▼
              │                        ┌───────────┐
              └────────  (없음)   ─────│ BurnedOut │
                                       └───────────┘
```

매 틱(`tickInterval`)마다 **활성 셀만** 순회합니다.

```csharp
private void SimulateFireTick()
{
    pendingIgnitions.Clear();

    for (int i = activeCells.Count - 1; i >= 0; i--) {
        var cell = activeCells[i];
        if (cell.state != CellState.Burning) { activeCells.RemoveAt(i); continue; }

        cell.burnTime += tickInterval;
        cell.hp       -= burnDamagePerTick * GetBurnDamageMultiplier(cell.material);

        if (cell.hp <= 0f) { cell.state = CellState.BurnedOut; activeCells.RemoveAt(i); continue; }

        SpreadHeatFrom(cell);   // 인접 4방향 셀의 temperature 증가, 점화 확률 계산
    }

    // 틱 마지막에 한꺼번에 새 셀들을 Burning으로 (이중 카운팅 방지)
    for (int i = 0; i < pendingIgnitions.Count; i++) Ignite(...);
}
```

#### 재질별 화재 규칙 (룩업 테이블)

| 재질 | HP 배율 | 점화 확률 배율 | 열 흡수 배율 | 시각 |
| --- | --- | --- | --- | --- |
| Wood | 1.0 | 1.0 | 1.0 | 일반 흙/풀 셀 |
| Water | — | 0 (`CanBurn=false`) | 0 | 짙은 푸른 셀 |
| Stone | 2.75 | 0.015 | 0.04 | 자갈/바위 |
| Oil | 0.65 | 1.8 | 1.0 | 검은 광택 셀 |

분기 대신 **switch 룩업**으로 응답을 분리해 데이터 추가가 쉽고, 디자이너가 inspector에서 즉시 튜닝할 수 있도록 코드와 데이터 경계를 명확히 했습니다.

#### 트러블슈팅

| 문제 | 원인 | 해결 |
| --- | --- | --- |
| 큰 그리드에서 프레임 드랍 | 모든 셀을 매 프레임 검사 | **`activeCells` 큐**로 Burning 셀만 갱신 → 평균 O(burning count) |
| 같은 셀이 두 번 점화돼 카운트 폭주 | 한 틱 안에서 즉시 점화 처리 시 다음 인접 셀이 같은 틱에 또 점화 트리거 | `pendingIgnitions` 버퍼에 모아 두고 **틱 마지막에 일괄 적용** |
| 물 셀이 가끔 발화 | `CanBurn`이 누락된 분기 경로 | `CanBurn(material) == false` 가드를 모든 전파 진입 직후에 둠 |
| 불꽃 VFX가 셀마다 너무 시끄럽고 라이트 비용 큼 | 원본 `PyroParticles/SmallFires` 가 라이트와 오디오 포함 | 셀용 래퍼 프리팹(`FireCellFlame_PyroSmallFires`)을 만들어 라이트/오디오 제거, 파티클만 남김 |

#### 대안과 비교

- **셀룰러 오토마타(전수 갱신)**: 코드는 단순하지만 셀 수가 늘면 비용이 그리드 면적에 비례 → 본 데모는 활성 큐 방식으로 회피.
- **물리 기반 열 확산 PDE**: 시뮬 정확도는 높지만 게임 연출에는 과잉. “보이는 결과” 차이는 미미하고 비용만 큼.
- **순수 VFX 트리거(영역 진입 시 다음 영역 발화)**: 가장 가볍지만 “재질·습도·연쇄” 같은 상호작용을 만들기 어려움.

#### 게임/기획 관점

- **활용 예**: “창고 화재 미션”, “마법 화염 전파(기름 위에 불 = 폭발 연쇄)”, “보스의 광역 화염장판”. 셀 데이터에 buff/debuff slot을 더하면 “물 셀 위 캐릭터는 화염 데미지 30% 감소” 같은 게임 룰로 확장됩니다.
- **기획 메모**: 정밀도가 아니라 **셀 단위 “이벤트화”** 가 핵심. 셀이 BurnedOut 되는 순간 트리거를 발사하면 미션 진행/사운드 큐/카메라 워크에 그대로 연결할 수 있습니다.

</details>

---

### 3. 복셀 건물 파괴 (Voxel Destruction)

<details>
<summary><b>기술 상세 펼치기</b></summary>

#### 왜 이 패턴인가

벽돌·콘크리트 같은 **정형 건물**은 “셀 단위로 부서지는 격자”로 다루는 게 직관적입니다. 본 데모는 단일 `VoxelDestructibleBuilding` 컴포넌트가 W×H×D 그리드를 들고, 다섯 가지 머티리얼(콘크리트/벽돌/나무/지붕/유리) 을 셀별 속성으로 관리합니다.

#### 처리 단계

```text
입력: ApplyExplosion(worldPoint, radius, damage, impulse)
   │
   ├─ 1) 반경 내 모든 셀의 hp 감소 (거리 falloff × 재질 응답)
   │
   ├─ 2) hp <= 0 셀을 removedCells 로 모음
   │
   ├─ 3) 제거된 셀 자리에 “디브리(잔해)” Rigidbody 미니큐브 스폰
   │       └─ maxDebrisPerBlast 로 상한 → 폭발 1회당 비용 캡
   │
   ├─ 4) BFS로 지면(고정 노드)과 연결성 확인 → 떨어진 청크 후보
   │       ├─ 청크 크기 >= minDetachedVoxels 인 것만 분리
   │       └─ maxActiveDetachedChunks 로 분리체 총 개수 캡
   │
   ├─ 5) 분리 청크를 별도 GameObject로 떼어내고
   │       MeshFilter + MeshRenderer + BoxCollider 들로 (compound) 구성
   │       └─ Compound BoxCollider 개수도 maxCompoundColliders 로 캡
   │
   └─ 6) 본체 메시 재생성 (visible face culling)
          └─ 살아있는 인접 셀 쪽 면은 그리지 않음 → 노출된 외곽면만 메시화
```

이 단계별 캡(`maxDebrisPerBlast`, `maxActiveDetachedChunks`, `maxCompoundColliders`) 이 **시각 품질을 유지하면서 한 폭발의 비용 상한**을 정직하게 보장합니다. 게임 양산 환경에서는 “최악의 한 프레임 비용”을 예측 가능하게 만드는 게 평균 비용보다 중요합니다.

#### 트러블슈팅

| 문제 | 원인 | 해결 |
| --- | --- | --- |
| 셀 하나 깰 때마다 전체 셀을 전부 큐브로 만들면 프레임 스파이크 | naive cube-per-cell rebuild | 살아있는 인접 셀 사이의 내부 면은 만들지 않고 **노출된 면만 메시화**. 데모 규모에서는 전체 재빌드로 단순성을 유지하고, `maxDebrisPerBlast`/청크 캡으로 최악 비용을 제한 |
| 멀리 떨어진 셀에도 데미지가 균등 적용돼 폭발이 “덩어리째 사라짐” | falloff 없음 | `falloff = 1 - distance/radius` 로 거리감쇠, 재질별 `damageResponse` 곱셈 |
| 분리 청크가 너무 많이 생겨 Rigidbody 폭증 | 모든 끊긴 그룹을 다 분리 | `minDetachedVoxels` 미만은 디브리 처리, 분리체는 `maxActiveDetachedChunks` 로 큐 회전(LRU) |
| Compound Collider 개수 폭주로 물리 비용 폭증 | 청크 하나에 셀 수만큼 BoxCollider | `maxCompoundColliders` 캡 + 외곽 셀만 콜라이더 할당 |
| 유리 머티리얼이 다른 머티리얼처럼 “두껍게” 부서짐 | 동일 hp/응답 | 유리는 hp 낮고 falloff 가파르게, 디브리 잔존시간 짧게 |

#### 대안과 비교

- **사전 분할 파편(1번 방식)**: 임의 위치/임의 크기 폭발에는 부적합. 격자 방식이 더 일반적.
- **Voronoi 런타임 분할**: 비정형 건물에 강하지만 비용/안정성 부담. 정형 건물에서는 격자가 훨씬 효율적.
- **Tile-based 2.5D 파괴**: 가볍지만 3D 깊이감이 떨어짐. 본 데모는 3D 격자 + 외곽면 메시화로 가성비를 잡음.

#### 게임/기획 관점

- **활용 예**: 멀티플레이 슈터의 파괴 가능한 엄폐물, RTS의 건물 파괴, 빌딩 시뮬레이션. **셀 상태가 enum이라 네트워크 동기화 비용이 예측 가능**(변경분만 전송) 한 것이 큰 장점.
- **기획 메모**: 격자 해상도 = 게임 룰의 단위. 셀이 크면 “벽 한 칸 부수기”라는 명확한 룰, 셀이 작으면 시각은 좋지만 비용 큼. 게임 디자인이 이 단위를 먼저 결정해야 시뮬도 그에 맞춰짐.

</details>

---

### 4. 구조 응력 붕괴 (Stress Collapse)

<details>
<summary><b>기술 상세 펼치기</b></summary>

#### 왜 이 패턴인가

“HP 0이면 즉시 부서진다”는 1번 패턴은 **다층 구조물**에 어울리지 않습니다. 기둥 하나가 부러지면 위층의 하중이 옆 기둥으로 옮겨가고, 어느 순간 임계를 넘은 기둥이 도미노처럼 무너지는 흐름이 필요합니다. 이 데모는 그 “연쇄 붕괴 감”을 흉내냅니다.

#### 데이터 구조

```text
StressCollapseBuilding
  ├── layers[]   : 층(Floor). weight, strength, pillars[], minPillarsForSupport
  └── 각 layer.pillars[] : pillarRoot, share, currentLoad, pillarStrength, failed

매 시뮬 스텝:
  1) Shooter / 폭발에서 기둥에 데미지 입력 (pillar.currentLoad ↑ or 직접 failed)
  2) pillar.failed = true → 해당 기둥의 하중 share 를 인접 기둥에 재분배
  3) 재분배 결과 currentLoad > pillarStrength 인 기둥은 pendingCollapseQueue 로
  4) survivingPillars <= minPillarsForSupport 인 층은 collapsed = true
     → Rigidbody 활성, AddExplosionForce + 랜덤 토크/사이드 포스
  5) 떨어지는 층은 추가 임펄스를 아래 기둥에 전달 → 다시 (2)로
  6) cascadeDelayPerLayer 만큼 한 스텝당 처리량을 제한 → “예쁜 도미노”
```

핵심 아이디어는 **한 프레임에 모든 붕괴를 끝내지 않는 것**입니다. `pendingCollapseQueue` 와 `cascadeDelayPerLayer` 가 “단계감”을 만들어내는 본질입니다.

#### 트러블슈팅

| 문제 | 원인 | 해결 |
| --- | --- | --- |
| 기둥이 다 부러지자마자 한 프레임에 건물이 사라짐 | 즉시 처리 루프 | 코루틴 큐 + per-layer delay (`pillarFailToCollapseDelay`, `cascadeDelayPerLayer`) |
| 층이 떨어지는데 “위로 튀어 오르는” 듯한 부자연스러운 모션 | 폭발 임펄스의 `upwardModifier` 가 큼 | `upwardModifier=0.15` 정도로 줄이고 `randomSideForce`/`randomTorque` 로 옆 + 회전 성분 부여 |
| 가운데 기둥만 빠지면 V자 붕괴가 아니라 통째로 떨어짐 | 재분배 가중치가 “전 기둥 균등” | `share` 값을 위치/길이로 차등 + 인접한 기둥에만 우선 재분배 |
| HUD/디버그가 무엇을 보고 있는지 모름 | 시각화 부재 | `StressVisualizer` 가 기둥 색을 currentLoad/strength 비율로 표시, `StressCollapseHud` 가 층별 상태 실시간 출력 |
| URP 환경에서 외부 건물/VFX 머티리얼이 분홍색 | Built-in 셰이더 잔존 | `StressUrpMaterialFixer` 를 데모 부트스트랩/이펙트 생성 시 호출해 URP 파티클 셰이더로 치환 |

#### 대안과 비교

- **풀-피직스 강체 조립체(Joint Break Force)**: 시뮬은 정확하지만 **튜닝이 매우 어렵고**, 일관된 “예쁜 무너짐”을 매번 얻기 어려움. 본 데모는 “데이터 그래프 + 코루틴 큐”로 일관성을 잡음.
- **애니메이션 클립으로 무너짐 재생**: 가장 가볍지만 “플레이어가 어디 부쉈는지”에 반응하지 않음. 본 데모는 입력 위치에 따라 결과가 달라짐.
- **Finite Element 시뮬**: 학술적으로는 우아하지만 게임 프레임 예산엔 부적합.

#### 게임/기획 관점

- **활용 예**: 보스전 마지막 페이즈 “스테이지가 무너지는 연출”, “건물 폭파 미션”, 미들엔드 트레일러용 컷씬.
- **기획 메모**: “플레이어 선택에 따라 무너지는 방향이 다르다”는 감각이 핵심. 가운데 기둥을 부수면 V자, 한쪽 끝을 부수면 사선 붕괴. 단계 큐를 굳이 만든 이유는 **카메라/사운드 큐와 동기화**하기 위해서.

</details>

---

### 5. 빔 소프트바디 차량 (Beam Soft-Body Vehicle)

<details>
<summary><b>기술 상세 펼치기</b></summary>

#### 왜 이 패턴인가

“차가 진짜로 찌그러지는 느낌”은 본격 자동차 시뮬(BeamNG 식)을 안 하더라도 **노드–빔 단순화 모델**로 흉내낼 수 있습니다. 이 데모는 그 단순화 모델을 의도적으로 작게 만들어, “느낌은 얻고 비용은 통제 가능”한 지점을 찾았습니다.

#### 구성

```text
BeamSoftBodyVehicle
  ├── nodes[]   : 차체 격자 정점 (mass, anchored, wheelNode, localPosition, velocity, damage)
  ├── beams[]   : 두 노드 간 스프링 제약 (stiffness, damping, yieldStrain, plasticity, breakStrain)
  ├── panels[]  : 시각용 패널 (실제 차체 메시는 패널 단위로 노드를 따라 변형)
  └── damage    : 노드/빔별 누적 손상 (시각·HUD 게이지)
```

매 `FixedUpdate`:

```csharp
float substep = Time.fixedDeltaTime / Mathf.Max(1, solverIterations);  // 보통 3회
for (int i = 0; i < solverIterations; i++) {
    SolveBeams(substep);   // 스프링/댐핑 → 노드 velocity 갱신
    SolveAnchors(substep); // anchored 노드를 rest position 으로 복귀
    IntegrateNodes(substep); // velocity → localPosition 적용 + 드래그
}
UpdatePanels();  // 노드 평균 위치를 패널 transform/scale에 매핑
UpdateVisuals();
```

#### 핵심 트릭: yield / plastic / break의 3단 임계

```csharp
// SolveBeams 내부
beam.strain = Mathf.Abs(stretch) / beam.restLength;

if (beam.strain > beam.yieldStrain) {
    // 영구 변형: restLength 를 현재 길이 쪽으로 보간 → "찌그러진 채로 남음"
    float plasticStep = Mathf.Clamp01(beam.plasticity * dt * (beam.strain - beam.yieldStrain + 0.05f));
    beam.restLength = Mathf.Lerp(beam.restLength, currentLength, plasticStep);
    a.damage = Mathf.Clamp01(a.damage + beam.strain * 0.006f);
    b.damage = Mathf.Clamp01(b.damage + beam.strain * 0.006f);
}

if (beam.strain > beam.breakStrain) {
    beam.broken = true;  // 빔 파괴: 이 빔은 다음 스텝부터 무시
}
```

- `yieldStrain` 미만: 완전 탄성, 복귀
- `yieldStrain ~ breakStrain`: 일부가 영구 변형으로 누적 (찌그러진 채로 남음)
- `breakStrain` 초과: 빔 파괴, 더 이상 힘 전달 안 함

이 3단 임계가 “충돌 → 잠깐 흔들리고 복귀”, “계속 박으면 누적 찌그러짐”, “세게 박으면 차체 일부가 끊겨 흔들거림” 의 차이를 만듭니다.

#### 트러블슈팅

| 문제 | 원인 | 해결 |
| --- | --- | --- |
| 스프링이 발산해 차체가 폭발하듯 튕김 | 한 스텝에 너무 큰 보정량 | `solverIterations=3` + `maxNodeDisplacement` 클램프 + `nodeDrag` 도입 |
| 충돌 직후 차가 미세하게 떨림 | yield 직전 진동 누적 | anchor 노드에 `anchorDamping` 적용, velocity 에 1-frame drag 곱셈 |
| 패널 메시가 노드를 못 따라가 어색 | 패널을 단순 부모-자식 종속 시키면 회전/스케일이 박살 | `SoftPanel` 에 `compressionAxis` / `minimumAxisScale` / `followStrength` 를 두고 **노드 평균 위치 → 패널 localPosition/Scale 보간** |
| 카메라가 변형 차체를 따라가다 멀미 유발 | 노드 한 개를 cam target으로 잡음 | `BeamSoftBodyFollowCamera` 가 **노드들의 평균 위치**를 따라가 변형 영향을 흡수 |
| 휠 노드에 차체 빔이 잘못 박혀 휠이 안 굴러감 | 노드 mass 균등 | 휠 노드는 `wheelNode=true` + 별도 mass/material/색으로 분리, 드라이브 컨트롤러가 직접 토크 적용 |

#### 대안과 비교

- **WheelCollider + 강체 차량**: 안정적이지만 “찌그러짐” 없음. 데미지를 표현하려면 시각만 따로 처리해야 함.
- **풀 BeamNG 스타일 시뮬**: 매우 사실적이지만 노드 수백 개 + 솔버 수십 회 반복 → 모바일/멀티에는 불가.
- **본 데모(저밀도 노드–빔 + 패널 디포머)**: 시뮬과 시각을 분리해 시뮬은 가볍게 유지 + 시각은 패널 단위로 변형. 카크래시 “느낌”에 집중.

#### 게임/기획 관점

- **활용 예**: 아케이드 레이싱의 차체 데미지 시각화, 추격 시퀀스의 충돌 연출, 차량 격투 게임 프로토타입.
- **기획 메모**: 양산 멀티플레이에서 “정확한 차체 변형 동기화”는 비용이 폭발적임 → 본 데모의 변형은 **클라이언트 시각 효과로만 두고, 게임 룰(HP/제어성)은 별도 데이터로 분리**하는 것이 현실적인 패턴.

</details>

---

### 6. 런타임 메시 슬라이싱 (Mesh Slicing)

<details>
<summary><b>기술 상세 펼치기</b></summary>

#### 왜 이 패턴인가

검·레이저로 사물을 베어내는 연출은 미리 분할로는 만들 수 없습니다. **임의 평면을 기준으로 메시를 두 개로 나누고, 잘린 단면(cap) 폴리곤을 새로 만들어 채워야** 합니다. 이 데모는 그 절차를 직접 구현했습니다.

#### 전체 흐름

```text
입력: SlicePlane(world plane, origin, normal), 대상: SliceTarget(+ Read/Write 가능한 MeshFilter)

MeshSlicer.Slice() 내부:
  1) plane을 대상 로컬 좌표계로 변환
  2) 각 서브메시별로 삼각형 순회
  3) 삼각형의 세 정점이 평면 어느 쪽인지 부호 분류 (+, -, 양쪽 걸침)
     - 같은 쪽 3개 → 한 쪽 메시에 그대로 추가
     - 양쪽 걸침 → 평면과의 교차점 2개 계산, 1개는 alone-side(삼각형 1개), 다른 쪽은 quad(삼각형 2개)
  4) 잘린 모서리(CapEdge)를 모아 cap 폴리곤으로 fan triangulation
     - upper cap은 -normal, lower cap은 +normal 방향으로 노멀 부여 (양쪽 단면이 서로 마주봄)
     - cap UV는 평면 위 (tangent, bitangent) 직교 기저로 평면 좌표계 투영
  5) 새 GameObject 2개 spawn (upper/lower), Rigidbody + BoxCollider 부착
  6) SliceImpactFx (파편/슬래시 라인/사운드) 트리거
```

서브메시 보존도 중요한 디테일입니다.

```csharp
// MeshSlicer.cs
int submeshCount = mesh.subMeshCount;
int capSubmesh   = submeshCount;  // cap을 마지막 서브메시로 추가
...
// 머티리얼 배열도 (원본 머티리얼들 + capMaterial) 로 재구성
Material[] result = new Material[submeshCount + 1];
```

→ 원본이 다중 머티리얼이어도 각 면이 원본 머티리얼을 유지하고, 단면만 `capMaterial` 로 별도 셰이딩됩니다.

#### 트러블슈팅

| 문제 | 원인 | 해결 |
| --- | --- | --- |
| cap이 가끔 뒤집혀 보이거나 구멍이 남음 | 폐곡선을 따라가다 분기에서 방향 선택 오류 | 정점을 평면 좌표계로 투영 → 2D 폴리곤 → 일관된 와인딩으로 fan triangulation (오목 폴리곤은 살짝 겹쳐도 “임팩트 스케일에서는” 시각적으로 충분) |
| 평면 위에 정점이 정확히 있는 경우 0으로 나누는 NaN | `da/(da-db)` 에서 분모 0 | `Epsilon = 1e-5f` 가드 + `SideOf(d) = d >= -Eps ? 1 : -1` 로 평면상 정점을 한쪽으로 강제 |
| upper/lower 단면이 같은 방향을 봐서 두 조각이 “안에서 비어 보임” | 양쪽 cap이 동일 노멀 | cap winding을 반대로(`upper: A→B`, `lower: B→A`) + 노멀 부호도 반전 |
| 잘린 조각이 회전 없이 떨어져 “정적으로 나뉜 듯” | Rigidbody 토크 부재 | 절단 방향에 수직한 작은 토크 + 평면 반대 방향 임펄스 부여 |
| 다중 서브메시 모델에서 한 머티리얼이 사라짐 | 머티리얼 배열을 capMaterial 단일로 덮어씀 | `BuildMaterialArray(src, submeshCount, capMaterial)` 로 원본 머티리얼 보존 + cap만 추가 |
| 잘린 조각이 충돌하지 않음 | convex `MeshCollider`는 복잡한 절단 메시에서 cook 실패 가능 | 절단 결과물에는 `BoxCollider`를 붙여 데모 플레이 안정성을 우선 |
| 비제이드(non-manifold) 메시에서 cap이 깨짐 | 폐곡선이 닫히지 않음 | `SliceTarget`이 붙은 Read/Write 가능 메시만 대상으로 제한하고, 복잡한 외부 메시는 사전 검수 대상으로 둠 |

#### 대안과 비교

- **사전 분할(1번 방식)**: 임의 방향 절단 불가. 칼 게임에는 부적합.
- **Geometry Shader / Compute로 GPU 분할**: 매우 빠르지만 결과 메시를 다시 CPU로 가져오는 비용 + Collider 재생성을 별도로 해야 함.
- **에셋 스토어 슬라이서(EzSlice 등)**: 잘 만들어져 있지만, **단면 머티리얼·UV·서브메시 보존**을 직접 통제하고 내부 동작을 이해하기 위해 직접 작성.

#### 게임/기획 관점

- **활용 예**: 검·레이저 액션 게임, 도형 퍼즐(특정 평면으로 자르기), 처형 컷씬, 광선검 류의 보스 필살기.
- **기획 메모**: “베이는 순간”의 임팩트는 단면 셰이더 + 슬래시 라인 + 카메라 짧은 줌인 + 사운드의 합. 슬라이서 자체의 정밀도보다 **VFX 분리(`SliceImpactFx`, `SliceJuice`)** 가 체감 품질을 결정.

</details>

---

## 공통 설계 패턴

여섯 데모를 만들며 반복해서 적용한 패턴이 있습니다. 다른 프로젝트에도 그대로 옮겨갈 수 있는 “재사용 가능한 의사결정”입니다.

| 패턴 | 적용 위치 | 의도 |
| --- | --- | --- |
| **입력 / 시뮬 / 표현 분리** | 전 데모 | `Shooter`, `DriveController` = 입력만. `Building`, `Vehicle` = 시뮬만. `MeshDeformer`, `Hud`, `Visualizer` = 표현만. 입력 디바이스를 갈아도 시뮬은 그대로 동작. |
| **좁은 시뮬레이션 진입점** | Impact / Voxel / Stress / Beam / Slicing | `TakeDamage`, `ApplyExplosion`, `ApplyImpact`, `TrySlice`처럼 외부 입력을 받는 지점을 작게 유지. 입력 방식이 바뀌어도 내부 시뮬레이션은 그대로 유지. |
| **시각과 시뮬의 분리** | Voxel / Beam / Slicing | 시뮬 노드(데이터)와 보이는 메시(표현)를 분리해, 시뮬을 가볍게 유지하면서 시각을 풍부하게. |
| **단계적 변화 + 큐** | Stress / Voxel / Fire | 한 프레임에 모든 변화를 끝내지 않고 큐로 다음 프레임에 분산 → 프레임 스파이크 방지 + “예쁜 단계감” 확보. |
| **비용 상한(캡)** | Voxel | `maxDebrisPerBlast`, `maxActiveDetachedChunks`, `maxCompoundColliders` 로 한 폭발의 최악 비용을 사전 보장. |
| **머티리얼 태그 룩업 테이블** | Impact / Fire / Voxel | if/else 대신 enum + switch 테이블로 분기, 디자이너가 inspector에서 즉시 튜닝. |
| **URP 호환 보정 인프라** | Stress / Beam / VFX | `UrpMaterialFixer` 계열로 외부 에셋의 Built-in 셰이더를 URP로 치환, 분홍색 머티리얼 문제를 반복 수작업이 아닌 헬퍼 코드로 처리. |
| **에디터 자동 구성** | `Assets/Editor` | 데모 씬을 메뉴 한 번으로 재구성/리셋. 회귀 검증 사이클을 짧게. |

---

## 트러블슈팅 & 배운 점

### 1. “부서지는 느낌”은 hp 0보다 단계 변화에서 결정된다

처음에는 hp가 0이 되면 즉시 파편으로 교체하는 1단계 처리로 시작했는데, “물체가 사라지고 다른 게 나타난다”에 가까운 인상이라 임팩트가 약했습니다. **온전 → 균열/응력 누적 → 파괴 사이에 중간 단계**를 두고 시각/사운드 큐를 단계마다 분리한 뒤에야 “부서지는 과정”으로 인식됐습니다.
**배운 점**: 시뮬보다 표현 단계 분할이 체감 품질을 결정. 작은 단계 하나 더가 카메라 셰이크보다 효과 큼.

### 2. 한 프레임에 끝내지 마라 — 단계 큐가 “예쁜 무너짐”을 만든다

응력 붕괴(4번)에서 기둥을 모두 동시에 후보화하니 한 프레임에 건물이 사라졌습니다. **`pendingCollapseQueue` + `cascadeDelayPerLayer`** 로 한 스텝당 처리 개수를 제한하고, 떨어지는 층이 아래로 추가 임펄스를 주도록 하니 도미노 패턴이 살아났습니다.
**배운 점**: 시뮬 정확도보다 **단계감**이 미적 변수. 큐 기반 분산은 화재(2번), 복셀(3번)에도 동일하게 적용됨.

### 3. 빔 소프트바디는 “발산하지 않게” 만드는 게 시작점

노드-빔 시스템의 첫 빌드는 충돌 한 번에 차체가 폭발하듯 튕겼습니다. **`solverIterations` 분할 + `maxNodeDisplacement` 클램프 + `nodeDrag`** 의 조합으로 한 스텝 안의 변화를 강제 제한했고, 변형률이 일정 이상이면 빔을 끊어 “과한 힘”을 시뮬레이션 밖으로 배출해 해결했습니다.
**배운 점**: 게임 시뮬은 “정확도”보다 “안정성”이 우선. 발산하면 정확도는 의미 없음.

### 4. 슬라이싱은 단면(cap)이 전부다

평면으로 자르는 것 자체는 어렵지 않지만, 잘린 면을 메우는 cap이 뒤집히거나 구멍 나면 시각이 즉시 무너집니다. **정점을 평면 좌표계로 투영 → 2D 폴리곤 → 일관된 와인딩 fan triangulation**, 그리고 **upper/lower cap의 노멀/와인딩을 반전** 하는 두 가지로 단면 정합성을 확보했습니다. 추가로 평면상 정점이 0/0 분모를 만드는 NaN 문제는 `Epsilon` 가드로 봉쇄.
**배운 점**: “이건 잘 되겠지” 싶은 수치 코드일수록 경계 케이스가 무섭다. Epsilon 가드 + “Read/Write 가능하고 비교적 단순한 메시만 슬라이스 대상으로 둔다”는 디자이너 계약이 효과적.

### 5. 복셀 파괴의 진짜 적은 메모리/콜라이더, 아니라 “최악의 한 프레임”

평균 비용이 아니라 “큰 폭발 한 번에 들어가는 최악 비용”이 게임 양산에서는 더 중요합니다. **디브리/청크/콜라이더 개수 캡(`maxDebrisPerBlast`, `maxActiveDetachedChunks`, `maxCompoundColliders`)** 으로 한 폭발의 비용 상한을 사전 보장한 뒤에는, 성능 측정이 “평균 fps”에서 “최악 frame time”으로 명확히 옮겨갔습니다.
**배운 점**: 시뮬 시스템 설계 = 비용 상한 설계.

### 6. URP 환경에서 외부 에셋의 분홍색 머티리얼은 인프라 문제로 풀어야 한다

차량/락/VFX 팩 다수가 Built-in 파이프라인 머티리얼이라 URP에서 분홍색이 나옵니다. 매번 손으로 바꾸는 대신 **`UrpMaterialFixer`/`StressUrpMaterialFixer`** 헬퍼를 만들고, 데모 부트스트랩이나 이펙트 생성 시 호출해 외부 에셋을 URP 기준으로 맞췄습니다.
**배운 점**: 반복되는 수작업은 작은 에디터 도구 하나로 인프라화하는 게 항상 이득. 코드 5분 = 향후 5시간.

### 7. 표현은 “데이터 변화의 결과”여야 한다

화재 데모를 만들기 전에는 “파티클이 옆 칸으로 번지는” 느낌으로 짜려고 했지만, 그렇게 만들면 게임 룰과 시각이 강하게 묶여 분리가 어렵습니다. **셀 데이터(`hp`, `temperature`, `wetness`, `state`)가 먼저 바뀌고, 파티클은 그 결과를 따라간다**는 원칙으로 다시 짠 뒤에는 다른 시스템과 합성(예: “물 셀 위에서는 폭발 화재 데미지 감소”)이 자연스럽게 가능해졌습니다.
**배운 점**: 게임 시스템은 “표현 → 데이터”가 아니라 “데이터 → 표현”으로 흐를 때 확장됨.

---

## 활용 시나리오와 양산 시 주의점

각 기술을 실제 게임 시스템에 붙인다면 어디에 쓰기 좋은지, 그리고 양산 단계에서 무엇을 조심해야 하는지 정리했습니다.

| 데모 | 이상적 게임 장르 | 기획 활용 포인트 | 양산 시 주의 |
| --- | --- | --- | --- |
| 1. Impact Destruction | RPG, 어드벤처, FPS | 환경 상호작용, 보상 박스, 퍼즐 트리거 | 파편 잔존 시간/카운트 캡으로 메모리 관리 |
| 2. Fire Spread | 생존, 전략, 액션 RPG | 화재 이벤트, 기름+불 연쇄, 광역 상태이상 | 그리드 해상도 = 게임 룰 단위, 미리 결정 필요 |
| 3. Voxel Destruction | 멀티플레이 슈터, RTS, 빌딩 게임 | 파괴 가능한 엄폐물, 동적 맵 변화 | 네트워크 변경분 전송 설계가 핵심 |
| 4. Stress Collapse | 액션, 어드벤처, 시네마틱 위주 게임 | 보스전 스테이지 붕괴, 미션 클라이맥스 연출 | 카메라/사운드 큐와 동기화된 단계 큐 필수 |
| 5. Beam Soft-Body | 아케이드 레이싱, 차량 격투 | 차체 데미지 시각화, 추격 시퀀스 | 멀티 동기화는 시각만, 게임 룰은 별도 데이터로 |
| 6. Mesh Slicing | 검 액션, 광선검, 도형 퍼즐 | 처형 컷씬, 결정타 연출, 자르기 퍼즐 | Read/Write 가능한 manifold 메시만 허용하는 디자이너 계약 명문화 |

**공통 원칙**: 게임 클라이언트 개발은 “시뮬레이션 정확도 ↔ 비용 ↔ 체감 품질”의 삼각형 위에서 항상 절충을 합니다. 본 프로젝트의 6개 데모는 각자 그 절충점이 어디에 있는지를 다르게 보여주는 샘플입니다.

---

## 프로젝트 구조

```text
3d_sample/
├── Assets/
│   ├── Scripts/
│   │   ├── Destruction/              # ✦ 모델 교체형 파괴 + 화재 전파
│   │   │   ├── BreakableObject.cs               (HP/머티리얼 태그/파편 스왑)
│   │   │   ├── DemoImpactShooter.cs             (Raycast/projectile 데미지 입력)
│   │   │   ├── DestructibleMaterialTag.cs       (재질 태그 enum)
│   │   │   ├── DestructibleMaterialType.cs
│   │   │   ├── ImpactProjectile.cs              (물리 발사체)
│   │   │   ├── FireGridSimulator.cs             (셀 상태머신 + 활성셀 큐)
│   │   │   └── FireCellHitbox.cs                (셀 → 다른 시뮬과의 충돌 브릿지)
│   │   │
│   │   ├── VoxelDestruction/         # ✦ 복셀 건물 파괴
│   │   │   ├── VoxelDestructibleBuilding.cs     (그리드 + 면 메시화 + BFS 분리)
│   │   │   └── VoxelDemoShooter.cs
│   │   │
│   │   ├── StressCollapse/           # ✦ 구조 응력 붕괴
│   │   │   ├── StressCollapseBuilding.cs        (하중 그래프 + 단계 큐)
│   │   │   ├── StressCollapseShooter.cs
│   │   │   ├── StressDamageReceiver.cs
│   │   │   ├── StressVisualizer.cs              (디버그 하중 색상)
│   │   │   ├── StressOrbitCamera.cs
│   │   │   ├── StressCameraShake.cs
│   │   │   ├── StressCollapseHud.cs             (층/기둥 상태 HUD)
│   │   │   ├── StressFxRuntime.cs               (런타임 이펙트 바인딩)
│   │   │   ├── StressEffectsBinder.cs
│   │   │   ├── StressDemoBootstrapper.cs
│   │   │   └── StressUrpMaterialFixer.cs        (URP 머티리얼 일괄 보정)
│   │   │
│   │   └── SoftBodyVehicle/          # ✦ 빔 소프트바디 차량
│   │       ├── BeamSoftBodyVehicle.cs           (노드/빔/패널 + 솔버)
│   │       ├── BeamSoftBodyDriveController.cs
│   │       ├── BeamSoftBodyMeshDeformer.cs      (노드 → 차체 메시 매핑)
│   │       ├── BeamSoftBodyCrashHandler.cs      (충돌 임펄스 분배)
│   │       ├── BeamSoftBodyFollowCamera.cs      (노드 평균 위치 추적)
│   │       ├── BeamSoftBodyHud.cs
│   │       └── UrpMaterialFixer.cs
│   │
│   ├── Slicing/                      # ✦ 런타임 메시 슬라이싱
│   │   ├── Runtime/
│   │   │   ├── MeshSlicer.cs                    (평면 분할 + 서브메시 보존)
│   │   │   ├── CapBuilder.cs                    (단면 cap fan triangulation)
│   │   │   ├── SliceMeshBuilder.cs              (정점/인덱스 누적 빌더)
│   │   │   ├── SliceVertex.cs / SlicePlane.cs / SliceResult.cs / CapEdge
│   │   │   ├── SliceTarget.cs                   (슬라이스 대상 마커 + Rigidbody 셋업)
│   │   │   ├── SliceController.cs               (입력 → 평면 → 슬라이서 호출)
│   │   │   ├── SliceImpactFx.cs / SliceJuice.cs (파편/슬래시 라인/짧은 줌)
│   │   │   ├── SliceArenaBootstrap.cs
│   │   │   ├── SliceCanvasUI.cs / SliceShaders.cs
│   │   │   ├── CapUnlit.shader
│   │   │   └── Slicing.asmdef
│   │   ├── Editor/                              (슬라이스 가이드/디버그 도구)
│   │   └── SlicingDemo.unity
│   │
│   ├── Editor/                       # ✦ 데모 씬 자동 구성 메뉴
│   ├── Scenes/
│   │   ├── DestructionDemo.unity
│   │   ├── FireSpreadDemo.unity
│   │   ├── VoxelDestructionDemo.unity
│   │   ├── StressCollapseDemo.unity
│   │   ├── BeamSoftBodyVehicleDemo.unity
│   │   └── SampleScene.unity
│   ├── DestructionDemo/             # 데모용 머티리얼/래퍼 프리팹
│   ├── FireSpreadDemo/
│   ├── VoxelDestructionDemo/
│   ├── StressCollapseDemo/
│   ├── SoftBodyVehicleDemo/
│   └── UniversalRenderPipelineGlobalSettings.asset
├── Packages/manifest.json            # URP, TMP, ShaderGraph, Burst/Mathematics(향후 최적화 후보)
├── ProjectSettings/ProjectVersion.txt # 2022.3.62f3
├── docs/
│   └── asset-policy.md               # 외부 에셋 제외 기준
├── LICENSE
└── README.md                         # ← 이 문서
```

외부 VFX 팩, 모델 팩, 대용량 텍스처, 로컬 캡처, 작업 메모는 `.gitignore`로 제외합니다. 자세한 기준은 `docs/asset-policy.md`에 정리했습니다.

---

## 실행 방법

### 사전 요구사항

- Unity Hub
- Unity **2022.3.62f3 (LTS)**
- (선택) Asset Store 원본 에셋 — 일부 데모 프리팹/씬의 시각 리소스 참조를 복구하려면 로컬에서 다시 import 해야 할 수 있습니다.

### 절차

1. Unity Hub에서 `Add project from disk` → `3d_sample/` 폴더 선택.
2. 동일 버전 에디터가 없으면 Unity Hub가 자동 설치를 제안합니다.
3. 에디터가 열리면 `Assets/Scenes/`(슬라이싱은 `Assets/Slicing/`) 에서 보고 싶은 데모 씬을 더블 클릭.
4. Play 버튼으로 실행.

### 데모별 조작 (기본값)

| 데모 | 주요 조작 |
| --- | --- |
| Impact Destruction | `Space` / 좌클릭 — 발사 |
| Fire Spread | 좌클릭 — 발화 트리거, `R` — 그리드 리셋 |
| Voxel Destruction | 좌클릭 — 데미지 입력, 우클릭 드래그 — 카메라 |
| Stress Collapse | 좌클릭 — 기둥 데미지, 마우스 — 오빗 카메라 |
| Beam Soft-Body Vehicle | `WASD` — 주행, `Space` — 브레이크, 마우스 — 카메라 |
| Mesh Slicing | 좌클릭 / 드래그 — 절단 평면 입력 |

> 입력 키는 씬마다 조금씩 다를 수 있으니, Play 직후 HUD/Inspector 에서 한 번 확인하는 것을 권장합니다.

---

## 핵심 요약

- Unity URP 위에서 **파괴·붕괴·슬라이싱 6종**을 외부 destruction 라이브러리 없이 직접 구현.
- 공통 원칙: **입력/시뮬/표현 3계층 분리**, **좁은 시뮬레이션 진입점**, **단계 큐로 프레임 스파이크 회피**, **비용 상한(캡) 설계**.
- 6개 데모 모두 “정확도보다 체감”을 기준으로 절충 — **데이터가 먼저 변하고 표현이 그 결과를 보여주는 흐름**.
- 개별 시스템마다 발생한 실제 트러블(빔 발산, cap 뒤집힘, 폭발 카운트 폭주, URP 머티리얼 깨짐) 을 **코드/데이터/인프라 세 층 모두**에서 해결.
- 다음 단계: 각 시스템을 **네트워크 동기화 가능한 형태**로 옮기는 것 — 셀 enum 변경분 전송, 클라이언트 시각 효과로의 분리.

---

## 알려진 한계와 에셋 정책

- Asset Store에서 받은 외부 패키지와 상용/무료 에셋 원본은 라이선스·용량 문제로 Git 저장소에 포함하지 않습니다.
- 일부 씬/프리팹은 로컬 외부 에셋 GUID를 참조하므로, 새로 클론한 환경에서는 모델·VFX·사운드 참조가 끊겨 보일 수 있습니다.
- 네트워크 동기화 코드는 포함되어 있지 않습니다. **단일 클라이언트 시뮬레이션**만 다룹니다.
- 빔 소프트바디는 실제 자동차 시뮬레이션을 의도하지 않은 단순화 모델입니다. 충돌·변형의 *느낌*에 초점을 맞췄습니다.
- 복셀/응력 시스템은 대규모 건물(수만 셀)을 가정하지 않습니다. 데모 규모에서 안정적으로 동작하도록 튜닝되어 있습니다.
- 메시 슬라이싱은 `SliceTarget`이 붙어 있고 Read/Write가 가능한 비교적 단순한 manifold 메시를 가정합니다. 임의의 외부 메시에 그대로 적용하면 cap 이 깨질 수 있습니다.
- URP 호환 보정은 외부 에셋에서 가장 빈번한 경우만 처리합니다. 일부 머티리얼은 수동 교체가 필요할 수 있습니다.

---

<div align="center">

**3D Destruction Sandbox** · Unity Side Project · 게임 클라이언트 관점에서의 파괴·붕괴·슬라이싱 학습

</div>

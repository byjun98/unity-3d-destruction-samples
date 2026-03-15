# Unity 3D Destruction Samples

Unity에서 파괴, 화재 전파, 복셀 붕괴, 소프트바디 차량 충돌, 메시 슬라이싱을 실험한 샘플 프로젝트입니다. 실제 상용 게임에서 쓰는 것처럼 복잡한 물리 계산을 전부 실시간으로 처리하기보다, 데이터 모델과 프리팹 교체, 파티클, 머티리얼 피드백을 조합해 결과가 자연스럽게 보이도록 구성했습니다.

## 샘플 구성

| 샘플 | 주요 씬 | 핵심 구현 |
| --- | --- | --- |
| 모델 교체형 파괴 | `Assets/Scenes/DestructionDemo.unity` | 정상 모델을 숨기고 미리 분리된 파편 프리팹으로 교체 |
| 그리드 기반 화재 전파 | `Assets/Scenes/FireSpreadDemo.unity` | 셀별 온도, HP, 습도, 재질 상태를 기반으로 연소 전파 |
| 복셀 건물 파괴 | `Assets/Scenes/VoxelDestructionDemo.unity` | 충격 지점을 기준으로 건물 조각을 분리하고 낙하 처리 |
| 스트레스 붕괴 | `Assets/Scenes/StressCollapseDemo.unity` | 누적 데미지와 구조 하중을 시각화하고 붕괴 연출 |
| 소프트바디 차량 | `Assets/Scenes/BeamSoftBodyVehicleDemo.unity` | 빔/노드 기반 차체 변형과 충돌 피드백 |
| 메시 슬라이싱 | `Assets/Slicing/SlicingDemo.unity` | 절단 평면 기준 메시 분리, 캡 생성, 절단 이펙트 |

## 에셋 정책

Asset Store에서 받은 외부 패키지와 상용/무료 에셋 원본은 라이선스와 용량 문제로 저장소에서 제외했습니다. 그래서 일부 씬이나 프리팹은 원본 에셋 참조가 끊겨 있을 수 있습니다.

이 저장소는 구현 코드, Unity 씬 구성, 직접 만든 래퍼 프리팹/머티리얼을 검토하기 위한 포트폴리오용 저장소입니다. 실행 화면은 별도 스크린샷과 설명으로 정리합니다.

## 주요 코드 위치

| 폴더 | 내용 |
| --- | --- |
| `Assets/Scripts/Destruction` | 파괴 데모와 화재 전파 공통 로직 |
| `Assets/Scripts/VoxelDestruction` | 복셀 건물 파괴 로직 |
| `Assets/Scripts/StressCollapse` | 구조물 스트레스 누적, 붕괴, HUD, 카메라, FX |
| `Assets/Scripts/SoftBodyVehicle` | 차량 빔 구조, 메시 변형, 충돌 처리 |
| `Assets/Slicing` | 런타임 메시 슬라이싱과 에디터 도구 |
| `Assets/Editor` | 데모 씬 자동 구성 메뉴 |

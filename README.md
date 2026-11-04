# api.buddham.co.kr


## 스키마 뼈대


```text
Canon (판본계통 : 팔리 / 칸큐르 / 고려대장경 ...)
  Traditon • School (전승 계통, 미확정 허용)
  Pitaka • Division (경장 / 율장 / 논장 / 대승 각 부 ) <- 유연한 층위
    Collection (Nikaya / Agama, Nikāya / Āgama)
      Group (풉 • 상윳따 • nipata nipāta, 가변 깊이의 자기 참조 트리)
        Sutra (경 = 추상적 작품 단위, 식별자 다수)
          Edition / Witness (언어 • 판본별 텍스트)
            Segment (문단/게송/행, 주소 가능한 최소 단위)

옆으로 : Parallel (병행경 관계) • Translation • Commentary • Speaker
위로 얹는 층: Dhamma (교리 주제 태그 -> Segment) • Lekhana(사경 세션 -> Segment 참조)

핵심은 Sutra(추상작품) 와 Edition(실제 원문 텍스트)을 분리하는 것
같은 경이 팔리 • 한문 • 한글로 존재하고 병행경과 연결되어도 구조가 깨지지 않음
사경은 원문을 복사하지 않고 Segment 를 참조 하게 하면 원문이 하나로 유지 됨
```

# Entity

- EntityBase 상속 : long Id 통일

- CanonHierarchy : 계층
  1. CanonTradition
  2. Canon
  3. CanonDivision
  4. CanonCollection
  5. CanonGroup
  (예) 아함은  CanonCollection 의 Kind = Agama 이고, 니까야는 Kind = Nikaya

- CanonText : 작품 원문, 텍스트
  1. CanonWork
  2. CanonWorkIdentifier
  3. CanonSpeaker
  4. CanonSource

- DhammaLekhan : 교리 • 사경
  1. DhammaTopic
  2. CanonSegmentTopic
  3. LekhanaSession
  4. LekhanaEntry

- ScriptureEnums : 열거형

## [client](https://buddham.co.kr)

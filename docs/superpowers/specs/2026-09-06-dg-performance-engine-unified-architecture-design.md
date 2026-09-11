# DG Performance Engine — Arquitetura Unificada e Especificação Canônica

**Data:** 2026-09-06  
**Status:** arquitetura consolidada para revisão  
**Produto oficial:** **DG Performance Engine**  
**Origem:** evolução direta do FF Performance Engine  
**Repositório atual:** `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`  
**Branch canônico de desenvolvimento:** `build/initial-product`

---

## 0. Propósito deste documento

Este documento une, em uma única especificação, três camadas que até aqui existiam em momentos diferentes do projeto:

1. a arquitetura original do **FF Performance Engine**, concebida como otimizador adaptativo Windows focado em Free Fire / Free Fire MAX sobre BlueStacks;
2. o que já foi efetivamente implementado e validado no repositório atual;
3. a expansão aprovada para o **DG Performance Engine**, que transforma essa base em um otimizador universal do PC e de jogos.

A regra central é **evolução sem recomeço**. Nenhum subsistema válido do FF Performance Engine deve ser descartado apenas porque o produto cresceu. O DG Performance Engine absorve a arquitetura anterior, generaliza seus contratos e cria novos motores ao redor dela.

Para evitar ambiguidade, cada grande bloco deste documento recebe uma classificação:

- **PRESERVADO** — já fazia parte da arquitetura original e continua válido;
- **EXPANDIDO** — já existia, mas passa a atender um escopo maior;
- **NOVO** — criado na expansão DG;
- **IMPLEMENTADO** — existe no branch atual e já possui implementação real;
- **VALIDADO** — além de implementado, possui checkpoint/CI conhecido como verde;
- **PLANEJADO** — arquitetura aprovada, porém ainda não implementada integralmente.

Este documento não trata exemplos visuais ou números de FPS como promessas. Valores de UI usados em mockups são ilustrativos. A lógica de produto só deve afirmar ganho quando houver evidência medida.

---

# 1. Visão do produto

## 1.1 Definição

O **DG Performance Engine** é um aplicativo nativo para Windows que analisa o PC real, identifica gargalos, cria uma baseline, gera e aplica otimizações reversíveis, mede o resultado e aprende quais configurações funcionam melhor para aquela combinação de máquina, sistema, jogo e objetivo.

O produto não deve funcionar como um catálogo de “tweaks gamer” universais. Sua unidade de decisão é evidência contextual:

```text
MÁQUINA + WINDOWS + DRIVER + JOGO + MODO + ESTADO + CONFIGURAÇÃO + EVIDÊNCIA
                                      ↓
                            MELHOR DECISÃO CONHECIDA
```

## 1.2 Filosofia técnica

A filosofia original do FF Performance Engine permanece como regra de engenharia do DG:

> **Observar mais do que alterar. Medir tudo que alterar. Desfazer tudo que não melhorar.**

Consequências práticas:

- nenhuma mudança é considerada boa só porque é popular;
- o sistema precisa saber qual estado existia antes da mudança;
- alterações relevantes devem ter rollback;
- benchmarks contaminados não podem virar evidência válida;
- uma medição isolada e ruidosa não deve promover um vencedor;
- o sistema deve conhecer a qualidade de seus próprios dados;
- quando não houver confiança suficiente, deve declarar incerteza em vez de fabricar causalidade;
- o programa deve medir seu próprio overhead.

## 1.3 Evolução do escopo

### FF Performance Engine original — PRESERVADO

O produto original já nasceu universal entre diferentes PCs Windows, mas seu workload principal era:

```text
Windows → BlueStacks → Free Fire / Free Fire MAX
```

Ele já previa:

- Hardware/System Scan;
- análise de Windows;
- diagnóstico de gargalo;
- benchmark híbrido;
- Auto Tuner Adaptativo/Profundo;
- cinco perfis vencedores;
- Telemetry Engine único;
- Guardian adaptativo;
- Performance e History;
- snapshots e rollback;
- Mini Mode;
- interface Clean/Dark Liquid Glass;
- arquitetura C#/.NET + C++ nativo.

### DG Performance Engine — EXPANDIDO

A nova fronteira é:

```text
Windows inteiro
+ hardware completo
+ qualquer jogo/workload suportado
+ launchers e emuladores
+ otimização permanente do PC
+ otimização temporária por sessão
+ limpeza profunda
+ aprendizado contínuo
+ tuning avançado CPU/GPU
```

Free Fire / BlueStacks deixa de ser “o produto inteiro” e passa a ser o **primeiro Game Adapter especializado e já funcional**.

---

# 2. Estado real do projeto no momento desta especificação

## 2.1 Repositório e checkpoint

O branch canônico continua sendo:

```text
build/initial-product
```

No momento desta consolidação, o head verificado é:

```text
b99ab0c92a76b1b69b7bb1673237e2522996e40c
feat: add automated A/B round controls to profiles
```

O Windows CI correspondente (`run #378`, id `34037208934`) concluiu com sucesso nesse SHA.

## 2.2 Estrutura implementada — IMPLEMENTADO / VALIDADO

A árvore atual preserva três projetos principais:

```text
src/
├── FFPerformanceEngine.App
├── FFPerformanceEngine.Core
└── FFPerformanceEngine.Native
```

O nome físico `FFPerformanceEngine.*` é legado funcional e **não deve ser massivamente renomeado agora**. A migração de identidade para `DGPerformanceEngine.*` será gradual.

## 2.3 Componentes reais já existentes

A base atual inclui, entre outros:

- aplicação Windows C#/.NET 8 + WPF;
- núcleo C++20/Win32;
- interop gerenciado/nativo;
- detecção e configuração de BlueStacks;
- automação de sessão BlueStacks/Free Fire;
- PresentMon e telemetria de frame;
- Auto Tuner e coordenação de runs;
- Guardian Supervisor e sessão contínua;
- Game State Detector;
- History e snapshots;
- Profiles e competição de perfis;
- Performance A/B real;
- configuração exata vinculada à evidência;
- fingerprint de ambiente;
- origem de Custom Validated a partir de evidência;
- desafio automático A/B entre perfil custom e incumbente;
- UI de rodada automática A/B em Profiles.

## 2.4 A/B e evidência — IMPLEMENTADO / VALIDADO

A estrutura atual inclui contratos equivalentes a:

```text
PerformanceEvidenceSnapshot
PerformanceABComparison
PerformanceComparisonSession
PerformanceProfileEvidenceBridge
```

Regras já estabelecidas:

- A e B são snapshots congelados, não “baseline vs valor atual” móvel;
- agregados são recalculados dos pontos realmente copiados;
- metadados falsos não podem fabricar FPS/frame-time;
- qualidade pode ser `Measured`, `Partial` ou indisponível;
- intervalos e contagem de evidência são preservados;
- deltas são explicitamente `B - A`;
- evidência histórica antiga pode ser visualizada sem automaticamente adquirir novos privilégios.

## 2.5 Origem e competição de perfis — IMPLEMENTADO / VALIDADO

A evidência pode carregar configuração exata, incluindo no adapter BlueStacks:

- instância;
- CPU cores;
- RAM;
- renderer;
- FPS target;
- resolução;
- DPI.

O fingerprint de ambiente inclui sinais estruturais da máquina e do software. O fluxo histórico segue:

```text
Observed → PendingValidation → Validated
```

Um `Custom Validated` pode desafiar papéis como:

- Recomendado;
- Máximo FPS;
- Menor Latência;
- Estabilidade;
- Qualidade.

A competição exige evidência A/B compatível, freshness e ausência de drift estrutural inadequado.

## 2.6 Rodada física A/B — IMPLEMENTADO / VALIDADO

O `ProfileChallengeRoundService` executa o ciclo controlado:

```text
validar
→ aplicar incumbente A
→ preparar jogo
→ medir A
→ limpar sessão A
→ aplicar candidato B
→ preparar jogo
→ medir B
→ limpar sessão B
→ restaurar baseline
→ salvar evidência Observed
```

Ele não deve matar uma instância BlueStacks que não pertença à sessão controlada.

## 2.7 Hardening de concorrência de benchmark — PLANEJADO E APROVADO

Há um problema arquitetural conhecido: o Auto Tuner e o Profile Challenge possuíam gates separados, portanto poderiam, em tese, disputar CPU/GPU/PresentMon e contaminar resultados.

A solução aprovada é um **Global Controlled Benchmark Lease**, compartilhado por todos os fluxos de medição controlada.

Regras:

- lease global, não por instância;
- um benchmark controlado por vez;
- suspender Guardian ativo quando necessário;
- executar medição;
- limpar/restaurar estado;
- reconciliar e restaurar Guardian;
- liberar lease em sucesso, erro, cancelamento e rollback.

Esse bloco continua obrigatório e deve ser integrado à arquitetura DG, não implementado como remendo apenas de UI.

---

# 3. Arquitetura unificada de alto nível

## 3.1 Visão estrutural

```text
DG PERFORMANCE ENGINE
│
├── App / Presentation Layer
│   ├── Main UI
│   ├── Mini Mode
│   └── Tray
│
├── Core Orchestration Layer
│   ├── Session Coordinator
│   ├── Optimization Coordinator
│   ├── Benchmark Lease Coordinator
│   ├── Profile Engine
│   ├── Policy Engine
│   └── History / Recovery Coordinator
│
├── Diagnostic & Capability Layer
│   ├── Environment Discovery
│   ├── Hardware Discovery
│   ├── Capability Model
│   ├── Bottleneck Analyzer
│   └── Recommendation Engine
│
├── Measurement & Evidence Layer
│   ├── Telemetry Engine
│   ├── Benchmark Engine
│   ├── Evidence / A-B Engine
│   ├── Data Quality
│   └── Confidence / Correlation
│
├── Optimization Engines
│   ├── System Optimizer
│   ├── Hardware Performance Engine
│   ├── Game Optimizer
│   ├── Deep Cleaner
│   ├── Universal Auto Tuner
│   └── Adaptive Guardian
│
├── Workload Layer
│   ├── Game Discovery Engine
│   ├── Generic Game Adapter
│   └── Specialized Game Adapters
│       └── Free Fire / BlueStacks
│
├── Safety & Recovery Layer
│   ├── Snapshot Engine
│   ├── Transaction / Rollback
│   ├── Instability Monitor
│   ├── Optional Safety Envelope
│   └── Last Known Good
│
└── Native / Platform Layer
    ├── Win32
    ├── process / priority / affinity
    ├── high-resolution timing
    ├── telemetry collectors
    ├── hardware/vendor adapters
    └── low-overhead native services
```

## 3.2 Regra de dependência

A UI nunca deve ser a dona da lógica de otimização. Main UI, Mini Mode e Tray observam o mesmo estado compartilhado e enviam intenções para o Core.

```text
Main UI ─┐
Mini UI ─┼─→ Shared Core State → Engines
Tray UI ─┘
```

O Mini Mode não implementa algoritmo pesado; solicita ações como:

```text
QuickBoostRequested
MidGameOptimizationRequested
ProfileSwitchRequested
GuardianPolicyChanged
```

O Core decide se a ação é válida, segura, compatível com o estado atual e mensurável.

---

# 4. Plataforma técnica

## 4.1 C#/.NET — PRESERVADO

Responsabilidades principais:

- WPF e presentation layer;
- orquestração;
- serviços de aplicação;
- profiles;
- history;
- Auto Tuner de alto nível;
- coordenação de sessões;
- persistência local;
- regras e políticas;
- integração de alto nível com Windows;
- adapters que não necessitam native hot path.

## 4.2 C++20 / Win32 — PRESERVADO E EXPANDIDO

Responsabilidades:

- telemetria de baixa latência;
- timers de alta precisão;
- controle de processos;
- afinidade e prioridade;
- sensores/hardware quando o native path for superior;
- vendor-specific hardware adapters quando necessário;
- operações cuja frequência torne overhead gerenciado relevante.

## 4.3 Interop

A fronteira gerenciado/nativo deve continuar estreita e contratual.

Princípios:

- modelos pequenos e explícitos;
- evitar acoplamento da UI ao native;
- falhas nativas retornam estados diagnósticos claros;
- o Core pode substituir uma implementação nativa sem obrigar telas a conhecer detalhes internos;
- capacidades indisponíveis devem aparecer como `Unsupported/Unavailable`, não como valores inventados.

---

# 5. Diagnostic & Capability Engine — EXPANDIDO

O DG não cria um segundo sistema de diagnóstico. Ele generaliza o `EnvironmentProbe`, Hardware/System Analysis, telemetria e Bottleneck Analysis do FF Performance Engine.

## 5.1 Environment Discovery

Deve identificar, quando disponível:

- Windows edition/version/build;
- arquitetura;
- modo de energia;
- drivers relevantes;
- monitor(es), resolução e refresh rate;
- processos e serviços relevantes;
- tarefas e apps de inicialização;
- virtualização;
- launchers;
- jogos e emuladores;
- estado de componentes internos do DG.

## 5.2 Hardware Discovery

### CPU

- modelo, vendor, geração quando inferível;
- physical/logical cores;
- topology;
- clocks e boost observáveis;
- utilização total e por core/thread;
- power/limits quando expostos;
- temperatura;
- sinais de throttling.

### GPU

- vendor e modelo;
- VRAM;
- clocks;
- utilização;
- power;
- temperatura;
- engines relevantes quando disponíveis;
- limites e capacidades expostas pelo driver.

### RAM

- capacidade;
- uso;
- disponibilidade;
- commit;
- pressure;
- participação de processos relevantes.

### Storage

- tipo de dispositivo quando detectável;
- espaço livre;
- latência e fila quando medíveis;
- atividade concorrente;
- localização de jogos, caches e dados temporários.

## 5.3 Capability Model — NOVO

O motor deve registrar o que aquela máquina **pode** ou **não pode** fazer.

Exemplos:

```text
SupportsGpuPowerLimit = true/false
SupportsGpuClockOffset = true/false
SupportsThermalTelemetry = true/false
SupportsPerProcessGpuPreference = true/false
SupportsGameConfigWrite = adapter-dependent
SupportsServiceTemporaryControl = permission-dependent
```

A UI Expert e os motores automáticos usam a mesma fonte de capacidade.

## 5.4 Bottleneck Analyzer — EXPANDIDO

Canais principais:

- CPU / single-thread;
- GPU;
- VRAM;
- RAM;
- I/O;
- thermal;
- power;
- background contention;
- frame pacing;
- renderer/engine limit;
- network, quando o sintoma for rede;
- unknown.

O sistema deve diferenciar correlação e causalidade. “Forte correlação com saturação de CPU” é válido; “a CPU causou” só quando houver evidência suficiente.

## 5.5 Full PC Scan

O “Full PC Scan” é a experiência de produto sobre esse engine, não um motor paralelo.

Fluxo recomendado:

```text
Primeira execução
→ Hardware Discovery
→ System Discovery
→ Capability Model
→ Telemetry bootstrap
→ Bottleneck analysis
→ Potential optimization map
→ Game Discovery
→ Recommended initial configuration
```

Uma nota única geral do PC **não é requisito canônico**. Scores por domínio podem existir, mas decisões internas devem usar métricas reais.

---

# 6. Telemetry Engine — PRESERVADO E EXPANDIDO

## 6.1 Fonte única de verdade

```text
                    TELEMETRY ENGINE
                         │
         ┌───────────────┼────────────────┐
         │               │                │
   Performance        Guardian        Auto Tuner
         │               │                │
         └───────────────┼────────────────┘
                         │
                      Profiles
```

Não devem existir três calculadores de FPS independentes.

## 6.2 Métricas

### Frame

- FPS average/median/min/max;
- 1% Low;
- 0.1% Low;
- frame time;
- P95/P99;
- variance;
- stutter classes;
- target retention.

### Sistema

- CPU total e por core/thread;
- GPU;
- RAM;
- VRAM;
- clocks;
- process impact;
- I/O.

### Thermals

- CPU/GPU temperature;
- thermal headroom;
- sustained performance;
- confirmed throttling;
- probable thermal degradation.

### Latency e rede

- frame/display latency quando mensurável;
- input pipeline estimate quando tecnicamente confiável;
- ping;
- jitter;
- packet loss;
- background traffic.

Métricas de naturezas incompatíveis não devem ser somadas para fabricar uma “latência total”.

## 6.3 Frequências adaptativas

```text
Frame metrics      → alta frequência
CPU/GPU            → média frequência
Thermals           → baixa frequência
Static hardware    → discovery/event-driven
```

## 6.4 Pipeline de armazenamento

```text
Collectors
→ realtime ring buffers
→ aggregator
→ 1 s aggregates
→ 10 s aggregates
→ session store
→ long-term summaries
```

A coleta não grava toda amostra diretamente em disco.

## 6.5 Data Quality

Toda evidência relevante deve carregar qualidade/coverage. Sensor ausente ou intervalo incompleto reduz confiança em vez de gerar defaults fictícios.

---

# 7. Evidence / A-B Engine — PRESERVADO E CENTRAL

O A/B deixa de ser uma funcionalidade de tela e vira a infraestrutura experimental comum do produto.

## 7.1 Modelo

```text
Baseline A
vs
Candidate B
→ evidence snapshots
→ compatibility checks
→ data quality
→ metric deltas
→ confidence
→ decision
```

## 7.2 Evidência contextual

Toda evidência que possa promover perfil deve estar associada a um snapshot de configuração e ambiente suficiente para explicar o que produziu o resultado.

A arquitetura deve evoluir de um fingerprint BlueStacks específico para um fingerprint universal com dimensões como:

```text
Machine
Windows build
CPU
GPU + driver
RAM
Display
Game / workload
Game version when available
Launcher/emulator/adapter version
Relevant profile configuration
Thermal/power context when material
```

## 7.3 Compatibilidade e freshness

Mudança estrutural pode:

- invalidar comparação;
- reduzir confiança;
- exigir revalidação;
- manter evidência apenas para visualização histórica.

O sistema não apaga conhecimento antigo, mas não o promove silenciosamente para um ambiente novo.

---

# 8. Controlled Benchmark Coordinator / Global Lease — NOVO HARDENING

Todos os benchmarks controlados usam um coordenador comum.

```text
Acquire Global Benchmark Lease
        ↓
Freeze/suspend conflicting adaptive behavior
        ↓
Snapshot
        ↓
Prepare workload
        ↓
Stabilize
        ↓
Measure
        ↓
Validate evidence
        ↓
Cleanup / restore
        ↓
Reconcile Guardian
        ↓
Release lease
```

## 8.1 Motivo

Benchmarks em paralelo contaminam:

- CPU;
- GPU;
- clocks/power;
- temperatures;
- PresentMon;
- background workload;
- session state.

Por isso o lease é **global**.

## 8.2 Requisitos de robustez

Release obrigatório em:

- sucesso;
- erro;
- cancelamento;
- exception;
- rollback;
- encerramento inesperado recuperável.

O estado do Guardian anterior ao benchmark deve ser reconciliado ao final, não simplesmente ligado/desligado cegamente.

---

# 9. Universal Auto Tuner — PRESERVADO E EXPANDIDO

## 9.1 Objetivo

Encontrar uma fronteira de vencedores, não um único preset.

Papéis:

- Recommended;
- Maximum FPS;
- Lowest Latency;
- Stability;
- Quality;
- Custom Validated.

Uma mesma configuração pode vencer múltiplos papéis.

## 9.2 Modos de busca

### Adaptativo

- começa curto;
- elimina regiões claramente ruins;
- aprofunda só onde houver ganho potencial;
- repete resultados incertos;
- encerra quando confiança/convergência forem suficientes.

### Profundo

- explora mais combinações;
- repete near-ties;
- exige maior confiança;
- estabiliza thermal quando necessário;
- favorece validação real;
- pode demorar mais por design.

## 9.3 Benchmark híbrido

```text
1. Hardware/system analysis
2. Baseline
3. Synthetic triage
4. Workload-specific candidate testing
5. Guided/automatic real workload validation
6. Real-session validation
7. Winner classification
```

No Free Fire adapter, as etapas workload-specific continuam BlueStacks/FF. Em outros jogos, o adapter substitui essa parte.

## 9.4 Espaço de otimização expandido

O Auto Tuner DG pode explorar, conforme capability/policy:

- System Optimizer settings;
- CPU policy;
- GPU policy;
- priority/affinity;
- memory/I/O;
- game settings;
- renderer;
- resolution;
- render scale;
- FPS target/cap;
- quality options;
- launch parameters suportados;
- session background policy;
- vendor hardware parameters suportados.

## 9.5 Resolução, render scale e qualidade — EXPANDIDO

O motor está autorizado a alterar automaticamente esses parâmetros para objetivos de desempenho.

Dois princípios coexistem:

- **Recommended** busca o melhor equilíbrio para aquela máquina/jogo;
- **Maximum FPS** pode ser agressivo e reduzir qualidade/resolução/render scale para perseguir FPS máximo.

O usuário pode definir e editar manualmente limites e preferências no Expert/Game Profile.

---

# 10. Profile Engine — PRESERVADO E EXPANDIDO

## 10.1 Hierarquia DG

```text
MACHINE
│
├── GLOBAL SYSTEM PROFILE
│   ├── Equilibrado
│   ├── Desempenho
│   └── Extremo
│
└── GAME / WORKLOAD
    ├── Recommended
    ├── Maximum FPS
    ├── Lowest Latency
    ├── Stability
    ├── Quality
    └── Custom Validated
```

## 10.2 Estados de evidência

```text
Manual
→ Observed
→ PendingValidation
→ Validated
```

Uma configuração manual pode virar candidata e ser validada pelo mesmo Evidence Engine.

## 10.3 Promoção automática — EXPANDIDO

O DG pode criar, validar e ativar automaticamente um perfil novo quando ele comprovadamente vencer o atual para o mesmo objetivo e ambiente compatível.

Promoção automática não significa medição única. Continua exigindo política de evidência, repetibilidade, compatibilidade e freshness.

## 10.4 Aprendizado por objetivo

O sistema não deve concluir que um perfil é “o melhor em tudo”. Exemplo:

```text
Maximum FPS ≠ Lowest Latency ≠ Quality ≠ Stability
```

---

# 11. Adaptive Guardian 2.0 — PRESERVADO E EXPANDIDO

## 11.1 Missão

Manter a sessão dentro da região de desempenho esperada pelo perfil atual, usando intervenções contextuais e mensuradas.

## 11.2 State machine universal

A máquina original BlueStacks/FF vira um modelo generalizável:

```text
OFFLINE
→ DESKTOP
→ WORKLOAD STARTING
→ WORKLOAD READY
→ GAME STARTING
→ LOBBY / PREP when adapter supports it
→ MATCH / ACTIVE WORKLOAD
→ MATCH END
→ POST-WORKLOAD
```

Adapters especializados fornecem estados mais ricos; o fallback genérico trabalha com `Desktop / Starting / Active / Ending`.

## 11.3 Detecção multimodal

Pode combinar:

- processo;
- janela/foreground;
- render activity;
- input pattern;
- frame pattern;
- launcher/emulator signals;
- adapter-specific signals.

A saída inclui estado + confiança.

## 11.4 Baseline dinâmica

Thresholds são relativos ao perfil/máquina, não universais.

## 11.5 Classificadores

- CPU Contention;
- GPU Saturation;
- Memory Pressure;
- VRAM Pressure;
- Frame-Time Instability;
- Background Load;
- Thermal Throttling;
- Network Instability;
- Renderer/Engine Stall;
- Scheduler Imbalance;
- Input/Frame Latency Spike;
- Unknown.

`Unknown` é estado legítimo.

## 11.6 Canary Change

```text
Detectar
→ confirmar anomalia
→ selecionar ação candidata
→ micro-snapshot
→ aplicar canary
→ medir antes/depois
→ KEEP / ROLLBACK
```

Se inconclusivo, rollback.

## 11.7 Cooldown e Action Budget

O Guardian evita “otimizar demais”. Modos podem alterar confiança mínima, cooldown e orçamento, mas não remover a exigência de avaliação.

## 11.8 Modos do Guardian

- Conservador;
- Adaptativo — padrão;
- Agressivo;
- Monitorar apenas / Guardian Lock.

Esses modos são diferentes dos modos globais `Equilibrado/Desempenho/Extremo`, embora possam ser mapeados por política.

## 11.9 Quick Boost vs Mid-Game Optimize

### Quick Boost

Aplica somente ações previamente validadas e compatíveis.

### Mid-Game Optimize

Executa diagnóstico rápido, seleciona uma ação Live-Safe/contextual, faz canary e keep/rollback.

---

# 12. System Optimizer — NOVO

O System Optimizer opera o Windows em dois planos distintos.

## 12.1 Persistent PC Optimization — “Otimizar este PC”

Alterações persistentes que fazem sentido fora de jogos, sempre com histórico e reversão.

Escopo conceitual:

- startup;
- serviços apropriados;
- power plan;
- configurações Windows relevantes;
- processos/background policies;
- armazenamento;
- memória;
- tarefas;
- outras otimizações persistentes comprováveis.

Fluxo:

```text
Analyze
→ proposal/recommended state
→ snapshot
→ apply transaction
→ validate health
→ persist History
→ allow restore
```

## 12.2 Session Optimization

Quando um jogo/workload inicia:

- aplicar política temporária;
- priorizar workload;
- reduzir interferência;
- suspender/reduzir processos secundários conforme modo;
- pausar tarefas apropriadas;
- ajustar serviços apropriados temporariamente;
- monitorar;
- restaurar ao sair.

Regra:

```text
snapshot → apply → monitor → restore
```

## 12.3 Modo Extremo

Pode agir agressivamente sobre recursos secundários, mas preserva dependências necessárias ao Windows e ao workload.

---

# 13. Hardware Performance Engine — NOVO / EXPANSÃO NATURAL

O hardware engine separa capabilities por vendor/plataforma.

## 13.1 CPU

Quando suportado:

- power policy;
- boost behavior;
- core parking/distribution quando aplicável;
- workload affinity/priority;
- limites sustentados expostos;
- thermal/power observability.

## 13.2 GPU

Quando exposto por driver/API:

- maximum performance preference;
- power limits;
- clock/offset controls;
- thermal behavior;
- workload preference;
- VRAM/renderer-related strategy.

NVIDIA, AMD e Intel não devem ser tratados por um adapter genérico cego.

## 13.3 RAM / I/O

- pressure reduction;
- background consumer analysis;
- I/O contention;
- workload preparation;
- prioridade ao workload quando suportada.

## 13.4 Extremo Automático + Expert Manual

Os dois usam o mesmo engine. Não existem duas implementações de tuning.

Expert oferece, conforme capability:

- aplicar temporariamente;
- testar A/B;
- salvar candidato;
- restaurar estado anterior.

---

# 14. Modos globais de otimização — NOVO

## 14.1 Equilibrado

Prioriza estabilidade, eficiência, temperatura e convivência com tarefas paralelas.

## 14.2 Desempenho — padrão

Busca ganhos mensuráveis sem aplicar agressividade sem benefício.

## 14.3 Extremo

Busca extrair o máximo potencial útil da máquina para o workload ativo.

Pode aceitar:

- mais energia;
- clocks sustentados;
- ventoinhas/temperatura maiores dentro do que a plataforma expõe;
- redução/suspensão de background;
- políticas de energia agressivas;
- prioridade elevada;
- tuning avançado CPU/GPU.

Extremo não é “ligar todos os tweaks”. Se uma mudança adiciona calor/consumo sem ganho, é candidata à rejeição.

---

# 15. Game Discovery Engine — NOVO

## 15.1 Fontes

O catálogo local pode combinar:

- Steam;
- Epic Games;
- Riot;
- Battle.net;
- EA App;
- Ubisoft Connect;
- Xbox/Microsoft Store;
- launchers independentes;
- emuladores;
- installed-app discovery;
- executáveis em execução;
- executáveis conhecidos fora de launchers.

## 15.2 Local Game Catalog

Cada entrada pode conter:

```text
GameIdentity
Executables
Launcher
Install paths
Engine when identifiable
Auxiliary processes
GameAdapter
Config locations
Active profile
Evidence history
Optimization history
Overlay preferences
```

## 15.3 Generic Game Adapter

Jogos ainda não especializados não ficam sem otimização. O fallback pode atuar em:

- system/session policies;
- process priority/affinity;
- hardware policies;
- telemetry;
- generic active-workload detection;
- user-configured game targets.

Adapters especializados adicionam conhecimento seguro sobre config/renderer/engine.

---

# 16. Game Optimizer e Game Adapters — EXPANDIDO

## 16.1 Contrato conceitual de adapter

Um adapter deve poder declarar:

```text
Identity discovery
Launch/workload processes
State detection capabilities
Config discovery
Config snapshot
Supported mutations
Mutation safety class
Restart requirements
Benchmark preparation
Telemetry annotations
Rollback
```

## 16.2 Free Fire / BlueStacks Adapter — PRESERVADO

Continua sendo a primeira especialização funcional.

Deve encapsular progressivamente:

- BlueStacks discovery;
- instance discovery;
- `bluestacks.conf` parsing;
- FF / FF MAX detection;
- CPU/RAM/FPS/resolution/DPI configuration;
- renderer handling;
- benchmark preparation;
- process/session ownership;
- PresentMon measurement;
- game-state detection;
- restart/cleanup rules;
- configuration fingerprints.

O objetivo é retirar conhecimento BlueStacks do Core universal ao longo do tempo sem reescrever a implementação existente de uma vez.

## 16.3 Configuração gráfica automática

Adapters podem permitir que Auto Tuner modifique:

- resolução;
- render scale;
- quality presets;
- opções individuais CPU/GPU-heavy;
- renderer;
- FPS cap/target;
- texture/streaming budgets;
- launch/config parameters suportados.

## 16.4 Integridade de jogos

A arquitetura não inclui mecanismos de bypass/neutralização de anti-cheat ou proteções de integridade. O DG pode otimizar agressivamente tudo que for acessível e reversível sem criar um subsistema de evasão.

---

# 17. Deep Cleaner — NOVO

## 17.1 Estrutura

```text
Deep Cleaner
├── Analysis
├── Deep Cleanup
└── Extreme Cleanup
```

## 17.2 Analysis

Classifica itens por tipo, confiança, tamanho, regenerabilidade e risco.

## 17.3 Deep Cleanup

Limpeza profunda para uso recorrente, com foco em resíduos conhecidos e descartáveis.

Pode incluir:

- TEMP;
- dumps;
- logs obsoletos;
- update leftovers;
- launchers/emulators known residue;
- instaladores antigos;
- caches apropriados;
- restos conhecidos de drivers/instalações.

## 17.4 Extreme Cleanup

Inclui Deep e permite remover caches regeneráveis saudáveis, inclusive:

- shader caches;
- caches de jogos;
- caches de launchers;
- caches de emuladores;
- versões antigas e resíduos conhecidos.

O programa avisa sobre recompilação/recriação e primeiro carregamento possivelmente mais lento, mas isso não bloqueia a opção Extreme.

## 17.5 Fronteira absoluta de dados pessoais

Nunca remover automaticamente, mesmo em Extreme:

- saves;
- mods;
- screenshots;
- gravações;
- presets;
- configurações pessoais;
- projetos;
- documentos;
- conteúdo criado pelo usuário.

Esses itens entram somente em revisão manual.

## 17.6 Histórico e quarentena

Quando tecnicamente apropriado:

- high-confidence disposable → delete direto;
- uncertain/high-risk → quarantine;
- personal/user-created → manual review.

History registra quantidade, tamanho, erros, skipped e quarantine.

---

# 18. Auto Optimize contínuo — NOVO

## 18.1 Missão

Perceber degradações e mudanças ao longo do tempo sem exigir que o usuário refaça manualmente diagnósticos completos.

Sinais possíveis:

- novos apps de startup;
- serviços/processos mais pesados;
- lixo acumulado;
- mudança de power plan;
- updates de Windows/driver;
- mudança de hardware/monitor;
- regressão de perfil;
- novo jogo instalado;
- mudança de comportamento térmico;
- mudança de padrões de uso.

## 18.2 Níveis

- observar;
- sugerir;
- aplicar automaticamente mudanças conhecidas/reversíveis e autorizadas.

Mudanças profundas/persistentes continuam ligadas a snapshot + History e à política de risco.

## 18.3 Aprendizado local por máquina/jogo

O Auto Optimize aprende:

- jogos mais usados;
- processos normalmente coexistentes;
- mudanças que ajudaram ou prejudicaram;
- padrões térmicos;
- comportamento por modo/perfil;
- validade histórica por ambiente.

Conhecimento antigo perde confiança quando o ambiente muda significativamente.

---

# 19. Safety, risco e rollback — EXPANDIDO

## 19.1 Hardware Safety Envelope — opcional

### ON

Usa limites conservadores/recomendados e pode bloquear combinações classificadas como de risco elevado conforme política.

### OFF

O usuário recebe máxima liberdade dentro do que hardware/driver expõe. O DG mantém avisos e histórico, mas não deve inventar capabilities ou tentar remover limites não expostos.

## 19.2 Risk Levels

- Safe;
- Low;
- Moderate;
- High;
- Critical.

## 19.3 Instabilidade real

Eventos críticos geram rollback automático de emergência:

- driver reset;
- crashes repetidos;
- temperatura crítica;
- throttling severo;
- perda clara de estabilidade.

High-risk pode seguir configuração do usuário:

- rollback automático;
- perguntar antes;
- somente alertar.

Moderado/baixo:

- alerta;
- History.

## 19.4 Snapshot hierarchy

### Micro-snapshot

Valores de uma intervenção Guardian/Live-Safe.

### Optimization snapshot

Estado afetado por uma rodada Auto Tuner/Expert/System Optimizer.

### Recovery point

Estado consolidado relevante para History/Last Known Good.

### Application backup

Dados internos do DG; não confundir com snapshot de tuning.

---

# 20. History / Recovery Engine — PRESERVADO E EXPANDIDO

History é memória auditável, não apenas log.

## 20.1 Eventos

Categorias conceituais:

```text
OPTIMIZATION
BENCHMARK
PROFILE
GUARDIAN
SNAPSHOT
SYSTEM
GAME
ADAPTER
EXPERT
CLEANER
RESTORE
ENVIRONMENT
```

## 20.2 Perguntas que History deve responder

- o que mudou?
- quando?
- quem/qual motor alterou?
- por quê?
- qual evidência justificou?
- melhorou?
- foi revertido?
- qual era o último estado comprovadamente bom?
- posso voltar?

## 20.3 Last Known Good

Só é atualizado por estado com evidência suficiente; não significa simplesmente “último estado usado”.

## 20.4 Restore

Antes de restaurar:

- mostrar diff;
- criar `Before Restore` snapshot;
- aplicar transacionalmente;
- permitir desfazer restauração quando possível.

## 20.5 Retenção

```text
raw telemetry
→ detailed sessions
→ aggregates
→ important events
→ long-term summaries
```

Defaults históricos originais como 7 dias de raw e 30 dias de sessões podem continuar como ponto de partida configurável, não como contrato eterno.

---

# 21. Interface e UX unificadas

A identidade antiga é preservada, mas o mapa de produto cresce.

## 21.1 Temas

### Clean

- frosted white;
- ice blue;
- azul claro;
- ciano sutil;
- Liquid Glass;
- sem vermelho como cor-base.

### Dark

- smoked glass;
- preto/grafite;
- branco/cinza;
- vermelho rubi como destaque;
- Liquid Glass.

Mini Mode mantém ARGB multicolorido em ambos.

## 21.2 Mapa de telas DG

```text
DG PERFORMANCE ENGINE
│
├── Home
├── Analyze / Diagnostic
├── Optimize
│   ├── Optimize This PC
│   └── Game / Workload Tuning
├── Games
├── Profiles
├── Guardian
├── Performance
├── Cleaner
├── Expert
├── History
├── Settings
└── Mini Mode / Overlay
```

As telas antigas não são obrigatoriamente renomeadas na primeira migração. Novas áreas podem entrar gradualmente.

## 21.3 Home — PRESERVADO / EXPANDIDO

Continua sendo estado geral + ação principal.

No DG passa a resumir também:

- saúde do PC;
- diagnóstico;
- jogos detectados;
- otimização persistente;
- Auto Optimize;
- Cleaner quando houver resíduos relevantes;
- workload atual;
- perfil global e perfil do jogo.

Sem transformar Home em Expert.

## 21.4 Analyze / Diagnostic — NOVO COMO TELA, NÃO COMO ENGINE

Expõe Full PC Scan, Hardware/System Model, bottlenecks, capabilities, headroom e recomendações.

## 21.5 Optimize — PRESERVADO / EXPANDIDO

Mantém:

- Adaptativo / Profundo;
- benchmark híbrido;
- progresso;
- candidato atual;
- melhor atual;
- resultados;
- antes/depois;
- rollback.

No DG ganha escopo:

- PC inteiro;
- jogo selecionado;
- objetivos globais e por jogo;
- hardware tuning conforme capability.

## 21.6 Games — NOVO

Catálogo local, adapter status, perfil ativo, recomendações, histórico e ações por jogo.

## 21.7 Profiles — PRESERVADO / EXPANDIDO

Perfis globais e por jogo, provenance, confiança, ambiente, A/B, custom validated e winners.

## 21.8 Guardian — PRESERVADO / EXPANDIDO

Centro de inteligência da sessão, com estado, baseline, intervenção, canary, cooldown, budget, reliability e queue pós-sessão.

## 21.9 Performance — PRESERVADO / EXPANDIDO

Views:

- Agora;
- Sessão;
- Partida/Active workload;
- Histórico.

Inclui timeline sincronizada, bottleneck, interval selection, stutters, thermal, latency, network, data quality e A/B.

## 21.10 Cleaner — NOVO

Analysis / Deep / Extreme, categorias, tamanho, regenerabilidade, review e History.

## 21.11 Expert — PRESERVADO / EXPANDIDO

Categorias:

- System;
- CPU;
- GPU;
- Memory;
- Storage/I-O;
- Game;
- Adapter-specific;
- Render;
- Display & FPS;
- Latency;
- Network;
- Thermals;
- Processes;
- Experiments.

Cada controle deve informar quando disponível:

- valor atual;
- recomendação;
- capability;
- risk;
- evidence;
- impacto medido;
- confidence;
- rollback state.

## 21.12 Settings — PRESERVADO / EXPANDIDO

Categorias consolidadas:

- Appearance;
- Mini Mode;
- ARGB;
- Auto Tuner;
- Guardian;
- System Optimizer;
- Games & Adapters;
- Cleaner;
- Expert/Safety;
- Hotkeys;
- Startup & Behavior;
- Notifications;
- Data & History;
- Backup & Restore;
- Advanced/Diagnostics.

Progressive disclosure continua regra visual.

---

# 22. Mini Mode / Overlay — PRESERVADO E UNIVERSALIZADO

## 22.1 Tamanhos

- Compact;
- Mini;
- Micro.

## 22.2 Métricas

No contexto universal, os defaults continuam:

- FPS;
- latency;
- CPU;
- GPU;
- profile;
- Guardian.

Dynamic Metric Priority pode trocar temporariamente uma métrica por temperatura/alerta mais relevante.

## 22.3 Comportamentos

- always-on-top;
- drag;
- snap/edge magnetism;
- position lock;
- multi-monitor;
- DPI-aware;
- resolution-aware;
- click-through Off/Always/Auto;
- hold-to-interact hotkey;
- auto collapse;
- game-specific persistence.

## 22.4 ARGB como linguagem de estado

- normal → efeito configurado;
- degradação → âmbar;
- análise → pulso;
- intervenção → vermelho/laranja;
- melhoria → verde/ciano;
- rollback → sinal curto e retorno ao normal.

## 22.5 Performance Budget

O overlay deve medir seu próprio custo. Auto rendering quality pode reduzir blur/refraction se houver impacto significativo.

---

# 23. Fluxos end-to-end

## 23.1 Primeira execução

```text
Launch DG
→ initialize stores
→ Environment/Hardware discovery
→ capabilities
→ Full PC Scan
→ Game Discovery
→ initial recommendations
→ create initial recovery point
→ Home
```

## 23.2 Otimizar este PC

```text
Analyze persistent state
→ recommend changes
→ snapshot
→ apply transaction
→ health verification
→ History
→ Auto Optimize continues observing
```

## 23.3 Abrir jogo reconhecido

```text
Game detected
→ resolve GameIdentity + Adapter
→ load compatible profile/evidence
→ apply session profile
→ prepare Guardian
→ start telemetry
→ Mini Mode
→ active gameplay/workload
→ Guardian adaptive control
→ post-session analysis
→ History/Profile confidence update
→ restore temporary state
```

## 23.4 Auto Tuner

```text
Acquire global benchmark lease
→ suspend/reconcile Guardian
→ diagnostic
→ baseline
→ synthetic triage
→ candidate search
→ real workload validation
→ A/B evidence
→ classify winners
→ save History
→ restore/reconcile state
→ release lease
```

## 23.5 Expert manual candidate

```text
User edits supported parameters
→ show risk/capability
→ snapshot
→ Apply Temporarily
→ optional A/B
→ Manual/Observed
→ validation rounds
→ Validated
→ eligible to challenge winner
```

## 23.6 Extreme Cleanup

```text
Scan
→ classify
→ show categories/impact
→ protect personal data
→ remove/quarantine
→ rescan
→ History
```

---

# 24. Dados e modelos canônicos

Os nomes finais de classes serão definidos no plano de implementação, mas os conceitos devem existir.

## 24.1 MachineContext

- machine identity/fingerprint;
- Windows;
- hardware;
- monitor;
- drivers;
- capabilities.

## 24.2 GameIdentity

- game id/name;
- executables;
- launcher;
- install paths;
- adapter id;
- version signals when available.

## 24.3 WorkloadSession

- machine;
- game/workload;
- active profile;
- mode;
- start/end;
- state transitions;
- telemetry reference;
- interventions.

## 24.4 ConfigurationSnapshot

Composição de system + hardware + game/adapter settings materialmente relevantes à evidência.

## 24.5 PerformanceEvidenceSnapshot

O contrato já existente evolui para suportar dimensões universais sem quebrar compatibilidade histórica.

## 24.6 Profile

- role;
- mode;
- machine/game key;
- config;
- evidence level;
- provenance;
- confidence;
- created/validated timestamps;
- environment fingerprint;
- compatibility state.

## 24.7 OptimizationTransaction

- before snapshot;
- requested mutations;
- actually applied mutations;
- status;
- verification;
- rollback path.

## 24.8 HistoryEvent

- category;
- actor/engine;
- reason;
- target;
- before/after;
- evidence links;
- result;
- rollback link.

---

# 25. Persistência, privacidade e dados

## 25.1 Local-first — PRESERVADO

O produto funciona sem conta e sem nuvem obrigatória.

Dados principais locais:

- settings;
- profiles;
- evidence;
- benchmarks;
- sessions;
- Guardian knowledge;
- Auto Optimize knowledge;
- History metadata;
- snapshots;
- backups internos.

## 25.2 Retenção

Deve ser configurável e escalonada por granularidade, evitando banco infinito.

## 25.3 Backup ≠ Snapshot

- Backup = dados internos do aplicativo;
- Snapshot = estado de tuning/configuração do PC/workload.

---

# 26. Tratamento de erro e recuperação

## 26.1 Princípio

Nenhuma falha de otimização deve deixar silenciosamente o PC em estado desconhecido.

## 26.2 Categorias

- capability unavailable;
- adapter unsupported;
- benchmark contaminated;
- workload exited;
- config write failed;
- rollback failed/partial;
- telemetry coverage insufficient;
- crash/driver reset;
- data integrity failure.

## 26.3 Comportamento

- abortar promoção quando evidência insuficiente;
- restaurar transação quando aplicável;
- registrar erro em History;
- marcar estado `Needs Attention` quando rollback não puder ser confirmado;
- nunca fabricar sucesso.

---

# 27. Performance e overhead do próprio DG

O otimizador não pode virar o gargalo.

Metas arquiteturais:

- hot paths no native quando necessário;
- ring buffers;
- agregação em memória;
- UI refresh desacoplado da frequência de coleta;
- telemetry adaptive sampling;
- Mini Mode Auto rendering quality;
- benchmark A/B com overlay ON vs OFF quando necessário;
- processos/sensores liberando handles corretamente;
- serviços persistentes sem polling excessivo.

---

# 28. Testes e validação

## 28.1 Disciplina

Continuar com:

```text
TDD
RED → GREEN → REFACTOR
→ Windows CI
→ artifact
```

## 28.2 Camadas de teste

### Unit/Core

- scoring;
- compatibility;
- fingerprints;
- transactions;
- profile promotion;
- policy decisions;
- cleaner classification;
- capability rules.

### Native

- interop;
- timing;
- process controls;
- sensor adapters;
- rollback primitives.

### Integration

- Auto Tuner + adapter;
- Guardian + telemetry;
- lease global;
- History + snapshots;
- Game Discovery;
- System Optimizer transactions.

### Adversarial tests

- bogus metadata;
- missing sensors;
- stale evidence;
- environment drift;
- concurrent benchmark attempts;
- process restart/rebinding;
- partial writes;
- rollback failure;
- user cancellation.

### Hardware-in-loop/manual

CI não substitui validação em hardware real para:

- sensor accuracy;
- Liquid Glass visual;
- overlay overhead;
- vendor GPU/CPU controls;
- game-specific adapters;
- thermal behavior.

---

# 29. Migração do FF Performance Engine para DG Performance Engine

## 29.1 Não fazer mass rename agora

Preservar checkpoints verdes e histórico Git.

## 29.2 Estratégia

### Fase A — novos contratos neutros

Novos subsistemas usam nomes universais onde possível.

### Fase B — encapsular FF/BlueStacks

Mover conhecimento específico para adapter boundaries progressivamente.

### Fase C — compatibility bridge

`FFPerformanceEngine.*` pode continuar referenciando interfaces neutras.

### Fase D — renome físico controlado

Somente após cobertura e estabilidade suficientes migrar assemblies/namespaces/projeto/repositório.

---

# 30. Mapa PRESERVADO / EXPANDIDO / NOVO

| Bloco | Origem | DG | Estado atual |
|---|---|---|---|
| C#/.NET + C++ | PRESERVADO | mesma divisão, mais adapters nativos | IMPLEMENTADO |
| Environment/Hardware Scan | PRESERVADO | universalizado | PARCIAL + PLANEJADO |
| Telemetry Engine | PRESERVADO | universalizado | IMPLEMENTADO/PARCIAL |
| Performance | PRESERVADO | multi-game/system | IMPLEMENTADO/PARCIAL |
| Bottleneck Analysis | PRESERVADO | universal | PARCIAL + PLANEJADO |
| A/B Evidence | PRESERVADO | infraestrutura central | IMPLEMENTADO/VALIDADO |
| Profiles | PRESERVADO | machine→game→role | IMPLEMENTADO + EXPANSÃO |
| Auto Tuner | PRESERVADO | Universal Auto Tuner | IMPLEMENTADO + EXPANSÃO |
| Guardian | PRESERVADO | Adaptive Guardian 2.0 | IMPLEMENTADO + EXPANSÃO |
| History/Snapshot | PRESERVADO | universal recovery | IMPLEMENTADO + EXPANSÃO |
| Mini Mode | PRESERVADO | universal workload HUD | IMPLEMENTADO/PARCIAL + EXPANSÃO |
| Global Benchmark Lease | NOVO hardening | obrigatório global | PLANEJADO/APROVADO |
| System Optimizer | NOVO | permanente + sessão | PLANEJADO/APROVADO |
| Hardware Performance Engine | NOVO/EXPANSÃO | CPU/GPU/RAM/I-O | PLANEJADO/APROVADO |
| Game Discovery | NOVO | catálogo universal | PLANEJADO/APROVADO |
| Game Adapter framework | EXPANDIDO | generic + specialized | PARCIAL + PLANEJADO |
| Free Fire/BlueStacks adapter | PRESERVADO | primeiro adapter | IMPLEMENTADO |
| Deep Cleaner | NOVO | Deep + Extreme | PLANEJADO/APROVADO |
| Auto Optimize contínuo | NOVO | aprendizado local | PLANEJADO/APROVADO |
| Modos Equilibrado/Desempenho/Extremo | NOVO | política global | PLANEJADO/APROVADO |
| Safety Envelope opcional | NOVO | risk policy | PLANEJADO/APROVADO |
| Emergency rollback | EXPANDIDO | crítico obrigatório | PLANEJADO/APROVADO |
| Expert hardware tuning | EXPANDIDO | vendor/capability-driven | PLANEJADO/APROVADO |

---

# 31. Decomposição de implementação recomendada

A arquitetura inteira é grande demais para um único plano de implementação confiável. Deve ser dividida em subprojetos sequenciais, cada um com spec/plan/testes próprios.

## Track 0 — Foundation Hardening

1. Global Controlled Benchmark Lease;
2. Guardian suspend/reconcile;
3. garantir exclusividade real Auto Tuner/Challenge;
4. preservar todos os testes atuais.

## Track 1 — Universal Diagnostic Foundation

1. MachineContext universal;
2. Hardware Discovery;
3. Capability Model;
4. Environment Fingerprint v2 compatível;
5. Bottleneck Analyzer universal.

## Track 2 — System Optimizer

1. mutation contracts;
2. persistent transactions;
3. session transactions;
4. restore;
5. History integration;
6. “Otimizar este PC”.

## Track 3 — Game Discovery + Adapter Framework

1. GameIdentity;
2. catalog;
3. generic adapter;
4. launcher scanners;
5. encapsular Free Fire/BlueStacks no contrato.

## Track 4 — Universal Telemetry/Evidence

1. metric schema v2;
2. hardware channels;
3. data quality universal;
4. A/B config snapshot universal;
5. compatibility migration.

## Track 5 — Universal Auto Tuner + Profiles

1. search space abstractions;
2. global/system profile dimensions;
3. game-specific candidate dimensions;
4. automatic validated winner promotion;
5. revalidation rules.

## Track 6 — Adaptive Guardian 2.0

1. generic workload state machine;
2. universal classifiers;
3. session optimizer actions;
4. learned action reliability;
5. post-session queue.

## Track 7 — Hardware Performance Engine

1. vendor capability adapters;
2. CPU controls;
3. GPU controls;
4. Expert integration;
5. Auto Tuner integration;
6. instability detection.

## Track 8 — Deep Cleaner

1. analyzer/classifier;
2. safe/deep/extreme policies;
3. personal-data protection;
4. quarantine/history;
5. UI.

## Track 9 — Auto Optimize

1. change detection;
2. recommendation engine;
3. local learning;
4. confidence decay;
5. auto-apply policy.

## Track 10 — DG UX Migration

1. Analyze;
2. Games;
3. Cleaner;
4. System Optimize;
5. update Home/Settings/Expert/Mini;
6. progressive branding migration.

---

# 32. Decisões explicitamente fora do escopo automático

A arquitetura não prevê como comportamento automático padrão:

- flash/modificação de BIOS;
- firmware flashing;
- remoção de proteções térmicas fundamentais;
- ultrapassar limites que hardware/driver não expõem;
- tensão arbitrária sem capability confiável;
- bypass/neutralização de anti-cheat ou mecanismos de integridade;
- exclusão automática de conteúdo pessoal;
- “registry cleaning” indiscriminado como promessa de FPS;
- promoção de perfil sem evidência adequada;
- inventar métricas quando sensor não estiver disponível.

---

# 33. Decisões aprovadas que devem ser tratadas como fechadas

1. Nome oficial: **DG Performance Engine**.
2. Não recomeçar nem descartar FF Performance Engine.
3. Free Fire/BlueStacks = primeiro adapter especializado.
4. C#/.NET + C++ nativo permanece.
5. Local-first permanece.
6. Telemetry Engine único.
7. Evidence/A-B é base científica comum.
8. Profiles por objetivo, não único “melhor”.
9. Auto Tuner Adaptativo + Profundo.
10. Benchmark híbrido.
11. Três modos globais: Equilibrado / Desempenho / Extremo; Desempenho padrão.
12. Extremo Automático + Expert Manual.
13. Tuning CPU/GPU avançado quando capability permite.
14. Hardware Safety Envelope opcional.
15. Eventos críticos reais causam emergency rollback.
16. Persistent PC Optimization e Session Optimization são camadas separadas.
17. “Otimizar este PC” persistente e reversível.
18. Game Discovery automático.
19. Resolução/render scale/qualidade podem ser ajustados automaticamente.
20. Recomendação inicial é orientada pelo diagnóstico; evidência real pode substituí-la.
21. Auto Optimize contínuo.
22. Aprendizado local por máquina/jogo.
23. Perfis podem ser promovidos/ativados automaticamente após validação suficiente.
24. Deep Cleanup + Extreme Cleanup.
25. Extreme pode remover caches regeneráveis saudáveis.
26. Conteúdo pessoal nunca é apagado automaticamente.
27. System Optimizer Extremo pode reduzir/suspender background temporariamente e restaurar depois.
28. Global benchmark lease é necessário para evitar contaminação entre Auto Tuner e Profile Challenge.

---

# 34. Pontos ainda não fechados ou dependentes de implementação

Estes itens não devem ser tratados como decisão definitiva sem nova validação:

- fórmula e presença de uma nota única geral do PC na primeira execução;
- APIs/vendor libraries exatas para cada família de CPU/GPU;
- ordem de implementação dos launchers após o adapter genérico;
- schema físico final do banco local;
- frequência exata de cada sensor por hardware;
- thresholds térmicos universais — devem vir de capability/contexto, não de números inventados;
- nomenclatura física final dos assemblies `DGPerformanceEngine.*`;
- quando o repositório será renomeado;
- política exata de auto-apply para mudanças persistentes de risco elevado;
- metas numéricas finais de overhead do Mini Mode em hardware real.

---

# 35. Critério de sucesso da arquitetura DG

O DG Performance Engine estará cumprindo sua arquitetura quando conseguir, em uma máquina compatível:

1. conhecer hardware, Windows e capabilities reais;
2. detectar jogos/workloads e selecionar adapter;
3. estabelecer baseline confiável;
4. identificar provável gargalo com confiança explícita;
5. aplicar otimizações persistentes ou de sessão por transações reversíveis;
6. medir FPS/frame-time/1% lows/thermals e demais sinais relevantes sem contaminar significativamente o workload;
7. comparar candidatos A/B com contexto exato;
8. promover perfis somente com evidência válida;
9. adaptar-se durante a sessão via Guardian;
10. aprender ao longo do tempo por máquina/jogo;
11. recuperar-se de erro/instabilidade com snapshot/rollback;
12. registrar toda decisão material no History;
13. oferecer Expert sem duplicar a lógica dos motores automáticos;
14. manter dados pessoais protegidos no Cleaner;
15. continuar aproveitando integralmente a base FF/BlueStacks já validada.

---

# 36. Resumo arquitetural final

```text
                         DG PERFORMANCE ENGINE
                                  │
             ┌────────────────────┴────────────────────┐
             │                                         │
        PC / WINDOWS                              GAME / WORKLOAD
             │                                         │
      Diagnostic Engine                         Game Discovery
      Capability Model                          Adapter Framework
      System Optimizer                          Generic Adapter
      Hardware Engine                           Specialized Adapters
      Deep Cleaner                              FreeFire/BlueStacks
             │                                         │
             └────────────────────┬────────────────────┘
                                  │
                        Universal Auto Tuner
                                  │
                           Telemetry Engine
                                  │
                         Evidence / A-B Engine
                                  │
                            Profile Engine
                                  │
                         Adaptive Guardian
                                  │
                    History / Snapshot / Rollback
                                  │
                    Main UI / Mini Mode / Tray
```

A arquitetura não substitui o projeto anterior. Ela formaliza a evolução:

```text
FF PERFORMANCE ENGINE
        ↓ preservado
base funcional e validada
        ↓ encapsulada
Free Fire / BlueStacks Game Adapter
        ↓ generalizada
DG PERFORMANCE ENGINE
```

O próximo passo após a revisão deste documento é transformar a arquitetura em planos de implementação por track, começando pelo **Track 0 — Global Controlled Benchmark Lease**, porque ele é o hardening pendente que protege a validade experimental de toda a expansão futura.

---

# 37. Proveniência da consolidação

Esta especificação foi consolidada a partir de:

- planejamento original e conversas de criação do FF Performance Engine;
- especificação de produto/UX aprovada em 04/09/2026;
- fontes históricas adicionadas posteriormente ao projeto;
- checkpoints de implementação do branch `build/initial-product`;
- decisões de expansão para DG Performance Engine tomadas em 06/09/2026;
- estado real do GitHub verificado no momento da consolidação.

Em caso de conflito futuro, esta especificação deve funcionar como documento de arquitetura atual, enquanto commits/testes continuam sendo a fonte de verdade para o que está efetivamente implementado.

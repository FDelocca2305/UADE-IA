# VaultBreakers — Entrega 2 (Inteligencia Artificial)

Juego de **sigilo / heist top-down** desarrollado en Unity. El jugador es un ladrón que se
infiltra en instalaciones vigiladas para **recolectar el botín** (monedas y diamantes) y cumplir
los objetivos de la misión, evitando o neutralizando a una guarnición de enemigos controlados por IA.

Toda la jugabilidad gira alrededor de la IA: percepción (Line of Sight), toma de decisiones
(FSM, árbol de decisiones, selección por pesos), **movimiento inteligente (Steering Behaviors)** y
**navegación (Pathfinding A\*)**.

---

## 1. Ficha del juego

| | |
|---|---|
| **Nombre** | VaultBreakers |
| **Género** | Sigilo / Heist top-down (acción-sigilo, mobile) |
| **Objetivo** | Infiltrarse, recolectar el botín (monedas/diamantes), cumplir los objetivos y sobrevivir a la guarnición de IA |
| **Plataforma** | Mobile (controles táctiles); en desktop el mouse simula el touch (`TouchSimulation`) |
| **Escenas jugables** | `Gameplay`, `Gameplay 2` |
| **Flujo de escenas** | `MainMenuScene` → `Loading` → `Gameplay` / `Gameplay 2` |

### Controles

**Mobile (principal)** — doble joystick virtual flotante (`PlayerTouchMovement.cs`):

- **Joystick izquierdo:** movimiento del personaje.
- **Joystick derecho:** apuntar y disparar (disparo continuo / ráfaga). Suelta el dedo para dejar de disparar.
- Tutoriales contextuales de "arrastrar" y "disparar" al iniciar el nivel.

**Desktop** — no hay control por teclado en el jugador de gameplay. El Player usa
`PlayerTouchMovement` (Enhanced Touch) junto con un componente `TouchSimulation`, por lo que en
desktop se juega **arrastrando los joysticks con el mouse** (que simula el touch).

> Existe un script suelto `TestPlayerController.cs` (movimiento WASD) usado durante el desarrollo,
> pero **no está asignado al prefab del jugador** ni a las escenas; no controla el juego real.

El arma usa cargador con **recarga automática** al vaciarse (`MainCharacter.cs`).

---

## 2. Arquitectura general de la IA

La IA está organizada en capas desacopladas que se combinan por agente:

```
                ┌─────────────────────────────┐
                │   PERCEPCIÓN — Line of Sight │  PlayerDetector.cs
                │   (FOV + raycast + niveles)  │
                └──────────────┬──────────────┘
                               │ resultado de detección
                               ▼
   ┌───────────────────────────────────────────────────────────┐
   │                  TOMA DE DECISIONES                         │
   │   FSM (StateMachine + States ScriptableObject)             │
   │   Árbol de Decisiones + Ruleta de pesos (Civil)            │
   └───────────────┬───────────────────────────┬───────────────┘
                   │ "a dónde ir"               │ "qué hacer"
                   ▼                            ▼
        ┌────────────────────┐      ┌────────────────────────────┐
        │  PATHFINDING (A\*)  │ ──▶ │   STEERING BEHAVIORS         │
        │  grafo + string-    │      │  Seek / Flee / Arrive /     │
        │  pulling LoS        │      │  Pursue / Evade + Obstacle  │
        └────────────────────┘      │  Avoidance + Flocking        │
                                     └────────────────────────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │  BLACKBOARD global  │  comunicación entre agentes
                    │  (posición jugador, │  (alertas, refuerzos,
                    │   nivel de alerta)  │   coordinación)
                    └─────────────────────┘
```

- **Percepción (LoS):** `Assets/2. Scripts/AI/Core/PlayerDetector.cs`. Distancia → FOV (principal + periférico) → raycast dual (rayo principal + elevado) → nivel de detección graduado (`Immediate / Clear / Partial / Peripheral / None`). El resultado alimenta tanto a la FSM como al árbol de decisiones.
- **Decisión:** FSM modular basada en ScriptableObjects (`Assets/2. Scripts/FSM/`) + árbol de decisiones con ruleta de pesos para los civiles (`Assets/2. Scripts/AI/DecisionTree/`).
- **Pathfinding:** A\* sin allocaciones sobre un grafo de waypoints bakeado en la escena, con suavizado por *string-pulling* (`Assets/2. Scripts/AI/Pathfinding/`).
- **Steering:** biblioteca de comportamientos de Reynolds (`Assets/2. Scripts/AI/Core/Steering/Steering.cs`) + evasión de obstáculos + flocking.
- **Blackboard:** estado compartido para coordinación multi-agente (`Assets/2. Scripts/Services/MicroServices/BlackboardService/`).


---

## 3. Qué sistemas usa cada tipo de agente

El proyecto tiene **4 arquetipos de IA** que actúan de forma claramente diferenciada:

### 🛡️ Guardia (`Guard.cs`) — enemigo hostil
- **Decisión:** FSM (Idle, Patrol, Chase, Attack, Investigate, Search, Cover, Detain, CallReinforcements).
- **Movimiento:** Steering **Seek / Arrive** (patrulla y reposicionamiento), **Pursuit** (persecución con predicción) y **Evade**, + **Obstacle Avoidance**.
- **Reacción al jugador:** según su **personalidad** (`AIPersonalityType`: Aggressive / Cautious / Conservative / Elite…), cambia el umbral de detección con el que investiga o ataca, su velocidad y su tiempo de reacción.
- **Coordinación:** publica/consulta el Blackboard (última posición conocida, pedido de refuerzos).

### 🏃 Civil (`Civilian.cs` + `CivilianDecisionTreeRunner.cs`) — agente reactivo
- **Decisión:** **Árbol de decisiones** cuya raíz es `HasLoS()`, combinado con una **ruleta de pesos** (utility) que elige una *stance*: **Escape / Attack / Dying** según distancia al refugio, decay de LoS y disponibilidad de camino.
- **Navegación:** usa **Pathfinding A\*** para huir hacia un nodo seguro, con suavizado de camino y seguimiento de waypoints.
- **Movimiento:** Steering **Flee / Evade** (escape inmediato y evasión predictiva) y **Seek / Arrive** (seguimiento de la ruta A\*).
- **Comportamientos:** Idle, Flee, Evade, Pursuit, Attack melee, Alertar, Dying.
- Es el agente que **integra los tres pilares**: decisión (árbol/ruleta) → pathfinding (A\*) → steering (Flee/Seek/Arrive/Evade).

### 🤝 Aliado (`Ally.cs`) — agente de apoyo (no hostil al jugador)
- **Decisión:** FSM propia (FollowPlayer, ChaseGuard, AttackGuard, InvestigateGuard, SearchGuard, TakeCover, ThrowSmoke).
- **Objetivo opuesto al guardia:** acompaña y protege al jugador, y **caza a los guardias** (no al jugador).
- **Movimiento:** Steering **Pursue / Arrive / Seek** + Obstacle Avoidance, con **perfil de flocking** opcional para moverse en grupo.

### 👑 Líder / AllyLeader (`Leader.cs`, `AllyLeader.cs`) — capa de coordinación
- Extiende al guardia/aliado con una **FSM de coordinación** (HoldPerimeter, CoordinateReinforcements, CoverFire).
- Gestiona un grupo de unidades, sondea el Blackboard por pedidos de refuerzo y posiciona a su escuadra.
- Está **colocado a mano en ambas escenas** (`Leader 1.prefab` en `Gameplay`, `LeaderAlly.prefab` en `Gameplay 2`) y también puede instanciarse vía `FactionSpawner.cs`.

| Agente | Decisión | Pathfinding | Steering principal | Rol |
|--------|----------|-------------|--------------------|-----|
| **Guardia** | FSM | — (steering directo) | Pursuit, Seek, Arrive, Evade | Detectar y neutralizar al jugador |
| **Civil** | Árbol de decisiones + Ruleta | **A\*** | Flee, Evade, Seek, Arrive | Huir / atacar según contexto |
| **Aliado** | FSM | — | Pursue, Arrive, Seek (+ Flocking) | Proteger al jugador y cazar guardias |
| **Líder** | FSM de coordinación | — | Seek/Arrive | Coordinar al escuadrón |

---

## 4. Estructura del proyecto (IA)

```
Assets/2. Scripts/
├── AI/
│   ├── Core/
│   │   ├── PlayerDetector.cs            # Line of Sight
│   │   ├── Steering/Steering.cs         # Seek, Flee, Arrive, Pursuit, Evade
│   │   ├── Steering/ObstacleAvoidance.cs
│   │   └── Flocking/                    # Separation, Cohesion, Alignment
│   ├── DecisionTree/                    # Árbol de decisiones (Civil)
│   └── Pathfinding/
│       ├── Runtime/AStarNoAlloc.cs      # A* sin allocaciones
│       ├── Runtime/PathSmoother.cs      # String-pulling (LoS)
│       ├── Runtime/GraphAssets.cs       # Grafo de navegación
│       └── Runtime/PathFollowerAgent.cs # Seguimiento de waypoints
├── FSM/                                 # StateMachine + States (Guard/Civil/Ally)
├── Characters/
│   ├── Enemies/Guard.cs, Leader.cs, Civilian/Civilian.cs
│   ├── Allies/Ally.cs, AllyLeader.cs
│   └── MainCharacter/MainCharacter.cs   # Jugador
├── Spawning/FactionSpawner.cs           # Spawn de facciones por oleadas (sistema extra)
└── Services/MicroServices/BlackboardService/   # Coordinación multi-agente
```
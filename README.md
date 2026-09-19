# ResetyKile

Jogo de plataforma 2D em pixel art, feito **100% em C# puro** (sem engine gráfica),
usando **Raylib-cs** como biblioteca de renderização/input. Desenvolvido solo por Rafael.

**Inspiração principal:** Celeste (gamefeel, dash, wall jump, precisão).
**Diferencial:** protagonista com katana, sistema de almas, modo Super com escudo,
e foco extremo em **efeitos visuais / juice / dopamina visual**.

---

## 🎮 Status atual do projeto

**Fase:** Protótipo jogável — combate core funcional, sem arte final, sem áudio.

### ✅ Já implementado

- **Janela pixel-perfect** (canvas interno 320×180 escalado, sem filtro)
- **Timestep fixo** (1/120s) com accumulator pattern
- **Input unificado:** teclado (WASD) + Xbox + PlayStation
- **Física do jogador (Kile):**
  - Corrida com aceleração/desaceleração
  - Pulo com altura variável (segurar = mais alto)
  - **Coyote time** (0.1s) e **Jump buffer** (0.1s)
  - **Dash em 8 direções** com cooldown, reset ao tocar o chão
  - **Wall slide + Wall jump**
- **Combate:**
  - Ataque de katana (hitbox à frente do Kile)
  - Inimigos com **2 HP** e IA de patrulha + perseguição (raio de detecção)
  - **Dano dividido:** se N inimigos estão na hitbox, cada um leva `dano / N`
  - Inimigos respawnam (APENAS PARA TESTE — no jogo final são finitos)
- **Vida e dano:**
  - Kile tem **5 HP**
  - Invencibilidade de 1s pós-dano (com pisca-pisca visual)
  - Knockback ao levar dano
  - Morte → tela "Voce morreu..." → respawn após 1.5s
- **Sistema de Souls:**
  - Cada inimigo morto = **1 soul**
  - Barra enche em **10 souls** = super pronto
  - Ao ativar super: **Souls zeram**
- **Modo Super (tecla E / B ou Circle no controle):**
  - Kile faz animação de "lamber a katana" (0.6s windup)
  - Dura 8 segundos
  - Kile fica rosa + aura quadrada pulsante
  - **Concede escudo:** o último coração cheio fica prateado pulsando
  - Escudo absorve 1 hit inteiro e quebra (não perde HP)
  - **NÃO regenera vida** (cura virá de itens no futuro)
- **HUD unificada (`UI/Hud.cs`):**
  - Linha 1: 5 corações (vermelho cheio / cinza vazio / prata com escudo)
  - Linha 2: 10 slots de soul (dourado / rosa quando pronto / cinza vazio)
  - Barra de tempo do super quando ativo
- **Animações com `time`** — coração do escudo pulsa, aura do super pulsa

### 🚧 Ainda NÃO implementado (roadmap)

- **Juice** (próximo passo): screenshake, hitstop, flash branco, rastro de dash,
  partículas, zoom punch, flash no hit
- **Sistema de animação** (SpriteSheet + AnimationPlayer com eventos de frame)
- **Combate 2.0:** inimigo telegrafa ataque, dash ofensivo, super com dano em área,
  inimigos voadores/atiradores
- **Tilemap via Tiled** (editor visual de fase)
- **Áudio** (sfx + música — o Rafael vai produzir a trilha)
- **Sistema de progressão:**
  - Checkpoints estilo Undertale (áreas seguras)
  - Baús tipo "Ender Chest" nesses checkpoints (guardar itens)
  - Itens de cura no inventário
  - Mundo contínuo, sem separação de fases/menu, progressão narrativa
- **Menu / Pause / Game Over reais**
- **Port** (futuro, inicialmente só Windows)

---

## 🗂️ Estrutura do projeto
ResetyKile/
├── ResetyKile.csproj ← .NET 8 + Raylib-cs 6.1.0
├── Program.cs ← entry point + loop principal (timestep fixo)
├── README.md ← este arquivo
│
├── Core/ ← sistemas base (independentes do jogo)
│ ├── MathUtil.cs ← Approach, ExpLerp, Clamp
│ ├── Renderer.cs ← canvas 320×180 pixel-perfect
│ └── Input.cs ← input unificado teclado + gamepad
│
├── Entities/ ← tudo que "vive" no mundo
│ ├── Player.cs ← o Kile (física, ataque, super, escudo)
│ └── Enemy.cs ← inimigo patrulha/perseguição, 2 HP
│
├── World/ ← cenário
│ └── Level.cs ← grid de tiles + colisão AABB
│
├── UI/
│ └── Hud.cs ← HUD unificada (vida + souls + super)
│
├── Render/ ← (vazio, futuro: juice e partículas)
├── Audio/ ← (vazio, futuro)
├── Data/ ← (vazio, futuro: configs, paleta)
└── Assets/ ← (vazio, futuro: sprites, som, mapas)

**Namespaces batem com pastas:**
`ResetyKile`, `ResetyKile.Core`, `ResetyKile.Entities`, `ResetyKile.World`, `ResetyKile.UI`.

---

## 🕹️ Controles

| Ação         | Teclado              | Xbox             | PlayStation      |
|--------------|----------------------|------------------|------------------|
| Mover        | A / D                | Analógico / D-pad| Analógico / D-pad|
| Pular        | Espaço / C           | A                | Cross            |
| Dash         | Shift / X            | X                | Square           |
| Atacar       | Z / J                | Y                | Triangle         |
| Super        | E / K                | B                | Circle           |
| Pause        | Esc                  | Start            | Options          |

**Dica pro dash:** segura uma direção (8 direções suportadas) antes de apertar.
Sem direção, dá dash na direção que o Kile está virado.

---

## 🧠 Decisões de design (importantes pra não esquecer)

- **Unidade do mundo:** 1 tile = 8px. Resolução interna 320×180.
- **Pulo alcança ~3-4 tiles.** Se mexer em `JumpSpeed` ou `Gravity`, refaz a conta:
  `altura_máx_px = JumpSpeed² / (2 * Gravity)`.
- **Inimigos com 2 HP** e dano dividido = se bater em vários de uma vez, não mata instantâneo.
  Isso desencoraja "spammar ataque no bolo", premia isolamento.
- **Super NÃO cura.** Cura virá de itens no inventário.
- **Escudo do super** = absorve 1 hit, não perde HP, quebra visualmente.
- **10 souls = super.** Não é pra farmar — o jogo vai ter progressão com poucos inimigos.
- **Respawn de inimigo é só pra teste.** No jogo final eles são finitos.

---

## ⚙️ Setup / Como rodar

Requer .NET SDK 8 e Raylib-cs.

```bash
cd ResetyKile
dotnet restore
dotnet run

Compilar versão final (Windows)
bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
O .exe sai em bin/Release/net8.0/win-x64/publish/.

🛠️ Notas técnicas / Armadilhas conhecidas
Ambiguidade de Color: Raylib-cs tem construtores Color(byte,byte,byte,byte) e
Color(int,int,int,int). Sempre que criar com literais, use (byte)255 no último
argumento pra evitar erro CS0121.

Entry point do C#: ao criar projeto com dotnet new console, o template inclui
top-level statements (Console.WriteLine("Hello, World!");). Isso compila junto com
uma classe Program e gera warning CS7022, além de ignorar o Main() da classe.
Sempre remover o top-level statement.

Timestep fixo: o loop principal roda Update em passos de 1/120s, acumulando o
frameDt. Não misture Raylib.GetFrameTime() dentro do Update — ele deve receber
sempre FixedDt.

Edge detection no input: JumpPressed, DashPressed, AttackPressed, SuperPressed
são consumidos (setados pra false) dentro do loop fixo, pra não duplicar um toque
rápido se um frame renderizar mais devagar.

Movimento e colisão separados em eixos (MoveX depois MoveY). Nunca mova na
diagonal de uma vez — quebra a colisão com tiles.

📌 Instruções pra IA / continuidade do projeto
Se você é uma IA lendo isso e o Rafael pediu pra continuar o projeto, contexto essencial:

Não sugerir engine. Ele quer C# puro + Raylib-cs. Se ele pedir pra migrar, ele avisa.

Rafael é iniciante em C#. Sempre mandar arquivos completos pra copiar e colar,
nunca "adicionar linha X no método Y". Ele apaga o arquivo inteiro e cola o novo.

Sempre dizer qual arquivo e onde fica (caminho completo).

Sempre dizer o que esperar ao rodar.

Ordem de juice: ele quer priorizar efeitos visuais / dopamina antes de arte final.

Anotação de progressão futura (não esquecer):

Checkpoints estilo Undertale (áreas seguras)

Baús tipo Ender Chest nesses checkpoints

Itens de cura no inventário

Mundo contínuo, progressão narrativa, sem "fases" separadas por menu

Próximo passo combinado: JUICE — screenshake, hitstop, flash branco,
rastro de dash, partículas, zoom punch.

Idioma: Rafael é brasileiro, prefere português nas explicações.

📝 Histórico de marcos
v0.1 — Fundação: janela, input, física do Kile, dash, wall jump, coyote time

v0.2 — Combate: inimigos, ataque de katana, dano dividido, HUD de vida e souls

v0.3 — Super + escudo: ativação, lamber katana, aura, coração prateado

v0.4 (próximo) — Juice: screenshake, hitstop, flash, partículas, rastro

Feito com muito café e vontade de fazer um jogo gostoso de jogar.

---

## 📌 Como usar o README daqui pra frente

**Salva como `README.md`** na raiz do projeto (`C:\Users\Rafael\ResetyKile\README.md`).

Quando você voltar (hoje à noite, amanhã, semana que vem), faz isso:

1. Abre uma conversa nova comigo
2. Cola **só o conteúdo do README** (ou manda o arquivo)
3. Diz: *"continua o ResetyKile, próximo passo: Juice"* (ou o que você quiser)

Com isso eu **recupero todo o contexto** — as decisões técnicas, a nota de progressão, os nomes das classes, tudo. É a sua "cápsula do tempo".

---

## 🎁 Bônus — o que fazer antes de sair

Se quiser deixar tudo bem guardado:

**1. Git (recomendo muito)** — se ainda não tem repositório:

```bash
cd C:\Users\Rafael\ResetyKile
git init
git add .
git commit -m "v0.3 - Super + escudo + combate com dano dividido"#   R e s e t y K i l e  
 
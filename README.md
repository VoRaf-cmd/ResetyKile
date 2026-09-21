# ResetyKile

Jogo de plataforma 2D em pixel art, feito em C# puro com Raylib-cs.
Sem engine gráfica. Desenvolvido solo por Rafael (VoRaf-cmd).

Inspiração: Celeste (gamefeel, dash, wall jump).
Diferencial: Kile (espadachim com katana), sistema de almas,
modo Super com escudo, e foco em juice / dopamina visual.

Repo: https://github.com/VoRaf-cmd/ResetyKile

---

## STATUS

Pré-alpha. Combate core funcional, sistema de sprites pronto, sem áudio.
Foco atual: arte do Kile (sprites) e refino visual.

---

## JÁ IMPLEMENTADO

### Motor
- Loop com timestep fixo (1/120s)
- Renderização pixel-perfect (320x180 escalado)
- Input unificado: teclado + Xbox + PlayStation
- Fullscreen borderless (F11 / Alt+Enter) com resolução nativa
- Esc não fecha mais a janela (SetExitKey Null)
- Pause com Esc / Start (congela tudo, inclusive animações da HUD)
- Reset com R / Select (fade preto + flash branco + reset de tudo)

### Kile (Player)
- Corrida com aceleração/desaceleração
- Pulo variável (segurar = mais alto)
- Coyote time (0.1s) e jump buffer (0.1s)
- Dash em 8 direções
- Wall slide + wall jump
- Stamina de dash: 3 raios, regenera no chão (1.2s/raio) e no ar (3s/raio)
- Drop through platform (S / D-pad baixo / analógico baixo)
- Sprite 16×24 com sistema de animação (SpriteSheet + AnimationPlayer)
- Offset vertical do sprite ajustável (SpriteYOffset)

### Combate
- Ataque de katana (hitbox à frente, 21×13)
- Ataque normal: dano dividido entre inimigos na hitbox
- Dano base: 2.0 (mata 1 inimigo de 2 HP sozinho)
- Dash-attack: hit kill em até 2 inimigos mais próximos,
  dano residual (0.5) nos demais se 3+
- Custo do dash normal: 1.0 raio
- Custo do dash-attack: 1.5 raio
- Inimigos com 2 HP, IA de patrulha + perseguição
- Knockback nos inimigos ao tomar hit
- Separação leve entre inimigos sobrepostos + stacking visual
- Inimigos respawnam (APENAS PARA TESTE - no jogo final são finitos)

### Vida e dano
- Vida máxima: 20 pontos (5 corações, 4 pontos por coração)
- Dano de inimigo comum: 2 (meio coração)
- Invencibilidade de 1s pós-dano (com pisca-pisca)
- Knockback ao levar dano
- Morte: tela "Voce morreu..." -> respawn após 1.5s

### Souls / Super
- Souls: 10 no total, ganhas 1 por inimigo morto
- Visual: 5 quadradinhos, cada um = 2 souls
- Super ativa com 10 souls (E / B ou Circle)
- Animação de "lamber katana" (0.6s windup)
- Duração: 8 segundos
- Efeito: Kile fica rosa + aura quadrada pulsante
- Concede ESCUDO: último coração cheio fica prateado pulsando
- Escudo absorve 1 hit inteiro, quebra sem perder HP
- Super NÃO regenera vida (cura virá de itens no futuro)
- Super = stamina infinita durante o efeito

### Katana (visual)
- Desenhada por CÓDIGO (não está no sprite do Kile)
- Linha horizontal com gradiente (cinza → branco)
- Borda preta (1px em cima e embaixo)
- Aparece durante o attack, crescendo da esquerda pra direita
- Animação baseada em TEMPO (não no frame do sprite)
- Efeito de "corta e volta": cresce (0→100%) e encolhe (100→0%)
- Classe: Render/Katana.cs

### Sprites (Kile)
- Tamanho: 16×24 por frame
- Formato: PNG spritesheet horizontal (1 linha)
- Sistema: SpriteSheet + AnimationPlayer + DashTrail com silhueta
- Animações carregadas: idle, run, jump, fall, dash, wall_slide,
  wall_jump, attack, lick, super_idle, hurt, death
- Fallback: se o PNG não existe, gera placeholder colorido
- Se o PNG existe mas não é múltiplo de 16, dá erro

### Dash Trail
- Cada fantasma é uma cópia do SPRITE do Kile (não retângulo)
- Cor: azul (silhueta 100% azul, sem mostrar cores do sprite)
- Frequência: a cada 4 ticks (menos que o dash original)
- Fade automático conforme a vida do fantasma

### HUD (UI/Hud.cs + UI/SessionStats.cs)
- Linha 1: 5 corações fracionados (25/50/75/100%)
- Linha 2: 3 raios de stamina com animação de carga (tremida)
- Linha 3: 5 quadradinhos de soul (meio-cheios quando 1 soul)
- Barra de tempo do super quando ativo
- Coração prateado pulsante quando tem escudo
- SessionStats: tempo de sessão (MM:SS) + pontos (+5 kill, +10 dash-kill)

### Juice (Render/)
- ScreenShake (trauma-based, decay 1.8/s)
- HitStop / freeze frame (60-150ms em impactos)
- Particles (burst por evento, pool de 512)
- DashTrail (silhuetas azuis com sprite)
- FloatingText (ex: "Damn!", "+5", "+10")
- Flash branco em inimigos ao tomar hit (IsHurt)
- Outline escura nos inimigos (corpo escurecido 35%)
- Sombra sob inimigos
- Fade preto + flash branco no reset

### Mundo
- Level OrientalVillage: chão, paredes, plataformas, estrutura de pagode
- Plataformas atravessáveis (one-way): pula por baixo, pisa em cima
- Colisão AABB com tiles sólidos e plataformas
- Tiles com cores por tipo (pedra/madeira/terra)

### Regras especiais
- "Damn!" aparece quando: Hp <= 4 (1 coração) OU acerta 2+ inimigos
- Hitstop NÃO acontece durante dash-attack (pra não travar o movimento)
- Dano dividido só em ataque normal
- Dash-attack ignora divisão
- Reset (R / Select) reseta player, inimigos, partículas, stats, tempo

---

## NÃO IMPLEMENTADO (roadmap)

### Arte / Visual
- Terminar sprites do Kile (run parcial, falta jump, fall, dash,
  wall_jump, lick, super_idle, hurt, death)
- Versão "_a" do Kile (inicial, sem katana, com mochila)
- Sprites dos inimigos
- Sprites dos tiles (chão, plataforma, pagode)
- Background / parallax
- Iluminação (vinheta, bloom, camada de cor)
- Paleta de cores própria

### Combate expandido
- Inimigos variados (voador, atirador, tanque)
- Inimigo telegrafa ataque
- Boss

### Progressão (anotação importante)
- Checkpoints estilo Undertale (áreas seguras)
- Baús tipo Ender Chest nesses checkpoints
- Itens de cura no inventário
- Mundo contínuo, sem separação de fases/menu
- Progressão narrativa
- Inimigos finitos no jogo final (respawn é só pra teste)
- Kile começa SEM katana (com mochila) -> acha katana em algum ponto

### Ferramentas
- Tilemap via Tiled (editor visual de fase)
- Áudio (SFX + trilha)
- Menu / Pause de verdade (com opções)
- Tela de Game Over

### Publish
- Build final .exe pra Windows
- itch.io (devlog)

---

## CONTROLES

Ação         | Teclado              | Xbox                  | PlayStation
-------------|----------------------|-----------------------|-------------------
Mover        | A / D                | Analógico esq / D-pad | Analógico esq / D-pad
Pular        | Espaço / C           | A                     | Cross
Dash         | Shift / X            | X                     | Square
Atacar       | Z / J                | RT (gatilho direito)  | R2 (gatilho direito)
Super        | E / K                | B                     | Circle
Pause        | Esc                  | Start                 | Options
Reset        | R                    | Select / L3 / R3      | Select / L3 / R3
Fullscreen   | F11 ou Alt+Enter     | —                     | —

Dica: segura direção antes de apertar dash (8 direções).
Sem direção, dash vai na direção que o Kile está virado.

Dash-attack: aperta dash + ataque no mesmo tick.

Drop through platform: S / D-pad baixo / analógico baixo em cima de plataforma.

---

## ESTRUTURA

ResetyKile/
├── ResetyKile.csproj          - .NET 8 + Raylib-cs 6.1.0
├── Program.cs                 - entry point + loop principal
├── README.md                  - este arquivo
├── Core/
│   ├── MathUtil.cs            - Approach, ExpLerp, Clamp
│   ├── Renderer.cs            - canvas 320x180 pixel-perfect
│   └── Input.cs               - input unificado
├── Entities/
│   ├── Player.cs              - o Kile (física, ataque, super, escudo, stamina, sprite)
│   └── Enemy.cs               - inimigo patrulha/perseguição, 2 HP, knockback
├── World/
│   └── Level.cs               - grid de tiles + colisão AABB
├── Render/
│   ├── ScreenShake.cs
│   ├── HitStop.cs
│   ├── Particles.cs
│   ├── DashTrail.cs
│   ├── FloatingText.cs
│   ├── SpriteSheet.cs         - loader de spritesheet + placeholder + versão tingida
│   ├── AnimationPlayer.cs     - controla frames por tempo
│   └── Katana.cs              - katana desenhada por código
├── UI/
│   ├── Hud.cs                 - HUD unificada
│   └── SessionStats.cs        - tempo + pontos
├── Audio/                     - (vazio)
├── Data/                      - (vazio)
└── Assets/
    └── sprites/
        └── kile/
            ├── idle.png       - 4 frames, 64x24
            ├── run.png        - 6 frames, 96x24
            ├── wall_slide.png - 2 frames, 32x24
            ├── attack.png     - 4 frames, 64x24
            └── ...            - outros (ainda não desenhados)

Namespaces batem com pastas.

---

## SETUP

Requer .NET SDK 8.

cd ResetyKile
dotnet restore
dotnet run

Build final (Windows):

dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

IMPORTANTE: a `raylib.dll` NÃO é embutida no .exe. Manda os 2 arquivos
(.exe + raylib.dll) pro amigo. Sem a DLL, o jogo não abre.

---

## NOTAS TÉCNICAS

- Ambiguidade de Color: Raylib-cs tem construtores Color(byte,byte,byte,byte)
  e Color(int,int,int,int). Sempre use (byte)255 no último argumento
  pra evitar erro CS0121.

- Entry point: ao criar projeto com 'dotnet new console', o template inclui
  top-level statements que conflitam com uma classe Program. Sempre remover.

- Timestep fixo: Update roda em 1/120s. Não use Raylib.GetFrameTime() dentro
  do Update. Sempre passe FixedDt.

- Edge detection no input: JumpPressed, DashPressed, AttackPressed, SuperPressed
  são consumidos dentro do loop fixo pra não duplicar.

- Movimento e colisão em eixos separados (MoveX depois MoveY). Nunca diagonal
  de uma vez.

- Separação entre inimigos é LEVE (não é colisão real). Só empurra se
  sobrepõem muito. Ajuste em Enemy.cs: MinSeparation (5f) e SeparateForce (40f).

- Parâmetros de tuning do Player ficam no topo da classe Player.cs.

- Parâmetros de dano e regras ficam no topo do Program.cs.

- Fullscreen: usa P/Invoke (EnumDisplaySettings) pra pegar a resolução
  nativa. Só funciona no Windows. Pra port Linux/Mac, precisa reescrever
  GetNativeResolution com API nativa ou usar fallback do Raylib.

- Sprites do Kile: 16x24 por frame, spritesheet horizontal (1 linha).
  Sempre desenhar virado pra DIREITA (o código faz flip quando anda pra esquerda).
  Se a animação parecer invertida, é o PNG que tá virado pra esquerda.

- Assets: o .csproj copia Assets/ pro build via <None Update="Assets\**\*.*">.
  Sem isso, os PNGs não são encontrados em runtime.

- Katana: desenhada por código, baseada no TEMPO restante do attack
  (não no frame do sprite). Isso evita que ela "pisque" — cresce suave
  e encolhe suave.

---

## INSTRUÇÕES PRA IA / CONTINUIDADE

Se você é uma IA lendo isso e o Rafael pediu pra continuar:

1. NÃO sugerir engine. Ele quer C# puro + Raylib-cs.
2. Rafael é iniciante em C#. Sempre mandar ARQUIVOS COMPLETOS pra copiar e colar,
   nunca "adicionar linha X no método Y". Ele apaga o arquivo inteiro e cola o novo.
   (Ou, se a mudança for MUITO pequena, usar Ctrl+F pra achar e trocar.)
3. Sempre dizer qual arquivo e onde fica.
4. Sempre dizer o que esperar ao rodar.
5. Prioridade: juice e gamefeel antes de arte final.
6. Idioma: português.
7. Anotação de progressão futura (NÃO ESQUECER):
   - Checkpoints estilo Undertale (áreas seguras)
   - Baús tipo Ender Chest nesses checkpoints
   - Itens de cura no inventário
   - Mundo contínuo, progressão narrativa, sem "fases" separadas por menu
   - Inimigos finitos no jogo final
   - Kile começa SEM katana (com mochila) -> acha katana
8. Ordem de trabalho combinada: sprites -> combate expandido ->
   sistema de progressão -> áudio -> menu -> iluminação -> publish.

---

## HISTÓRICO DE MARCOS

- v0.1 - Fundação: janela, input, física do Kile, dash, wall jump, coyote
- v0.2 - Combate: inimigos, ataque de katana, dano dividido, HUD vida/souls
- v0.3 - Super + escudo: ativação, lamber katana, aura, coração prateado
- v0.4 - Juice: screenshake, hitstop, partículas, dash trail, floating text, knockback
- v0.5 - Stamina (3 raios), vida 20 pontos, souls 10 (5 quadradinhos),
         dash-attack com hit kill em 2 + residual, HUD reformulada,
         coração fracionado vertical, "Damn!" com 1 coração ou hit duplo
- v0.6 - Ataque remapeado para RT/R2, pause com gameTime,
         fullscreen borderless em resolução nativa (F11/Alt+Enter)
- v0.7 - Sistema de sprite (SpriteSheet + AnimationPlayer),
         Kile 16x24, zoom 1.0 estilo Celeste, Renderer com escala proporcional
- v0.8 - Level OrientalVillage, plataformas atravessáveis,
         drop through (S/D-pad/analógico), reset com R/Select,
         correção de IA dos inimigos em plataformas
- v0.9 - Dash trail com silhueta azul (sprite tingido), SpriteSheet com
         GetTintedTexture, Esc não fecha mais a janela
- v0.10 - Katana desenhada por código com animação fluida (baseada em tempo),
          borda preta, crescimento suave (cresce e encolhe)

---

Feito com muito café e vontade de fazer um jogo gostoso de jogar.
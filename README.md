# ResetyKile

Jogo de plataforma 2D em pixel art, feito em C# puro com Raylib-cs.
Sem engine gráfica. Desenvolvido solo por Rafael (VoRaf-cmd).

Inspiração: Celeste (gamefeel, dash, wall jump).
Diferencial: Kile (espadachim com katana), sistema de almas,
modo Super com escudo, e foco em juice / dopamina visual.

Repo: https://github.com/VoRaf-cmd/ResetyKile

---

## STATUS

Pré-alpha. Combate core funcional, sem arte final, sem áudio.
Foco atual: gamefeel e juice.

---

## JÁ IMPLEMENTADO

### Motor
- Loop com timestep fixo (1/120s)
- Renderização pixel-perfect (320x180 escalado)
- Input unificado: teclado + Xbox + PlayStation

### Kile (Player)
- Corrida com aceleração/desaceleração
- Pulo variável (segurar = mais alto)
- Coyote time (0.1s) e jump buffer (0.1s)
- Dash em 8 direções
- Wall slide + wall jump
- Stamina de dash: 3 raios, regenera no chão (1.2s/raio) e no ar (3s/raio)

### Combate
- Ataque de katana (hitbox à frente)
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
- Super ativa com 10 souls (tecla E / B ou Circle)
- Animação de "lamber katana" (0.6s windup)
- Duração: 8 segundos
- Efeito: Kile fica rosa + aura quadrada pulsante
- Concede ESCUDO: último coração cheio fica prateado pulsando
- Escudo absorve 1 hit inteiro, quebra sem perder HP
- Super NÃO regenera vida (cura virá de itens no futuro)
- Super = stamina infinita durante o efeito

### HUD (UI/Hud.cs)
- Linha 1: 5 corações fracionados (25/50/75/100%)
- Linha 2: 3 raios de stamina com animação de carga (tremida)
- Linha 3: 5 quadradinhos de soul (meio-cheios quando 1 soul)
- Barra de tempo do super quando ativo
- Coração prateado pulsante quando tem escudo

### Juice (Render/)
- ScreenShake (trauma-based, decay 1.8/s)
- HitStop / freeze frame (60-150ms em impactos)
- Particles (burst por evento, pool de 512)
- DashTrail (fantasmas ciano durante dash)
- FloatingText (ex: "Damn!")
- Flash branco em inimigos ao tomar hit (IsHurt)
- Outline escura nos inimigos (corpo escurecido 35%)
- Sombra sob inimigos

### Regras especiais
- "Damn!" aparece quando: Hp <= 4 (1 coração) OU acerta 2+ inimigos
- Hitstop NÃO acontece durante dash-attack (pra não travar o movimento)
- Dano dividido só em ataque normal
- Dash-attack ignora divisão

---

## NÃO IMPLEMENTADO (roadmap)

### Arte / Visual
- Sistema de SpriteSheet + AnimationPlayer
- Paleta de cores própria
- Background / parallax

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

### Ferramentas
- Tilemap via Tiled (editor visual de fase)
- Áudio (SFX + trilha)
- Menu / Pause / Game Over

### Publish
- Build final .exe pra Windows
- itch.io (devlog)

---

## CONTROLES

Ação         | Teclado         | Xbox              | PlayStation
-------------|-----------------|-------------------|-------------------
Mover        | A / D           | Analógico / D-pad | Analógico / D-pad
Pular        | Espaço / C      | A                 | Cross
Dash         | Shift / X       | X                 | Square
Atacar       | Z / J           | Y                 | Triangle
Super        | E / K           | B                 | Circle
Pause        | Esc             | Start             | Options

Dica: segura direção antes de apertar dash (8 direções).
Sem direção, dash vai na direção que o Kile está virado.

Dash-attack: aperta dash + ataque no mesmo tick.

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
│   ├── Player.cs              - o Kile (física, ataque, super, escudo, stamina)
│   └── Enemy.cs               - inimigo patrulha/perseguição, 2 HP, knockback
├── World/
│   └── Level.cs               - grid de tiles + colisão AABB
├── Render/
│   ├── ScreenShake.cs
│   ├── HitStop.cs
│   ├── Particles.cs
│   ├── DashTrail.cs
│   └── FloatingText.cs
├── UI/
│   └── Hud.cs                 - HUD unificada
├── Audio/                     - (vazio)
├── Data/                      - (vazio)
└── Assets/                    - (vazio)

Namespaces batem com pastas.

---

## SETUP

Requer .NET SDK 8.

cd ResetyKile
dotnet restore
dotnet run

Build final (Windows):

dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

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
8. Ordem de trabalho combinada: terminar juice e combate core,
   depois animação, depois Tilemap, depois áudio, depois progressão.

---

## HISTÓRICO DE MARCOS

- v0.1 - Fundação: janela, input, física do Kile, dash, wall jump, coyote
- v0.2 - Combate: inimigos, ataque de katana, dano dividido, HUD vida/souls
- v0.3 - Super + escudo: ativação, lamber katana, aura, coração prateado
- v0.4 - Juice: screenshake, hitstop, partículas, dash trail, floating text, knockback
- v0.5 - Stamina (3 raios), vida 20 pontos, souls 10 (5 quadradinhos),
         dash-attack com hit kill em 2 + residual, HUD reformulada,
         coração fracionado vertical, "Damn!" com 1 coração ou hit duplo

---

Feito com muito café e vontade de fazer um jogo gostoso de jogar.
# Skyline (Unity 6 / Android)

Jogo de voo lateral em Unity, com arte placeholder gerada por código.
Nasceu como réplica de Flappy Bird e virou outra coisa: em vez de toques
discretos, o controlo é contínuo.

**Como se joga:** mantém o dedo no ecrã para o motor ficar ligado e desliza
para cima e para baixo para escolher a altura. Tirar o dedo desliga o motor e
o avião cai. Passa entre os prédios e as nuvens de tempestade.

- **Unity:** 6000.1.12f1
- **Plataforma alvo:** Android (retrato, ARM64, IL2CPP)
- **Pacote:** `com.srnando.flappyclone`

## Como abrir

1. Unity Hub → **Add** → selecione a pasta `flappy-bird-unity`.
2. Abra o projeto e a cena `Assets/Scenes/Game.unity`.
3. Play. No Editor, mantém o botão do rato pressionado e move o cursor para
   cima e para baixo (a barra de espaço também liga o motor, mas sem direção).

## Menu "Flappy" (no Editor)

Tudo neste projeto é gerado por script — nada foi montado à mão. Se algo sumir
ou você quiser recomeçar do zero, use o menu **Flappy** na barra do Editor:

| Item                                     | O que faz                                                                                                     |
| ---------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| `0 - Gerar tudo`                         | Roda todos os passos abaixo em sequência                                                                      |
| `1 - Gerar arte placeholder`             | Recria os PNGs em `Assets/Art` (não toca na arte manual)                                                      |
| `1b - Gerar sons placeholder`            | Sintetiza os WAVs que faltarem em `Assets/Audio` — não escreve por cima dos que já lá estão                   |
| `1c - Ajustar importação da arte manual` | Repõe pivô, PPU e filtro do `lightning.png`, do `cloud.png`, do `plane.png`, das peças do avião, dos prédios e do `A320.png` da loja |
| `1d - Repor nuvem placeholder`           | Volta a pôr a faixa de nuvens gerada por código                                                               |
| `1e - Repor avião placeholder`           | Volta a pôr o avião gerado por código                                                                         |
| `1g - Repor pombo placeholder`           | Volta a pôr os dois fotogramas do pombo gerados por código                                                    |
| `1f - Repor sons placeholder`            | Volta a pôr os seis sons sintetizados, por cima do que lá estiver                                             |
| `2 - Montar cena do jogo`                | Recria `Assets/Scenes/Game.unity` e o prefab dos obstáculos                                                   |
| `3 - Configurar Android`                 | Aplica os Player Settings de Android                                                                          |
| `4 - Gerar APK`                          | Compila `Builds/Android/FlappyClone.apk`                                                                      |
| `9 - Salvar preview PNG`                 | Renderiza a cena para `preview.png` na raiz                                                                   |

> `2 - Montar cena` **sobrescreve** a cena. Se você editar a cena à mão, não rode de novo.

## Estrutura

```
Assets/
├─ Art/                  PNGs gerados (troque por arte final quando quiser)
├─ Audio/                WAVs gerados (troque por sons a sério quando quiser)
├─ Editor/               Scripts de geração — não vão para o build
│  ├─ Bootstrap.cs       Ponto de entrada (também usado via linha de comando)
│  ├─ PlaceholderArt.cs  Desenha os sprites pixel a pixel
│  ├─ PlaceholderAudio.cs Sintetiza os efeitos sonoros por código
│  ├─ SceneBuilder.cs    Monta a cena e o prefab dos canos
│  ├─ AndroidSetup.cs    Player Settings de Android
│  └─ BuildScript.cs     Geração do APK
├─ Prefabs/ObstaclePair.prefab
├─ Scenes/Game.unity
├─ Settings/GameConfig.asset   ← todos os números de balanceamento
└─ Scripts/
   ├─ GameConfig.cs        ScriptableObject com resposta do motor, gravidade, vão...
   ├─ GameManager.cs       Estado (Ready/Playing/GameOver), pontos, recorde
   ├─ Airplane.cs          Motor ligado/desligado, perseguição da altura do dedo
   ├─ EngineSmoke.cs       Rasto de fumo que sai do escape enquanto voa
   ├─ ObstacleSpawner.cs   Cria pares prédio+relâmpago no intervalo configurado
   ├─ ObstaclePair.cs      Movimento e descarte de cada par
   ├─ PigeonSpawner.cs     Decide quando um par traz pombo e larga-o no céu
   ├─ Pigeon.cs            O pombo: voa, fica tonto com a pancada e foge
   ├─ ScoreZone.cs         Gatilho de +1 ponto no vão
   ├─ ScrollingTiles.cs    Nuvens, arbustos e chão infinitos (com parallax)
   ├─ UIController.cs      Placar, recorde no canto e telas de início / fim
   ├─ GameInput.cs         Toque mantido, posição do dedo, rato e teclado
   ├─ Ads/                 Camada de anúncios (ver secção abaixo)
   ├─ AudioManager.cs      Efeitos curtos e o ciclo do motor
   ├─ CrashEffects.cs      Sorteia e toca as animações de queda
   ├─ FxParticle.cs        Partícula descartável (faísca, fumo, destroço, anel)
   ├─ CameraShake.cs       Tremor da câmera
   └─ ScreenFlash.cs       Clarão de ecrã inteiro
```

Os sprites dos obstáculos usam **pivô personalizado**: o prédio tem o pivô no
telhado e cresce para baixo, o relâmpago tem o pivô na ponta e cresce para cima.
É isso que permite encostar os dois às bordas do vão, sem espelhar nada. Se
substituir a arte, mantenha os pivôs.

Os obstáculos de baixo são os prédios `predio *_Prancheta 1.png` de `Assets/Art`,
que entram por ordem alfabética e se repetem em roda: prédio 1, prédio 2,
prédio 3, prédio 1... A lista sai da pasta, por isso exportar um quarto prédio
(`predio 4_Prancheta 1.png`) e rodar `1c` + `2` chega para ele entrar no jogo.
O `1c` iguala a **largura da parede** dos três — não a da imagem, que inclui
telhado e antena — e cada obstáculo estica o prédio o que falta para encostar
ao chão, nunca o encolhe.

O `lightning.png` e o `cloud.png` são os ficheiros **desenhados à mão**
(Illustrator, filtro Bilinear). O `1 - Gerar arte placeholder` nunca lhes toca —
quem lhes acerta as definições de importação é `Flappy → 1c`. O raio é
225×797 a 200 PPU. O sprite é curto de propósito: o
`ObstaclePair` estica-o na vertical a cada obstáculo, para cobrir do vão até
fora do ecrã, e isso dá comprimentos diferentes a cada relâmpago de graça.

O colisor do relâmpago é um **polígono** gerado da própria forma, não um
retângulo: com um retângulo o avião morria no ar vazio ao lado do ziguezague.

## O céu

`#24606E`, um azul de dia de chuva. É a cor de fundo da câmara — não há sprite
nenhum a fazer de céu — e vive no `BuildCamera` do `SceneBuilder`.

Veio do `#4EC0CA` original por um caminho só: **o mesmo tom e a mesma
saturação, com metade do brilho** (185° → 191°, 53% → 51%, luminosidade 55% →
29%). Escurecer sem mexer no tom é o que mantém o jogo reconhecível — parece o
mesmo céu num dia mau, e não um jogo com outra paleta.

Ao escurecer, o amarelo do raio e o vermelho do avião ganharam contraste, e o
contorno `#1D1D1B` da barriga da faixa de nuvens continua a separá-la do céu.
O chão e os arbustos ficaram na mesma, propositadamente: com tudo escuro o
ecrã fechava-se, e é a faixa verde em baixo que ainda dá luz à cena.

## A faixa de nuvens

O topo do ecrã tem uma faixa de nuvens de tempestade que rola sozinha, a
**metade** da velocidade do chão — o mesmo `ScrollingTiles` dos arbustos, só
com outro multiplicador. É de dentro dela que os relâmpagos nascem: a faixa
desenha na ordem 8, à frente do relâmpago (5) e atrás do avião (20), por isso a
ponta de cima do raio fica escondida na nuvem em vez de acabar no ar. Quem manda
o raio até lá dentro é o `lightningTopY` do `GameConfig`.

A faixa é só pintura: não tem colisor. O topo dela está preso em `y = 5,4`
(`CloudTopY`, no `SceneBuilder`), um pouco acima da borda do ecrã, e a altura
sai do próprio sprite — desenhar uma faixa mais grossa não pede código nenhum.

### Redesenhar a faixa (Illustrator)

| Item      | Valor                                                         |
| --------- | ------------------------------------------------------------- |
| Prancheta | **1344 × 352 px**, RGB, fundo transparente                    |
| No mundo  | 6,72 × 1,76 unidades (200 PPU) — a largura de um tile do chão |
| Ficheiro  | `Assets/Art/cloud.png`, PNG-24 com alfa                       |
| Pivô      | centro (posto pelo `1c`)                                      |

A **largura é a única medida sagrada**: o `1c` calcula o PPU a partir dela para
o tile dar sempre 6,72 unidades, portanto podes desenhar a 672, 1344 ou 2048 px
de largura — muda a nitidez, não o tamanho no jogo. A altura é livre: a faixa
cresce para baixo a partir de `y = 5,4`.

Três linhas horizontais mandam na composição (medidas a partir do topo da
prancheta de 352 px):

- **0 px — topo.** Tem de estar pintado a toda a largura, sem contorno: esta
  borda é cortada pelo ecrã e nunca se vê.
- **80 px — topo do ecrã** (`y = 5`). Só daqui para baixo é que o jogador vê
  alguma coisa. É aqui que o desenho tem de começar a valer a pena.
- **280 px — linha de segurança** (`y = 4`). Nenhum pixel pintado deve passar
  daqui. O vão mais alto tem o topo em `y = 3,4` e o jogador não pode ter dúvidas
  sobre o que é pintura e o que mata.

**Emenda em ciclo.** São dois tiles a rodar em círculo, por isso a borda direita
tem de continuar na esquerda. No Illustrator: desenha para além da prancheta,
seleciona tudo e faz `Object → Transform → Move` com _Horizontal_ `1344 px`,
_Vertical_ `0` e botão **Copy** — depois o mesmo com `-1344 px`. Cada forma que
sai por um lado passa a ter a gémea a entrar pelo outro. Na exportação liga
**Use Artboards**, que corta o resto.

**Estilo.** O céu é `#24606E` e o raio é `#FFD642` com contorno `#1D1D1B`. A
nuvem tem de ser escura o suficiente para o amarelo saltar — algo entre
`#9CA4C0` no topo iluminado e `#525A78` na barriga funciona. Usa o mesmo
`#1D1D1B` do raio no contorno de baixo, com uns 8 px de espessura a 200 PPU,
para a família bater certo. Evita quase-brancos: o placar é texto branco e cai
justamente por cima da faixa.

**Detalhes que ajudam.** A barriga recortada em bolhas de tamanhos diferentes é
o que faz o raio parecer sair de um sítio concreto. Não desenhes raios na faixa
— esses nascem no `ObstaclePair`, em `x` sorteado. Nada de sombra projetada a
descer abaixo da linha de segurança.

Depois de trocar o ficheiro: `Flappy → 1c` (importação) e `Flappy → 2` (a cena
guarda a posição da faixa, que depende da altura do desenho). Se quiseres a
faixa gerada por código de volta, `Flappy → 1d`.

## Balanceamento

Selecione `Assets/Settings/GameConfig.asset` no Inspector. Os campos que mais
mudam a dificuldade:

| Campo                  | Padrão | Efeito                                                |
| ---------------------- | ------ | ----------------------------------------------------- |
| `obstacleGap`          | 2.8    | Tamanho do vão entre a torre e a nuvem                |
| `obstacleSpeed`        | 3.2    | Velocidade **inicial** de rolagem                     |
| `speedPerPoint`        | 0.06   | Quanto a velocidade sobe por ponto                    |
| `maxObstacleSpeed`     | 6.5    | Teto da velocidade (atingido por volta dos 55 pontos) |
| `speedRamp`            | 0.6    | Suavidade da aceleração a cada ponto                  |
| `spawnSpacing`         | 4.3    | Distância entre pares, em unidades                    |
| `followSpeed`          | 7.5    | Velocidade vertical máxima com o motor ligado         |
| `followResponsiveness` | 7      | Quão colado ao dedo o avião fica                      |
| `followAccel`          | 60     | Quão seca é a resposta ao mudar de direção            |
| `gravityScale`         | 3.2    | Peso da queda com o motor desligado                   |

Mudanças valem na hora, sem recompilar.

### Como funciona a progressão

`GameManager.CurrentSpeed` é a fonte única de velocidade — obstáculos, chão e
arbustos leem dela. O alvo é `obstacleSpeed + pontos × speedPerPoint`, limitado
por `maxObstacleSpeed`, e a velocidade real persegue esse alvo a
`speedRamp` unidades por segundo, para cada ponto não dar um solavanco.

O spawn é por **distância**, não por tempo: o intervalo é
`spawnSpacing ÷ CurrentSpeed`. Sem isso, acelerar afastaria os pares e
anularia boa parte do aumento de dificuldade.

## Som

Os seis efeitos são **sintetizados por código** em `PlaceholderAudio.cs` e
gravados como WAV 16 bits mono a 44,1 kHz. Nenhum ficheiro vem de fora.

| Ficheiro         | Quando toca                  | Como é feito                                         |
| ---------------- | ---------------------------- | ---------------------------------------------------- |
| `sfx_start.wav`  | Ao arrancar a partida        | Serra a varrer 70→150 Hz com um sopro de ruído       |
| `sfx_score.wav`  | A cada ponto                 | Dois bips quadrados, 880 Hz e 1318 Hz                |
| `crash.mp3`      | No impacto (prédio ou chão)  | Ruído branco sobre um grave a cair de 200 para 32 Hz |
| `Relampago_sound.wav` | No impacto contra o relâmpago | Ficheiro à parte, não é sintetizado             |
| `sfx_record.wav` | Ao bater o recorde           | Arpejo de quatro notas                               |
| `sfx_engine.wav` | Em ciclo, com o dedo no ecrã | Serras a 90 e 180 Hz moduladas a 18 Hz               |
| `sfx_pigeon.wav` | Ao levar com um pombo        | Baque grave, grasnido em serra a descer aos solavancos e três tosses de motor |

O motor tem fonte própria porque é o único som contínuo: o volume esvanece
quando tiras o dedo e a **altura sobe com a velocidade do jogo**, de 0,92 a
1,35. Aos 55 pontos ouve-se que o avião está no limite.

Detalhe do ciclo: todas as frequências do motor são múltiplos de 1/duração
(2 Hz, com 0,5 s de amostra). Se não fossem, a emenda do loop daria um estalo
audível a cada meio segundo.

### Trocar por sons a sério

Põe os teus **WAV** em `Assets/Audio` com os mesmos nomes (`sfx_crash.wav`,
`sfx_score.wav`, ...). Tem de ser `.wav`: o `LoadClip` do SceneBuilder procura
por extensão fixa, e um `.mp3` largado na pasta é importado pelo Unity mas nunca
chega à cena. Converter é uma linha de `ffmpeg`:

```
ffmpeg -i som.mp3 -ac 1 -ar 44100 -sample_fmt s16 Assets/Audio/sfx_crash.wav
```

O único requisito de conteúdo é o `sfx_engine` emendar bem em ciclo — o MP3, que
leva silêncio de padding do codificador, estala a cada volta. Bons pontos de
partida: **jsfxr** e **ChipTone** no browser para efeitos retro,
**freesound.org** e **pixabay.com/sound-effects** para gravações reais.

Ao contrário da arte, aqui não é preciso ter cuidado com o gerador: o `1b` só
escreve os que faltarem. Quem quiser os sintetizados de volta usa o `1f`.

## Anúncios (banner AdMob)

O jogo fala apenas com `AdsService`. Por trás dele está um `IAdsBackend`, com
duas implementações: `AdMobBackend` e `FakeAdsBackend`. Trocar de rede é
escrever uma terceira — nada no resto do projeto muda.

Por omissão o banner aparece **só fora da partida**: ecrã inicial e fim de jogo.
O campo `Show During Gameplay`, no Inspector do `AdsService`, inverte isso.

### O símbolo ADMOB

`AdMobBackend.cs` está inteiro dentro de um `#if ADMOB`. Sem o plugin da Google
instalado o ficheiro compila para nada, o projeto continua a compilar e o jogo
usa o `FakeAdsBackend`, que só liga um retângulo cinzento no lugar do banner.

Quem liga o símbolo é `Assets/Editor/AdMobDefine.cs`: procura o tipo
`GoogleMobileAds.Api.MobileAds` nos assemblies carregados e acerta o símbolo
sozinho. Instalar o plugin basta; remover também.

No Editor o `FakeAdsBackend` é usado **sempre**, mesmo com o plugin instalado,
porque anúncios reais não aparecem fora de um aparelho.

### Instalar o plugin

1. Descarregar o _Google Mobile Ads Unity Plugin_ das versões oficiais no GitHub
   (`googleads/googleads-mobile-unity`), o ficheiro `.unitypackage`
2. No Unity: `Assets → Import Package → Custom Package` e importar tudo
3. `Assets → Google Mobile Ads → Settings` e colar o **App ID** da AdMob —
   é o que vai para o manifesto Android, e sem ele a aplicação fecha ao arrancar
4. Confirmar no Console que apareceu `[Ads] Símbolo ADMOB ligado`

### IDs de teste e IDs reais

O `AdsService` arranca com `Use Test Ads` ligado, usando o ID público de teste
da Google. Funciona em qualquer aparelho, sem conta, e é seguro carregar nele.

**Só desliga esse campo na versão que vais publicar**, depois de colar o teu ID
em `Android Banner Unit Id`. Clicar nos próprios anúncios reais é tráfego
inválido, e a suspensão da AdMob costuma ser definitiva.

### A faixa reservada

O jogo reinicia com um toque em qualquer sítio, o que junto de um banner produz
cliques acidentais. Enquanto o banner está visível, `AdsService` define
`GameInput.BannerSafeFraction` e os 16% inferiores do ecrã deixam de aceitar o
toque de reinício. Ajusta-se em `Safe Area Fraction`.

### Antes de publicar

- Política de privacidade acessível por URL, exigida pela Play Console
- Formulário de segurança de dados preenchido, declarando a recolha para anúncios
- Consentimento RGPD na Europa, através do _User Messaging Platform_ que vem
  no próprio plugin
- Classificação etária coerente com anúncios de terceiros

## O recorde no canto

O canto superior esquerdo mostra `RECORDE  n` durante toda a partida — desde o
ecrã inicial até ao impacto. É o `BestText`, ancorado ao canto do Canvas, por
isso fica no sítio em qualquer proporção de ecrã.

O `GameManager` só grava o novo recorde no fim da partida, mas deixar o canto a
mostrar um número **menor** do que o placar no meio do ecrã parecia avaria: mal
a pontuação passa o recorde, é ela que aparece ali, a dourado (o mesmo
`#FFD642` do selo de novo recorde). Assim o jogador vê o momento em que passa a
sua melhor marca, sem esperar pelo painel do fim.

Desaparece quando o painel de fim de jogo entra — esse já traz o recorde ao
centro, e dois no ecrã era repetição.

## Rasto de fumo

O `EngineSmoke` (no próprio avião) larga sopros de fumo pelo escape. Com o dedo
no ecrã saem 26 por segundo e mais opacos; sem dedo caem para 9 e quase
transparentes — dá para ver o motor a trabalhar sem olhar para o dedo.

Os sopros **não são filhos do avião**: nascem num objeto à parte e ficam
parados no ar, com velocidade `-CurrentSpeed`, a mesma do cenário. É isso que
faz o rasto ficar para trás em vez de andar colado à cauda, e é isso que o
deixa continuar a dissipar-se depois de o avião desaparecer na queda.

O ponto de saída é uma **fração do sprite** (`nozzleFraction`, −0,46 em X), não
uma coordenada fixa: trocar a arte do avião por outra de tamanho diferente não
deixa o fumo a sair do sítio errado. Desenham na ordem 19 — atrás do avião (20)
e à frente das torres (5).

Reutiliza o `spark.png` e o `FxParticle` dos efeitos de queda; todos os números
estão no Inspector do avião.

## Animações de queda

Ao bater, o avião real desaparece e `CrashEffects` sorteia uma de quatro
variantes — nunca a mesma duas vezes seguidas:

| Variante  | O que acontece                                                              |
| --------- | --------------------------------------------------------------------------- |
| `Boom`    | Clarão, onda de choque e três bolas de fogo que enchem o ecrã               |
| `Debris`  | O avião parte-se nas suas peças vetorizadas, que caem e ressaltam no chão   |
| `Spiral`  | Roda descontrolado a mergulhar, com rasto de fumo e rebentamento ao aterrar |
| `BoltPop` | Cómica: desmancha-se numa chuva de 28 miudezas (rodas, farolim, antena)     |

Nenhuma usa Animator ou AnimationClip — são todas `FxParticle` configurados de
forma diferente, o que as torna fáceis de ajustar e de acrescentar. Para uma
quinta variante, escreva o método e some 1 ao `Random.Range(0, 4)` de `Play`.

### As peças do avião

Os destroços não são estilhaços genéricos: são as **16 peças vetorizadas à
mão** — corpo, hélice de cima e de baixo, rodas, trens, janelas, porta,
antena, farolim. Quando o avião se desfaz, desfaz-se naquilo de que é feito.

A lista **não está escrita em lado nenhum**. Tudo o que estiver em `Assets/Art`
com o sufixo `_Prancheta 1.png` — o carimbo que o Illustrator põe no que sai da
prancheta 1 — entra nos destroços. Exportar uma peça nova é o suficiente para
ela aparecer na queda; apagar uma tira-a de lá. Depois de mexer nas peças:
`Flappy → 1c` (importação) e `Flappy → 2` (a cena guarda a lista).

**O tamanho.** As peças partilham um PPU só, senão a hélice fina e a fuselagem
gorda deixavam de bater certo uma com a outra. O número sai da **peça mais
larga** — a fuselagem, que fica com a largura do avião inteiro (`PlaneW`,
1,25 unidades). Como o PPU é calculado do desenho, e não o contrário,
reexportar a prancheta ao dobro da resolução não muda nada no jogo.

**O peso.** Cada peça é atirada conforme o seu próprio tamanho: o que é miúdo
voa mais longe e roda mais depressa, o que é grande tomba pesado. Sai do
`sprite.bounds`, portanto uma peça nova ganha o comportamento certo sozinha.

**As miudezas.** A chuva do `BoltPop` sai da **metade mais pequena** das peças,
ordenadas pelo tamanho — não de uma lista escrita à mão. Ganhar ou perder peças
na prancheta não obriga a acertar nada.

> Nenhum destroço é gerado por código. Os quatro `piece_*.png` e o `bolt.png`
> foram apagados com o código que os desenhava. O `spark.png` e o `ring.png`
> ficam: são fogo, fumo e onda de choque, não peças do avião.

`crashDuration` (1.5 s) é o tempo que a animação tem o ecrã só para si antes do
painel de fim de jogo aparecer. O placar fica visível durante a queda, e um
toque apressado não consegue saltar a animação.

### A pausa antes da desgraça

No instante do embate o mundo pára quase por completo (`Time.timeScale` a 2 %)
durante `hitstopDuration` (0,25 s), o motor cala-se, e o avião abre um par de
olhos enormes virados para quem está a jogar. Só quando a pausa acaba é que
começa tudo o resto — fim de jogo, som do embate, destroços.

É a lei mais velha do desenho animado: primeiro o coiote percebe, só depois é
que cai. Sem a pausa, o embate era um estrondo; com ela, é uma reação.

Os olhos são o `eyes.png`, gerado por código como o `spark` e o `ring`, e vivem
como filho desligado do avião. A largura sai da `PlaceholderArt.EyesW` (0,32
unidades — mais de um quarto do avião inteiro, de propósito: uns olhos à escala
real não se viam num telemóvel). A posição sai do `sprite.bounds` do avião, não
de números fixos, portanto redesenhar o avião não os deixa a flutuar.

A contagem é em **tempo real** (`WaitForSecondsRealtime`): com o `timeScale` no
chão, uma espera normal duraria cinquenta vezes mais do que o número diz. Acima
de 0,4 s deixa de ler-se como susto e passa a parecer que o jogo bloqueou.

### O chapéu cai por último

O `Chapeu_Prancheta 1.png` não entra em nenhuma das quatro variantes: o
`SceneBuilder` tira-o da lista dos destroços antes de a entregar ao
`CrashEffects` e passa-o à parte. Depois da explosão ele é atirado ao ar de
leve, roda devagar e aterra sozinho — pouco mais de um segundo depois, quando
tudo o resto já assentou e ainda dentro do `crashDuration`. É a última coisa que
se mexe no ecrã, e larga um pozinho ao pousar.

A queda não é cronometrada à mão: a corrotina espera que o chapéu chegue ao
chão, porque a altura da batida muda de partida para partida. É o mesmo padrão
do `TrailThenImpact`.

> É a única peça reconhecida **pelo nome** (`SceneBuilder.HatPart`). O resto do
> projeto evita isso, mas aqui não há alternativa — nada no desenho diz que
> aquela peça é um chapéu. Renomeá-la só perde o gag: os destroços voltam a
> tê-la no meio deles, como antes.

## A frase do fim de jogo

Por baixo dos números, o painel mostra uma frase escolhida pelo que acabou de
acontecer (`DeathQuips`). Não é decoração: é o que faz a pessoa ler o painel em
vez de carregar logo, e é a linha que vai parar a um print.

| Situação                    | Exemplo                            |
| --------------------------- | ---------------------------------- |
| Zero pontos                 | `YOU DIDN'T EVEN TAKE OFF.`        |
| Recorde novo, 30+           | `THAT ONE ACTUALLY COUNTED.`       |
| Falhou o recorde por um     | `ONE. YOU WERE ONE SHORT.`         |
| Empatou com o recorde       | `EXACTLY YOUR BEST. NOT ONE MORE.` |
| Morreu no relâmpago         | `THE LIGHTNING WASN'T MOVING.`     |
| 40+                         | `OKAY, THIS IS A PROBLEM NOW.`     |

A ordem dos casos é a graduação da piada: o mais raro e mais específico vem
primeiro. Dentro de cada caso há duas ou três frases, e nunca sai a mesma duas
vezes seguidas — repetida de imediato, a piada não é meia piada, é nenhuma.

Para acrescentar frases, é só o `Assets/Scripts/DeathQuips.cs`: os textos e a
ordem dos casos estão lá, e mais nada no jogo precisa de saber.

## O estendal

Às vezes o prédio 4 traz um estendal no telhado. Não mata nem dá pontos: quem
voa rente ao telhado atravessa-o, o estendal abana, e o avião sai de lá com uma
peça de roupa agarrada à hélice — até ao fim da partida, a esvoaçar. É um
prémio por voar ganancioso, e a razão para ir à procura do próximo.

- **Só no prédio 4.** Foi desenhado em cima do telhado dele. O prédio escolhe-se
  pelo número (`SceneBuilder.ClotheslineTowerNumber`), o mesmo critério que liga
  cada prédio à pasta dos cacos.
- **`clotheslineChance`** (0,4) é a probabilidade de cada vez que o prédio 4
  calha. Como ele é um em cada quatro obstáculos, dá mais ou menos um estendal
  a cada dez.
- **Uma peça de cada vez.** Um estendal novo troca a peça, nunca a mesma duas
  vezes seguidas. Três em cima de um avião de 1,25 unidades eram um borrão.
- **É um trigger, não um colisor**, e a caixa cobre só a zona da roupa: passar
  no ar entre a corda e o telhado sem tocar em pano não veste nada.
- **Na queda** a peça solta-se e voa com os destroços. Se o avião deitar abaixo
  um prédio com estendal, o estendal vai com os cacos em vez de ficar no ar.

### A arte

```
Assets/Art/
├─ EstendalComRoupa.png      o estendal inteiro, para o telhado
└─ RoupaEstendal/            as peças soltas, uma por ficheiro
   ├─ Calca_Ganga.png
   ├─ Cuecas.png
   ├─ Meia.png
   └─ Tshirt_Vermelha.png
```

As duas regras que mais facilmente se partem:

1. **O estendal fica na raiz, a roupa na pasta.** Tudo o que for PNG dentro de
   `RoupaEstendal/` entra no sorteio da hélice — o estendal lá dentro ia parar
   ao nariz do avião.
2. **Nada de sufixo `_Prancheta 1` na raiz.** Um ficheiro na raiz com esse
   sufixo, e que não comece por `predio`, é apanhado como peça do avião e voa
   nos destroços. Dentro da subpasta não há problema: a varredura não entra lá.

Exporta-se a qualquer resolução, mas **todas as peças ao mesmo fator**. O `1c`
dá ao estendal 0,75 unidades de largura (`ClotheslineW`) e à roupa um PPU só,
tirado da maior medida da maior peça (`LaundryH`, 0,36). É assim que a meia
continua mais pequena que as calças sem ninguém escrever tamanhos à mão.

Acrescentar uma peça é exportá-la para a pasta e correr `Flappy → 1c` e
`Flappy → 2`.

## O pombo

De vez em quando um pombo atravessa o céu contra o avião. **Não mata nem dá
pontos** — é perigo falso. Quem lhe bate leva com um engasgo do motor, uma
nuvem de penas e um letreiro (`BIRD STRIKE!`), e o pombo rodopia tonto e foge
a bater as asas, a largar penas pelo caminho.

- **Nasce com um par de obstáculos**, à altura do vão — mesmo na linha em que o
  jogador quer voar. Voa mais depressa que o cenário, na medida certa para
  chegar ao avião a meio caminho entre dois prédios (`pigeonLead`, 0,5), onde
  há folga para recuperar. A conta está no `PigeonSpawner.SpeedFactor` e não
  depende da velocidade do jogo.
- **Raridade:** nunca nos primeiros `pigeonFirstPair` (4) pares, depois
  `pigeonChance` (0,15) por par, com `pigeonCooldownPairs` (4) de espera. Dá
  mais ou menos um pombo a cada doze pares. Um par com estendal nunca traz
  pombo: duas piadas juntas tiram a graça uma à outra.
- **O engasgo:** durante `pigeonStallDuration` (0,4 s) o motor não responde ao
  dedo e o avião afunda com `pigeonStallGravity` (1) — cerca de 0,8 unidades,
  bem menos do que com o dedo levantado. Se o dedo já estava levantado, cai
  como sempre. O fumo segue o `Airplane.EngineRunning`, não o dedo, e o motor
  tosse seis baforadas escuras (`EngineSmoke.Cough`).
- **A culpa:** morrer até `pigeonBlameWindow` (2 s) depois do pombo troca a
  frase do painel por `THE PIGEON WON.` e companhia (ver `DeathQuips`).
- **É um trigger** redondo só do corpo (`bodyRadius`, 0,14): a ponta da asa não
  conta.
- **Na queda** o cenário pára, mas o pombo não — passa por cima dos destroços
  como se nada fosse.

### A arte

O pombo é **desenhado à mão**: dois fotogramas na raiz de `Assets/Art`,
`pigeon_up.png` e `pigeon_down.png`, **virados para a esquerda** e com fundo
transparente. O `Flappy → 1` nunca lhes toca — quem lhes acerta a importação é
o `1c` — e o `1g` volta a pôr o pombo gerado por código. A pena (`feather.png`)
continua gerada.

- **Um PPU só para os dois**, tirado do mais largo, para `PigeonW` (0,55
  unidades, do bico à cauda). Exportados à mesma resolução, ficam do mesmo
  tamanho no jogo seja qual for a resolução.
- **Pivô ao centro da imagem.** É à volta dele que o pombo rodopia e é lá que
  fica o trigger. Exportar os dois da mesma prancheta, com o corpo ao meio,
  impede o corpo de dar saltos ao bater as asas; recortados à justa, cada um
  fica centrado no seu próprio desenho.
- **Nada de sufixo `_Prancheta 1`**: na raiz, é o que faz um PNG ir parar aos
  destroços do avião.

## Gerar o APK

Pelo Editor: menu **Flappy → 4 - Gerar APK**.

Pela linha de comando:

```bash
"C:/Program Files/Unity/Hub/Editor/6000.1.12f1/Editor/Unity.exe" -batchmode -quit -projectPath "C:/Users/ferna/Desktop/flappy-bird-unity" -executeMethod BuildScript.BuildApk -logFile build.log
```

Requer os módulos **Android Build Support + OpenJDK + SDK/NDK** instalados pelo
Unity Hub. Se o build reclamar de SDK/NDK/JDK, confira em
_Edit → Preferences → External Tools_.

## Próximos passos

- Sons (bater asa, ponto, colisão) e animação de 3 quadros do pássaro
- Menu inicial separado e transições
- Ícone do app e splash screen
- Assinatura do APK (keystore) para publicar

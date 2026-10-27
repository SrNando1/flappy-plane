using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Gera os sprites do jogo por código (pixel art simples) e os importa
/// já configurados. Trocar por arte final é só substituir os PNGs em Assets/Art.
/// </summary>
public static class PlaceholderArt
{
    public const float PPU = 50f;
    public const string Dir = "Assets/Art";

    /// <summary>Largura de um tile da faixa de nuvens, em unidades — a mesma do chão.</summary>
    public const float CloudTileW = 6.72f;

    /// <summary>
    /// Largura do avião, em unidades. O desenho à mão é bem mais alongado que o
    /// placeholder (2,9:1 contra 1,7:1), por isso não herda a largura dele: este
    /// valor é o que lhe dá a mesma mancha no ecrã que o placeholder tinha.
    /// </summary>
    public const float PlaneW = 1.25f;

    /// <summary>O A320 da loja, desenhado à mão, na raiz da arte.</summary>
    public const string A320Name = "A320";

    /// <summary>
    /// Largura do A320, em unidades. Mais comprido que a Cessna, mas o desenho
    /// é mais esguio (3,5:1): com 1,5 fica com a mesma altura no ecrã, e o vão
    /// entre prédios custa o mesmo a passar com um avião ou com o outro.
    /// </summary>
    public const float A320W = 1.5f;

    /// <summary>
    /// Largura da parede de um prédio, em unidades — perto da da torre gerada
    /// por código, que era 1,28 de imagem para 1,04 de corpo. Os três prédios
    /// vieram com molduras diferentes (um tem telhado de duas águas a sair para
    /// fora, outro uma antena fina lá em cima), por isso o que se iguala é o
    /// corpo e não a imagem inteira: senão o prédio de telhado largo ficava com
    /// a parede mais estreita que os outros e o vão mudava de prédio para
    /// prédio sem se ver porquê.
    /// </summary>
    public const float TowerBodyW = 1.04f;

    /// <summary>
    /// Largura dos olhos do embate, em unidades — mais de um quarto do avião
    /// inteiro. É suposto serem grandes de mais: uns olhos à escala de um
    /// avião a sério não se veem num telemóvel, e a piada morria por ninguém
    /// dar por ela.
    /// </summary>
    public const float EyesW = 0.32f;

    /// <summary>Largura do desenho dos olhos, em píxeis. Manda no PPU deles.</summary>
    const int EyesPx = 72;

    /// <summary>
    /// Largura do pombo, do bico à ponta da cauda, em unidades — quase metade do
    /// avião. Um pombo a sério ao lado de uma Cessna era um ponto; grande de
    /// mais é o que o faz ler-se num telemóvel, pela mesma razão dos olhos. O
    /// desenho à mão tem bico e cauda compridos, e o corpo fica com menos de
    /// metade disto.
    /// </summary>
    public const float PigeonW = 0.55f;

    /// <summary>Os dois fotogramas do pombo, desenhados à mão, na raiz da arte.</summary>
    static readonly string[] PigeonFrames = { "pigeon_up", "pigeon_down" };

    /// <summary>Largura do desenho do pombo, em píxeis. Manda no PPU dele.</summary>
    const int PigeonPx = 112;

    /// <summary>Comprimento de uma pena solta, em unidades.</summary>
    public const float FeatherW = 0.14f;

    const int FeatherPx = 32;

    /// <summary>
    /// Diâmetro da moeda, em unidades — cerca de um terço do avião. Pequena o
    /// bastante para cinco em linha caberem entre dois prédios.
    /// </summary>
    public const float CoinW = 0.4f;

    const int CoinPx = 40;

    /// <summary>O estendal desenhado à mão, na raiz da arte. Sem sufixo de prancheta.</summary>
    public const string ClotheslineName = "EstendalComRoupa";

    /// <summary>
    /// Largura do estendal, em unidades — uns 70 % da parede de um prédio, a
    /// medida com que foi desenhado em cima do prédio 4. A altura vem atrás,
    /// pela proporção do desenho.
    /// </summary>
    public const float ClotheslineW = 0.75f;

    /// <summary>
    /// As peças de roupa soltas, que podem calhar na hélice. Numa pasta à parte
    /// de propósito: na raiz, uma peça com sufixo de prancheta ia parar aos
    /// destroços do avião, e o estendal inteiro ia parar à hélice.
    /// </summary>
    public const string LaundryDir = Dir + "/RoupaEstendal";

    /// <summary>
    /// A maior medida da maior peça, em unidades, já presa à hélice. Com 0,36
    /// as calças fazem um terço do nariz e as cuecas ficam com 0,26 — abaixo de
    /// 0,3 as cuecas eram um selo no nariz, que num telemóvel ninguém lia.
    /// </summary>
    public const float LaundryH = 0.36f;

    static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    static readonly Color32 Ink = new Color32(38, 40, 48, 255);

    [MenuItem("Flappy/1 - Gerar arte placeholder")]
    public static void Generate()
    {
        Directory.CreateDirectory(Dir);

        Save("tower", BuildTower(), new Vector2(0.5f, 1f));   // pivô no telhado
        Save("ground", BuildGround(), null);
        Save("bushes", BuildBushes(), null);

        // efeitos de queda
        // fogo e onda de choque esticam dezenas de vezes: filtro suave, não Point
        Save("spark", BuildSpark(), null, true);
        Save("ring", BuildRing(), null, true);

        // os olhos são redondos e o avião é arte lisa: filtro suave, e o PPU
        // sai da largura que se quer no mundo, como no resto da arte à mão
        Save("eyes", BuildEyes(), null, true, EyesPx / EyesW);

        // a pena que o pombo larga. O pombo em si é desenhado à mão e o
        // gerador não lhe toca — para o de código, é o 1g
        Save("feather", BuildFeather(), null, true, FeatherPx / FeatherW);

        // a moeda que se apanha entre os obstáculos
        Save("coin", BuildCoin(), null, true, CoinPx / CoinW);

        // os destroços já não são gerados: são as peças vetorizadas à mão,
        // importadas pelo 1c (ver PlanePartPaths)

        AssetDatabase.Refresh();
        Debug.Log("[Flappy] Arte placeholder gerada em " + Dir);
    }

    /// <summary>
    /// Arte desenhada à mão: o gerador nunca lhe toca, mas as definições de
    /// importação têm de estar certas na mesma. O pivô do raio fica na ponta
    /// de baixo, que é o que encosta à borda do vão; o da faixa de nuvens fica
    /// ao centro, como o chão.
    /// </summary>
    [MenuItem("Flappy/1c - Ajustar importação da arte manual")]
    public static void ConfigureHandDrawn()
    {
        // o raio tem PPU fixo: o tamanho dele não interessa, o ObstaclePair
        // estica-o na vertical de qualquer maneira
        ImportHandDrawn("lightning", 200f, new Vector2(0.5f, 0f), "o obstáculo de cima");
        ImportCloud();
        ImportPlane();
        ImportPlaneParts();
        ImportTowers();
        ImportClothesline();
        ImportLaundry();
        ImportPigeon();
        ImportShopPlane(A320Name, A320W);
    }

    /// <summary>
    /// Um avião da loja. O PPU sai da largura que se quer no mundo, medida no
    /// sprite e não na textura, pelo motivo do pombo: o desenho vem do
    /// Illustrator com mais de 2048 px e a textura sai encolhida pelo limite.
    /// </summary>
    static void ImportShopPlane(string name, float width)
    {
        const float probePpu = 100f;
        const int maxSize = 1024;   // no ecrã, mesmo na montra da loja, não passa disto
        var center = new Vector2(0.5f, 0.5f);
        string path = Dir + "/" + name + ".png";

        if (!ImportHandDrawn(name, probePpu, center, "a loja fica sem esse avião")) return;

        ApplyImportSettings(path, probePpu, center, true, maxSize);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) return;

        float ppu = probePpu * sprite.bounds.size.x / width;
        ApplyImportSettings(path, ppu, center, true, maxSize);
        Debug.Log("[Flappy] " + name + ".png a " + ppu.ToString("0.#") + " PPU → " + width.ToString("0.##") + " unidades de largura.");
    }

    /// <summary>
    /// Sufixo com que o Illustrator carimba tudo o que sai da prancheta 1
    /// (<c>Corpo_Prancheta 1.png</c>, <c>Elice Cima_Prancheta 1.png</c>, ...).
    /// É por ele que as peças do avião são reconhecidas.
    /// </summary>
    public const string PartSuffix = "_Prancheta 1";

    /// <summary>
    /// Os prédios saem da mesma prancheta que as peças do avião e trazem o
    /// mesmo sufixo — é o nome que os separa. Entram no jogo por ordem
    /// alfabética (<c>predio 1</c>, <c>predio 2</c>, <c>predio 3</c>), por isso
    /// um décimo prédio teria de se chamar <c>predio 04</c> e companhia.
    /// </summary>
    public const string TowerPrefix = "predio";

    /// <summary>
    /// Os cacos de cada prédio, cortados no Illustrator e exportados um a um,
    /// vivem em <c>Assets/Art/Predio&lt;n&gt;Destrocos/1x</c> — a pasta é o que
    /// liga os cacos ao prédio de onde saíram. Ficam fora da <see cref="Dir"/>
    /// raiz de propósito: não trazem o <see cref="PartSuffix"/>, para não
    /// entrarem na roda dos obstáculos nem nos destroços do avião. Um prédio
    /// sem pasta simplesmente não desaba.
    /// </summary>
    public static string DebrisDir(string towerAssetName)
    {
        string number = TowerNumber(towerAssetName);
        return number.Length == 0 ? null : Dir + "/Predio" + number + "Destrocos/1x";
    }

    /// <summary>
    /// O número do prédio, tirado do nome do asset — <c>predio 3_Prancheta 1</c>
    /// dá <c>"3"</c>. Pára no primeiro caracter que não é dígito, senão o "1" do
    /// sufixo da prancheta entrava na conta.
    /// </summary>
    public static string TowerNumber(string towerAssetName)
    {
        if (string.IsNullOrEmpty(towerAssetName)) return "";

        var digits = new System.Text.StringBuilder();
        foreach (char c in towerAssetName)
        {
            if (char.IsDigit(c)) digits.Append(c);
            else if (digits.Length > 0) break;
        }

        return digits.ToString();
    }

    /// <summary>
    /// As peças vetorizadas em que o avião se desfaz, por ordem alfabética.
    /// Não há lista escrita em lado nenhum: exportar mais uma peça da mesma
    /// prancheta chega para ela entrar nos destroços — menos os prédios, que
    /// partilham a prancheta mas são obstáculos, não fuselagem.
    /// </summary>
    public static string[] PlanePartPaths()
    {
        return PartPaths(false);
    }

    /// <summary>Os prédios que fazem de torre, por ordem alfabética.</summary>
    public static string[] TowerPartPaths()
    {
        return PartPaths(true);
    }

    /// <summary>
    /// Os cacos de um prédio, por ordem de nome. A ordem não corresponde à
    /// posição que cada caco tinha na parede — é só a da exportação — mas como
    /// são todos pedaços da mesma fachada, tanto faz qual cai de onde.
    /// Exportar mais um caco para a pasta chega para ele entrar no desabamento.
    /// </summary>
    public static string[] DebrisPaths(string towerAssetName)
    {
        string dir = DebrisDir(towerAssetName);
        if (dir == null || !Directory.Exists(dir)) return new string[0];

        var paths = Directory.GetFiles(dir, "*.png");
        for (int i = 0; i < paths.Length; i++) paths[i] = paths[i].Replace('\\', '/');

        System.Array.Sort(paths, System.StringComparer.Ordinal);
        return paths;
    }

    static string[] PartPaths(bool towers)
    {
        if (!Directory.Exists(Dir)) return new string[0];

        var picked = new List<string>();
        foreach (var file in Directory.GetFiles(Dir, "*" + PartSuffix + ".png"))
        {
            string path = file.Replace('\\', '/');
            bool isTower = Path.GetFileName(path)
                               .StartsWith(TowerPrefix, System.StringComparison.OrdinalIgnoreCase);

            if (isTower == towers) picked.Add(path);
        }

        var paths = picked.ToArray();
        System.Array.Sort(paths, System.StringComparer.Ordinal);
        return paths;
    }

    /// <summary>
    /// As peças saíram todas da mesma prancheta, à mesma resolução, por isso
    /// partilham um PPU só — é o que mantém a hélice fina ao lado da fuselagem
    /// gorda. O número sai da **peça mais larga** (a fuselagem), que fica com a
    /// largura do avião inteiro: reexportar a prancheta ao dobro da resolução
    /// não muda nada no jogo, e não depende de qual `plane.png` está instalado.
    /// </summary>
    static void ImportPlaneParts()
    {
        var paths = PlanePartPaths();
        if (paths.Length == 0)
        {
            Debug.LogWarning("[Flappy] Nenhuma peça '*" + PartSuffix + ".png' em " + Dir +
                             " — o avião não se vai desfazer em nada.");
            return;
        }

        // primeira passagem: entrar na base de dados para o tamanho ser legível
        foreach (var path in paths)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        int widest = 0;
        foreach (var path in paths)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) widest = Mathf.Max(widest, tex.width);
        }

        if (widest == 0)
        {
            Debug.LogError("[Flappy] Não consegui ler o tamanho de nenhuma peça em " + Dir + ".");
            return;
        }

        float ppu = widest / PlaneW;
        foreach (var path in paths)
        {
            ApplyImportSettings(path, ppu, new Vector2(0.5f, 0.5f), true, 2048);
        }

        Debug.Log("[Flappy] " + paths.Length + " peças do avião a " + ppu.ToString("0.#") +
                  " PPU (a mais larga tem " + widest + " px → " + PlaneW.ToString("0.##") + " unidades).");
    }

    /// <summary>
    /// Os prédios que fazem de torre. Cada um vem com a sua moldura — telhado
    /// de duas águas, antena, cornija — por isso o PPU sai da largura do
    /// <b>corpo</b> e não da imagem: assim os três aparecem com paredes da
    /// mesma largura, e é a parede que o jogador tem de contornar. O pivô fica no
    /// meio do topo, como no tower.png que substituem, porque é por lá que o
    /// ObstaclePair os pendura na borda do vão.
    /// </summary>
    static void ImportTowers()
    {
        var paths = TowerPartPaths();
        if (paths.Length == 0)
        {
            Debug.LogWarning("[Flappy] Nenhum prédio '" + TowerPrefix + "*" + PartSuffix + ".png' em " +
                             Dir + " — os obstáculos ficam com a torre gerada por código.");
            return;
        }

        foreach (var path in paths)
        {
            int body = BodyWidthPixels(path);
            if (body == 0)
            {
                Debug.LogError("[Flappy] " + path + " parece vazio — não dá para medir o corpo do prédio.");
                continue;
            }

            float ppu = body / TowerBodyW;
            ApplyImportSettings(path, ppu, new Vector2(0.5f, 1f), true, 2048);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Debug.Log("[Flappy] " + Path.GetFileName(path) + ": corpo de " + body + " px a " +
                      ppu.ToString("0.#") + " PPU → " +
                      (sprite != null ? sprite.bounds.size.ToString() : "?") + " unidades.");

            ImportDebris(Path.GetFileNameWithoutExtension(path), ppu);
        }
    }

    /// <summary>
    /// Os cacos de um prédio herdam o PPU do prédio de onde foram cortados —
    /// saíram da mesma prancheta à mesma resolução, portanto é o mesmo número
    /// ou saem com o tamanho errado. É por isto que não há PPU escrito à mão em
    /// lado nenhum: cortar cacos de um prédio novo e correr o <c>1c</c> chega.
    /// O pivô vai ao centro, para cada caco rodar sobre si próprio quando voa.
    /// </summary>
    static void ImportDebris(string towerAssetName, float ppu)
    {
        var paths = DebrisPaths(towerAssetName);
        if (paths.Length == 0) return;   // prédio sem cacos cortados: não desaba

        foreach (var path in paths)
        {
            ApplyImportSettings(path, ppu, new Vector2(0.5f, 0.5f), true, 2048);
        }

        Debug.Log("[Flappy] " + paths.Length + " cacos de " + towerAssetName + " a " +
                  ppu.ToString("0.#") + " PPU.");
    }

    /// <summary>
    /// Largura do corpo do prédio, em pixels: a <b>mediana</b> das linhas com
    /// tinta. É a mediana e não o máximo porque o telhado sai para fora e a
    /// antena é um risco fino — qualquer um deles daria um número que não é o
    /// da parede. Lê o PNG do disco em vez do asset importado para não precisar
    /// de o marcar como legível só para isto.
    /// </summary>
    static int BodyWidthPixels(string path)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(path)))
        {
            Object.DestroyImmediate(tex);
            return 0;
        }

        var px = tex.GetPixels32();
        int w = tex.width, h = tex.height;
        Object.DestroyImmediate(tex);

        var spans = new List<int>();
        for (int y = 0; y < h; y++)
        {
            int first = -1, last = -1;
            for (int x = 0; x < w; x++)
            {
                if (px[y * w + x].a <= 20) continue;
                if (first < 0) first = x;
                last = x;
            }

            if (first >= 0) spans.Add(last - first + 1);
        }

        if (spans.Count == 0) return 0;

        spans.Sort();
        return spans[spans.Count / 2];
    }

    /// <summary>
    /// O avião tem de medir sempre <see cref="PlaneW"/> unidades de largura,
    /// senão deixa de bater certo com a cápsula do colisor, que é fixa no
    /// SceneBuilder. Por isso o PPU sai da largura do desenho, e não o
    /// contrário: podes reexportá-lo do Illustrator em qualquer resolução
    /// que o tamanho no mundo não muda.
    /// </summary>
    static void ImportPlane()
    {
        string path = Dir + "/plane.png";
        if (!ImportHandDrawn("plane", PPU, new Vector2(0.5f, 0.5f), "o avião")) return;

        // só depois da primeira passagem é que a textura está na base de dados
        // no tamanho inteiro — antes disso a largura podia vir cortada pelo
        // limite antigo e o PPU sairia errado
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return;

        float ppu = tex.width / PlaneW;
        ApplyImportSettings(path, ppu, new Vector2(0.5f, 0.5f), true, 2048);
        Debug.Log("[Flappy] plane.png: " + tex.width + "×" + tex.height + " px a "
                  + ppu.ToString("0.#") + " PPU → " + PlaneW.ToString("0.##") + " × "
                  + (tex.height / ppu).ToString("0.##") + " unidades.");
    }

    /// <summary>
    /// O estendal. O PPU sai da largura do desenho, como no avião, para
    /// reexportar noutra resolução não mudar nada no jogo. O pivô fica no meio
    /// da base, onde os pés dos postes pousam no telhado.
    /// </summary>
    static void ImportClothesline()
    {
        string path = Dir + "/" + ClotheslineName + ".png";
        var pivot = new Vector2(0.5f, 0f);
        if (!ImportHandDrawn(ClotheslineName, PPU, pivot, "o estendal")) return;

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return;

        float ppu = tex.width / ClotheslineW;
        ApplyImportSettings(path, ppu, pivot, true, 2048);
        Debug.Log("[Flappy] " + ClotheslineName + ".png: " + tex.width + "×" + tex.height + " px a "
                  + ppu.ToString("0.#") + " PPU → " + ClotheslineW.ToString("0.##") + " × "
                  + (tex.height / ppu).ToString("0.##") + " unidades.");
    }

    /// <summary>
    /// Os dois fotogramas do pombo. Um PPU só para os dois, tirado do mais largo:
    /// saem da mesma prancheta à mesma resolução, por isso é o mesmo PPU que os
    /// deixa do mesmo tamanho, mesmo que o recorte de cada um não bata certo.
    ///
    /// A largura mede-se no próprio sprite, e não na textura: o pombo vem do
    /// Illustrator com mais de 2048 px, a textura sai encolhida pelo limite, e
    /// a largura dela já não diria o tamanho do desenho.
    /// </summary>
    static void ImportPigeon()
    {
        const float probePpu = 100f;
        const int maxSize = 1024;   // no ecrã o pombo mal chega aos 150 px
        var center = new Vector2(0.5f, 0.5f);

        float widest = 0f;
        foreach (var name in PigeonFrames)
        {
            if (!ImportHandDrawn(name, probePpu, center, "o pombo")) return;

            ApplyImportSettings(Dir + "/" + name + ".png", probePpu, center, true, maxSize);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "/" + name + ".png");
            if (sprite != null) widest = Mathf.Max(widest, sprite.bounds.size.x);
        }

        if (widest <= 0f) return;

        float ppu = probePpu * widest / PigeonW;
        foreach (var name in PigeonFrames)
        {
            ApplyImportSettings(Dir + "/" + name + ".png", ppu, center, true, maxSize);
        }

        Debug.Log("[Flappy] pombo a " + ppu.ToString("0.#") + " PPU → " + PigeonW.ToString("0.##") + " unidades de largura.");
    }

    /// <summary>
    /// A roupa solta. Um PPU só para todas, tirado da maior medida da maior
    /// peça — como nas peças do avião, é assim que a meia continua mais pequena
    /// que as calças sem ninguém escrever tamanhos à mão. Pivô ao centro, para
    /// rodarem direito quando voam nos destroços.
    /// </summary>
    static void ImportLaundry()
    {
        var paths = LaundryPaths();
        if (paths.Length == 0)
        {
            Debug.LogWarning("[Flappy] Nenhuma peça em " + LaundryDir + " — o estendal abana mas não veste a hélice.");
            return;
        }

        foreach (var path in paths)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        int biggest = 0;
        foreach (var path in paths)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) biggest = Mathf.Max(biggest, Mathf.Max(tex.width, tex.height));
        }

        if (biggest == 0)
        {
            Debug.LogError("[Flappy] Não consegui ler o tamanho de nenhuma peça em " + LaundryDir + ".");
            return;
        }

        float ppu = biggest / LaundryH;
        foreach (var path in paths)
        {
            ApplyImportSettings(path, ppu, new Vector2(0.5f, 0.5f), true, 2048);
        }

        Debug.Log("[Flappy] " + paths.Length + " peças de roupa a " + ppu.ToString("0.#") + " PPU.");
    }

    /// <summary>As peças de roupa, por ordem de nome. Tudo o que for PNG na pasta conta.</summary>
    public static string[] LaundryPaths()
    {
        if (!Directory.Exists(LaundryDir)) return new string[0];

        var paths = Directory.GetFiles(LaundryDir, "*.png");
        for (int i = 0; i < paths.Length; i++) paths[i] = paths[i].Replace('\\', '/');

        System.Array.Sort(paths, System.StringComparer.Ordinal);
        return paths;
    }

    /// <summary>
    /// A faixa de nuvens tem de medir sempre <see cref="CloudTileW"/> unidades
    /// de largura, senão a emenda do ciclo abre buracos. Por isso o PPU sai da
    /// largura do desenho, e não o contrário: podes redesenhá-la em qualquer
    /// resolução que o tile continua do mesmo tamanho no mundo.
    /// </summary>
    static void ImportCloud()
    {
        string path = Dir + "/cloud.png";
        if (!ImportHandDrawn("cloud", 100f, new Vector2(0.5f, 0.5f), "a faixa de nuvens")) return;

        // só depois da primeira passagem é que a textura está na base de dados
        // no tamanho inteiro — antes disso a largura podia vir cortada pelo
        // limite antigo e o PPU sairia errado
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return;

        ApplyImportSettings(path, tex.width / CloudTileW, new Vector2(0.5f, 0.5f), true, 2048);
        Debug.Log("[Flappy] cloud.png: " + tex.width + "×" + tex.height + " px a "
                  + (tex.width / CloudTileW).ToString("0.#") + " PPU.");
    }

    /// <summary>Importa um PNG manual com filtro suave e textura até 2048 px.</summary>
    static bool ImportHandDrawn(string name, float ppu, Vector2 pivot, string what)
    {
        string path = Dir + "/" + name + ".png";

        if (!File.Exists(path))
        {
            Debug.LogWarning("[Flappy] " + path + " não existe — " + what + " vai ficar sem sprite.");
            return false;
        }

        // força a entrada na base de dados antes de mexer nas definições:
        // um ficheiro copiado para a pasta com o Editor aberto pode ainda
        // não ter importador nenhum
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        ApplyImportSettings(path, ppu, pivot, true, 2048);

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogError("[Flappy] " + name + ".png continua a não ser um Sprite. Acerte à mão no Inspector.");
            return false;
        }

        Debug.Log("[Flappy] " + name + ".png pronto: " + sprite.bounds.size + " unidades.");
        return true;
    }

    /// <summary>
    /// Repõe a faixa de nuvens gerada por código. A do jogo é desenhada à mão —
    /// este item existe para quem não a tiver, ou a quiser de volta.
    /// </summary>
    [MenuItem("Flappy/1d - Repor nuvem placeholder")]
    public static void GenerateClouds()
    {
        Directory.CreateDirectory(Dir);
        Save("cloud", BuildClouds(), new Vector2(0.5f, 0.5f));   // pivô ao centro, como o chão
        AssetDatabase.Refresh();
        Debug.Log("[Flappy] cloud.png reposto com a faixa gerada por código.");
    }

    /// <summary>
    /// Repõe o avião gerado por código. O do jogo é desenhado à mão —
    /// este item existe para quem não o tiver, ou o quiser de volta.
    /// </summary>
    [MenuItem("Flappy/1e - Repor avião placeholder")]
    public static void GeneratePlane()
    {
        Directory.CreateDirectory(Dir);
        Save("plane", BuildPlane(), null);
        AssetDatabase.Refresh();
        Debug.Log("[Flappy] plane.png reposto com o avião gerado por código.");
    }

    /// <summary>
    /// Repõe o pombo gerado por código. O do jogo é desenhado à mão — este item
    /// existe para quem não o tiver, ou o quiser de volta.
    /// </summary>
    [MenuItem("Flappy/1g - Repor pombo placeholder")]
    public static void GeneratePigeon()
    {
        Directory.CreateDirectory(Dir);
        Save(PigeonFrames[0], BuildPigeon(true), null, true, PigeonPx / PigeonW);
        Save(PigeonFrames[1], BuildPigeon(false), null, true, PigeonPx / PigeonW);
        AssetDatabase.Refresh();
        Debug.Log("[Flappy] pigeon_up.png e pigeon_down.png repostos com o pombo gerado por código.");
    }

    // ---------------------------------------------------------------- sprites

    static Texture2D BuildPlane()
    {
        const int w = 48, h = 28;
        var px = NewBuffer(w, h);

        var body = new Color32(228, 74, 62, 255);
        var bodyDark = new Color32(178, 46, 40, 255);
        var wing = new Color32(198, 58, 50, 255);
        var glass = new Color32(168, 226, 252, 255);
        var metal = new Color32(206, 210, 220, 255);

        // leme vertical, atrás
        for (int y = 14; y < 27; y++)
        for (int x = 3; x < 15; x++)
        {
            if (y - 14 > (14 - x) * 1.6f) continue;
            px[y * w + x] = bodyDark;
        }

        // estabilizador horizontal
        for (int y = 12; y <= 16; y++)
        for (int x = 2; x <= 11; x++)
        {
            px[y * w + x] = bodyDark;
        }

        // fuselagem
        FillEllipse(px, w, h, 23f, 14f, 20f, 5.2f, body, body);

        // asa principal, projetada para baixo
        for (int y = 4; y <= 13; y++)
        for (int x = 16; x <= 34; x++)
        {
            if (x - 16 < (13 - y) * 1.1f) continue;
            px[y * w + x] = wing;
        }

        // faixa branca ao longo da fuselagem
        for (int y = 14; y <= 15; y++)
        for (int x = 8; x <= 36; x++)
        {
            if (px[y * w + x].a == 0) continue;
            px[y * w + x] = metal;
        }

        // cockpit
        FillEllipse(px, w, h, 30f, 17.5f, 5f, 2.8f, glass, glass);

        // cone e hélice
        for (int y = 11; y <= 17; y++)
        for (int x = 41; x <= 44; x++)
        {
            px[y * w + x] = metal;
        }
        for (int y = 3; y <= 25; y++)
        {
            px[y * w + 45] = Ink;
            px[y * w + 46] = Ink;
        }

        Outline(px, w, h, body, bodyDark, wing, glass, metal);
        return ToTexture(px, w, h);
    }

    static Texture2D BuildTower()
    {
        const int w = 64, h = 400, roofH = 26;
        var px = NewBuffer(w, h);

        var wall = new Color32(96, 108, 134, 255);
        var wallLight = new Color32(128, 140, 166, 255);
        var wallDark = new Color32(68, 78, 100, 255);
        var roof = new Color32(74, 84, 108, 255);
        var winOn = new Color32(250, 214, 122, 255);
        var winOff = new Color32(58, 68, 92, 255);

        // corpo do prédio
        for (int y = 0; y < h - roofH; y++)
        for (int x = 4; x <= 59; x++)
        {
            Color32 c = wall;
            if (x >= 8 && x <= 14) c = wallLight;
            else if (x >= 50 && x <= 56) c = wallDark;
            if (x <= 5 || x >= 58) c = Ink;
            px[y * w + x] = c;
        }

        // janelas: 4 colunas, acesas de forma determinística
        for (int row = 0; row * 22 + 20 < h - roofH; row++)
        for (int col = 0; col < 4; col++)
        {
            bool on = Hash(row, col) % 5 != 0;
            int x0 = 10 + col * 12;
            int y0 = 8 + row * 22;

            for (int y = y0; y < y0 + 12; y++)
            for (int x = x0; x < x0 + 8; x++)
            {
                px[y * w + x] = on ? winOn : winOff;
            }
        }

        // telhado, mais largo (no topo da textura = pivô)
        for (int y = h - roofH; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            Color32 c = roof;
            if (x >= 4 && x <= 10) c = wallLight;
            if (x <= 1 || x >= 62 || y >= h - 2 || y <= h - roofH + 1) c = Ink;
            px[y * w + x] = c;
        }

        return ToTexture(px, w, h);
    }

    static Texture2D BuildGround()
    {
        const int w = 336, h = 56;
        var px = NewBuffer(w, h);

        var grassLight = new Color32(160, 226, 96, 255);
        var grass = new Color32(110, 190, 60, 255);
        var sand = new Color32(222, 216, 149, 255);
        var sandStripe = new Color32(210, 202, 118, 255);
        var sandDark = new Color32(190, 180, 110, 255);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            Color32 c;
            if (y >= h - 4) c = grassLight;
            else if (y >= h - 13) c = grass;
            else if (y >= h - 16) c = Ink;
            else if (y <= 2) c = sandDark;
            else c = ((x + y) / 9) % 2 == 0 ? sand : sandStripe;

            px[y * w + x] = c;
        }

        return ToTexture(px, w, h);
    }

    static Texture2D BuildBushes()
    {
        const int w = 336, h = 48;
        var px = NewBuffer(w, h);

        var bush = new Color32(88, 176, 108, 255);
        var bushLight = new Color32(126, 208, 138, 255);

        for (int x = 0; x < w; x++)
        {
            // ondas somadas para uma silhueta irregular que fecha no loop
            float t = x / (float)w * Mathf.PI * 2f;
            float top = 20f
                      + Mathf.Sin(t * 4f) * 7f
                      + Mathf.Sin(t * 9f + 1.3f) * 4f
                      + Mathf.Sin(t * 15f + 0.7f) * 2.5f;

            int topY = Mathf.Clamp(Mathf.RoundToInt(top), 4, h - 1);
            for (int y = 0; y <= topY; y++)
            {
                px[y * w + x] = y >= topY - 3 ? bushLight : bush;
            }
        }

        return ToTexture(px, w, h);
    }

    /// <summary>
    /// Faixa de nuvens de tempestade para o topo do ecrã: é de dentro dela que
    /// os relâmpagos nascem. Emenda em ciclo como o chão — a onda da silhueta
    /// usa frequências inteiras e as bolhas do fundo dão a volta pela borda.
    /// </summary>
    static Texture2D BuildClouds()
    {
        const int w = 336, h = 88;
        const int puffs = 15;
        var px = NewBuffer(w, h);

        var light = new Color32(156, 164, 192, 255);
        var mid = new Color32(116, 124, 154, 255);
        var dark = new Color32(82, 90, 120, 255);
        var edge = new Color32(54, 60, 84, 255);

        // bolhas penduradas na linha de baixo: sem elas a nuvem fica uma
        // onda lisa, que não parece nuvem nenhuma
        var cx = new float[puffs];
        var cy = new float[puffs];
        var cr = new float[puffs];

        for (int i = 0; i < puffs; i++)
        {
            cx[i] = (i + 0.5f) * w / puffs + (int)(Hash(i, 1) % 11) - 5;
            cr[i] = 6 + (int)(Hash(i, 2) % 11);
            cy[i] = CloudBase(cx[i], w) + cr[i] * 0.45f;
        }

        var mask = new bool[w * h];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (y >= CloudBase(x, w))
            {
                mask[y * w + x] = true;
                continue;
            }

            for (int i = 0; i < puffs; i++)
            {
                float dx = Mathf.Abs(x - cx[i]);
                dx = Mathf.Min(dx, w - dx);      // a bolha da ponta continua do outro lado
                float dy = y - cy[i];
                if (dx * dx + dy * dy <= cr[i] * cr[i])
                {
                    mask[y * w + x] = true;
                    break;
                }
            }
        }

        // tom pela espessura por baixo do pixel: o rebordo virado para o chão
        // escurece, o miolo alto fica iluminado
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (!mask[y * w + x]) continue;

            int depth = 0;
            while (depth < 8 && y - depth >= 0 && mask[(y - depth) * w + x]) depth++;

            Color32 c;
            if (depth <= 2) c = edge;
            else if (depth <= 7) c = dark;
            else c = y >= CloudInner(x, w) ? light : mid;

            px[y * w + x] = c;
        }

        return ToTexture(px, w, h);
    }

    /// <summary>Silhueta de baixo da faixa de nuvens, em pixels.</summary>
    static float CloudBase(float x, int w)
    {
        float t = x / w * Mathf.PI * 2f;
        return 22f
             + Mathf.Sin(t * 3f) * 9f
             + Mathf.Sin(t * 7f + 2.1f) * 5f
             + Mathf.Sin(t * 13f + 0.9f) * 2.5f;
    }

    /// <summary>Linha onde o corpo da nuvem passa a topo iluminado.</summary>
    static float CloudInner(float x, int w)
    {
        float t = x / w * Mathf.PI * 2f;
        return CloudBase(x, w) + 26f
             + Mathf.Sin(t * 5f + 0.4f) * 5f
             + Mathf.Sin(t * 11f + 1.7f) * 2.5f;
    }

    /// <summary>
    /// Bolha branca com bordo esbatido. É tingida em tempo de execução para
    /// servir de fogo, brasa e fumo.
    /// </summary>
    static Texture2D BuildSpark()
    {
        const int w = 64, h = 64;
        var px = NewBuffer(w, h);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = (x - 31.5f) / 31.5f;
            float dy = (y - 31.5f) / 31.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > 1f) continue;

            byte a = (byte)Mathf.RoundToInt(Mathf.SmoothStep(255f, 0f, Mathf.InverseLerp(0.35f, 1f, d)));
            px[y * w + x] = new Color32(255, 255, 255, a);
        }

        return ToTexture(px, w, h);
    }

    /// <summary>
    /// Os olhos arregalados do instante do embate. O avião é um desenho a
    /// sério, sem cara nenhuma — sem isto não há como ele reagir, e é a reação
    /// dele que faz rir, não o estrondo. As pupilas ficam desencontradas do
    /// centro de propósito: um olhar torto lê-se como pânico, um olhar
    /// centrado lê-se como boneco.
    /// </summary>
    static Texture2D BuildEyes()
    {
        const int w = EyesPx, h = 44;
        var px = NewBuffer(w, h);

        var white = new Color32(255, 255, 255, 255);

        // dois píxeis de intervalo entre os brancos: é o que basta para o
        // Outline meter lá uma linha e os olhos não virarem um borrão só
        FillEllipse(px, w, h, 19f, 22f, 16f, 18f, white, white);
        FillEllipse(px, w, h, 53f, 22f, 16f, 18f, white, white);

        // pupilas pequenas e empurradas para a frente do avião — para onde ele
        // ia bater, e para baixo, que é onde a coisa vai acabar
        FillEllipse(px, w, h, 25f, 19f, 5f, 6f, Ink, Ink);
        FillEllipse(px, w, h, 59f, 19f, 5f, 6f, Ink, Ink);

        Outline(px, w, h, white);
        return ToTexture(px, w, h);
    }

    /// <summary>
    /// Um pombo de perfil, virado para a esquerda — voa contra o avião. O olho
    /// é grande, laranja e arregalado de propósito: o pombo vê o avião a vir e
    /// não faz nada, e é essa cara que tem graça.
    /// </summary>
    /// <param name="wingsUp">Qual dos dois fotogramas do bater de asa.</param>
    static Texture2D BuildPigeon(bool wingsUp)
    {
        const int w = PigeonPx, h = 76;
        var px = NewBuffer(w, h);

        var tail = new Color32(118, 124, 138, 255);
        var tailTip = new Color32(68, 72, 84, 255);
        var body = new Color32(156, 162, 176, 255);
        var belly = new Color32(190, 194, 204, 255);
        var neckGreen = new Color32(90, 150, 126, 255);
        var neckPurple = new Color32(128, 98, 150, 255);
        var head = new Color32(104, 110, 128, 255);
        var beak = new Color32(64, 60, 66, 255);
        var cere = new Color32(236, 232, 226, 255);
        var eyeRing = new Color32(242, 142, 44, 255);
        var feet = new Color32(206, 112, 122, 255);
        var wing = new Color32(178, 184, 196, 255);
        var wingTip = new Color32(78, 82, 94, 255);
        var white = new Color32(255, 255, 255, 255);

        // de trás para a frente: o que é desenhado depois tapa o que já lá está
        // a cauda é uma cunha a abrir para trás, com a barra escura na ponta
        var tailShape = new[] { new Vector2(70f, 41f), new Vector2(70f, 31f), new Vector2(107f, 34f), new Vector2(107f, 45f) };
        FillConvex(px, w, h, tailShape, tail);
        FillConvex(px, w, h, new[]
        {
            Vector2.Lerp(tailShape[0], tailShape[3], 0.75f),
            Vector2.Lerp(tailShape[1], tailShape[2], 0.75f),
            tailShape[2],
            tailShape[3],
        }, tailTip);

        // as patas penduradas ficam por baixo do corpo: só a ponta espreita
        FillEllipse(px, w, h, 54f, 17f, 5f, 4f, feet, feet);
        FillEllipse(px, w, h, 66f, 17f, 5f, 4f, feet, feet);

        FillEllipse(px, w, h, 60f, 34f, 30f, 16f, body, body);
        FillEllipse(px, w, h, 56f, 27f, 20f, 8f, belly, belly);

        FillEllipse(px, w, h, 34f, 42f, 11f, 10f, neckGreen, neckGreen);
        FillEllipse(px, w, h, 39f, 36f, 6f, 5f, neckPurple, neckPurple);
        FillEllipse(px, w, h, 24f, 51f, 12f, 11f, head, head);

        FillConvex(px, w, h, new[] { new Vector2(14f, 53f), new Vector2(3f, 48f), new Vector2(14f, 45f) }, beak);
        FillEllipse(px, w, h, 15f, 52f, 3f, 2f, cere, cere);

        FillEllipse(px, w, h, 22f, 54f, 6f, 6f, eyeRing, eyeRing);
        FillEllipse(px, w, h, 22f, 54f, 4f, 4f, white, white);
        FillEllipse(px, w, h, 20f, 54f, 2.2f, 2.4f, Ink, Ink);

        // a asa sai do ombro e vai à ponta; o último terço é escuro, como nos
        // pombos a sério, e é o que faz o bater de asa ver-se de longe
        var shoulderFront = new Vector2(50f, wingsUp ? 40f : 34f);
        var shoulderBack = new Vector2(78f, wingsUp ? 40f : 34f);
        var tipBack = wingsUp ? new Vector2(86f, 74f) : new Vector2(84f, 2f);
        var tipFront = wingsUp ? new Vector2(66f, 72f) : new Vector2(64f, 4f);

        FillConvex(px, w, h, new[] { shoulderFront, shoulderBack, tipBack, tipFront }, wing);
        FillConvex(px, w, h, new[]
        {
            Vector2.Lerp(shoulderFront, tipFront, 0.66f),
            Vector2.Lerp(shoulderBack, tipBack, 0.66f),
            tipBack,
            tipFront,
        }, wingTip);

        OutlineAll(px, w, h);
        return ToTexture(px, w, h);
    }

    /// <summary>Uma pena: oval clara com o cálamo ao meio.</summary>
    static Texture2D BuildFeather()
    {
        const int w = FeatherPx, h = 14;
        var px = NewBuffer(w, h);

        var vane = new Color32(236, 238, 242, 255);
        var shaft = new Color32(168, 172, 184, 255);

        FillEllipse(px, w, h, 16f, 7f, 13f, 4.5f, vane, vane);
        for (int x = 3; x < 29; x++) px[7 * w + x] = shaft;

        OutlineAll(px, w, h);
        return ToTexture(px, w, h);
    }

    /// <summary>
    /// Uma moeda de ouro, redonda e simples: aro escuro, disco dourado, anel
    /// gravado ao meio e um brilho no canto — o mínimo que já se lê como moeda.
    /// </summary>
    static Texture2D BuildCoin()
    {
        const int w = CoinPx, h = CoinPx;
        var px = NewBuffer(w, h);

        var gold = new Color32(255, 209, 92, 255);
        var goldDark = new Color32(214, 158, 40, 255);
        var goldLight = new Color32(255, 234, 168, 255);

        FillEllipse(px, w, h, 20f, 20f, 18f, 18f, gold, goldDark);
        FillEllipse(px, w, h, 20f, 20f, 13f, 13f, gold, goldDark);
        FillEllipse(px, w, h, 15f, 25f, 5f, 4f, goldLight, goldLight);

        Outline(px, w, h, gold, goldDark, goldLight);
        return ToTexture(px, w, h);
    }

    /// <summary>Anel fino, usado como onda de choque ao ser esticado.</summary>
    static Texture2D BuildRing()
    {
        const int w = 64, h = 64;
        var px = NewBuffer(w, h);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = x - 31.5f;
            float dy = y - 31.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);

            float edge = Mathf.Abs(d - 27f);
            if (edge > 4f) continue;

            byte a = (byte)Mathf.RoundToInt(Mathf.Lerp(255f, 0f, edge / 4f));
            px[y * w + x] = new Color32(255, 255, 255, a);
        }

        return ToTexture(px, w, h);
    }

    // ---------------------------------------------------------------- helpers

    static Color32[] NewBuffer(int w, int h)
    {
        var px = new Color32[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Clear;
        return px;
    }

    static uint Hash(int a, int b)
    {
        uint n = (uint)(a * 73856093 ^ b * 19349663);
        n ^= n >> 13;
        n *= 2246822519u;
        n ^= n >> 16;
        return n;
    }

    static bool InEllipse(int x, int y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx;
        float dy = (y - cy) / ry;
        return dx * dx + dy * dy <= 1f;
    }

    /// <summary>Verdadeiro se o pixel pertence à máscara mas encosta em algo fora dela.</summary>
    static bool IsEdge(bool[] mask, int w, int h, int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;

            int nx = x + dx, ny = y + dy;
            if (ny >= h) continue;               // topo da textura sai do ecrã, sem contorno
            if (nx < 0 || ny < 0 || nx >= w) return true;
            if (!mask[ny * w + nx]) return true;
        }
        return false;
    }

    static void FillEllipse(Color32[] px, int w, int h, float cx, float cy, float rx, float ry, Color32 fill, Color32 edge)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = (x - cx) / rx;
            float dy = (y - cy) / ry;
            float d = dx * dx + dy * dy;

            if (d <= 0.78f) px[y * w + x] = fill;
            else if (d <= 1f) px[y * w + x] = edge;
        }
    }

    /// <summary>Pinta um polígono convexo, com os vértices em qualquer sentido.</summary>
    static void FillConvex(Color32[] px, int w, int h, Vector2[] poly, Color32 fill)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float cx = x + 0.5f, cy = y + 0.5f;
            bool above = false, below = false;

            for (int i = 0; i < poly.Length; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % poly.Length];
                float cross = (b.x - a.x) * (cy - a.y) - (b.y - a.y) * (cx - a.x);

                if (cross > 0f) above = true;
                else if (cross < 0f) below = true;
            }

            if (!(above && below)) px[y * w + x] = fill;
        }
    }

    /// <summary>Contorna em Ink tudo o que tenha cor, seja qual for — para desenhos com muitas.</summary>
    static void OutlineAll(Color32[] px, int w, int h)
    {
        var copy = (Color32[])px.Clone();

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (copy[y * w + x].a != 0) continue;

            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                if (copy[ny * w + nx].a != 0) px[y * w + x] = Ink;
            }
        }
    }

    /// <summary>Desenha um padrão de texto (linhas de cima para baixo) ampliado.</summary>
    static void DrawPattern(Color32[] px, int w, int h, string[] pattern, int originX, int originY, int scale, Color32 color)
    {
        for (int row = 0; row < pattern.Length; row++)
        for (int col = 0; col < pattern[row].Length; col++)
        {
            if (pattern[row][col] != '#') continue;

            for (int sy = 0; sy < scale; sy++)
            for (int sx = 0; sx < scale; sx++)
            {
                int x = originX + col * scale + sx;
                int y = originY + (pattern.Length - 1 - row) * scale + sy;
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                px[y * w + x] = color;
            }
        }
    }

    /// <summary>Contorna em Ink os pixels vazios vizinhos das cores informadas.</summary>
    static void Outline(Color32[] px, int w, int h, params Color32[] targets)
    {
        var copy = (Color32[])px.Clone();

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (copy[y * w + x].a != 0) continue;
            if (!HasNeighbour(copy, w, h, x, y, targets)) continue;
            px[y * w + x] = Ink;
        }
    }

    static bool HasNeighbour(Color32[] px, int w, int h, int x, int y, Color32[] targets)
    {
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            int nx = x + dx, ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;

            var c = px[ny * w + nx];
            foreach (var t in targets)
            {
                if (c.r == t.r && c.g == t.g && c.b == t.b && c.a == t.a) return true;
            }
        }
        return false;
    }

    static Texture2D ToTexture(Color32[] px, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    static void Save(string name, Texture2D tex, Vector2? pivot, bool smooth = false, float ppu = PPU)
    {
        string path = Dir + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        ApplyImportSettings(path, ppu, pivot, smooth);
    }

    /// <summary>Definições de sprite comuns à arte gerada e à desenhada à mão.</summary>
    public static void ApplyImportSettings(string path, float ppu, Vector2? pivot, bool smooth, int maxSize = 1024)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("[Flappy] Não encontrei a textura em " + path);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = maxSize;

        if (pivot.HasValue)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot.Value;
            importer.SetTextureSettings(settings);
        }

        importer.SaveAndReimport();
    }
}

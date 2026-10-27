using UnityEngine;

/// <summary>
/// A frase que aparece por baixo dos números no painel de fim de jogo.
///
/// Não é decoração. É o que faz a pessoa ler o painel em vez de carregar logo
/// para jogar outra vez, e é a linha que vai parar a um print. Por isso é
/// escolhida pelo que acabou mesmo de acontecer: morrer a um ponto do recorde
/// não merece a mesma frase que morrer a zero.
///
/// As frases estão em inglês, como o resto da UI do jogo.
/// </summary>
public static class DeathQuips
{
    // ------------------------------------------------------------- as frases

    static readonly string[] Nothing =
    {
        "YOU DIDN'T EVEN TAKE OFF.",
        "ZERO. THE PLANE WAS FINE.",
        "THAT WAS THE EASY PART.",
    };

    static readonly string[] BigRecord =
    {
        "NEW BEST. GO TELL SOMEONE.",
        "THAT ONE ACTUALLY COUNTED.",
    };

    static readonly string[] Record =
    {
        "BEATING YOURSELF ISN'T HARD.",
        "NEW BEST. LOW BAR, BUT STILL.",
    };

    static readonly string[] OneShort =
    {
        "ONE. YOU WERE ONE SHORT.",
        "ONE MORE POINT. THAT'S ALL.",
    };

    static readonly string[] Tie =
    {
        "EXACTLY YOUR BEST. NOT ONE MORE.",
        "A PERFECT TIE. WITH YOURSELF.",
    };

    static readonly string[] Lightning =
    {
        "THE LIGHTNING WASN'T MOVING.",
        "YOU FLEW INTO THE BRIGHT THING.",
    };

    static readonly string[] PigeonBlame =
    {
        "THE PIGEON WON.",
        "YOU LOST TO A PIGEON.",
        "THE PIGEON IS FINE, BY THE WAY.",
    };

    static readonly string[] One =
    {
        "ONE. ONE WHOLE POINT.",
        "ONE BUILDING. WELL DONE.",
    };

    static readonly string[] Deep =
    {
        "OKAY, THIS IS A PROBLEM NOW.",
        "PUT THE PHONE DOWN. NO, DON'T.",
    };

    static readonly string[] Decent =
    {
        "RESPECTABLE. NOT GOOD. RESPECTABLE.",
        "THE BUILDING WAS THERE THE WHOLE TIME.",
    };

    static readonly string[] Common =
    {
        "THE GROUND IS STILL UNDEFEATED.",
        "GRAVITY 1, YOU 0.",
        "THE SKY IS BIG. YOU FOUND A WALL.",
    };

    /// <summary>
    /// A última frase mostrada. Estático de propósito: o reinício recarrega a
    /// cena e um campo de instância nascia limpo a cada morte, que é
    /// exatamente quando é preciso lembrar-se.
    /// </summary>
    static string last;

    /// <param name="highScore">
    /// O recorde <b>já com esta partida contada</b> — é o que o
    /// <see cref="GameManager"/> tem na mão quando o painel sobe. Por isso é
    /// que empatar com o recorde e bater o recorde se distinguem pelo
    /// <paramref name="newRecord"/>, e não pelos números.
    /// </param>
    /// <param name="blamePigeon">Morreu pouco depois de levar com um pombo.</param>
    public static string For(int score, int highScore, bool newRecord, bool hitLightning, bool blamePigeon = false)
    {
        return Pick(Bucket(score, highScore, newRecord, hitLightning, blamePigeon));
    }

    /// <summary>
    /// A ordem é a graduação da piada: o que é mais raro e mais específico vem
    /// primeiro. Falhar o recorde por um ponto dói mais do que ter levado um
    /// raio, e ter levado um raio é mais interessante do que ter feito quinze.
    /// O pombo passa à frente do raio: é mais raro, e perder para um pombo é
    /// mais humilhante do que perder para o céu.
    /// </summary>
    static string[] Bucket(int score, int highScore, bool newRecord, bool hitLightning, bool blamePigeon)
    {
        if (score == 0) return Nothing;
        if (newRecord) return score >= 30 ? BigRecord : Record;
        if (highScore - score == 1) return OneShort;
        if (score == highScore) return Tie;
        if (blamePigeon) return PigeonBlame;
        if (hitLightning) return Lightning;
        if (score == 1) return One;
        if (score >= 40) return Deep;
        if (score >= 15) return Decent;
        return Common;
    }

    /// <summary>
    /// Nunca a mesma frase duas vezes seguidas. Uma piada repetida de imediato
    /// não é meia piada — é nenhuma, e ainda faz o painel parecer avariado.
    /// </summary>
    static string Pick(string[] lines)
    {
        if (lines.Length == 1) return last = lines[0];

        string pick;
        do
        {
            pick = lines[Random.Range(0, lines.Length)];
        }
        while (pick == last);

        return last = pick;
    }
}

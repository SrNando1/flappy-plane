using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Liga e desliga o símbolo ADMOB conforme o plugin da Google esteja ou não
/// no projeto. É isto que permite ter o código da AdMob escrito sem quebrar
/// a compilação de quem clona o repositório sem o plugin.
/// </summary>
[InitializeOnLoad]
public static class AdMobDefine
{
    const string Symbol = "ADMOB";
    const string PluginType = "GoogleMobileAds.Api.MobileAds";

    static AdMobDefine()
    {
        bool installed = TypeExists(PluginType);

        Apply(NamedBuildTarget.Android, installed);
        Apply(NamedBuildTarget.Standalone, installed);
    }

    static bool TypeExists(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (assembly.GetType(fullName, false) != null) return true;
            }
            catch
            {
                // assemblies dinâmicos podem recusar a consulta — seguir em frente
            }
        }
        return false;
    }

    static void Apply(NamedBuildTarget target, bool wanted)
    {
        string current = PlayerSettings.GetScriptingDefineSymbols(target);
        var symbols = new List<string>(current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

        // sai sem escrever quando já está como deve estar,
        // senão cada recompilação disparava outra
        if (symbols.Contains(Symbol) == wanted) return;

        if (wanted) symbols.Add(Symbol);
        else symbols.Remove(Symbol);

        PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
        Debug.Log($"[Ads] Símbolo {Symbol} {(wanted ? "ligado" : "desligado")} para {target}.");
    }
}

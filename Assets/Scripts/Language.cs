using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using YG;

public class Language : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdownItem;
    [SerializeField] private YandexGame yandexGame;
    [SerializeField] private string CurrentLanguage = "en";
    [SerializeField] private string[] Languages;
    [SerializeField] private TextMeshProUGUI debugtext;
    

    [ContextMenu("Init")]
    public void Init()
    {
        yandexGame = FindObjectOfType<YandexGame>();
        dropdownItem = FindObjectOfType<TMP_Dropdown>();

        Languages = new string[]
        {
            "ru",
            "en",
            "tr",
            "az",
            "be",
            "he",
            // "hy",
            // "ka",
            "et",
            "fr",
            "kk",
            "ky",
            "lt",
            "lv",
            "ro",
            "tg",
            "tk",
            "uk",
            "uz",
            "es",
            "pt",
            // "ar",
            "id",
            // "ja",
            "it",
            "de",
            // "hi",
        };
    }


    private bool ferstStart = true;

    
    private void OnEnable() => YandexGame.SwitchLangEvent += OnSetLanguage;

    private void OnDisable() => YandexGame.SwitchLangEvent -= OnSetLanguage;
    
    
    public void SetNextLanguage()
    {
        int NextIndex = System.Array.IndexOf(Languages, CurrentLanguage) + 1;   

        if(NextIndex > Languages.Length - 1) return;

        SetLanguage(NextIndex);
    }
    public void SetPervLanguage()
    {
        int PrevIndex = System.Array.IndexOf(Languages, CurrentLanguage) - 1;    
        
        if(PrevIndex < 0) return;

        SetLanguage(PrevIndex);
    }

    public void SetLanguage(int index)
    {
        CurrentLanguage = Languages[index];

        yandexGame._SwitchLanguage(CurrentLanguage);

        dropdownItem.value = System.Array.IndexOf(Languages, CurrentLanguage);    
        debugtext.text += $"Вызван метод SetLanguage() с аргументом{index}\n";
    }

    private void OnSetLanguage(string lang)
    {
        if (!ferstStart) return;

        ferstStart = false;

        debugtext.text += $"Вызван метод OnSetLanguage() делегатом {lang}\n";
     
        SetLanguage(System.Array.IndexOf(Languages, lang));
    }
    
    [ContextMenu("InitDropdownItem")]
    public void InitDropdownItem()
    {
        dropdownItem.ClearOptions();
        dropdownItem.AddOptions(new List<string>(Languages));
        dropdownItem.value = System.Array.IndexOf(Languages, CurrentLanguage);    
    }

    private void Update() 
    {
        if(Input.GetKey(KeyCode.RightAlt) && Input.GetKeyDown(KeyCode.F1))
        {
            //переключение дебаг текста
            debugtext.gameObject.SetActive(!debugtext.gameObject.activeSelf);
        }

        if(Input.GetKey(KeyCode.RightAlt) && Input.GetKeyDown(KeyCode.F2))
        {
            debugtext.text += YandexGame.lang + "\n";
        }

        if(Input.GetKey(KeyCode.RightAlt) && Input.GetKeyDown(KeyCode.F3))
        {
            yandexGame._LanguageRequest();
        }
        
    }
}

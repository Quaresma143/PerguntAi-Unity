using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))] // Garante que só pões isto em botões
public class ClickSoundHelper : MonoBehaviour
{
    void Start()
    {
        Button btn = GetComponent<Button>();

        // Adiciona o som ao clique
        btn.onClick.AddListener(() => {
            if (UIManager.instance != null)
            {
                UIManager.instance.PlayClickSound();
            }
        });
    }
}
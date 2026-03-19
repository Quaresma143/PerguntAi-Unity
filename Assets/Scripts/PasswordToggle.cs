using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PasswordToggle : MonoBehaviour
{
    [Header("Arrasta o Input Field aqui")]
    public TMP_InputField inputField;

    [Header("Opcional: Imagem do Olho")]
    public Image eyeIcon;

    private bool isVisible = false;

    public void ToggleVisibility()
    {
        isVisible = !isVisible;

        if (isVisible)
        {
            // Mostra o texto normal (Password visível)
            inputField.contentType = TMP_InputField.ContentType.Standard;
            if (eyeIcon != null) eyeIcon.color = Color.green; // Muda cor para indicar "ativo"
        }
        else
        {
            // Esconde com asteriscos
            inputField.contentType = TMP_InputField.ContentType.Password;
            if (eyeIcon != null) eyeIcon.color = Color.white;
        }

        // Força o Unity a atualizar o visual do texto imediatamente
        inputField.ForceLabelUpdate();
    }
}
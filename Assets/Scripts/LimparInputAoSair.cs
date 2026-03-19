using UnityEngine;
using TMPro; // Importante para o InputField

public class LimparInputAoSair : MonoBehaviour
{
    // Esta função corre automaticamente quando o objeto é desativado (quando mudas de ecrã)
    private void OnDisable()
    {
        TMP_InputField input = GetComponent<TMP_InputField>();
        if (input != null)
        {
            input.text = ""; // Limpa o texto
        }
    }
}
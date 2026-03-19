using UnityEngine;
using TMPro; // Importante para mexer no Texto

public class LimparTextoAoSair : MonoBehaviour
{
    // Quando o objeto é desligado (mudas de ecrã), ele limpa o texto
    private void OnDisable()
    {
        TextMeshProUGUI texto = GetComponent<TextMeshProUGUI>();
        if (texto != null)
        {
            texto.text = ""; // Apaga tudo
        }
    }
}
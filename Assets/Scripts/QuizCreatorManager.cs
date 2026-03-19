using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;

public class QuizCreatorManager : MonoBehaviour
{
    [Header("UI - Geral")]
    public GameObject manualQuizPanel;
    public TMP_Dropdown dropdownTipo;    // ARRASTAR O DROPDOWN AQUI
    public TMP_InputField tituloQuizInput;
    public TMP_InputField textoPerguntaInput;

    [Header("UI - Contentores (Pai)")]
    public GameObject containerMultipla; // ARRASTAR O CONTAINER DAS OPÇÕES
    public GameObject containerEscrita;  // ARRASTAR O CONTAINER DA RESPOSTA ÚNICA

    [Header("UI - Escolha Múltipla (Listas)")]
    public TMP_InputField[] inputsOpcoes; // ARRASTAR OS 4 INPUTS AQUI (Size 4)
    public Toggle[] togglesCorretas;      // ARRASTAR OS 4 TOGGLES AQUI (Size 4)

    [Header("UI - Resposta Escrita")]
    public TMP_InputField inputRespostaEscrita; // ARRASTAR O INPUT GRANDE ÚNICO

    [Header("UI - Feedback")]
    public TextMeshProUGUI textoContador; // Opcional
    public TextMeshProUGUI textoFeedback;

    // --- ESTRUTURAS SWAGGER ---
    [System.Serializable]
    public class SwaggerOption { public string text; public bool isCorrect; public string optionIndex; }

    [System.Serializable]
    public class SwaggerQuestion
    {
        public string text;
        public string type; // "MULTIPLE_CHOICE" ou "WRITTEN"
        public int orderIndex;
        public int pointsBase;
        public List<SwaggerOption> options;
    }

    [System.Serializable]
    public class SwaggerQuiz
    {
        public string title;
        public string description;
        public int timePerQuestion;
        public bool allowPowerups;
        public List<SwaggerQuestion> questions;
    }
    // ---------------------------

    private List<SwaggerQuestion> listaPerguntas = new List<SwaggerQuestion>();

    private void Start()
    {
        // Deteta quando mudas o Dropdown para trocar os painéis
        if (dropdownTipo) dropdownTipo.onValueChanged.AddListener(MudarTipoPergunta);
    }

    public void MudarTipoPergunta(int index)
    {
        // Index 0 = Múltipla, Index 1 = Escrita
        bool isMultipla = (index == 0);

        if (containerMultipla) containerMultipla.SetActive(isMultipla);
        if (containerEscrita) containerEscrita.SetActive(!isMultipla);
    }

    public void AbrirCriador()
    {
        manualQuizPanel.SetActive(true);
        listaPerguntas.Clear();
        tituloQuizInput.text = "";
        LimparCampos();
        AtualizarContador();

        // Reset ao dropdown para começar em Escolha Múltipla
        if (dropdownTipo) { dropdownTipo.value = 0; MudarTipoPergunta(0); }
    }

    public void FecharCriador()
    {
        manualQuizPanel.SetActive(false);
        if (UIManager.instance) UIManager.instance.LoadAndOpenCreateRoom();
    }

    public void AdicionarPergunta()
    {
        if (string.IsNullOrEmpty(textoPerguntaInput.text)) { MostrarErro("Escreve a pergunta!"); return; }

        SwaggerQuestion novaP = new SwaggerQuestion();
        novaP.text = textoPerguntaInput.text;
        novaP.pointsBase = 100;
        novaP.orderIndex = listaPerguntas.Count;
        novaP.options = new List<SwaggerOption>();

        // MÚLTIPLA ESCOLHA
        if (dropdownTipo.value == 0)
        {
            novaP.type = "MULTIPLE_CHOICE";
            int preenchidas = 0;
            bool temCorreta = false;

            for (int i = 0; i < inputsOpcoes.Length; i++)
            {
                if (string.IsNullOrEmpty(inputsOpcoes[i].text)) continue;
                SwaggerOption op = new SwaggerOption();
                op.text = inputsOpcoes[i].text;
                op.isCorrect = togglesCorretas[i].isOn;
                op.optionIndex = i.ToString();

                if (op.isCorrect) temCorreta = true;
                novaP.options.Add(op);
                preenchidas++;
            }

            if (preenchidas < 2) { MostrarErro("Mínimo 2 opções!"); return; }
            if (!temCorreta) { MostrarErro("Marca a resposta correta!"); return; }
        }
        // RESPOSTA ESCRITA
        else
        {
            novaP.type = "WRITTEN";
            if (string.IsNullOrEmpty(inputRespostaEscrita.text)) { MostrarErro("Define a resposta certa!"); return; }

            SwaggerOption op = new SwaggerOption();
            op.text = inputRespostaEscrita.text;
            op.isCorrect = true;
            op.optionIndex = "0";
            novaP.options.Add(op);
        }

        listaPerguntas.Add(novaP);
        MostrarSucesso("Adicionada (" + (dropdownTipo.value == 0 ? "Múltipla" : "Escrita") + ")");
        LimparCampos();
        AtualizarContador();
    }

    public void EnviarQuizParaAPI()
    {
        if (string.IsNullOrEmpty(tituloQuizInput.text)) { MostrarErro("Falta o Título!"); return; }

        // MÍNIMO 5 PERGUNTAS
        if (listaPerguntas.Count < 5)
        {
            MostrarErro("Mínimo 5 perguntas! Faltam " + (5 - listaPerguntas.Count));
            return;
        }

        StartCoroutine(EnviarLoop());
    }

    IEnumerator EnviarLoop()
    {
        MostrarSucesso("A enviar...");
        SwaggerQuiz payload = new SwaggerQuiz
        {
            title = tituloQuizInput.text,
            description = "Quiz Custom App",
            timePerQuestion = 20,
            allowPowerups = true,
            questions = listaPerguntas
        };

        string json = JsonUtility.ToJson(payload);
        string url = "https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/Quiz";

        using (UnityWebRequest w = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            w.uploadHandler = new UploadHandlerRaw(bodyRaw);
            w.downloadHandler = new DownloadHandlerBuffer();
            w.SetRequestHeader("Content-Type", "application/json");

            yield return w.SendWebRequest();

            if (w.result == UnityWebRequest.Result.Success)
            {
                MostrarSucesso("Quiz Criado!");
                yield return new WaitForSeconds(1.5f);
                FecharCriador();
            }
            else
            {
                MostrarErro("Erro API: " + w.responseCode);
            }
        }
    }

    void LimparCampos()
    {
        textoPerguntaInput.text = "";

        foreach (var i in inputsOpcoes) i.text = "";
        foreach (var t in togglesCorretas) t.isOn = false;
        if (togglesCorretas.Length > 0) togglesCorretas[0].isOn = true;

        if (inputRespostaEscrita) inputRespostaEscrita.text = "";
    }

    void AtualizarContador()
    {
        if (textoContador)
        {
            // Se tiveres menos de 5, mostra "X / 5 (Mínimo)"
            if (listaPerguntas.Count < 5)
            {
                textoContador.text = "Perguntas: " + listaPerguntas.Count + " / 5 (Mínimo)";
                textoContador.color = Color.white; // Ou amarelo/laranja
            }
            // Se já tiveres 5 ou mais, mostra só o total "Total: 6", "Total: 7"...
            else
            {
                textoContador.text = "Total de Perguntas: " + listaPerguntas.Count;
                textoContador.color = Color.green; // Fica verde para saberes que já podes enviar!
            }
        }
    }
    void MostrarErro(string m) { if (textoFeedback) { textoFeedback.text = m; textoFeedback.color = Color.red; } }
    void MostrarSucesso(string m) { if (textoFeedback) { textoFeedback.text = m; textoFeedback.color = Color.green; } }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using System.Linq;
using System.Threading;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    private SynchronizationContext _mainThreadContext;

    [Header("Screen Objects")]
    public GameObject splashUI; public GameObject loginUI; public GameObject registerUI;
    public GameObject mainMenuUI; public GameObject playSelectionUI; public GameObject joinRoomUI;
    public GameObject createRoomUI; public GameObject lobbyUI; public GameObject optionsUI;
    public GameObject creditsPopupUI;

    [Header("Profile System")]
    public GameObject profileUI; public GameObject statsAreaPanel; public GameObject changePassAreaPanel;
    public TMP_InputField inputEditUsername; public TextMeshProUGUI txtDisplayEmail;
    public TextMeshProUGUI usernameFeedbackTxt;
    public TextMeshProUGUI txtJogosJogados, txtVitorias, txtPontosTotais, txtTop3, txtNivel, txtTaxaAcerto;
    public Image avatarImage; public Sprite[] avatarSprites; private int currentAvatarIndex = 0;
    public TMP_InputField passAntigaInput, passNovaInput, passConfirmarInput;
    public TextMeshProUGUI feedbackPassTxt;

    [Header("Game UI")]
    public GameObject gameUI; public TextMeshProUGUI textoPergunta; public TextMeshProUGUI textoPontos;
    public GameObject containerBotoes; public GameObject[] botoesResposta;
    public GameObject containerEscrita; public TMP_InputField inputResposta;
    public Button btnEnviarEscrita; public Slider timerBar;

    [Header("Host & Lobby")]
    public Button btnHostStartGame; public Button btnHostFecharSala;
    public GameObject hostSpectatorUI; public TextMeshProUGUI textoLeaderboardHost;
    public GameObject endGameUI; public TextMeshProUGUI textoPontuacaoFinal;
    public TMP_InputField pinInput; public TextMeshProUGUI lobbyPinText;
    public TextMeshProUGUI statusTextoLobby; public TextMeshProUGUI listaJogadoresLobbyText;
    public Transform quizListContent; public GameObject quizItemPrefab;
    public Button btnGerarSala; public TMP_InputField inputTopicQuiz; public Button btnCriarNovoQuiz;

    [Header("Configs")]
    public float delayBeforeLogin = 5f;
    public Button btnOpenOptions, btnCloseOptions, btnLogout;
    public GameObject optionsContent;
    public Button btnSoundToggle, btnVibrationToggle, btnCredits, btnCloseCredits, btnReportBug;
    public TextMeshProUGUI txtSoundLabel, txtVibrationLabel;
    public Color colorOn = Color.green; public Color colorOff = Color.red;
    public AudioSource audioSource; public AudioClip sfxClick, sfxCorrect, sfxWrong;

    [Header("Player Data")]
    public string idJogadorLogado;    // UID do Firebase
    public string idInternoBaseDados; // GUID da Base de Dados (Corrigido)
    public string nomeJogadorLogado;

    private string quizSelecionadoId; private string myRoomPlayerId; private string currentRoomId;
    public List<Game_Question> perguntasDoJogo = new List<Game_Question>();

    private int indicePerguntaAtual = 0; private bool podeResponder = false;
    private int pontuacaoVisual = 0; private bool isHost = false;
    private bool jogoEmAndamento = false; public float tempoMaximo = 30f;
    private float tempoRestante; private bool timerAtivo = false;
    private bool isSoundOn = true; private bool isVibrationOn = true;
    private bool isStartingGame = false; // Variável de controlo
    private AuthManager authManager;

    private void Awake()
    {
        if (instance == null) instance = this; else Destroy(this);
        _mainThreadContext = SynchronizationContext.Current;
        authManager = FindFirstObjectByType<AuthManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (splashUI) splashUI.SetActive(true);
        SetAllUIFalseExceptSplash();
        StartCoroutine(ShowSplashThenLogin());

        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
        isVibrationOn = PlayerPrefs.GetInt("VibrationOn", 1) == 1;

        currentAvatarIndex = PlayerPrefs.GetInt("AvatarIndex", 0);
        AtualizarAvatarUI();

        if (btnGerarSala) btnGerarSala.onClick.AddListener(BotaoCriarSala);
        if (btnEnviarEscrita) btnEnviarEscrita.onClick.AddListener(VerificarRespostaEscrita);
        if (btnHostStartGame) btnHostStartGame.onClick.AddListener(HostComecarJogoAcao);
        if (btnCriarNovoQuiz) btnCriarNovoQuiz.onClick.AddListener(AcaoGerarQuiz);
        if (btnHostFecharSala) btnHostFecharSala.onClick.AddListener(BotaoFecharSalaAcao);
        if (btnOpenOptions) btnOpenOptions.onClick.AddListener(OpenOptions);
        if (btnCloseOptions) btnCloseOptions.onClick.AddListener(CloseOptions);
        if (btnLogout) btnLogout.onClick.AddListener(AcaoLogout);
        if (btnCredits) btnCredits.onClick.AddListener(OpenCredits);
        if (btnCloseCredits) btnCloseCredits.onClick.AddListener(CloseCredits);
        if (btnReportBug) btnReportBug.onClick.AddListener(ReportBugAction);

        UpdateOptionsUI();
    }

    private void Update()
    {
        if (!isHost && timerAtivo && podeResponder)
        {
            tempoRestante -= Time.deltaTime;
            if (timerBar) timerBar.value = tempoRestante / tempoMaximo;
            if (tempoRestante <= 0) TempoEsgotado();
        }
    }

    #region Configs & Avatar
    void UpdateOptionsUI()
    {
        if (txtSoundLabel) txtSoundLabel.text = isSoundOn ? "ON" : "OFF";
        if (txtVibrationLabel) txtVibrationLabel.text = isVibrationOn ? "ON" : "OFF";
        if (btnSoundToggle && btnSoundToggle.targetGraphic) btnSoundToggle.targetGraphic.color = isSoundOn ? colorOn : colorOff;
        if (btnVibrationToggle && btnVibrationToggle.targetGraphic) btnVibrationToggle.targetGraphic.color = isVibrationOn ? colorOn : colorOff;
        AudioListener.volume = isSoundOn ? 1f : 0f;
    }
    public void ToggleSound() { isSoundOn = !isSoundOn; PlayerPrefs.SetInt("SoundOn", isSoundOn ? 1 : 0); PlayerPrefs.Save(); UpdateOptionsUI(); if (isSoundOn) PlayClickSound(); }
    public void ToggleVibration()
    {
        isVibrationOn = !isVibrationOn;
        PlayerPrefs.SetInt("VibrationOn", isVibrationOn ? 1 : 0);
        PlayerPrefs.Save();
        UpdateOptionsUI();

        if (isVibrationOn)
        {
            // Só tenta vibrar se estivermos no Android ou iOS
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate(); 
#endif
        }
    }
    public void MudarAvatarEsq() { if (avatarSprites.Length == 0) return; currentAvatarIndex--; if (currentAvatarIndex < 0) currentAvatarIndex = avatarSprites.Length - 1; AtualizarAvatarUI(); }
    public void MudarAvatarDir() { if (avatarSprites.Length == 0) return; currentAvatarIndex++; if (currentAvatarIndex >= avatarSprites.Length) currentAvatarIndex = 0; AtualizarAvatarUI(); }
    void AtualizarAvatarUI() { if (avatarImage != null && avatarSprites.Length > 0) { avatarImage.sprite = avatarSprites[currentAvatarIndex]; PlayerPrefs.SetInt("AvatarIndex", currentAvatarIndex); PlayerPrefs.Save(); } }
    #endregion

    #region Profile & Stats
    public void OpenProfile()
    {
        HideAll();
        profileUI.SetActive(true);

        if (statsAreaPanel) statsAreaPanel.SetActive(true);
        if (changePassAreaPanel) changePassAreaPanel.SetActive(false);
        if (inputEditUsername != null) inputEditUsername.text = nomeJogadorLogado;

        if (Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            idJogadorLogado = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            Debug.Log("[PROFILE] A usar UID do Firebase: " + idJogadorLogado);
        }

        StartCoroutine(CarregarDadosJogadorAPI());
    }
    public void ShowPass() { if (changePassAreaPanel) changePassAreaPanel.SetActive(true); if (statsAreaPanel) statsAreaPanel.SetActive(false); }
    public void CloseChangePassword() { if (changePassAreaPanel) changePassAreaPanel.SetActive(false); if (statsAreaPanel) statsAreaPanel.SetActive(true); }
    public void ConfirmarTrocaPassword()
    {
        // 1. Verificar se algum campo está vazio (incluindo o Confirmar)
        if (string.IsNullOrEmpty(passAntigaInput.text) ||
            string.IsNullOrEmpty(passNovaInput.text) ||
            string.IsNullOrEmpty(passConfirmarInput.text))
        {
            feedbackPassTxt.text = "Preenche todos os campos!";
            feedbackPassTxt.color = Color.red;
            return;
        }

        // 2. SEGURANÇA: Verificar se a Nova coincide com a Confirmação
        if (passNovaInput.text != passConfirmarInput.text)
        {
            feedbackPassTxt.text = "A confirmação não coincide!";
            feedbackPassTxt.color = Color.red;
            return; // <--- Pára aqui se forem diferentes
        }

        // 3. Verificar tamanho mínimo
        if (passNovaInput.text.Length < 6)
        {
            feedbackPassTxt.text = "A password nova é muito curta!";
            feedbackPassTxt.color = Color.red;
            return;
        }

        // Feedback visual enquanto espera
        feedbackPassTxt.text = "A atualizar...";
        feedbackPassTxt.color = Color.yellow;

        // 4. Enviar para o AuthManager
        StartCoroutine(authManager.TrocarPasswordFirebase(passAntigaInput.text, passNovaInput.text, (sucesso, msg) =>
        {
            feedbackPassTxt.text = msg;
            feedbackPassTxt.color = sucesso ? Color.green : Color.red;

            // Se correu bem, limpa os campos para ninguém ver a password
            if (sucesso)
            {
                passAntigaInput.text = "";
                passNovaInput.text = "";
                passConfirmarInput.text = "";
            }
        }));
    }

    IEnumerator CarregarDadosJogadorAPI()
    {
        string url = "https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/PlayerProfile/firebase/" + idJogadorLogado;
        Debug.Log("[API] A pedir Perfil: " + url);

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonBruto = www.downloadHandler.text;
                Game_PlayerProfile dados = JsonUtility.FromJson<Game_PlayerProfile>(jsonBruto);

                // 1. Nível (Calculado: 1 nível a cada 1000 pontos)
                int nivelCalculado = 1 + (dados.totalPoints / 1000);
                if (txtNivel) txtNivel.text = "Nível " + nivelCalculado;

                // 2. Jogos
                if (txtJogosJogados) txtJogosJogados.text = "Jogos: " + dados.totalGames;

                // 3. Vitórias
                if (txtVitorias) txtVitorias.text = "Vitórias: " + dados.totalWins;

                // 4. Winrate (Cálculo de percentagem de vitórias)
                if (txtTaxaAcerto)
                {
                    if (dados.totalGames > 0)
                    {
                        float winRateValue = ((float)dados.totalWins / dados.totalGames) * 100f;
                        txtTaxaAcerto.text = "Winrate: " + winRateValue.ToString("F0") + "%";
                    }
                    else
                    {
                        txtTaxaAcerto.text = "Winrate: 0%";
                    }
                }

                // 5. Top 3
                if (txtTop3) txtTop3.text = "Top 3: " + dados.totalTop3;

                // 6. Pontos
                if (txtPontosTotais) txtPontosTotais.text = "Pontos: " + dados.totalPoints;

                // Atualizar Nome no Input
                if (!string.IsNullOrEmpty(dados.displayName))
                {
                    inputEditUsername.text = dados.displayName;
                    nomeJogadorLogado = dados.displayName;
                }
            }
            else
            {
                Debug.LogError("[API] Erro ao carregar perfil: " + www.error);
            }
        }
    }
    public void GuardarNovoUsername() { if (!string.IsNullOrEmpty(inputEditUsername.text)) StartCoroutine(EnviarUpdateUsername(inputEditUsername.text)); }
    IEnumerator EnviarUpdateUsername(string n) { string url = "https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/PlayerProfile/" + idJogadorLogado; string json = "{\"preferredName\": \"" + n + "\"}"; using (UnityWebRequest w = new UnityWebRequest(url, "PUT")) { byte[] b = Encoding.UTF8.GetBytes(json); w.uploadHandler = new UploadHandlerRaw(b); w.downloadHandler = new DownloadHandlerBuffer(); w.SetRequestHeader("Content-Type", "application/json"); yield return w.SendWebRequest(); if (w.result == UnityWebRequest.Result.Success) { nomeJogadorLogado = n; usernameFeedbackTxt.text = "Guardado!"; usernameFeedbackTxt.color = Color.green; } } }
    #endregion

    #region Multiplayer
    IEnumerator LobbyLoop()
    {
        while (lobbyUI.activeSelf)
        {
            using (UnityWebRequest w = UnityWebRequest.Get("https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/Room/" + currentRoomId + "/players"))
            {
                yield return w.SendWebRequest();
                if (w.result == UnityWebRequest.Result.Success)
                {
                    Game_RoomPlayerList l = JsonUtility.FromJson<Game_RoomPlayerList>("{\"items\":" + w.downloadHandler.text + "}");
                    StringBuilder sb = new StringBuilder(); sb.AppendLine($"<b>Jogadores: {l.items.Count}</b>\n");
                    foreach (var p in l.items) { int nivel = 1 + (p.totalXp / 1000); sb.AppendLine($"- <color=#00FF00>[Nv. {nivel}]</color> {p.name}"); }
                    listaJogadoresLobbyText.text = sb.ToString();
                }
            }
            yield return new WaitForSeconds(2f);
        }
    }

    IEnumerator UpdateLeaderboard(TextMeshProUGUI t, string tit)
    {
        if (string.IsNullOrEmpty(currentRoomId)) yield break;
        using (UnityWebRequest w = UnityWebRequest.Get("https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/Room/" + currentRoomId + "/leaderboard"))
        {
            yield return w.SendWebRequest();
            if (w.result == UnityWebRequest.Result.Success)
            {
                Game_Leaderboard l = JsonUtility.FromJson<Game_Leaderboard>("{\"items\":" + w.downloadHandler.text + "}");
                StringBuilder sb = new StringBuilder(); sb.AppendLine($"<b>{tit}:</b>\n");
                var ord = l.items.OrderByDescending(p => p.score).ToList();
                int pos = 1;
                foreach (var p in ord) { int nivel = 1 + (p.totalXp / 1000); sb.AppendLine($"{pos}. <size=70%>Nv.{nivel}</size> {p.name} - <color=yellow>{p.score} pts</color>"); pos++; }
                if (t) t.text = sb.ToString();
            }
        }
    }
    #endregion

    public void MostrarPergunta(int i)
    {
        if (i >= perguntasDoJogo.Count) { EndGame(); return; }
        indicePerguntaAtual = i; Game_Question p = perguntasDoJogo[i];
        if (p.options == null) p.options = new List<Game_Option>();
        textoPergunta.text = p.text; tempoRestante = tempoMaximo; podeResponder = true; timerAtivo = true;
        if (p.type == "WRITTEN") { containerBotoes.SetActive(false); containerEscrita.SetActive(true); inputResposta.text = ""; }
        else
        {
            containerBotoes.SetActive(true); containerEscrita.SetActive(false);
            for (int k = 0; k < botoesResposta.Length; k++)
            {
                if (k < p.options.Count)
                {
                    botoesResposta[k].SetActive(true); botoesResposta[k].GetComponentInChildren<TextMeshProUGUI>().text = p.options[k].text;
                    string id = p.options[k].optionId; bool c = p.options[k].isCorrect; Button btn = botoesResposta[k].GetComponent<Button>();
                    btn.GetComponent<Image>().color = Color.white; btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => Resp(id, c, btn.gameObject));
                }
                else botoesResposta[k].SetActive(false);
            }
        }
    }

    public void Resp(string oid, bool corr, GameObject b)
    {
        if (!podeResponder) return;
        podeResponder = false; timerAtivo = false;
        if (corr) { b.GetComponent<Image>().color = Color.green; pontuacaoVisual += 100; PlayCorrectSound(); if (textoPontos) textoPontos.text = pontuacaoVisual + " PTS"; }
        else
        {
            b.GetComponent<Image>().color = Color.red; PlayWrongSound();
            var correta = perguntasDoJogo[indicePerguntaAtual].options.Find(x => x.isCorrect);
            if (correta != null) textoPergunta.text = $"<color=red>ERRADO!</color>\nA correta era:\n<color=yellow>{correta.text}</color>";
        }
        StartCoroutine(SendAns(perguntasDoJogo[indicePerguntaAtual].questionId, oid, ""));
    }

    public void VerificarRespostaEscrita()
    {
        if (!podeResponder) return;
        var p = perguntasDoJogo[indicePerguntaAtual];
        string r = inputResposta.text; podeResponder = false; timerAtivo = false;
        if (Norm(r) == Norm(p.options[0].text)) { pontuacaoVisual += 100; textoPergunta.text = $"<color=green>CORRETO!</color>\n{p.options[0].text}"; PlayCorrectSound(); if (textoPontos) textoPontos.text = pontuacaoVisual + " PTS"; }
        else { textoPergunta.text = $"<color=red>ERRADO!</color>\nA resposta era:\n<color=yellow>{p.options[0].text}</color>"; PlayWrongSound(); }
        StartCoroutine(SendAns(p.questionId, p.options[0].optionId, r));
    }

    IEnumerator SendAns(string q, string o, string txt = "")
    {
        Game_AnswerReq r = new Game_AnswerReq { roomPlayerId = myRoomPlayerId, questionId = q, selectedOptionId = o, answerText = txt };
        using (UnityWebRequest w = new UnityWebRequest("https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/Answer", "POST"))
        {
            byte[] b = Encoding.UTF8.GetBytes(JsonUtility.ToJson(r)); w.uploadHandler = new UploadHandlerRaw(b); w.downloadHandler = new DownloadHandlerBuffer(); w.SetRequestHeader("Content-Type", "application/json");
            yield return w.SendWebRequest(); yield return new WaitForSeconds(2f); MostrarPergunta(indicePerguntaAtual + 1);
        }
    }

    #region Helpers & System
    public void LoginScreen() { HideAll(); loginUI.SetActive(true); }
   
    // Adiciona isto para o botão de registo funcionar
    public void OpenRegister()
    {
        HideAll();
        if (registerUI != null) registerUI.SetActive(true);
    }

    // Adiciona isto para o botão de voltar ao login
    public void OpenLogin()
    {
        HideAll();
        if (loginUI != null) loginUI.SetActive(true);
    }
    public void MainMenuScreen() { HideAll(); mainMenuUI.SetActive(true); }
    public void HideAll() { loginUI.SetActive(false); registerUI.SetActive(false); mainMenuUI.SetActive(false); playSelectionUI.SetActive(false); joinRoomUI.SetActive(false); createRoomUI.SetActive(false); lobbyUI.SetActive(false); gameUI.SetActive(false); endGameUI.SetActive(false); hostSpectatorUI.SetActive(false); optionsUI.SetActive(false); profileUI.SetActive(false); if (changePassAreaPanel) changePassAreaPanel.SetActive(false); }
    IEnumerator ShowSplashThenLogin() { yield return new WaitForSeconds(delayBeforeLogin); splashUI.SetActive(false); loginUI.SetActive(true); }
    public void PlayClickSound() { if (audioSource && sfxClick) audioSource.PlayOneShot(sfxClick); }
    public void PlayCorrectSound() { if (audioSource && sfxCorrect) audioSource.PlayOneShot(sfxCorrect); }
    public void PlayWrongSound() { if (audioSource && sfxWrong) audioSource.PlayOneShot(sfxWrong); }
    string Norm(string t) { return t.Trim().ToLower().Normalize(NormalizationForm.FormD); }
    void SetAllUIFalseExceptSplash() { HideAll(); }
    void TempoEsgotado() { podeResponder = false; timerAtivo = false; textoPergunta.text = "<color=red>TEMPO ESGOTADO!</color>"; PlayWrongSound(); StartCoroutine(DelayNext()); }
    IEnumerator DelayNext() { yield return new WaitForSeconds(2.5f); MostrarPergunta(indicePerguntaAtual + 1); }
    public void AcaoLogout() { idJogadorLogado = ""; if (authManager) authManager.SignOut(); HideAll(); loginUI.SetActive(true); }
    public void OpenOptions() { HideAll(); optionsUI.SetActive(true); }
    public void CloseOptions() { HideAll(); mainMenuUI.SetActive(true); }
    public void OpenCredits()
    {
        if (optionsContent) optionsContent.SetActive(false); // Esconde os botões de opções
        if (creditsPopupUI) creditsPopupUI.SetActive(true);  // Mostra os créditos
    }

    public void CloseCredits()
    {
        if (creditsPopupUI) creditsPopupUI.SetActive(false); // Esconde os créditos
        if (optionsContent) optionsContent.SetActive(true);  // Volta a mostrar os botões de opções
    }
    public void ReportBugAction()
    {
        // Podes meter quantos quiseres, separados por vírgula
        string emails = "A046542@ipmaia.pt,A046854@ipmaia.pt";

        string subject = UnityEngine.Networking.UnityWebRequest.EscapeURL("Bug Report - Jogo PerguntaAI").Replace("+", "%20");

        Application.OpenURL("mailto:" + emails + "?subject=" + subject);
    }
    public void BackToMenuFromGame()
    {
        HideAll();
        mainMenuUI.SetActive(true);
        jogoEmAndamento = false;

        // --- IMPORTANTE: RESETAR O BLOQUEIO ---
        isStartingGame = false; // Permite começar um jogo novo no futuro
        if (btnHostStartGame != null) btnHostStartGame.interactable = true; // Volta a acender o botão
                                                                            // --------------------------------------

        StopAllCoroutines();
    }
    public void OpenPlaySelection() { HideAll(); playSelectionUI.SetActive(true); }
    public void OpenJoinRoom() { HideAll(); joinRoomUI.SetActive(true); }
    public void ConfirmarPIN() { if (pinInput.text.Length == 6) StartCoroutine(EntrarSala(pinInput.text)); }
    public void LoadAndOpenCreateRoom() { playSelectionUI.SetActive(false); createRoomUI.SetActive(true); StartCoroutine(GetQuizzes()); }
    public void HostComecarJogoAcao()
    {
        // 1. Bloqueio Lógico Imediato
        if (isStartingGame) return;
        isStartingGame = true; // Tranca a porta mal entras

        // 2. Bloqueio Visual
        if (btnHostStartGame != null) btnHostStartGame.interactable = false;

        // 3. Chama a API
        StartCoroutine(APIStartGame());
    }
    public void BotaoFecharSalaAcao() { if (isHost) StartCoroutine(APIEndGame()); else BackToMenuFromGame(); }
    public void AcaoGerarQuiz() { if (inputTopicQuiz && !string.IsNullOrEmpty(inputTopicQuiz.text)) StartCoroutine(GerarQuiz(inputTopicQuiz.text)); }

    IEnumerator GerarQuiz(string t) { string u = "https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/Quiz/generate"; using (UnityWebRequest w = new UnityWebRequest(u, "POST")) { byte[] b = Encoding.UTF8.GetBytes("\"" + t + "\""); w.uploadHandler = new UploadHandlerRaw(b); w.downloadHandler = new DownloadHandlerBuffer(); w.SetRequestHeader("Content-Type", "application/json"); yield return w.SendWebRequest(); if (w.result == UnityWebRequest.Result.Success) { inputTopicQuiz.text = ""; StartCoroutine(GetQuizzes()); } } }
    IEnumerator GetQuizzes() { using (UnityWebRequest w = UnityWebRequest.Get("https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/quiz")) { yield return w.SendWebRequest(); if (w.result == UnityWebRequest.Result.Success) { foreach (Transform c in quizListContent) Destroy(c.gameObject); Game_QuizList list = JsonUtility.FromJson<Game_QuizList>("{ \"items\": " + w.downloadHandler.text + "}"); foreach (var q in list.items) { GameObject i = Instantiate(quizItemPrefab, quizListContent); i.GetComponentInChildren<TextMeshProUGUI>().text = q.title; string id = q.quizId; i.GetComponent<Button>().onClick.AddListener(() => SelQuiz(id, i)); } } } }
    void SelQuiz(string id, GameObject b) { quizSelecionadoId = id; foreach (Transform c in quizListContent) c.GetComponent<Image>().color = Color.white; b.GetComponent<Image>().color = Color.green; }
    public void BotaoCriarSala() { if (!string.IsNullOrEmpty(quizSelecionadoId)) StartCoroutine(CriarSala()); }

    IEnumerator CriarSala()
    {
        Game_CreateRoomReq req = new Game_CreateRoomReq { hostId = idInternoBaseDados, quizId = quizSelecionadoId };
        using (UnityWebRequest w = new UnityWebRequest("https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/Room", "POST"))
        {
            byte[] b = Encoding.UTF8.GetBytes(JsonUtility.ToJson(req));
            w.uploadHandler = new UploadHandlerRaw(b);
            w.downloadHandler = new DownloadHandlerBuffer();
            w.SetRequestHeader("Content-Type", "application/json");
            yield return w.SendWebRequest();

            if (w.result == UnityWebRequest.Result.Success)
            {
                Game_CreateRoomRes res = JsonUtility.FromJson<Game_CreateRoomRes>(w.downloadHandler.text);
                isHost = true;
                currentRoomId = res.roomId;
                createRoomUI.SetActive(false);
                lobbyUI.SetActive(true);

                // Ativa o botão Start porque tu és o Host
                if (btnHostStartGame != null) btnHostStartGame.gameObject.SetActive(true);

                lobbyPinText.text = "PIN: " + res.pinCode;
                StartCoroutine(LobbyLoop());
            }
        }
    }

    IEnumerator EntrarSala(string pin)
    {
        Game_JoinRoomReq r = new Game_JoinRoomReq
        {
            pinCode = pin,
            playerId = idInternoBaseDados,
            displayName = nomeJogadorLogado
        };

        using (UnityWebRequest w = new UnityWebRequest("https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/Room/join", "POST"))
        {
            byte[] b = Encoding.UTF8.GetBytes(JsonUtility.ToJson(r));
            w.uploadHandler = new UploadHandlerRaw(b);
            w.downloadHandler = new DownloadHandlerBuffer();
            w.SetRequestHeader("Content-Type", "application/json");

            yield return w.SendWebRequest();

            if (w.result == UnityWebRequest.Result.Success)
            {
                Game_JoinRoomRes res = JsonUtility.FromJson<Game_JoinRoomRes>(w.downloadHandler.text);
                isHost = false;
                myRoomPlayerId = res.roomPlayerId;
                currentRoomId = res.roomId;
                joinRoomUI.SetActive(false);
                lobbyUI.SetActive(true);

                // Esconde o botão Start porque tu NÃO és o Host
                if (btnHostStartGame != null) btnHostStartGame.gameObject.SetActive(false);

                lobbyPinText.text = "PIN: " + pin;

                string qId = res.quizId;
                if (string.IsNullOrEmpty(qId)) qId = quizSelecionadoId;
                if (!string.IsNullOrEmpty(qId)) StartCoroutine(CarregarPerguntas(qId));

                StartCoroutine(LobbyLoop());
                StartCoroutine(WaitGameStart());
            }
            else
            {
                Debug.LogError($"[JOIN] ERRO {w.responseCode}: {w.downloadHandler.text}");
            }
        }
    }

    IEnumerator APIStartGame()
    {
        // CORREÇÃO: Usar idInternoBaseDados em vez de idJogadorLogado
        string url = "https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/Room/" + currentRoomId + "/start?hostId=" + idInternoBaseDados;

        Debug.Log("[HOST] A tentar começar jogo: " + url); // Debug para veres no PC

        using (UnityWebRequest w = new UnityWebRequest(url, "POST"))
        {
            w.downloadHandler = new DownloadHandlerBuffer();
            yield return w.SendWebRequest();

            if (w.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[HOST] Jogo Começou!");
                lobbyUI.SetActive(false);
                hostSpectatorUI.SetActive(true);
                StartCoroutine(HostLoop());
            }
            else
            {
                // Vais ver este erro na consola se falhar
                Debug.LogError("[HOST ERRO] " + w.responseCode + ": " + w.downloadHandler.text);
            }
        }
    }

    IEnumerator HostLoop()
    {
        while (hostSpectatorUI.activeSelf)
        {
            StartCoroutine(UpdateLeaderboard(textoLeaderboardHost, "TOP"));
            yield return new WaitForSeconds(3f);
        }
    }

    IEnumerator APIEndGame()
    {
        // CORREÇÃO: Usar idInternoBaseDados aqui também
        string url = "https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/Room/" + currentRoomId + "/end?hostId=" + idInternoBaseDados;

        using (UnityWebRequest w = new UnityWebRequest(url, "POST"))
        {
            w.downloadHandler = new DownloadHandlerBuffer();
            yield return w.SendWebRequest();

            Debug.Log("[HOST] Sala Fechada.");
            BackToMenuFromGame();
        }
    }

    IEnumerator WaitGameStart()
    {
        bool c = false;
        while (!c)
        {
            using (UnityWebRequest w = UnityWebRequest.Get("https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/Room/" + currentRoomId + "/status"))
            {
                yield return w.SendWebRequest();
                if (w.result == UnityWebRequest.Result.Success && JsonUtility.FromJson<Game_RoomStatus>(w.downloadHandler.text).status == "STARTED")
                    c = true;
            }
            yield return new WaitForSeconds(1f);
        }
        StartGame();
    }

    void StartGame()
    {
        pontuacaoVisual = 0;
        lobbyUI.SetActive(false);
        gameUI.SetActive(true);
        jogoEmAndamento = true;
        StartCoroutine(GameLoop());
        MostrarPergunta(0);
    }

    IEnumerator GameLoop()
    {
        while (jogoEmAndamento)
        {
            using (UnityWebRequest w = UnityWebRequest.Get("https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/Room/" + currentRoomId + "/status"))
            {
                yield return w.SendWebRequest();
                if (w.result == UnityWebRequest.Result.Success && JsonUtility.FromJson<Game_RoomStatus>(w.downloadHandler.text).status == "FINISHED")
                {
                    BackToMenuFromGame();
                    yield break;
                }
            }
            yield return new WaitForSeconds(3f);
        }
    }

    IEnumerator CarregarPerguntas(string qId)
    {
        string urlLista = "https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/quiz/" + qId + "/question";
        using (UnityWebRequest w = UnityWebRequest.Get(urlLista))
        {
            yield return w.SendWebRequest();
            if (w.result == UnityWebRequest.Result.Success)
            {
                Game_QuestionList lista = JsonUtility.FromJson<Game_QuestionList>("{ \"items\": " + w.downloadHandler.text + "}");
                perguntasDoJogo.Clear();
                foreach (var p in lista.items)
                {
                    using (UnityWebRequest wD = UnityWebRequest.Get("https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/question/" + p.questionId))
                    {
                        yield return wD.SendWebRequest();
                        if (wD.result == UnityWebRequest.Result.Success)
                            perguntasDoJogo.Add(JsonUtility.FromJson<Game_Question>(wD.downloadHandler.text));
                    }
                }
            }
        }
    }

    void EndGame()
    {
        gameUI.SetActive(false);
        endGameUI.SetActive(true);
        StartCoroutine(UpdateLeaderboard(textoPontuacaoFinal, "FIM"));
        StartCoroutine(WaitHostClose());
    }

    IEnumerator WaitHostClose()
    {
        bool f = false;
        while (!f)
        {
            using (UnityWebRequest w = UnityWebRequest.Get("https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/Room/" + currentRoomId + "/status"))
            {
                yield return w.SendWebRequest();
                if (w.result == UnityWebRequest.Result.Success && JsonUtility.FromJson<Game_RoomStatus>(w.downloadHandler.text).status == "FINISHED")
                    f = true;
            }
            yield return new WaitForSeconds(2f);
        }
        BackToMenuFromGame();
    }

    #endregion
} 
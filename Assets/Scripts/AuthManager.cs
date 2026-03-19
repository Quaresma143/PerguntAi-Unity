using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System;

public class AuthManager : MonoBehaviour
{
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseUser User;

    [Header("Login")]
    public TMP_InputField emailLoginField;
    public TMP_InputField passwordLoginField;
    public TMP_Text warningLoginText;
    public TMP_Text confirmLoginText;

    [Header("Register")]
    public TMP_InputField usernameRegisterField;
    public TMP_InputField emailRegisterField;
    public TMP_InputField passwordRegisterField;
    public TMP_InputField passwordRegisterVerifyField;
    public TMP_Text warningRegisterText;

    [Serializable]
    private class API_RegisterPayload
    {
        public string firebaseUid;
        public string displayName;
    }

    void Awake()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available) InitializeFirebase();
            else Debug.LogError("[FIREBASE] Erro de Dependências: " + dependencyStatus);
        });
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        Debug.Log("[FIREBASE] Inicializado com sucesso.");
    }

    public void LoginButton() { StartCoroutine(Login(emailLoginField.text, passwordLoginField.text)); }
    public void RegisterButton()
    {
        // 1. Verifica se os campos de password coincidem
        if (passwordRegisterField.text != passwordRegisterVerifyField.text)
        {
            warningRegisterText.text = "As passwords não coincidem!";
            Debug.LogWarning("[AUTH] O utilizador tentou registar com passwords diferentes.");
            return; // <--- O MÁGICO "RETURN": Pára o código aqui e não deixa criar conta!
        }

        // 2. (Opcional) Verifica se a password é curta demais
        if (passwordRegisterField.text.Length < 6)
        {
            warningRegisterText.text = "A password deve ter pelo menos 6 caracteres.";
            return;
        }

        // 3. Se passou nas verificações, então avança para o Firebase
        StartCoroutine(Register(emailRegisterField.text, passwordRegisterField.text, usernameRegisterField.text));
    }

    private IEnumerator Login(string _email, string _password)
    {
        Debug.Log($"[LOGIN] Tentativa para: {_email}");
        warningLoginText.text = "A entrar...";

        var LoginTask = auth.SignInWithEmailAndPasswordAsync(_email, _password);
        yield return new WaitUntil(() => LoginTask.IsCompleted);

        if (LoginTask.Exception != null)
        {
            Debug.LogError("[LOGIN] Falha no Firebase: " + LoginTask.Exception.GetBaseException().Message);
            warningLoginText.text = "Falha no login. Verifica os dados.";
        }
        else
        {
            User = LoginTask.Result.User;
            Debug.Log($"[LOGIN] Firebase OK. UID: {User.UserId}");

            if (!User.IsEmailVerified)
            {
                Debug.LogWarning("[LOGIN] E-mail não verificado. A enviar link...");
                warningLoginText.text = "<color=yellow>E-mail não verificado. Reenviámos o link!</color>";

                var resendTask = User.SendEmailVerificationAsync();
                yield return new WaitUntil(() => resendTask.IsCompleted);

                yield return new WaitForSeconds(1f);
                auth.SignOut();
            }
            else
            {
                Debug.Log("[LOGIN] E-mail verificado. A pedir perfil à API...");
                confirmLoginText.text = "Sucesso!";
                yield return StartCoroutine(ObterPerfilDoServidor(User.UserId));
            }
        }
    }

    private IEnumerator Register(string _email, string _password, string _username)
    {
        Debug.Log($"[REGISTER] Tentativa para: {_email} com nome: {_username}");

        if (string.IsNullOrEmpty(_username)) { warningRegisterText.text = "Falta User"; yield break; }

        warningRegisterText.text = "A criar conta...";
        var RegisterTask = auth.CreateUserWithEmailAndPasswordAsync(_email, _password);
        yield return new WaitUntil(() => RegisterTask.IsCompleted);

        if (RegisterTask.Exception != null)
        {
            Debug.LogError("[REGISTER] Falha no Firebase: " + RegisterTask.Exception.GetBaseException().Message);
            warningRegisterText.text = "Erro no registo.";
        }
        else
        {
            User = RegisterTask.Result.User;
            Debug.Log($"[REGISTER] Firebase Criado. UID: {User.UserId}. A atualizar DisplayName...");

            UserProfile profile = new UserProfile { DisplayName = _username };
            var profileTask = User.UpdateUserProfileAsync(profile);
            yield return new WaitUntil(() => profileTask.IsCompleted);

            Debug.Log("[REGISTER] DisplayName atualizado. A enviar verificação...");
            var emailTask = User.SendEmailVerificationAsync();
            yield return new WaitUntil(() => emailTask.IsCompleted);

            Debug.Log("[REGISTER] A enviar dados para a API do Gonçalo...");
            yield return StartCoroutine(RegistarNaBaseDeDados(User.UserId, _username, _email));

            warningRegisterText.text = "Sucesso! Verifica a pasta SPAM.";
            yield return new WaitForSeconds(1.5f);

            auth.SignOut();
            if (UIManager.instance != null) UIManager.instance.LoginScreen();
        }
    }

    IEnumerator ObterPerfilDoServidor(string firebaseId)
    {
        string url = "https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/PlayerProfile/firebase/" + firebaseId;
        Debug.Log($"[API] A pedir dados: {url}");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[API] Resposta Bruta: " + www.downloadHandler.text);

                try
                {
                    Game_PlayerProfile perfil = JsonUtility.FromJson<Game_PlayerProfile>(www.downloadHandler.text);

                    if (UIManager.instance != null)
                    {
                        // AQUI ESTÁ A MUDANÇA: Guardamos os dois IDs separadamente
                        UIManager.instance.idJogadorLogado = firebaseId;   // UID do Firebase
                        UIManager.instance.idInternoBaseDados = perfil.id; // GUID da Base de Dados

                        UIManager.instance.nomeJogadorLogado = !string.IsNullOrEmpty(perfil.displayName) ? perfil.displayName : "Jogador";
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[API] Erro ao processar JSON: " + e.Message);
                }

                if (UIManager.instance != null) UIManager.instance.MainMenuScreen();
            }
            else
            {
                Debug.LogError($"[API] Erro {www.responseCode}: {www.error}");
            }
        }
    }

    IEnumerator RegistarNaBaseDeDados(string firebaseId, string nome, string email)
    {
        string url = "https://perguntaai-api-hscqgrgderdhcde0.francecentral-01.azurewebsites.net/api/PlayerProfile/register";

        API_RegisterPayload payload = new API_RegisterPayload
        {
            firebaseUid = firebaseId,
            displayName = nome
        };

        string json = JsonUtility.ToJson(payload);
        Debug.Log($"[API POST] A enviar para o SQL: {json}");

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log("[API POST] Perfil criado no SQL com sucesso!");
            else
                Debug.LogError($"[API POST ERROR] Código: {www.responseCode} | Erro: {www.error}");
        }
    }

    // --- NOVA FUNÇÃO DE MUDAR PASSWORD ---
    public IEnumerator TrocarPasswordFirebase(string passAntiga, string passNova, Action<bool, string> callback)
    {
        var user = auth.CurrentUser;
        if (user == null)
        {
            callback(false, "Ninguém logado!");
            yield break;
        }

        // 1. RE-AUTENTICAÇÃO (Segurança do Firebase obriga a isto)
        Credential credential = EmailAuthProvider.GetCredential(user.Email, passAntiga);

        var reauthTask = user.ReauthenticateAsync(credential);
        yield return new WaitUntil(() => reauthTask.IsCompleted);

        if (reauthTask.Exception != null)
        {
            Debug.LogError("[AUTH] Erro ao re-autenticar: " + reauthTask.Exception.GetBaseException().Message);
            callback(false, "Password antiga incorreta!");
            yield break;
        }

        // 2. ATUALIZAR A PASSWORD
        var updateTask = user.UpdatePasswordAsync(passNova);
        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.Exception != null)
        {
            Debug.LogError("[AUTH] Erro ao atualizar: " + updateTask.Exception.GetBaseException().Message);
            callback(false, "Erro: Password fraca ou inválida.");
        }
        else
        {
            Debug.Log("[AUTH] Password alterada com sucesso!");
            callback(true, "Password alterada com sucesso!");
        }
    }

    public void SignOut() { if (auth != null) auth.SignOut(); }
}
using System;
using System.Collections.Generic;

// --- PERFIL DO JOGADOR ---
[Serializable]
public class Game_PlayerProfile
{
    public string id;           // Este é o GUID da Base de Dados (ex: 5bbbfd55...)
    public string firebaseUid;  // Este é o UID do Firebase
    public string displayName;
    public int totalGames;
    public int totalWins;
    public int totalTop3;
    public int totalPoints;
}

[Serializable]
public class Game_CreatePlayerReq
{
    public string firebaseUid;
    public string displayName;
}

[Serializable]
public class Game_UpdatePlayerReq
{
    public string preferredName;
}

// --- QUIZ & QUESTÕES ---
[Serializable]
public class Game_Question
{
    public string questionId;
    public string text;
    public string type;
    public List<Game_Option> options;
}

[Serializable]
public class Game_Option
{
    public string optionId;
    public string text;
    public bool isCorrect;
}

[Serializable] public class Game_QuestionList { public List<Game_Question> items; }
[Serializable] public class Game_QuizList { public List<Game_QuizItem> items; }
[Serializable] public class Game_QuizItem { public string quizId; public string title; }

// --- SALAS (ROOMS) ---
[Serializable] public class Game_CreateRoomReq { public string hostId; public string quizId; }
[Serializable] public class Game_CreateRoomRes { public string roomId; public string pinCode; }
[Serializable] public class Game_JoinRoomReq { public string pinCode; public string playerId; public string displayName; }
[Serializable] public class Game_JoinRoomRes { public string roomId; public string roomPlayerId; public string quizId; }
[Serializable] public class Game_RoomPlayerList { public List<Game_RoomPlayer> items; }

[Serializable]
public class Game_RoomPlayer
{
    public string name;
    public int totalXp;
}

[Serializable] public class Game_RoomStatus { public string status; }

// --- JOGO & LEADERBOARD ---
[Serializable]
public class Game_AnswerReq
{
    public string roomPlayerId;
    public string questionId;
    public string selectedOptionId;
    public string answerText;
}

[Serializable] public class Game_Leaderboard { public List<Game_LeaderboardItem> items; }

[Serializable]
public class Game_LeaderboardItem
{
    public string name;
    public int score;
    public int totalXp;
}